using System;

namespace Overworked.Spawner
{
    [Serializable]
    public class SpawnRule
    {
        public string id;
        public string type; // "interval" or "event"
        public float intervalSecondsMin;
        public float intervalSecondsMax;
        public string[] emailPool; // Email types to draw from
        public string[] emailTags; // Optional tag filter
        public float weight;
        public int burstCount;
        public float burstIntervalSeconds;
        public string triggerEvent; // For event-based rules
        public float activeAfterSeconds;
        public float activeUntilSeconds; // -1 = forever
    }

    [Serializable]
    public class SpawnRuleCollection
    {
        public SpawnRule[] rules;
    }
}
