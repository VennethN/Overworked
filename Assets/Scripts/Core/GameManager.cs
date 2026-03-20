using UnityEngine;
using Overworked.Email;
using Overworked.Scoring;
using Overworked.Spawner;
using Overworked.UI;

namespace Overworked.Core
{
    public enum GameState { Menu, Playing, Paused, GameOver }
    public enum GameMode { Arcade, Story }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float dayLengthSeconds = 300f;

        [Header("References")]
        [SerializeField] private EmailSpawner emailSpawner;
        [SerializeField] private UIManager uiManager;

        private GameState _state = GameState.Menu;
        private GameMode _mode = GameMode.Arcade;
        private float _timeRemaining;
        private float _currentDayLength;

        public GameState State => _state;
        public GameMode CurrentMode => _mode;
        public float TimeRemaining => _timeRemaining;
        public float DayLength => _currentDayLength;
        public EmailSpawner Spawner => emailSpawner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _currentDayLength = dayLengthSeconds;
        }

        private void Start()
        {
            uiManager?.ShowModeSelect();
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

        public void StartArcade()
        {
            _mode = GameMode.Arcade;
            _currentDayLength = dayLengthSeconds;
            emailSpawner?.ResetToDefaultRules();
            emailSpawner?.SetActivePools(new[] { "general", "hr", "spam" });
            emailSpawner?.SetSpawnEmailIdWhitelist(null);
            StartGame();
        }

        public void StartStoryDay(float dayLength, float difficulty, string spawnRulesOverride, string[] emailPools, string[] spawnEmailIds = null)
        {
            _mode = GameMode.Story;
            _currentDayLength = dayLength;

            if (!string.IsNullOrEmpty(spawnRulesOverride))
                emailSpawner?.LoadRulesOverride(spawnRulesOverride);
            else
                emailSpawner?.ResetToDefaultRules();

            // Set active email pools for this day
            if (emailPools != null && emailPools.Length > 0)
                emailSpawner?.SetActivePools(emailPools);

            emailSpawner?.SetSpawnEmailIdWhitelist(spawnEmailIds);

            var diff = emailSpawner?.GetComponent<DifficultyController>();
            if (diff != null)
                diff.SetDifficultyOverride(difficulty);

            StartGame();
        }

        public void StartGame()
        {
            _timeRemaining = _currentDayLength;
            _state = GameState.Playing;

            // Reset systems
            EmailManager.Instance?.ClearInbox();
            ScoreManager.Instance?.ResetScore();

            var diff = emailSpawner?.GetComponent<DifficultyController>();
            if (_mode == GameMode.Arcade)
                diff?.ResetDifficulty();

            emailSpawner?.StartSpawning();
            uiManager?.HideGameOver();
            uiManager?.ShowInbox();

            GameEvents.FireGameStarted();
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

            if (_mode == GameMode.Arcade)
            {
                // Save arcade high score
                var save = SaveManager.Load();
                if (finalScore.totalScore > save.arcadeHighScore)
                {
                    save.arcadeHighScore = finalScore.totalScore;
                    SaveManager.Save(save);
                }
                uiManager?.ShowGameOver(finalScore);
            }
            // Story mode end is handled by StoryManager via OnGameOver event
        }

        public void ReturnToMenu()
        {
            _state = GameState.Menu;
            emailSpawner?.StopSpawning();
            emailSpawner?.SetSpawnEmailIdWhitelist(null);
            EmailManager.Instance?.ClearInbox();
            uiManager?.ShowModeSelect();
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
