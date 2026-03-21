using System;
using UnityEngine;
using Overworked.Actions;
using Overworked.Core;
using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.Scoring
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private int spamDeleteBonus = 5;
        [SerializeField] private float streakBonusPerLevel = 0.1f;
        [SerializeField] private float maxStreakMultiplier = 3.0f;

        private ScoreData _score;
        private int _currentStreak;

        /// <summary>Fired whenever the score changes. Param is the delta (positive or negative).</summary>
        public event Action<int> OnScoreChanged;

        public ScoreData CurrentScore => _score;
        public int CurrentStreak => _currentStreak;
        public float StreakMultiplier => Mathf.Min(1f + _currentStreak * streakBonusPerLevel, maxStreakMultiplier);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnEmailReplied += HandleEmailReplied;
            GameEvents.OnTaskCompleted += HandleTaskCompleted;
            GameEvents.OnTaskFailed += HandleTaskFailed;
            GameEvents.OnEmailExpired += HandleEmailExpired;
            GameEvents.OnEmailDeleted += HandleEmailDeleted;
        }

        private void OnDisable()
        {
            GameEvents.OnEmailReplied -= HandleEmailReplied;
            GameEvents.OnTaskCompleted -= HandleTaskCompleted;
            GameEvents.OnTaskFailed -= HandleTaskFailed;
            GameEvents.OnEmailExpired -= HandleEmailExpired;
            GameEvents.OnEmailDeleted -= HandleEmailDeleted;
        }

        public void ResetScore()
        {
            _score = default;
            _currentStreak = 0;
        }

        private void AddScore(int delta)
        {
            if (delta == 0) return;
            _score.totalScore += delta;
            Debug.Log($"[Score] AddScore({delta}) total={_score.totalScore} subscribers={OnScoreChanged?.GetInvocationList()?.Length ?? 0}");
            OnScoreChanged?.Invoke(delta);
        }

        private void HandleEmailReplied(EmailInstance email, ReplyResult result)
        {
            int points = Mathf.RoundToInt(result.ScoreChange * StreakMultiplier);

            if (email.Definition.parsedType == EmailType.Spam)
            {
                // Replying to spam is always bad
                _score.spamReplied++;
                AddScore(result.ScoreChange); // No streak bonus for mistakes
                ResetStreak();
                return;
            }

            if (result.IsCorrect)
            {
                _score.correctReplies++;
                AddScore(points);
                IncrementStreak();
            }
            else
            {
                _score.wrongReplies++;
                AddScore(result.ScoreChange); // No streak bonus for wrong answers
                ResetStreak();
            }
        }

        private void HandleTaskCompleted(EmailInstance email)
        {
            _score.tasksCompleted++;
            int baseScore = 15;
            if (email.Definition.taskTrigger != null)
            {
                string taskId = email.Definition.taskTrigger.taskId;
                // Score varies by task type but we use a default for now
            }
            int points = Mathf.RoundToInt(baseScore * StreakMultiplier);
            AddScore(points);
            IncrementStreak();
        }

        private void HandleTaskFailed(EmailInstance email)
        {
            _score.tasksFailed++;
            AddScore(-10);
            ResetStreak();
        }

        private void HandleEmailExpired(EmailInstance email)
        {
            _score.emailsExpired++;

            int penalty = email.Definition.parsedPriority switch
            {
                EmailPriority.Critical => -40,
                EmailPriority.High => -20,
                EmailPriority.Medium => -10,
                EmailPriority.Low => -5,
                _ => 0
            };

            AddScore(penalty);
            ResetStreak();
        }

        private void HandleEmailDeleted(EmailInstance email)
        {
            if (email.Definition.parsedType == EmailType.Spam)
            {
                _score.spamDeleted++;
                AddScore(spamDeleteBonus);
                IncrementStreak();
            }
            else if (email.IsCompleted || email.IsActedUpon || email.IsExpired
                     || email.Definition.parsedType == EmailType.Info)
            {
                // Safe to delete: done emails, expired emails, and info emails
            }
            else
            {
                // Deleting an active real email is a penalty
                AddScore(-10);
                ResetStreak();
            }
        }

        private void IncrementStreak()
        {
            _currentStreak++;
            if (_currentStreak > _score.highestStreak)
                _score.highestStreak = _currentStreak;
        }

        private void ResetStreak()
        {
            _currentStreak = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
