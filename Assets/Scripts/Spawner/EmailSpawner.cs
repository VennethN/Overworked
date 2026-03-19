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

        private void Start()
        {
            LoadRules();

            GameEvents.OnGameEvent += HandleGameEvent;
            GameEvents.OnGameStarted += StartSpawning;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameEvent -= HandleGameEvent;
            GameEvents.OnGameStarted -= StartSpawning;
        }

        private void LoadRules()
        {
            TextAsset asset = Resources.Load<TextAsset>(rulesJsonPath);
            if (asset == null)
            {
                Debug.LogWarning($"EmailSpawner: Could not load rules at Resources/{rulesJsonPath}");
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

            Debug.Log($"EmailSpawner: Loaded {_rules.Count} spawn rules.");
        }

        public void StartSpawning()
        {
            _isSpawning = true;
            _gameTime = 0f;
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

            EmailDefinition def = EmailManager.Instance.Database.GetRandomFromPool(rule.emailPool, rule.emailTags);
            if (def == null)
            {
                Debug.LogWarning($"EmailSpawner: No emails found for rule '{rule.id}'");
                return;
            }

            // Apply difficulty scaling to expiration
            if (difficultyController != null && def.expirationSeconds > 0)
            {
                // Create a modified copy so we don't alter the database entry
                // For now we apply difficulty at the EmailInstance level instead
            }

            EmailManager.Instance.ReceiveEmail(def);
        }
    }
}
