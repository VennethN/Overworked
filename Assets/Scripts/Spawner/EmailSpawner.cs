using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Overworked.Core;
using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.Spawner
{
    public class EmailSpawner : MonoBehaviour
    {
        [SerializeField] private string rulesJsonPath = "Data/Rules/spawn_rules";
        [SerializeField] private DifficultyController difficultyController;

        private List<SpawnRule> _rules = new();
        private Dictionary<string, float> _ruleNextFireTime = new();
        private SpawnRuleEvaluator _evaluator = new();
        private float _gameTime;
        private bool _isSpawning;

        // Active email pools for the current session
        private string[] _activePools;

        // When set, only emails with at least one of these tags can spawn
        private string[] _spawnTagFilter;

        // When set, interval/event spawns pick only from these ids (still filtered by rule type/tags)
        private string[] _spawnEmailIdWhitelist;

        // Prevent the same email type from spawning too many times in a row
        private string _lastSpawnedType;
        private int _consecutiveTypeCount;
        private const int MAX_CONSECUTIVE_SAME_TYPE = 4;

        // Default pools for arcade mode
        private static readonly string[] DefaultPools = { "general", "hr", "spam" };

        private void Start()
        {
            LoadRulesFromPath(rulesJsonPath);

            GameEvents.OnGameEvent += HandleGameEvent;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameEvent -= HandleGameEvent;
        }

        private void LoadRulesFromPath(string path)
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning($"EmailSpawner: Could not load rules at Resources/{path}");
                return;
            }

            SpawnRuleCollection collection = JsonUtility.FromJson<SpawnRuleCollection>(asset.text);
            if (collection?.rules == null) return;

            foreach (SpawnRule rule in collection.rules)
            {
                _rules.Add(rule);
                if (rule.type == "interval")
                {
                    _ruleNextFireTime[rule.id] = rule.activeAfterSeconds +
                        Random.Range(rule.intervalSecondsMin, rule.intervalSecondsMax);
                }
            }

            Debug.Log($"EmailSpawner: Loaded {_rules.Count} spawn rules from {path}.");
        }

        public void LoadRulesOverride(string path)
        {
            _rules.Clear();
            _ruleNextFireTime.Clear();
            LoadRulesFromPath(path);
        }

        public void ResetToDefaultRules()
        {
            _rules.Clear();
            _ruleNextFireTime.Clear();
            LoadRulesFromPath(rulesJsonPath);
        }

        /// <summary>
        /// Set which email pools the spawner draws from.
        /// Pool names correspond to JSON file names: "general", "hr", "spam", etc.
        /// </summary>
        public void SetActivePools(string[] pools)
        {
            _activePools = pools;
            Debug.Log($"EmailSpawner: Active pools set to [{string.Join(", ", pools)}]");
        }

        /// <summary>
        /// Only spawn emails that have at least one of these tags. Null or empty = no restriction.
        /// </summary>
        public void SetSpawnEmailTagFilter(string[] tags)
        {
            _spawnTagFilter = (tags != null && tags.Length > 0) ? (string[])tags.Clone() : null;
        }

        /// <summary>
        /// Restrict random spawns to these email ids (null or empty = use active pools only).
        /// </summary>
        public void SetSpawnEmailIdWhitelist(string[] emailIds)
        {
            if (emailIds == null || emailIds.Length == 0)
            {
                _spawnEmailIdWhitelist = null;
                Debug.Log("EmailSpawner: Spawn whitelist cleared (pool-based spawning).");
                return;
            }

            _spawnEmailIdWhitelist = (string[])emailIds.Clone();
            Debug.Log($"EmailSpawner: Spawn whitelist active ({_spawnEmailIdWhitelist.Length} ids).");
        }

        public void StartSpawning()
        {
            _isSpawning = true;
            _gameTime = 0f;
            _lastSpawnedType = null;
            _consecutiveTypeCount = 0;

            // Default to all pools if not explicitly set
            if (_activePools == null)
                _activePools = DefaultPools;
        }

        public void StopSpawning()
        {
            _isSpawning = false;
        }

        private void Update()
        {
            if (!_isSpawning) return;

            _gameTime += Time.deltaTime;

            foreach (SpawnRule rule in _rules)
            {
                if (rule.type != "interval") continue;
                if (!_ruleNextFireTime.ContainsKey(rule.id)) continue;

                if (_evaluator.ShouldFire(rule, _gameTime, _ruleNextFireTime[rule.id]))
                {
                    SpawnFromRule(rule);
                    float diffMultiplier = difficultyController != null
                        ? difficultyController.SpawnRateMultiplier
                        : 1f;
                    _ruleNextFireTime[rule.id] = _gameTime + _evaluator.GetNextInterval(rule, diffMultiplier);
                }
            }
        }

        private void HandleGameEvent(string eventId)
        {
            foreach (SpawnRule rule in _rules)
            {
                if (rule.type != "event") continue;
                if (rule.triggerEvent != eventId) continue;
                if (!_evaluator.IsRuleActive(rule, _gameTime)) continue;

                StartCoroutine(BurstSpawn(rule));
            }
        }

        private IEnumerator BurstSpawn(SpawnRule rule)
        {
            for (int i = 0; i < rule.burstCount; i++)
            {
                SpawnFromRule(rule);
                if (rule.burstIntervalSeconds > 0)
                    yield return new WaitForSeconds(rule.burstIntervalSeconds);
            }
        }

        private void SpawnFromRule(SpawnRule rule)
        {
            if (EmailManager.Instance == null) return;

            // Retry loop: find an email matching rule filters AND day-level tier tags
            for (int attempt = 0; attempt < 10; attempt++)
            {
                EmailDefinition def;
                if (_spawnEmailIdWhitelist != null && _spawnEmailIdWhitelist.Length > 0)
                {
                    def = EmailManager.Instance.Database.GetRandomFromIdList(
                        _spawnEmailIdWhitelist, rule.emailPool, rule.emailTags);
                }
                else
                {
                    def = EmailManager.Instance.Database.GetRandomFromPools(
                        _activePools, rule.emailPool, rule.emailTags);
                }

                if (def == null) return;

                // Day-level tag filter: email must have at least one of the allowed tier tags
                if (_spawnTagFilter != null && _spawnTagFilter.Length > 0 && !HasAnyTag(def, _spawnTagFilter))
                    continue;

                // Prevent too many consecutive emails of the same type
                string defType = def.type ?? "info";
                if (defType == _lastSpawnedType && _consecutiveTypeCount >= MAX_CONSECUTIVE_SAME_TYPE)
                    continue;

                // Track consecutive type
                if (defType == _lastSpawnedType)
                    _consecutiveTypeCount++;
                else
                {
                    _lastSpawnedType = defType;
                    _consecutiveTypeCount = 1;
                }

                EmailManager.Instance.ReceiveEmail(def);
                return;
            }
        }

        private static bool HasAnyTag(EmailDefinition def, string[] requiredTags)
        {
            if (def.tags == null) return false;
            foreach (string required in requiredTags)
                foreach (string tag in def.tags)
                    if (tag == required) return true;
            return false;
        }
    }
}
