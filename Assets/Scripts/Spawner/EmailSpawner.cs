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

        public void StartSpawning()
        {
            _isSpawning = true;
            _gameTime = 0f;

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

            EmailDefinition def = EmailManager.Instance.Database.GetRandomFromPools(
                _activePools, rule.emailPool, rule.emailTags);

            if (def == null) return; // No matching emails in active pools — silently skip

            EmailManager.Instance.ReceiveEmail(def);
        }
    }
}
