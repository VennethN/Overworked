using System;
using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Scoring;

namespace Overworked.UI
{
    public class HUDController
    {
        private readonly VisualElement _root;
        private readonly Label _scoreLabel;
        private readonly Label _streakLabel;
        private readonly Label _dayTimer;
        private readonly Label _emailCount;
        private readonly Button _themeToggleBtn;

        public event Action OnThemeToggleClicked;

        public HUDController(VisualElement root)
        {
            _root = root;
            _scoreLabel = root.Q<Label>("score-label");
            _streakLabel = root.Q<Label>("streak-label");
            _dayTimer = root.Q<Label>("day-timer");
            _emailCount = root.Q<Label>("email-count");
            _themeToggleBtn = root.Q<Button>("theme-toggle-btn");

            _themeToggleBtn?.RegisterCallback<ClickEvent>(_ => OnThemeToggleClicked?.Invoke());
        }

        public void UpdateThemeButtonLabel(bool isLightMode)
        {
            if (_themeToggleBtn != null)
                _themeToggleBtn.text = isLightMode ? "Dark" : "Light";
        }

        public void UpdateScore(ScoreData score, int streak)
        {
            if (_scoreLabel != null)
                _scoreLabel.text = score.totalScore.ToString();

            if (_streakLabel != null)
            {
                if (streak > 1)
                    _streakLabel.text = $"Streak x{streak}!";
                else
                    _streakLabel.text = "";
            }
        }

        public void UpdateTimer(float secondsRemaining)
        {
            if (_dayTimer == null) return;

            int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
            int seconds = Mathf.FloorToInt(secondsRemaining % 60f);
            _dayTimer.text = $"{minutes}:{seconds:D2}";

            _dayTimer.RemoveFromClassList("hud-timer--warning");
            _dayTimer.RemoveFromClassList("hud-timer--danger");

            if (secondsRemaining <= 30f)
                _dayTimer.AddToClassList("hud-timer--danger");
            else if (secondsRemaining <= 60f)
                _dayTimer.AddToClassList("hud-timer--warning");
        }

        public void UpdateEmailCount(int count)
        {
            if (_emailCount != null)
                _emailCount.text = count.ToString();
        }
    }
}
