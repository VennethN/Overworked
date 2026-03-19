using System;
using Overworked.Email;
using Overworked.Actions;
using Overworked.Scoring;

namespace Overworked.Core
{
    public static class GameEvents
    {
        // Email lifecycle
        public static event Action<EmailInstance> OnEmailReceived;
        public static event Action<EmailInstance> OnEmailOpened;
        public static event Action<EmailInstance> OnEmailExpired;
        public static event Action<EmailInstance> OnEmailDeleted;

        // Actions
        public static event Action<EmailInstance, ReplyResult> OnEmailReplied;
        public static event Action<EmailInstance> OnTaskCompleted;
        public static event Action<EmailInstance> OnTaskFailed;

        // Game state
        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<ScoreData> OnGameOver;

        // Spawner triggers
        public static event Action<string> OnGameEvent;

        // Story mode
        public static event Action<int> OnDayStarted;
        public static event Action<int, bool> OnDayEnded;
        public static event Action OnDialogueStarted;
        public static event Action OnDialogueCompleted;

        // Fire methods
        public static void FireEmailReceived(EmailInstance e) => OnEmailReceived?.Invoke(e);
        public static void FireEmailOpened(EmailInstance e) => OnEmailOpened?.Invoke(e);
        public static void FireEmailExpired(EmailInstance e) => OnEmailExpired?.Invoke(e);
        public static void FireEmailDeleted(EmailInstance e) => OnEmailDeleted?.Invoke(e);
        public static void FireEmailReplied(EmailInstance e, ReplyResult r) => OnEmailReplied?.Invoke(e, r);
        public static void FireTaskCompleted(EmailInstance e) => OnTaskCompleted?.Invoke(e);
        public static void FireTaskFailed(EmailInstance e) => OnTaskFailed?.Invoke(e);
        public static void FireGameStarted() => OnGameStarted?.Invoke();
        public static void FireGamePaused() => OnGamePaused?.Invoke();
        public static void FireGameResumed() => OnGameResumed?.Invoke();
        public static void FireGameOver(ScoreData data) => OnGameOver?.Invoke(data);
        public static void FireGameEvent(string eventId) => OnGameEvent?.Invoke(eventId);
        public static void FireDayStarted(int day) => OnDayStarted?.Invoke(day);
        public static void FireDayEnded(int day, bool passed) => OnDayEnded?.Invoke(day, passed);
        public static void FireDialogueStarted() => OnDialogueStarted?.Invoke();
        public static void FireDialogueCompleted() => OnDialogueCompleted?.Invoke();

        public static void ClearAll()
        {
            OnEmailReceived = null;
            OnEmailOpened = null;
            OnEmailExpired = null;
            OnEmailDeleted = null;
            OnEmailReplied = null;
            OnTaskCompleted = null;
            OnTaskFailed = null;
            OnGameStarted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameOver = null;
            OnGameEvent = null;
            OnDayStarted = null;
            OnDayEnded = null;
            OnDialogueStarted = null;
            OnDialogueCompleted = null;
        }
    }
}
