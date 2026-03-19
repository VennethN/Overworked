using UnityEngine;
using Overworked.Email;
using Overworked.Scoring;
using Overworked.Spawner;
using Overworked.UI;

namespace Overworked.Core
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float dayLengthSeconds = 300f;

        [Header("References")]
        [SerializeField] private EmailSpawner emailSpawner;
        [SerializeField] private UIManager uiManager;

        private GameState _state = GameState.Menu;
        private float _timeRemaining;

        public GameState State => _state;
        public float TimeRemaining => _timeRemaining;
        public float DayLength => dayLengthSeconds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-start for now (replace with menu later)
            StartGame();
        }

        private void Update()
        {
            if (_state != GameState.Playing) return;

            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                EndGame();
            }
        }

        public void StartGame()
        {
            _timeRemaining = dayLengthSeconds;
            _state = GameState.Playing;

            // Reset systems
            EmailManager.Instance?.ClearInbox();
            ScoreManager.Instance?.ResetScore();

            emailSpawner?.StartSpawning();
            uiManager?.HideGameOver();
            uiManager?.ShowInbox();

            GameEvents.FireGameStarted();
            Debug.Log("Game Started!");
        }

        public void PauseGame()
        {
            if (_state != GameState.Playing) return;
            _state = GameState.Paused;
            Time.timeScale = 0f;
            GameEvents.FireGamePaused();
        }

        public void ResumeGame()
        {
            if (_state != GameState.Paused) return;
            _state = GameState.Playing;
            Time.timeScale = 1f;
            GameEvents.FireGameResumed();
        }

        public void EndGame()
        {
            _state = GameState.GameOver;
            emailSpawner?.StopSpawning();

            ScoreData finalScore = ScoreManager.Instance != null
                ? ScoreManager.Instance.CurrentScore
                : default;

            GameEvents.FireGameOver(finalScore);
            uiManager?.ShowGameOver(finalScore);

            Debug.Log($"Game Over! Final Score: {finalScore.totalScore}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                GameEvents.ClearAll();
            }
        }
    }
}
