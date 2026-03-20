using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.UI
{
    public class ReplyPanelController
    {
        private readonly VisualElement _root;
        private readonly VisualElement _optionsContainer;
        private readonly Label _prompt;
        private Action<EmailInstance, int> _onReplyChosen;
        private EmailInstance _currentEmail;

        public ReplyPanelController(VisualElement root, Action<EmailInstance, int> onReplyChosen)
        {
            _root = root;
            _onReplyChosen = onReplyChosen;
            _optionsContainer = root.Q("reply-options");
            _prompt = root.Q<Label>("reply-prompt");
        }

        public void ShowForEmail(EmailInstance email)
        {
            _currentEmail = email;
            _optionsContainer.Clear();

            if (_prompt != null)
                _prompt.text = "Choose your reply:";

            ReplyOption[] options = email.Definition.replyOptions;
            if (options == null) return;

            for (int i = 0; i < options.Length; i++)
            {
                int index = i; // Capture for closure
                var btn = new Button(() => OnOptionClicked(index));
                btn.text = options[i].text;
                btn.AddToClassList("reply-option-btn");
                _optionsContainer.Add(btn);
            }
        }

        private void OnOptionClicked(int index)
        {
            if (_currentEmail == null) return;
            _onReplyChosen?.Invoke(_currentEmail, index);
        }

        public void ShowFeedback(string text, bool isCorrect)
        {
            _optionsContainer.Clear();

            var feedback = new Label(text);
            feedback.AddToClassList("reply-feedback");
            feedback.AddToClassList(isCorrect ? "reply-feedback--correct" : "reply-feedback--wrong");
            _optionsContainer.Add(feedback);
        }
    }
}
