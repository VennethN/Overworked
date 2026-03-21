using System;
using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Story.Data;

namespace Overworked.UI
{
    public class DialogueController
    {
        private readonly VisualElement _root;
        private readonly Action _onComplete;
        private DialogueLine[] _lines;
        private int _currentIndex;

        private Label _speakerLabel;
        private Label _bodyLabel;
        private Label _avatarLabel;
        private VisualElement _avatarCircle;
        private Button _continueBtn;
        private Label _counterLabel;

        public DialogueController(VisualElement root, DialogueLine[] lines, Action onComplete)
        {
            _root = root;
            _lines = lines;
            _onComplete = onComplete;
            _currentIndex = 0;

            BuildUI();
            ShowCurrentLine();
        }

        private void BuildUI()
        {
            _root.Clear();

            // --- Full-screen dark backdrop ---
            var overlay = new VisualElement();
            overlay.AddToClassList("overlay");
            overlay.style.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 0.97f);

            // --- Monitor bezel (outer housing) ---
            var bezel = new VisualElement();
            bezel.style.flexGrow = 1;
            bezel.style.marginTop = 18;
            bezel.style.marginBottom = 14;
            bezel.style.marginLeft = 24;
            bezel.style.marginRight = 24;
            SetRadius(bezel, 10);
            bezel.style.backgroundColor = new Color(0.055f, 0.058f, 0.065f, 1f);
            SetBorder(bezel, 2, new Color(0.1f, 0.1f, 0.12f, 1f));

            // --- Screen area (inside the bezel) ---
            var screen = new VisualElement();
            screen.style.flexGrow = 1;
            screen.style.marginTop = 14;
            screen.style.marginBottom = 8;
            screen.style.marginLeft = 16;
            screen.style.marginRight = 16;
            SetRadius(screen, 4);
            screen.style.backgroundColor = new Color(0.025f, 0.048f, 0.058f, 1f);
            SetBorder(screen, 1, new Color(0.1f, 0.22f, 0.2f, 0.18f));

            // --- Center container ---
            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            container.style.flexGrow = 1;
            container.style.paddingLeft = 56;
            container.style.paddingRight = 56;

            // --- Dialogue window ---
            var box = new VisualElement();
            box.style.width = 420;
            SetRadius(box, 6);
            box.style.overflow = Overflow.Hidden;

            // Title bar (terminal window style)
            var titleBar = new VisualElement();
            titleBar.style.backgroundColor = new Color(0.1f, 0.13f, 0.2f, 1f);
            titleBar.style.paddingTop = 6;
            titleBar.style.paddingBottom = 6;
            titleBar.style.paddingLeft = 10;
            titleBar.style.paddingRight = 10;
            titleBar.style.flexDirection = FlexDirection.Row;
            titleBar.style.alignItems = Align.Center;

            // Window control dots
            var dots = new VisualElement();
            dots.style.flexDirection = FlexDirection.Row;
            dots.style.marginRight = 8;
            Color[] dotColors =
            {
                new Color(0.85f, 0.3f, 0.3f, 0.65f),
                new Color(0.85f, 0.65f, 0.2f, 0.65f),
                new Color(0.3f, 0.75f, 0.4f, 0.65f)
            };
            foreach (var c in dotColors)
            {
                var dot = new VisualElement();
                dot.style.width = 7;
                dot.style.height = 7;
                SetRadius(dot, 4);
                dot.style.backgroundColor = c;
                dot.style.marginRight = 4;
                dots.Add(dot);
            }
            titleBar.Add(dots);

            var titleLabel = new Label("pesan-masuk");
            titleLabel.style.fontSize = 8;
            titleLabel.style.color = new Color(0.45f, 0.52f, 0.62f, 0.8f);
            titleBar.Add(titleLabel);

            box.Add(titleBar);

            // Content panel
            var content = new VisualElement();
            content.style.backgroundColor = new Color(0.09f, 0.12f, 0.16f, 1f);
            content.style.paddingTop = 22;
            content.style.paddingBottom = 16;
            content.style.paddingLeft = 24;
            content.style.paddingRight = 24;

            // Avatar + speaker row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 14;

            _avatarCircle = new VisualElement();
            _avatarCircle.style.width = 34;
            _avatarCircle.style.height = 34;
            SetRadius(_avatarCircle, 17);
            _avatarCircle.style.backgroundColor = new Color(0.376f, 0.51f, 0.965f, 0.2f);
            _avatarCircle.style.alignItems = Align.Center;
            _avatarCircle.style.justifyContent = Justify.Center;
            _avatarCircle.style.marginRight = 12;

            _avatarLabel = new Label();
            _avatarLabel.style.fontSize = 14;
            _avatarLabel.style.color = new Color(0.376f, 0.51f, 0.965f, 1f);
            _avatarLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _avatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _avatarCircle.Add(_avatarLabel);
            headerRow.Add(_avatarCircle);

            _speakerLabel = new Label();
            _speakerLabel.style.fontSize = 13;
            _speakerLabel.style.color = new Color(0.945f, 0.96f, 0.976f, 1f);
            _speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(_speakerLabel);

            content.Add(headerRow);

            // Body text
            _bodyLabel = new Label();
            _bodyLabel.style.fontSize = 11;
            _bodyLabel.style.color = new Color(0.722f, 0.757f, 0.808f, 1f);
            _bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            _bodyLabel.style.marginBottom = 20;
            content.Add(_bodyLabel);

            // Footer row: counter + continue button
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Center;
            footer.style.justifyContent = Justify.SpaceBetween;

            _counterLabel = new Label();
            _counterLabel.style.fontSize = 9;
            _counterLabel.style.color = new Color(0.392f, 0.455f, 0.545f, 1f);
            footer.Add(_counterLabel);

            _continueBtn = new Button(() => Advance());
            _continueBtn.text = "Lanjut \u25B8";
            _continueBtn.style.paddingTop = 7;
            _continueBtn.style.paddingBottom = 7;
            _continueBtn.style.paddingLeft = 20;
            _continueBtn.style.paddingRight = 20;
            _continueBtn.style.fontSize = 10;
            _continueBtn.style.backgroundColor = new Color(0.376f, 0.51f, 0.965f, 1f);
            _continueBtn.style.color = Color.white;
            SetBorder(_continueBtn, 0, Color.clear);
            SetRadius(_continueBtn, 6);
            footer.Add(_continueBtn);

            content.Add(footer);
            box.Add(content);
            container.Add(box);
            screen.Add(container);

            // --- Status bar at bottom of screen ---
            var statusBar = new VisualElement();
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.justifyContent = Justify.SpaceBetween;
            statusBar.style.paddingLeft = 12;
            statusBar.style.paddingRight = 12;
            statusBar.style.paddingBottom = 6;
            statusBar.style.paddingTop = 4;
            SetBorder(statusBar, 0, Color.clear);
            statusBar.style.borderTopWidth = 1;
            statusBar.style.borderTopColor = new Color(0.1f, 0.18f, 0.16f, 0.12f);

            var statusLeft = new Label("Terminal v2.1");
            statusLeft.style.fontSize = 7;
            statusLeft.style.color = new Color(0.2f, 0.32f, 0.28f, 0.45f);
            statusBar.Add(statusLeft);

            var statusRight = new Label("CONNECTED");
            statusRight.style.fontSize = 7;
            statusRight.style.color = new Color(0.18f, 0.45f, 0.3f, 0.45f);
            statusBar.Add(statusRight);

            screen.Add(statusBar);
            bezel.Add(screen);

            // --- Power LED on bezel ---
            var ledRow = new VisualElement();
            ledRow.style.alignItems = Align.Center;
            ledRow.style.paddingBottom = 6;
            ledRow.style.paddingTop = 2;

            var led = new VisualElement();
            led.style.width = 6;
            led.style.height = 6;
            SetRadius(led, 3);
            led.style.backgroundColor = new Color(0.2f, 0.7f, 0.3f, 0.55f);
            ledRow.Add(led);

            bezel.Add(ledRow);
            overlay.Add(bezel);
            _root.Add(overlay);

            overlay.schedule.Execute(() => overlay.AddToClassList("overlay--visible"));
        }

        private void ShowCurrentLine()
        {
            if (_lines == null || _currentIndex >= _lines.Length) return;

            var line = _lines[_currentIndex];

            var save = Overworked.Core.SaveManager.Load();
            string pName = string.IsNullOrEmpty(save.playerName) ? "Pegawai Baru" : save.playerName;
            string speakerName = line.speaker == "Kamu" ? pName : line.speaker;

            _speakerLabel.text = speakerName;
            _bodyLabel.text = line.text.Replace("{PlayerName}", pName);
            _counterLabel.text = $"{_currentIndex + 1} / {_lines.Length}";

            // Avatar letter from speaker name
            if (!string.IsNullOrEmpty(speakerName))
                _avatarLabel.text = speakerName[0].ToString().ToUpper();

            // Different color for player
            bool isPlayer = line.avatar == "player";
            var accentColor = isPlayer
                ? new Color(0.063f, 0.725f, 0.506f, 1f)
                : new Color(0.376f, 0.51f, 0.965f, 1f);

            _avatarLabel.style.color = accentColor;
            _avatarCircle.style.backgroundColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.2f);

            // Last line shows different button text
            _continueBtn.text = _currentIndex >= _lines.Length - 1 ? "Mulai \u25B8" : "Lanjut \u25B8";
        }

        private void Advance()
        {
            _currentIndex++;
            if (_currentIndex >= _lines.Length)
            {
                _onComplete?.Invoke();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        private static void SetRadius(VisualElement el, float r)
        {
            el.style.borderTopLeftRadius = r;
            el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r;
            el.style.borderBottomRightRadius = r;
        }

        private static void SetBorder(VisualElement el, float w, Color c)
        {
            el.style.borderTopWidth = w;
            el.style.borderBottomWidth = w;
            el.style.borderLeftWidth = w;
            el.style.borderRightWidth = w;
            el.style.borderTopColor = c;
            el.style.borderBottomColor = c;
            el.style.borderLeftColor = c;
            el.style.borderRightColor = c;
        }
    }
}
