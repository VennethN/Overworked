using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Core;
using Overworked.Story.Data;

namespace Overworked.UI
{
    public class ModeSelectController
    {
        private readonly VisualElement _root;
        private readonly Action _onArcade;
        private readonly Action<int> _onStoryDay;

        private VisualElement _mainView;
        private VisualElement _daySelectView;
        private StoryCollection _storyData;

        public ModeSelectController(VisualElement root, Action onArcade, Action<int> onStoryDay)
        {
            _root = root;
            _onArcade = onArcade;
            _onStoryDay = onStoryDay;

            LoadStoryData();
            BuildUI();
        }

        private void LoadStoryData()
        {
            var asset = Resources.Load<TextAsset>("Data/Story/story_data");
            if (asset != null)
                _storyData = JsonUtility.FromJson<StoryCollection>(asset.text);
        }

        private void BuildUI()
        {
            _root.Clear();

            var overlay = new VisualElement();
            overlay.AddToClassList("overlay");
            overlay.style.backgroundColor = new Color(0.067f, 0.094f, 0.153f, 1f);

            // Main view (mode selection)
            _mainView = new VisualElement();
            _mainView.style.alignItems = Align.Center;
            _mainView.style.justifyContent = Justify.Center;
            _mainView.style.flexGrow = 1;
            BuildMainView(_mainView);
            overlay.Add(_mainView);

            // Day select view (hidden initially)
            _daySelectView = new VisualElement();
            _daySelectView.style.alignItems = Align.Center;
            _daySelectView.style.justifyContent = Justify.FlexStart;
            _daySelectView.style.flexGrow = 1;
            _daySelectView.style.paddingTop = 60;
            _daySelectView.style.display = DisplayStyle.None;
            BuildDaySelectView(_daySelectView);
            overlay.Add(_daySelectView);

            _root.Add(overlay);

            // Fade in
            overlay.schedule.Execute(() => overlay.AddToClassList("overlay--visible"));
        }

        private void BuildMainView(VisualElement container)
        {
            // Title
            var title = new Label("OVERWORKED");
            title.style.fontSize = 48;
            title.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 4;
            title.style.marginBottom = 8;
            container.Add(title);

            var subtitle = new Label("Email Management Game");
            subtitle.style.fontSize = 16;
            subtitle.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
            subtitle.style.marginBottom = 32;
            container.Add(subtitle);

            var save = SaveManager.Load();

            var nameInput = new TextField();
            nameInput.label = "Nama Pegawai:";
            nameInput.value = string.IsNullOrEmpty(save.playerName) ? "Pegawai Baru" : save.playerName;
            nameInput.style.width = 300;
            nameInput.style.marginBottom = 48;
            nameInput.RegisterValueChangedCallback(evt => {
                var s = SaveManager.Load();
                s.playerName = evt.newValue;
                SaveManager.Save(s);
            });
            container.Add(nameInput);

            // Mode buttons row
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.Center;

            var arcadeBtn = CreateModeCard(
                "\ue003", // Material Icons: games
                "ARCADE",
                "Mode tanpa batas\nRaih skor tertinggi!",
                new Color(0.376f, 0.51f, 0.965f, 1f),
                () => _onArcade?.Invoke()
            );
            row.Add(arcadeBtn);

            var spacer = new VisualElement();
            spacer.style.width = 24;
            row.Add(spacer);

            var storyBtn = CreateModeCard(
                "\ue865", // Material Icons: menu_book
                "CERITA",
                "7 hari tantangan\ndengan alur cerita",
                new Color(0.063f, 0.725f, 0.506f, 1f),
                () => ShowDaySelect()
            );
            row.Add(storyBtn);

            container.Add(row);

            // High score
            if (save.arcadeHighScore > 0)
            {
                var highScore = new Label($"Skor Tertinggi: {save.arcadeHighScore}");
                highScore.style.fontSize = 14;
                highScore.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
                highScore.style.marginTop = 32;
                container.Add(highScore);
            }
        }

        private VisualElement CreateModeCard(string icon, string title, string desc, Color accent, Action onClick)
        {
            var card = new Button();
            card.style.width = 220;
            card.style.paddingTop = 32;
            card.style.paddingBottom = 32;
            card.style.paddingLeft = 24;
            card.style.paddingRight = 24;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(0.118f, 0.161f, 0.212f, 1f);
            card.style.borderTopLeftRadius = 12;
            card.style.borderTopRightRadius = 12;
            card.style.borderBottomLeftRadius = 12;
            card.style.borderBottomRightRadius = 12;
            card.style.borderTopWidth = 2;
            card.style.borderTopColor = accent;
            card.style.borderBottomWidth = 0;
            card.style.borderLeftWidth = 0;
            card.style.borderRightWidth = 0;
            card.text = "";

            var iconLabel = new Label(icon);
            iconLabel.AddToClassList("sidebar-icon");
            iconLabel.style.fontSize = 36;
            iconLabel.style.color = accent;
            iconLabel.style.marginBottom = 16;
            iconLabel.style.width = new StyleLength(StyleKeyword.Auto);
            card.Add(iconLabel);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 20;
            titleLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 2;
            titleLabel.style.marginBottom = 8;
            card.Add(titleLabel);

            var descLabel = new Label(desc);
            descLabel.style.fontSize = 13;
            descLabel.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
            descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(descLabel);

            card.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());

            return card;
        }

        private void ShowDaySelect()
        {
            _mainView.style.display = DisplayStyle.None;
            _daySelectView.style.display = DisplayStyle.Flex;
        }

        private void ShowMain()
        {
            _mainView.style.display = DisplayStyle.Flex;
            _daySelectView.style.display = DisplayStyle.None;
        }

        private void BuildDaySelectView(VisualElement container)
        {
            // Header row
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.width = 500;
            header.style.marginBottom = 32;

            var backBtn = new Button(() => ShowMain());
            backBtn.text = "\u2190  Kembali";
            backBtn.style.fontSize = 14;
            backBtn.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
            backBtn.style.backgroundColor = Color.clear;
            backBtn.style.borderTopWidth = 0;
            backBtn.style.borderBottomWidth = 0;
            backBtn.style.borderLeftWidth = 0;
            backBtn.style.borderRightWidth = 0;
            backBtn.style.paddingLeft = 0;
            header.Add(backBtn);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            header.Add(spacer);

            var titleLabel = new Label("MODE CERITA");
            titleLabel.style.fontSize = 20;
            titleLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 2;
            header.Add(titleLabel);

            container.Add(header);

            // Day list
            if (_storyData?.days == null)
            {
                var noData = new Label("Data cerita belum tersedia.");
                noData.style.fontSize = 14;
                noData.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
                container.Add(noData);
                return;
            }

            var save = SaveManager.Load();

            var scrollView = new ScrollView();
            scrollView.style.width = 500;
            scrollView.style.maxHeight = 450;

            foreach (var day in _storyData.days)
            {
                bool unlocked = save.IsDayUnlocked(day.dayNumber, day.unlockedAfterDay);
                int bestScore = save.GetBestScore(day.dayNumber);
                bool passed = bestScore >= day.scoreGoal;

                var dayRow = CreateDayRow(day, unlocked, bestScore, passed);
                scrollView.Add(dayRow);
            }

            container.Add(scrollView);
        }

        private VisualElement CreateDayRow(DayDefinition day, bool unlocked, int bestScore, bool passed)
        {
            var row = new Button();
            row.text = "";
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 16;
            row.style.paddingBottom = 16;
            row.style.paddingLeft = 20;
            row.style.paddingRight = 20;
            row.style.marginBottom = 4;
            row.style.borderTopLeftRadius = 8;
            row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8;
            row.style.borderBottomRightRadius = 8;
            row.style.borderTopWidth = 0;
            row.style.borderBottomWidth = 0;
            row.style.borderLeftWidth = 0;
            row.style.borderRightWidth = 0;
            row.style.backgroundColor = unlocked
                ? new Color(0.118f, 0.161f, 0.212f, 1f)
                : new Color(0.08f, 0.11f, 0.15f, 1f);
            row.style.opacity = unlocked ? 1f : 0.5f;

            // Day number badge
            var dayBadge = new Label($"{day.dayNumber}");
            dayBadge.style.fontSize = 16;
            dayBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            dayBadge.style.color = passed
                ? new Color(0.29f, 0.87f, 0.5f, 1f)
                : new Color(0.945f, 0.96f, 0.976f, 1f);
            dayBadge.style.width = 32;
            dayBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            dayBadge.style.marginRight = 16;
            row.Add(dayBadge);

            // Info column
            var info = new VisualElement();
            info.style.flexGrow = 1;

            var titleLabel = new Label(day.title);
            titleLabel.style.fontSize = 14;
            titleLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            info.Add(titleLabel);

            var goalLabel = new Label($"Target: {day.scoreGoal} skor  \u2022  {day.dayLengthSeconds}s");
            goalLabel.style.fontSize = 12;
            goalLabel.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
            goalLabel.style.marginTop = 2;
            info.Add(goalLabel);

            row.Add(info);

            // Score / lock status
            if (!unlocked)
            {
                var lockIcon = new Label("\ue897"); // Material Icons: lock
                lockIcon.AddToClassList("sidebar-icon");
                lockIcon.style.fontSize = 20;
                lockIcon.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
                lockIcon.style.width = new StyleLength(StyleKeyword.Auto);
                row.Add(lockIcon);
            }
            else if (bestScore > 0)
            {
                var scoreLabel = new Label($"{bestScore}");
                scoreLabel.style.fontSize = 14;
                scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                scoreLabel.style.color = passed
                    ? new Color(0.29f, 0.87f, 0.5f, 1f)
                    : new Color(0.973f, 0.443f, 0.443f, 1f);
                row.Add(scoreLabel);
            }

            if (unlocked)
            {
                int dayNum = day.dayNumber;
                row.RegisterCallback<ClickEvent>(_ => _onStoryDay?.Invoke(dayNum));
            }

            return row;
        }
    }
}
