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
        private readonly Button _settingsBtn;

        public event Action OnThemeToggleClicked;
        public event Action OnSettingsClicked;

        public HUDController(VisualElement root)
        {
            _root = root;
            _scoreLabel = root.Q<Label>("score-label");
            _streakLabel = root.Q<Label>("streak-label");
            _dayTimer = root.Q<Label>("day-timer");
            _emailCount = root.Q<Label>("email-count");
            _themeToggleBtn = root.Q<Button>("theme-toggle-btn");
            _settingsBtn = root.Q<Button>("settings-btn");

            _themeToggleBtn?.RegisterCallback<ClickEvent>(_ => OnThemeToggleClicked?.Invoke());
            _settingsBtn?.RegisterCallback<ClickEvent>(_ => OnSettingsClicked?.Invoke());
        }

        public void UpdateThemeButtonLabel(bool isLightMode)
        {
            if (_themeToggleBtn != null)
                _themeToggleBtn.text = isLightMode ? "Dark" : "Light";
        }

        private int _lastScore;

        public void UpdateScore(ScoreData score, int streak)
        {
            if (_scoreLabel != null)
            {
                int delta = score.totalScore - _lastScore;
                _scoreLabel.text = score.totalScore.ToString();

                // Pop on score change
                if (delta != 0 && _lastScore != 0)
                {
                    UIEffects.Pop(_scoreLabel, delta > 0 ? 1.2f : 1.15f, 150);
                }
                _lastScore = score.totalScore;
            }

            if (_streakLabel != null)
            {
                if (streak > 1)
                {
                    _streakLabel.text = $"Streak x{streak}!";
                    _streakLabel.AddToClassList("hud-streak-active");
                }
                else
                {
                    _streakLabel.text = "";
                    _streakLabel.RemoveFromClassList("hud-streak-active");
                }
            }
        }

        private bool _wasDanger;

        public void UpdateTimer(float secondsRemaining)
        {
            if (_dayTimer == null) return;

            int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
            int seconds = Mathf.FloorToInt(secondsRemaining % 60f);
            _dayTimer.text = $"{minutes}:{seconds:D2}";

            _dayTimer.RemoveFromClassList("hud-timer--warning");
            _dayTimer.RemoveFromClassList("hud-timer--danger");
            _dayTimer.RemoveFromClassList("hud-timer-danger");

            if (secondsRemaining <= 30f)
            {
                _dayTimer.AddToClassList("hud-timer--danger");
                _dayTimer.AddToClassList("hud-timer-danger");

                // Pulse effect on transition to danger
                if (!_wasDanger)
                {
                    _wasDanger = true;
                    UIEffects.Pop(_dayTimer, 1.3f, 200);
                }
            }
            else if (secondsRemaining <= 60f)
            {
                _dayTimer.AddToClassList("hud-timer--warning");
                _wasDanger = false;
            }
            else
            {
                _wasDanger = false;
            }
        }

        public void UpdateEmailCount(int count)
        {
            if (_emailCount != null)
                _emailCount.text = count.ToString();
        }
    }
}
