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
        private readonly Action _onSettings;

        private VisualElement _nameInputView;
        private VisualElement _mainView;
        private VisualElement _daySelectView;
        private VisualElement _creditsView;
        private StoryCollection _storyData;
        private Texture2D _titleTexture;

        public ModeSelectController(VisualElement root, Action onArcade, Action<int> onStoryDay, Action onSettings = null)
        {
            _root = root;
            _onArcade = onArcade;
            _onStoryDay = onStoryDay;
            _onSettings = onSettings;

            _titleTexture = Resources.Load<Texture2D>("Art/OverworkedText");
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

            // Name input view (shown initially)
            _nameInputView = new VisualElement();
            _nameInputView.style.alignItems = Align.Center;
            _nameInputView.style.justifyContent = Justify.Center;
            _nameInputView.style.flexGrow = 1;
            BuildNameInputView(_nameInputView);
            overlay.Add(_nameInputView);

            // Main view (mode selection)
            _mainView = new VisualElement();
            _mainView.style.alignItems = Align.Center;
            _mainView.style.justifyContent = Justify.Center;
            _mainView.style.flexGrow = 1;
            _mainView.style.display = DisplayStyle.None;
            BuildMainView(_mainView);
            overlay.Add(_mainView);

            // Day select view (hidden initially)
            _daySelectView = new VisualElement();
            _daySelectView.style.alignItems = Align.Center;
            _daySelectView.style.justifyContent = Justify.FlexStart;
            _daySelectView.style.flexGrow = 1;
            _daySelectView.style.paddingTop = 40;
            _daySelectView.style.display = DisplayStyle.None;
            BuildDaySelectView(_daySelectView);
            overlay.Add(_daySelectView);

            // Credits view (hidden initially)
            _creditsView = new VisualElement();
            _creditsView.style.alignItems = Align.Center;
            _creditsView.style.justifyContent = Justify.FlexStart;
            _creditsView.style.flexGrow = 1;
            _creditsView.style.paddingTop = 40;
            _creditsView.style.display = DisplayStyle.None;
            BuildCreditsView(_creditsView);
            overlay.Add(_creditsView);

            _root.Add(overlay);

            // Fade in
            overlay.schedule.Execute(() => overlay.AddToClassList("overlay--visible"));
        }

        private VisualElement CreateTitleImage()
        {
            var img = new VisualElement();
            if (_titleTexture != null)
            {
                img.style.backgroundImage = new StyleBackground(_titleTexture);
                // 2172x328 aspect ratio ≈ 6.62:1
                img.style.width = 320;
                img.style.height = 48;
                img.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            else
            {
                // Fallback to text if image fails to load
                var label = new Label("OVERWORKED");
                label.style.fontSize = 32;
                label.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.letterSpacing = 3;
                img.Add(label);
            }
            img.style.marginTop = 16;
            img.style.marginBottom = 6;
            return img;
        }

        private void BuildNameInputView(VisualElement container)
        {
            container.Add(CreateTitleImage());

            var subtitle = new Label("Profil Pegawai");
            subtitle.style.fontSize = 13;
            subtitle.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
            subtitle.style.marginBottom = 32;
            container.Add(subtitle);

            var save = SaveManager.Load();

            var nameInput = new TextField();
            nameInput.label = "Nama:";
            nameInput.value = string.IsNullOrEmpty(save.playerName) ? "" : save.playerName;
            nameInput.style.width = 240;
            nameInput.style.marginBottom = 24;
            nameInput.style.alignItems = Align.Center;
            container.Add(nameInput);

            // Placeholder text
            var placeholder = new Label("Masukkan namamu...");
            placeholder.style.position = Position.Absolute;
            placeholder.style.left = 8;
            placeholder.style.top = 0;
            placeholder.style.bottom = 0;
            placeholder.style.unityTextAlign = TextAnchor.MiddleLeft;
            placeholder.style.fontSize = 13;
            placeholder.style.color = new Color(0.45f, 0.5f, 0.58f, 0.7f);
            placeholder.pickingMode = PickingMode.Ignore;

            nameInput.schedule.Execute(() => {
                var inputPart = nameInput.Q("unity-text-input");
                if (inputPart != null)
                {
                    inputPart.style.overflow = Overflow.Visible;
                    if (string.IsNullOrEmpty(nameInput.value))
                        inputPart.Add(placeholder);
                }
            });

            nameInput.RegisterValueChangedCallback(evt => {
                if (string.IsNullOrEmpty(evt.newValue))
                {
                    var inputPart = nameInput.Q("unity-text-input");
                    if (inputPart != null && placeholder.parent != inputPart)
                        inputPart.Add(placeholder);
                }
                else
                {
                    placeholder.RemoveFromHierarchy();
                }
            });

            // Auto-focus the text field so keyboard works immediately (handles WebGL canvas focus too)
            WebGLTextFieldFix.FocusTextField(nameInput);

            nameInput.schedule.Execute(() => {
                var labelPart = nameInput.Q<Label>();
                if (labelPart != null) {
                    labelPart.style.color = new Color(0.72f, 0.76f, 0.81f, 1f);
                    labelPart.style.unityTextAlign = TextAnchor.MiddleLeft;
                    labelPart.style.minWidth = 50;
                }
                var inputPart = nameInput.Q("unity-text-input");
                if (inputPart != null) {
                    inputPart.style.backgroundColor = new Color(0.118f, 0.161f, 0.212f, 1f);
                    inputPart.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
                    inputPart.style.borderTopWidth = 1;
                    inputPart.style.borderBottomWidth = 1;
                    inputPart.style.borderLeftWidth = 1;
                    inputPart.style.borderRightWidth = 1;
                    inputPart.style.borderTopColor = new Color(0.376f, 0.51f, 0.965f, 0.5f);
                    inputPart.style.borderBottomColor = new Color(0.376f, 0.51f, 0.965f, 0.5f);
                    inputPart.style.borderLeftColor = new Color(0.376f, 0.51f, 0.965f, 0.5f);
                    inputPart.style.borderRightColor = new Color(0.376f, 0.51f, 0.965f, 0.5f);
                    inputPart.style.borderTopLeftRadius = 5;
                    inputPart.style.borderTopRightRadius = 5;
                    inputPart.style.borderBottomLeftRadius = 5;
                    inputPart.style.borderBottomRightRadius = 5;
                    inputPart.style.paddingTop = 5;
                    inputPart.style.paddingBottom = 5;
                    inputPart.style.paddingLeft = 8;
                    inputPart.style.paddingRight = 8;
                }
            });

            var continueBtn = new Button(() => {
                WebGLTextFieldFix.StopKeepFocus(nameInput);
                var s = SaveManager.Load();
                s.playerName = string.IsNullOrWhiteSpace(nameInput.value) ? "Pegawai Baru" : nameInput.value;
                SaveManager.Save(s);

                _nameInputView.style.display = DisplayStyle.None;
                _mainView.style.display = DisplayStyle.Flex;
            });
            continueBtn.text = "Lanjut";
            continueBtn.style.paddingTop = 10;
            continueBtn.style.paddingBottom = 10;
            continueBtn.style.paddingLeft = 24;
            continueBtn.style.paddingRight = 24;
            continueBtn.style.fontSize = 14;
            continueBtn.style.backgroundColor = new Color(0.15f, 0.65f, 0.35f, 1f);
            continueBtn.style.color = Color.white;
            continueBtn.style.borderTopWidth = 0;
            continueBtn.style.borderBottomWidth = 0;
            continueBtn.style.borderLeftWidth = 0;
            continueBtn.style.borderRightWidth = 0;
            continueBtn.style.borderTopLeftRadius = 5;
            continueBtn.style.borderTopRightRadius = 5;
            continueBtn.style.borderBottomLeftRadius = 5;
            continueBtn.style.borderBottomRightRadius = 5;

            container.Add(continueBtn);

            var quitBtn = new Button(() => Application.Quit());
            quitBtn.text = "Keluar";
            quitBtn.style.marginTop = 16;
            quitBtn.style.paddingTop = 10;
            quitBtn.style.paddingBottom = 10;
            quitBtn.style.paddingLeft = 24;
            quitBtn.style.paddingRight = 24;
            quitBtn.style.fontSize = 14;
            quitBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
            quitBtn.style.color = Color.white;
            quitBtn.style.borderTopWidth = 0;
            quitBtn.style.borderBottomWidth = 0;
            quitBtn.style.borderLeftWidth = 0;
            quitBtn.style.borderRightWidth = 0;
            quitBtn.style.borderTopLeftRadius = 5;
            quitBtn.style.borderTopRightRadius = 5;
            quitBtn.style.borderBottomLeftRadius = 5;
            quitBtn.style.borderBottomRightRadius = 5;
            container.Add(quitBtn);
        }

        private void BuildMainView(VisualElement container)
        {
            // Title
            container.Add(CreateTitleImage());

            var subtitle = new Label("Email Management Game");
            subtitle.style.fontSize = 13;
            subtitle.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
            subtitle.style.marginBottom = 32;
            container.Add(subtitle);

            var save = SaveManager.Load();

            // Mode buttons row
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.Center;

            var arcadeBtn = CreateModeCard(
                "\ue30f", // Material Icons: sports_esports (gamepad)
                "ARCADE",
                "Mode tanpa batas\nRaih skor tertinggi!",
                new Color(0.376f, 0.51f, 0.965f, 1f),
                () => _onArcade?.Invoke()
            );
            row.Add(arcadeBtn);

            var spacer = new VisualElement();
            spacer.style.width = 16;
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
                highScore.style.fontSize = 12;
                highScore.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
                highScore.style.marginTop = 24;
                container.Add(highScore);
            }

            // Settings button
            if (_onSettings != null)
            {
                var settingsBtn = new Button(() => _onSettings.Invoke());
                settingsBtn.text = "Settings";
                settingsBtn.style.marginTop = 20;
                settingsBtn.style.paddingTop = 8;
                settingsBtn.style.paddingBottom = 8;
                settingsBtn.style.paddingLeft = 20;
                settingsBtn.style.paddingRight = 20;
                settingsBtn.style.fontSize = 12;
                settingsBtn.style.backgroundColor = new Color(0.118f, 0.161f, 0.212f, 1f);
                settingsBtn.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
                settingsBtn.style.borderTopWidth = 1;
                settingsBtn.style.borderBottomWidth = 1;
                settingsBtn.style.borderLeftWidth = 1;
                settingsBtn.style.borderRightWidth = 1;
                settingsBtn.style.borderTopColor = new Color(0.235f, 0.306f, 0.416f, 1f);
                settingsBtn.style.borderBottomColor = new Color(0.235f, 0.306f, 0.416f, 1f);
                settingsBtn.style.borderLeftColor = new Color(0.235f, 0.306f, 0.416f, 1f);
                settingsBtn.style.borderRightColor = new Color(0.235f, 0.306f, 0.416f, 1f);
                settingsBtn.style.borderTopLeftRadius = 5;
                settingsBtn.style.borderTopRightRadius = 5;
                settingsBtn.style.borderBottomLeftRadius = 5;
                settingsBtn.style.borderBottomRightRadius = 5;
                container.Add(settingsBtn);
            }

            // Credits button
            var creditsBtn = new Button(() =>
            {
                _mainView.style.display = DisplayStyle.None;
                _creditsView.style.display = DisplayStyle.Flex;
            });
            creditsBtn.text = "Credits";
            creditsBtn.style.marginTop = 8;
            creditsBtn.style.paddingTop = 8;
            creditsBtn.style.paddingBottom = 8;
            creditsBtn.style.paddingLeft = 20;
            creditsBtn.style.paddingRight = 20;
            creditsBtn.style.fontSize = 12;
            creditsBtn.style.backgroundColor = new Color(0.118f, 0.161f, 0.212f, 1f);
            creditsBtn.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
            creditsBtn.style.borderTopWidth = 1;
            creditsBtn.style.borderBottomWidth = 1;
            creditsBtn.style.borderLeftWidth = 1;
            creditsBtn.style.borderRightWidth = 1;
            creditsBtn.style.borderTopColor = new Color(0.235f, 0.306f, 0.416f, 1f);
            creditsBtn.style.borderBottomColor = new Color(0.235f, 0.306f, 0.416f, 1f);
            creditsBtn.style.borderLeftColor = new Color(0.235f, 0.306f, 0.416f, 1f);
            creditsBtn.style.borderRightColor = new Color(0.235f, 0.306f, 0.416f, 1f);
            creditsBtn.style.borderTopLeftRadius = 5;
            creditsBtn.style.borderTopRightRadius = 5;
            creditsBtn.style.borderBottomLeftRadius = 5;
            creditsBtn.style.borderBottomRightRadius = 5;
            container.Add(creditsBtn);

            // Scale hint
            var scaleHint = new Label("Ctrl +/\u2212 untuk ubah ukuran tampilan");
            scaleHint.style.fontSize = 10;
            scaleHint.style.color = new Color(0.392f, 0.455f, 0.545f, 0.7f);
            scaleHint.style.marginTop = 16;
            scaleHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            container.Add(scaleHint);
        }

        private VisualElement CreateModeCard(string icon, string title, string desc, Color accent, Action onClick)
        {
            var card = new Button();
            card.style.width = 160;
            card.style.paddingTop = 24;
            card.style.paddingBottom = 24;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(0.118f, 0.161f, 0.212f, 1f);
            card.style.borderTopLeftRadius = 10;
            card.style.borderTopRightRadius = 10;
            card.style.borderBottomLeftRadius = 10;
            card.style.borderBottomRightRadius = 10;
            card.style.borderTopWidth = 2;
            card.style.borderTopColor = accent;
            card.style.borderBottomWidth = 0;
            card.style.borderLeftWidth = 0;
            card.style.borderRightWidth = 0;
            card.text = "";

            var iconLabel = new Label(icon);
            iconLabel.AddToClassList("sidebar-icon");
            iconLabel.style.fontSize = 26;
            iconLabel.style.color = accent;
            iconLabel.style.marginBottom = 12;
            iconLabel.style.width = new StyleLength(StyleKeyword.Auto);
            card.Add(iconLabel);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 16;
            titleLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 1;
            titleLabel.style.marginBottom = 6;
            card.Add(titleLabel);

            var descLabel = new Label(desc);
            descLabel.style.fontSize = 11;
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

        public void ShowDaySelectPublic()
        {
            _nameInputView.style.display = DisplayStyle.None;
            _mainView.style.display = DisplayStyle.None;
            _creditsView.style.display = DisplayStyle.None;
            _daySelectView.Clear();
            BuildDaySelectView(_daySelectView);
            _daySelectView.style.display = DisplayStyle.Flex;
        }

        private void ShowMain()
        {
            _nameInputView.style.display = DisplayStyle.None;
            _mainView.style.display = DisplayStyle.Flex;
            _daySelectView.style.display = DisplayStyle.None;
            _creditsView.style.display = DisplayStyle.None;
        }

        private void BuildCreditsView(VisualElement container)
        {
            // Header row
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.width = 450;
            header.style.marginBottom = 24;

            var backBtn = new Button(() => ShowMain());
            backBtn.text = "\u2190  Kembali";
            backBtn.style.fontSize = 13;
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

            var titleLabel = new Label("CREDITS");
            titleLabel.style.fontSize = 16;
            titleLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 2;
            header.Add(titleLabel);

            container.Add(header);

            // Load credits content
            string creditsText = "";
            var asset = Resources.Load<TextAsset>("credits");
            if (asset != null)
                creditsText = asset.text;
            else
                creditsText = "Credits file not found.";

            // Parse markdown into styled labels
            var scrollView = new ScrollView();
            scrollView.style.width = 450;
            scrollView.style.maxHeight = 400;

            var lines = creditsText.Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line))
                {
                    var sp = new VisualElement();
                    sp.style.height = 8;
                    scrollView.Add(sp);
                    continue;
                }

                if (line.StartsWith("---"))
                {
                    var hr = new VisualElement();
                    hr.style.height = 1;
                    hr.style.backgroundColor = new Color(0.235f, 0.306f, 0.416f, 1f);
                    hr.style.marginTop = 8;
                    hr.style.marginBottom = 8;
                    hr.style.width = Length.Percent(100);
                    scrollView.Add(hr);
                    continue;
                }

                var lbl = new Label();
                lbl.style.whiteSpace = WhiteSpace.Normal;
                lbl.style.width = Length.Percent(100);

                if (line.StartsWith("# "))
                {
                    lbl.text = line.Substring(2).Trim();
                    lbl.style.fontSize = 18;
                    lbl.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
                    lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    lbl.style.marginBottom = 8;
                }
                else if (line.StartsWith("## "))
                {
                    lbl.text = line.Substring(3).Trim();
                    lbl.style.fontSize = 14;
                    lbl.style.color = new Color(0.3f, 0.85f, 0.95f, 1f);
                    lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    lbl.style.marginTop = 12;
                    lbl.style.marginBottom = 4;
                }
                else if (line.StartsWith("### "))
                {
                    lbl.text = line.Substring(4).Trim();
                    lbl.style.fontSize = 13;
                    lbl.style.color = new Color(0.95f, 0.7f, 0.2f, 1f);
                    lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    lbl.style.marginTop = 8;
                    lbl.style.marginBottom = 2;
                }
                else if (line.StartsWith("- "))
                {
                    lbl.text = "\u2022  " + line.Substring(2).Trim().Replace("**", "");
                    lbl.style.fontSize = 11;
                    lbl.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
                    lbl.style.marginLeft = 12;
                    lbl.style.marginBottom = 2;
                }
                else
                {
                    lbl.text = line.Replace("*", "");
                    lbl.style.fontSize = 11;
                    lbl.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
                    lbl.style.marginBottom = 2;
                }

                scrollView.Add(lbl);
            }

            container.Add(scrollView);
        }

        private void BuildDaySelectView(VisualElement container)
        {
            // Header row
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.width = 400;
            header.style.marginBottom = 24;

            var backBtn = new Button(() => ShowMain());
            backBtn.text = "\u2190  Kembali";
            backBtn.style.fontSize = 13;
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
            titleLabel.style.fontSize = 16;
            titleLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 2;
            header.Add(titleLabel);

            container.Add(header);

            // Day list
            if (_storyData?.days == null)
            {
                var noData = new Label("Data cerita belum tersedia.");
                noData.style.fontSize = 12;
                noData.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
                container.Add(noData);
                return;
            }

            var save = SaveManager.Load();

            var scrollView = new ScrollView();
            scrollView.style.width = 400;
            scrollView.style.maxHeight = 360;

            // Campaign mode: find the next day the player should play
            int nextPlayableDay = save.lastCompletedDay + 1;

            foreach (var day in _storyData.days)
            {
                bool completed = save.lastCompletedDay >= day.dayNumber;
                bool isNext = day.dayNumber == nextPlayableDay;
                int bestScore = save.GetBestScore(day.dayNumber);
                bool passed = bestScore >= day.scoreGoal;

                var dayRow = CreateDayRow(day, completed, isNext, bestScore, passed);
                scrollView.Add(dayRow);
            }

            container.Add(scrollView);

            // Story complete message if all days done
            bool allComplete = save.lastCompletedDay >= 7;
            if (allComplete)
            {
                var completeLabel = new Label("CERITA SELESAI");
                completeLabel.style.fontSize = 16;
                completeLabel.style.color = new Color(0.29f, 0.87f, 0.5f, 1f);
                completeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                completeLabel.style.letterSpacing = 2;
                completeLabel.style.marginTop = 16;
                completeLabel.style.marginBottom = 4;
                container.Add(completeLabel);

                var completeHint = new Label("Reset cerita untuk memulai dari awal dengan pilihan berbeda.");
                completeHint.style.fontSize = 10;
                completeHint.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
                completeHint.style.marginBottom = 8;
                completeHint.style.unityTextAlign = TextAnchor.MiddleCenter;
                container.Add(completeHint);
            }

            // Reset story button
            if (allComplete)
            {
                var resetBtn = new Button();
                resetBtn.text = "Reset Cerita";
                resetBtn.style.marginTop = 12;
                resetBtn.style.paddingTop = 8;
                resetBtn.style.paddingBottom = 8;
                resetBtn.style.paddingLeft = 24;
                resetBtn.style.paddingRight = 24;
                resetBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
                resetBtn.style.color = Color.white;
                resetBtn.style.fontSize = 12;
                resetBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                resetBtn.style.borderTopLeftRadius = 6;
                resetBtn.style.borderTopRightRadius = 6;
                resetBtn.style.borderBottomLeftRadius = 6;
                resetBtn.style.borderBottomRightRadius = 6;
                resetBtn.style.borderTopWidth = 0;
                resetBtn.style.borderBottomWidth = 0;
                resetBtn.style.borderLeftWidth = 0;
                resetBtn.style.borderRightWidth = 0;

                var confirmRow = new VisualElement();
                confirmRow.style.display = DisplayStyle.None;
                confirmRow.style.alignItems = Align.Center;
                confirmRow.style.marginTop = 8;

                resetBtn.clicked += () =>
                {
                    resetBtn.style.display = DisplayStyle.None;
                    confirmRow.style.display = DisplayStyle.Flex;
                };

                var confirmLabel = new Label("Yakin reset semua progress cerita?");
                confirmLabel.style.fontSize = 11;
                confirmLabel.style.color = new Color(0.973f, 0.443f, 0.443f, 1f);
                confirmLabel.style.marginBottom = 8;
                confirmRow.Add(confirmLabel);

                var btnGroup = new VisualElement();
                btnGroup.style.flexDirection = FlexDirection.Row;
                btnGroup.style.justifyContent = Justify.Center;

                var yesBtn = new Button();
                yesBtn.text = "Ya, Reset";
                yesBtn.style.paddingTop = 6;
                yesBtn.style.paddingBottom = 6;
                yesBtn.style.paddingLeft = 16;
                yesBtn.style.paddingRight = 16;
                yesBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
                yesBtn.style.color = Color.white;
                yesBtn.style.fontSize = 11;
                yesBtn.style.borderTopLeftRadius = 6;
                yesBtn.style.borderTopRightRadius = 6;
                yesBtn.style.borderBottomLeftRadius = 6;
                yesBtn.style.borderBottomRightRadius = 6;
                yesBtn.style.borderTopWidth = 0;
                yesBtn.style.borderBottomWidth = 0;
                yesBtn.style.borderLeftWidth = 0;
                yesBtn.style.borderRightWidth = 0;
                yesBtn.style.marginRight = 8;
                yesBtn.clicked += () =>
                {
                    var s = SaveManager.Load();
                    s.ResetStory();
                    SaveManager.Save(s);
                    ShowDaySelectPublic();
                };
                btnGroup.Add(yesBtn);

                var noBtn = new Button();
                noBtn.text = "Batal";
                noBtn.style.paddingTop = 6;
                noBtn.style.paddingBottom = 6;
                noBtn.style.paddingLeft = 16;
                noBtn.style.paddingRight = 16;
                noBtn.style.backgroundColor = new Color(0.235f, 0.306f, 0.416f, 1f);
                noBtn.style.color = Color.white;
                noBtn.style.fontSize = 11;
                noBtn.style.borderTopLeftRadius = 6;
                noBtn.style.borderTopRightRadius = 6;
                noBtn.style.borderBottomLeftRadius = 6;
                noBtn.style.borderBottomRightRadius = 6;
                noBtn.style.borderTopWidth = 0;
                noBtn.style.borderBottomWidth = 0;
                noBtn.style.borderLeftWidth = 0;
                noBtn.style.borderRightWidth = 0;
                noBtn.clicked += () =>
                {
                    confirmRow.style.display = DisplayStyle.None;
                    resetBtn.style.display = DisplayStyle.Flex;
                };
                btnGroup.Add(noBtn);

                confirmRow.Add(btnGroup);
                container.Add(resetBtn);
                container.Add(confirmRow);
            }

            // Achievements section — always visible
            var achieveTitle = new Label("ENDINGS");
            achieveTitle.style.fontSize = 12;
            achieveTitle.style.color = new Color(0.58f, 0.639f, 0.722f, 1f);
            achieveTitle.style.marginTop = 20;
            achieveTitle.style.marginBottom = 8;
            achieveTitle.style.letterSpacing = 2;
            container.Add(achieveTitle);

            var achieveRow = new VisualElement();
            achieveRow.style.flexDirection = FlexDirection.Row;
            achieveRow.style.justifyContent = Justify.Center;
            achieveRow.style.flexWrap = Wrap.Wrap;
            achieveRow.style.width = 400;

            AddEndingBadge(achieveRow, save, "survive", "Bertahan", new Color(0.376f, 0.647f, 0.98f, 1f));
            AddEndingBadge(achieveRow, save, "breakdown", "Breakdown", new Color(0.973f, 0.443f, 0.443f, 1f));
            AddEndingBadge(achieveRow, save, "resign", "Resign", new Color(0.973f, 0.682f, 0.275f, 1f));
            AddEndingBadge(achieveRow, save, "secret", "Kebenaran", new Color(0.29f, 0.87f, 0.5f, 1f));

            container.Add(achieveRow);
        }

        private VisualElement CreateDayRow(DayDefinition day, bool completed, bool isNext, int bestScore, bool passed)
        {
            bool playable = isNext;

            var row = new Button();
            row.text = "";
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 16;
            row.style.paddingRight = 16;
            row.style.marginBottom = 3;
            row.style.borderTopLeftRadius = 6;
            row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6;
            row.style.borderBottomRightRadius = 6;
            row.style.borderTopWidth = playable ? 1 : 0;
            row.style.borderBottomWidth = 0;
            row.style.borderLeftWidth = 0;
            row.style.borderRightWidth = 0;
            row.style.borderTopColor = new Color(0.063f, 0.725f, 0.506f, 1f);
            row.style.backgroundColor = playable
                ? new Color(0.118f, 0.18f, 0.22f, 1f)
                : completed
                    ? new Color(0.1f, 0.14f, 0.18f, 1f)
                    : new Color(0.08f, 0.11f, 0.15f, 1f);
            row.style.opacity = (playable || completed) ? 1f : 0.4f;

            // Day number badge
            var dayBadge = new Label($"{day.dayNumber}");
            dayBadge.style.fontSize = 14;
            dayBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            dayBadge.style.color = completed
                ? new Color(0.29f, 0.87f, 0.5f, 1f)
                : playable
                    ? new Color(0.063f, 0.725f, 0.506f, 1f)
                    : new Color(0.392f, 0.455f, 0.545f, 1f);
            dayBadge.style.width = 28;
            dayBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            dayBadge.style.marginRight = 12;
            row.Add(dayBadge);

            // Info column
            var info = new VisualElement();
            info.style.flexGrow = 1;

            var titleLabel = new Label(day.title);
            titleLabel.style.fontSize = 13;
            titleLabel.style.color = (playable || completed)
                ? new Color(0.945f, 0.96f, 0.976f, 1f)
                : new Color(0.392f, 0.455f, 0.545f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            info.Add(titleLabel);

            string statusText = completed
                ? $"Selesai  \u2022  Skor: {bestScore}"
                : playable
                    ? $"Target: {day.scoreGoal} skor  \u2022  {day.dayLengthSeconds}s"
                    : "Terkunci";
            var goalLabel = new Label(statusText);
            goalLabel.style.fontSize = 11;
            goalLabel.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
            goalLabel.style.marginTop = 2;
            info.Add(goalLabel);

            row.Add(info);

            // Right side indicator
            if (completed)
            {
                var checkLabel = new Label(passed ? "\u2713" : "\u2717");
                checkLabel.style.fontSize = 16;
                checkLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                checkLabel.style.color = passed
                    ? new Color(0.29f, 0.87f, 0.5f, 1f)
                    : new Color(0.973f, 0.443f, 0.443f, 1f);
                row.Add(checkLabel);
            }
            else if (!playable)
            {
                var lockIcon = new Label("\ue897");
                lockIcon.AddToClassList("sidebar-icon");
                lockIcon.style.fontSize = 16;
                lockIcon.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
                lockIcon.style.width = new StyleLength(StyleKeyword.Auto);
                row.Add(lockIcon);
            }
            else
            {
                var playIcon = new Label("\u25B6");
                playIcon.style.fontSize = 14;
                playIcon.style.color = new Color(0.063f, 0.725f, 0.506f, 1f);
                row.Add(playIcon);
            }

            if (playable)
            {
                int dayNum = day.dayNumber;
                row.RegisterCallback<ClickEvent>(_ => _onStoryDay?.Invoke(dayNum));
            }

            return row;
        }

        private static void StyleResetButton(Button btn, Color bg, Color textColor)
        {
            btn.style.paddingTop = 6;
            btn.style.paddingBottom = 6;
            btn.style.paddingLeft = 14;
            btn.style.paddingRight = 14;
            btn.style.fontSize = 11;
            btn.style.backgroundColor = bg;
            btn.style.color = textColor;
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = 5;
            btn.style.borderTopRightRadius = 5;
            btn.style.borderBottomLeftRadius = 5;
            btn.style.borderBottomRightRadius = 5;
        }

        private void AddEndingBadge(VisualElement parent, SaveData save, string endingId, string label, Color color)
        {
            bool unlocked = save.HasEnding(endingId);

            var badge = new VisualElement();
            badge.style.paddingTop = 6;
            badge.style.paddingBottom = 6;
            badge.style.paddingLeft = 12;
            badge.style.paddingRight = 12;
            badge.style.marginRight = 6;
            badge.style.marginBottom = 6;
            badge.style.borderTopLeftRadius = 4;
            badge.style.borderTopRightRadius = 4;
            badge.style.borderBottomLeftRadius = 4;
            badge.style.borderBottomRightRadius = 4;
            badge.style.backgroundColor = unlocked
                ? new Color(color.r, color.g, color.b, 0.15f)
                : new Color(0.08f, 0.11f, 0.15f, 1f);
            badge.style.borderTopWidth = 1;
            badge.style.borderBottomWidth = 1;
            badge.style.borderLeftWidth = 1;
            badge.style.borderRightWidth = 1;
            badge.style.borderTopColor = unlocked ? color : new Color(0.2f, 0.25f, 0.3f, 1f);
            badge.style.borderBottomColor = unlocked ? color : new Color(0.2f, 0.25f, 0.3f, 1f);
            badge.style.borderLeftColor = unlocked ? color : new Color(0.2f, 0.25f, 0.3f, 1f);
            badge.style.borderRightColor = unlocked ? color : new Color(0.2f, 0.25f, 0.3f, 1f);

            var text = new Label(unlocked ? label : "???");
            text.style.fontSize = 10;
            text.style.color = unlocked ? color : new Color(0.3f, 0.35f, 0.4f, 1f);
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.Add(text);

            badge.tooltip = unlocked ? label : "???";

            parent.Add(badge);
        }
    }
}
