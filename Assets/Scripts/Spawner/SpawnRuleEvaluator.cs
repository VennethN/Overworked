using UnityEngine;

namespace Overworked.Spawner
{
    public class SpawnRuleEvaluator
    {
        public bool IsRuleActive(SpawnRule rule, float gameTime)
        {
            if (gameTime < rule.activeAfterSeconds)
                return false;

            if (rule.activeUntilSeconds > 0 && gameTime > rule.activeUntilSeconds)
                return false;

            return true;
        }

        public bool ShouldFire(SpawnRule rule, float gameTime, float nextFireTime)
        {
            if (rule.type != "interval") return false;
            if (!IsRuleActive(rule, gameTime)) return false;
            return gameTime >= nextFireTime;
        }

        public float GetNextInterval(SpawnRule rule, float difficultyMultiplier)
        {
            float baseInterval = Random.Range(rule.intervalSecondsMin, rule.intervalSecondsMax);
            // Higher difficulty = shorter intervals (faster spawning)
            return baseInterval / Mathf.Max(difficultyMultiplier, 0.1f);
        }
    }
}
