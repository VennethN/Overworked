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

            var overlay = new VisualElement();
            overlay.AddToClassList("overlay");
            overlay.style.backgroundColor = new Color(0.043f, 0.059f, 0.094f, 0.95f);

            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            container.style.flexGrow = 1;
            container.style.paddingLeft = 56;
            container.style.paddingRight = 56;

            // Dialogue box
            var box = new VisualElement();
            box.style.width = 420;
            box.style.backgroundColor = new Color(0.118f, 0.161f, 0.212f, 1f);
            box.style.borderTopLeftRadius = 12;
            box.style.borderTopRightRadius = 12;
            box.style.borderBottomLeftRadius = 12;
            box.style.borderBottomRightRadius = 12;
            box.style.paddingTop = 22;
            box.style.paddingBottom = 16;
            box.style.paddingLeft = 24;
            box.style.paddingRight = 24;
            box.style.borderTopWidth = 1;
            box.style.borderTopColor = new Color(0.376f, 0.51f, 0.965f, 0.3f);
            box.style.borderBottomWidth = 0;
            box.style.borderLeftWidth = 0;
            box.style.borderRightWidth = 0;

            // Avatar + speaker row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 14;

            _avatarCircle = new VisualElement();
            _avatarCircle.style.width = 34;
            _avatarCircle.style.height = 34;
            _avatarCircle.style.borderTopLeftRadius = 17;
            _avatarCircle.style.borderTopRightRadius = 17;
            _avatarCircle.style.borderBottomLeftRadius = 17;
            _avatarCircle.style.borderBottomRightRadius = 17;
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

            box.Add(headerRow);

            // Body text
            _bodyLabel = new Label();
            _bodyLabel.style.fontSize = 11;
            _bodyLabel.style.color = new Color(0.722f, 0.757f, 0.808f, 1f);
            _bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            _bodyLabel.style.marginBottom = 20;
            box.Add(_bodyLabel);

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
            _continueBtn.style.borderTopWidth = 0;
            _continueBtn.style.borderBottomWidth = 0;
            _continueBtn.style.borderLeftWidth = 0;
            _continueBtn.style.borderRightWidth = 0;
            _continueBtn.style.borderTopLeftRadius = 6;
            _continueBtn.style.borderTopRightRadius = 6;
            _continueBtn.style.borderBottomLeftRadius = 6;
            _continueBtn.style.borderBottomRightRadius = 6;
            footer.Add(_continueBtn);

            box.Add(footer);
            container.Add(box);
            overlay.Add(container);
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
    }
}
