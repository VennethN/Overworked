using UnityEngine;

namespace Overworked.Spawner
{
    public class DifficultyController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0f, 1f, 1f, 2.5f);
        [SerializeField] private AnimationCurve expirationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
        [SerializeField] private float difficultyPlateauSeconds = 300f;

        private float _elapsedGameTime;

        /// <summary>
        /// Multiplier for spawn rate. 1.0 = normal, 2.0 = emails arrive twice as fast.
        /// </summary>
        public float SpawnRateMultiplier => spawnRateCurve.Evaluate(NormalizedTime);

        /// <summary>
        /// Multiplier for expiration times. 1.0 = normal, 0.5 = emails expire in half the time.
        /// </summary>
        public float ExpirationMultiplier => expirationCurve.Evaluate(NormalizedTime);

        public float ElapsedTime => _elapsedGameTime;

        private float NormalizedTime => Mathf.Clamp01(_elapsedGameTime / difficultyPlateauSeconds);

        private void Update()
        {
            _elapsedGameTime += Time.deltaTime;
        }

        public void ResetDifficulty()
        {
            _elapsedGameTime = 0f;
        }

        public void SetDifficultyOverride(float normalizedDifficulty)
        {
            _elapsedGameTime = Mathf.Clamp01(normalizedDifficulty) * difficultyPlateauSeconds;
        }
    }
}
