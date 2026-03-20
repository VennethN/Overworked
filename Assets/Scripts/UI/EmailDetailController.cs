using System;
using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.UI
{
    public class EmailDetailController
    {
        private readonly VisualElement _root;
        private readonly Label _sender;
        private readonly Label _address;
        private readonly Label _subject;
        private readonly Label _body;
        private readonly ProgressBar _expiryBar;
        private readonly Button _backBtn;
        private readonly Button _replyBtn;
        private readonly Button _taskBtn;
        private readonly Button _deleteBtn;
        private readonly VisualElement _inlineReplySlot;
        private readonly Label _avatarLetter;
        private readonly VisualElement _statusBanner;
        private readonly Label _statusText;

        private EmailInstance _currentEmail;
        private Action<EmailInstance, int> _onReplyChosen;

        public EmailDetailController(
            VisualElement root,
            Action onBack,
            Action onReply,
            Action onTask,
            Action onDelete)
        {
            _root = root;
            _sender = root.Q<Label>("detail-sender");
            _address = root.Q<Label>("detail-address");
            _subject = root.Q<Label>("detail-subject");
            _body = root.Q<Label>("detail-body");
            _expiryBar = root.Q<ProgressBar>("expiry-bar");
            _backBtn = root.Q<Button>("back-btn");
            _replyBtn = root.Q<Button>("reply-btn");
            _taskBtn = root.Q<Button>("task-btn");
            _deleteBtn = root.Q<Button>("delete-btn");
            _inlineReplySlot = root.Q("inline-reply-slot");
            _avatarLetter = root.Q<Label>("detail-avatar-letter");
            _statusBanner = root.Q("detail-status-banner");
            _statusText = root.Q<Label>("detail-status-text");

            _backBtn?.RegisterCallback<ClickEvent>(_ => onBack?.Invoke());
            _replyBtn?.RegisterCallback<ClickEvent>(_ => ToggleInlineReply());
            _taskBtn?.RegisterCallback<ClickEvent>(_ => onTask?.Invoke());
            _deleteBtn?.RegisterCallback<ClickEvent>(_ => onDelete?.Invoke());
        }

        public void SetReplyCallback(Action<EmailInstance, int> onReplyChosen)
        {
            _onReplyChosen = onReplyChosen;
        }

        public EmailInstance CurrentEmail => _currentEmail;

        public void ShowEmail(EmailInstance email)
        {
            _currentEmail = email;

            if (_sender != null) _sender.text = email.Definition.sender;
            if (_address != null) _address.text = $"<{email.Definition.senderAddress}>";
            if (_subject != null) _subject.text = email.Definition.subject;
            if (_body != null) _body.text = email.Definition.body;
            if (_avatarLetter != null && !string.IsNullOrEmpty(email.Definition.sender))
                _avatarLetter.text = email.Definition.sender[0].ToString().ToUpper();

            // Show/hide action buttons based on type
            bool isReply = email.Definition.parsedType == EmailType.Reply || email.Definition.parsedType == EmailType.Spam;
            bool isTask = email.Definition.parsedType == EmailType.Task;

            if (_replyBtn != null)
                _replyBtn.style.display = isReply ? DisplayStyle.Flex : DisplayStyle.None;
            if (_taskBtn != null)
                _taskBtn.style.display = isTask ? DisplayStyle.Flex : DisplayStyle.None;

            // Disable actions if already acted upon
            bool canAct = !email.IsActedUpon && !email.IsExpired && !email.IsCompleted;
            if (_replyBtn != null) _replyBtn.SetEnabled(canAct);
            if (_taskBtn != null) _taskBtn.SetEnabled(canAct);

            HideInlineReply(immediate: true);
            UpdateExpiryBar();
            UpdateStatusBanner();
        }

        private void UpdateStatusBanner()
        {
            if (_statusBanner == null) return;

            bool isDone = _currentEmail.IsCompleted || _currentEmail.IsActedUpon;

            _statusBanner.RemoveFromClassList("detail-status-banner--done");
            _statusBanner.RemoveFromClassList("detail-status-banner--expired");

            if (isDone)
            {
                _statusBanner.style.display = DisplayStyle.Flex;
                _statusBanner.AddToClassList("detail-status-banner--done");
                if (_statusText != null) _statusText.text = "\u2713  Email ini sudah selesai — aman untuk dihapus";
            }
            else if (_currentEmail.IsExpired)
            {
                _statusBanner.style.display = DisplayStyle.Flex;
                _statusBanner.AddToClassList("detail-status-banner--expired");
                if (_statusText != null) _statusText.text = "\u2717  Email ini sudah expired";
            }
            else
            {
                _statusBanner.style.display = DisplayStyle.None;
            }
        }

        private void ToggleInlineReply()
        {
            if (_inlineReplySlot == null || _currentEmail == null) return;

            bool isVisible = _inlineReplySlot.ClassListContains("inline-reply-slot--visible");
            if (isVisible)
            {
                HideInlineReply();
                return;
            }

            _inlineReplySlot.Clear();
            _inlineReplySlot.style.display = DisplayStyle.Flex;
            _inlineReplySlot.RemoveFromClassList("inline-reply-slot--visible");

            var prompt = new Label("Pilih balasan:");
            prompt.AddToClassList("reply-prompt");
            _inlineReplySlot.Add(prompt);

            var options = _currentEmail.Definition.replyOptions;
            if (options == null) return;

            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var btn = new Button(() => _onReplyChosen?.Invoke(_currentEmail, index));
                btn.text = options[i].text;
                btn.AddToClassList("reply-option-btn");
                _inlineReplySlot.Add(btn);
            }

            // Trigger fade-in on next frame
            _inlineReplySlot.schedule.Execute(() =>
                _inlineReplySlot.AddToClassList("inline-reply-slot--visible"));
        }

        public void HideInlineReply(bool immediate = false)
        {
            if (_inlineReplySlot == null) return;

            if (immediate)
            {
                _inlineReplySlot.RemoveFromClassList("inline-reply-slot--visible");
                _inlineReplySlot.Clear();
                _inlineReplySlot.style.display = DisplayStyle.None;
                return;
            }

            _inlineReplySlot.RemoveFromClassList("inline-reply-slot--visible");
            _inlineReplySlot.schedule.Execute(() =>
            {
                _inlineReplySlot.Clear();
                _inlineReplySlot.style.display = DisplayStyle.None;
            }).ExecuteLater(200);
        }

        public void UpdateExpiryBar()
        {
            if (_currentEmail == null || _expiryBar == null) return;

            if (!_currentEmail.CanExpire || _currentEmail.IsExpired)
            {
                _expiryBar.style.display = DisplayStyle.None;
                return;
            }

            _expiryBar.style.display = DisplayStyle.Flex;
            float ratio = _currentEmail.TimeRemaining / _currentEmail.Definition.expirationSeconds;
            _expiryBar.value = ratio * 100f;
        }
    }
}
