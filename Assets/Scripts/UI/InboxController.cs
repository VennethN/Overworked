using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.UI
{
    public class InboxController
    {
        private readonly VisualElement _root;
        private readonly ScrollView _emailList;
        private readonly Label _emailCount;
        private readonly VisualTreeAsset _itemTemplate;
        private readonly Action<EmailInstance> _onEmailClicked;
        private readonly Dictionary<string, VisualElement> _itemElements = new();

        private string _activeTab = "all";
        private string _searchQuery = "";
        private IReadOnlyList<EmailInstance> _lastInbox;

        private static readonly string[] TAB_IDS = { "all", "utama", "pekerjaan", "sosial", "promosi" };
        private readonly Dictionary<string, Button> _tabButtons = new();

        private readonly VisualElement _sidebar;
        private readonly TextField _searchField;

        // Filler: divider lines that fill empty space below emails
        private VisualElement _fillerContainer;
        private Label _fillerLabel;
        private const int FILLER_DIVIDER_COUNT = 30;

        private readonly Dictionary<string, Label> _tabTextLabels = new();

        // Material Icons codepoints
        private static readonly Dictionary<string, string> TAB_ICONS = new()
        {
            { "all", "\ue156" },      // inbox
            { "utama", "\ue838" },    // star
            { "pekerjaan", "\ue8f9" }, // work
            { "sosial", "\ue7ef" },   // group
            { "promosi", "\ue54e" },  // local_offer (tag)
        };

        public InboxController(VisualElement root, VisualElement sidebar, VisualTreeAsset itemTemplate, Action<EmailInstance> onEmailClicked)
        {
            _root = root;
            _sidebar = sidebar;
            _itemTemplate = itemTemplate;
            _onEmailClicked = onEmailClicked;
            _emailList = root.Q<ScrollView>("email-list");
            _emailCount = sidebar?.Q<Label>("email-count");

            _searchField = sidebar?.Q<TextField>("search-field");
            if (_searchField != null)
            {
                _searchField.value = "";
                _searchField.textEdition.placeholder = "Cari email...";

                _searchField.RegisterValueChangedCallback(evt =>
                {
                    _searchQuery = evt.newValue?.Trim().ToLower() ?? "";
                    if (_lastInbox != null)
                        Refresh(_lastInbox);
                });
            }

            SetupTabs();
            CreateFiller();
        }

        private void CreateFiller()
        {
            _fillerContainer = new VisualElement();
            _fillerContainer.AddToClassList("inbox-filler");

            for (int i = 0; i < 5; i++)
            {
                var div = new VisualElement();
                div.AddToClassList("inbox-divider");
                _fillerContainer.Add(div);
            }

            _fillerLabel = new Label("Menunggu email masuk...");
            _fillerLabel.AddToClassList("inbox-filler-label");
            _fillerContainer.Add(_fillerLabel);

            for (int i = 0; i < FILLER_DIVIDER_COUNT - 5; i++)
            {
                var div = new VisualElement();
                div.AddToClassList("inbox-divider");
                _fillerContainer.Add(div);
            }

            _emailList.Add(_fillerContainer);
        }

        private void SetupTabs()
        {
            if (_sidebar == null) return;
            foreach (string tabId in TAB_IDS)
            {
                var btn = _sidebar.Q<Button>($"tab-{tabId}");
                if (btn == null) continue;

                _tabButtons[tabId] = btn;

                // Clear button text and add icon + text label children
                btn.text = "";

                var icon = new Label();
                icon.AddToClassList("sidebar-icon");
                icon.text = TAB_ICONS.GetValueOrDefault(tabId, "");
                btn.Add(icon);

                string baseName = tabId switch
                {
                    "all" => "Semua",
                    "utama" => "Utama",
                    "pekerjaan" => "Pekerjaan",
                    "sosial" => "Sosial",
                    "promosi" => "Promosi",
                    _ => tabId
                };
                var textLabel = new Label(baseName);
                textLabel.AddToClassList("sidebar-text");
                btn.Add(textLabel);
                _tabTextLabels[tabId] = textLabel;

                string capturedId = tabId;
                btn.RegisterCallback<ClickEvent>(_ => SelectTab(capturedId));
            }
        }

        private void SelectTab(string tabId)
        {
            _activeTab = tabId;

            foreach (var kvp in _tabButtons)
            {
                kvp.Value.EnableInClassList("sidebar-item--active", kvp.Key == tabId);
            }

            if (_lastInbox != null)
                Refresh(_lastInbox);
        }

        private bool EmailMatchesTab(EmailInstance email)
        {
            if (_activeTab == "all") return true;
            return email.Definition.parsedCategory.ToString().ToLower() == _activeTab;
        }

        private bool EmailMatchesSearch(EmailInstance email)
        {
            if (string.IsNullOrEmpty(_searchQuery)) return true;

            string sender = email.Definition.sender?.ToLower() ?? "";
            string subject = email.Definition.subject?.ToLower() ?? "";
            string body = email.Definition.body?.ToLower() ?? "";

            return sender.Contains(_searchQuery)
                || subject.Contains(_searchQuery)
                || body.Contains(_searchQuery);
        }

        public void Refresh(IReadOnlyList<EmailInstance> inbox)
        {
            _lastInbox = inbox;

            // Build filtered list based on active tab + search
            var filtered = new List<EmailInstance>();
            for (int i = 0; i < inbox.Count; i++)
            {
                if (EmailMatchesTab(inbox[i]) && EmailMatchesSearch(inbox[i]))
                    filtered.Add(inbox[i]);
            }

            // Remove elements not in filtered list
            var toRemove = new List<string>();
            foreach (var kvp in _itemElements)
            {
                bool found = false;
                for (int i = 0; i < filtered.Count; i++)
                {
                    if (filtered[i].InstanceId == kvp.Key) { found = true; break; }
                }
                if (!found) toRemove.Add(kvp.Key);
            }
            foreach (string id in toRemove)
            {
                _emailList.Remove(_itemElements[id]);
                _itemElements.Remove(id);
            }

            // Add new elements and update existing
            for (int i = 0; i < filtered.Count; i++)
            {
                EmailInstance email = filtered[i];
                if (!_itemElements.ContainsKey(email.InstanceId))
                {
                    VisualElement item = CreateEmailItem(email);
                    _emailList.Add(item);
                    _itemElements[email.InstanceId] = item;

                    // Slide-in animation for new emails
                    UIEffects.SlideIn(item, 30f, 200);
                }
                else
                {
                    UpdateEmailItem(_itemElements[email.InstanceId], email);
                }
            }

            // Ensure filler is always the last child
            _fillerContainer.RemoveFromHierarchy();
            _emailList.Add(_fillerContainer);

            // Show label only when no filtered emails
            if (_fillerLabel != null)
            {
                if (filtered.Count > 0)
                {
                    _fillerLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    _fillerLabel.style.display = DisplayStyle.Flex;
                    if (!string.IsNullOrEmpty(_searchQuery))
                        _fillerLabel.text = "Tidak ada email yang cocok dengan pencarian";
                    else if (inbox.Count > 0)
                        _fillerLabel.text = "Tidak ada email di kategori ini";
                    else
                        _fillerLabel.text = "Menunggu email masuk...";
                }
            }

            if (_emailCount != null)
                _emailCount.text = $"{filtered.Count} email{(filtered.Count != 1 ? "s" : "")}";
            UpdateTabCounts(inbox);
        }

        private void UpdateTabCounts(IReadOnlyList<EmailInstance> inbox)
        {
            var counts = new Dictionary<string, int>();
            foreach (string tabId in TAB_IDS)
                counts[tabId] = 0;

            for (int i = 0; i < inbox.Count; i++)
            {
                EmailInstance email = inbox[i];
                if (email.IsCompleted || email.IsActedUpon || email.IsExpired) continue;
                string cat = email.Definition.parsedCategory.ToString().ToLower();
                if (counts.ContainsKey(cat))
                    counts[cat]++;
                counts["all"]++;
            }

            foreach (var kvp in _tabButtons)
            {
                int count = counts.GetValueOrDefault(kvp.Key, 0);
                if (!_tabTextLabels.TryGetValue(kvp.Key, out Label textLabel)) continue;

                string baseName = kvp.Key switch
                {
                    "all" => "Semua",
                    "utama" => "Utama",
                    "pekerjaan" => "Pekerjaan",
                    "sosial" => "Sosial",
                    "promosi" => "Promosi",
                    _ => kvp.Key
                };
                textLabel.text = count > 0 ? $"{baseName} ({count})" : baseName;
            }
        }

        public void UpdateTimers(IReadOnlyList<EmailInstance> inbox)
        {
            for (int i = 0; i < inbox.Count; i++)
            {
                EmailInstance email = inbox[i];
                if (_itemElements.TryGetValue(email.InstanceId, out VisualElement element))
                {
                    UpdateTimerLabel(element, email);
                    UpdateItemState(element, email);
                }
            }
        }

        private VisualElement CreateEmailItem(EmailInstance email)
        {
            VisualElement item;
            if (_itemTemplate != null)
            {
                item = _itemTemplate.Instantiate();
                // The template wraps content in a TemplateContainer, get the actual item
                item = item.Q("email-item") ?? item;
            }
            else
            {
                item = CreateEmailItemFallback();
            }

            UpdateEmailItem(item, email);

            item.RegisterCallback<ClickEvent>(_ => _onEmailClicked?.Invoke(email));

            return item;
        }

        private VisualElement CreateEmailItemFallback()
        {
            var item = new VisualElement { name = "email-item" };
            item.AddToClassList("email-item");

            var statusIcon = new Label { name = "status-icon" };
            statusIcon.AddToClassList("status-icon");
            statusIcon.AddToClassList("status-icon--pending");
            item.Add(statusIcon);

            var priorityDot = new VisualElement { name = "priority-dot" };
            priorityDot.AddToClassList("priority-indicator");
            item.Add(priorityDot);

            var content = new VisualElement();
            content.AddToClassList("email-item-content");

            var sender = new Label { name = "sender" };
            sender.AddToClassList("email-sender");
            content.Add(sender);

            var subject = new Label { name = "subject" };
            subject.AddToClassList("email-subject");
            content.Add(subject);

            item.Add(content);

            var meta = new VisualElement();
            meta.AddToClassList("email-item-meta");

            var stateBadge = new Label { name = "state-badge" };
            stateBadge.style.display = DisplayStyle.None;
            meta.Add(stateBadge);

            var timer = new Label { name = "timer" };
            timer.AddToClassList("email-timer");
            meta.Add(timer);

            var badge = new Label { name = "type-badge" };
            badge.AddToClassList("type-badge");
            meta.Add(badge);

            item.Add(meta);

            return item;
        }

        private void UpdateEmailItem(VisualElement element, EmailInstance email)
        {
            var sender = element.Q<Label>("sender");
            var subject = element.Q<Label>("subject");
            var badge = element.Q<Label>("type-badge");
            var priorityDot = element.Q("priority-dot");

            if (sender != null) sender.text = email.Definition.sender;
            if (subject != null) subject.text = email.Definition.subject;

            // Type badge
            if (badge != null)
            {
                badge.text = email.Definition.parsedType.ToString().ToUpper();
                badge.RemoveFromClassList("type-badge--reply");
                badge.RemoveFromClassList("type-badge--task");
                badge.RemoveFromClassList("type-badge--spam");
                badge.RemoveFromClassList("type-badge--info");
                badge.AddToClassList($"type-badge--{email.Definition.type}");
            }

            // Priority dot
            if (priorityDot != null)
            {
                SetPriorityClass(priorityDot, email.Definition.parsedPriority);
            }

            UpdateTimerLabel(element, email);
            UpdateItemState(element, email);
        }

        private void UpdateTimerLabel(VisualElement element, EmailInstance email)
        {
            var timer = element.Q<Label>("timer");
            if (timer == null) return;

            if (!email.CanExpire || email.IsExpired || email.IsCompleted || email.IsActedUpon)
            {
                timer.text = "";
                return;
            }

            int seconds = Mathf.CeilToInt(email.TimeRemaining);
            timer.text = $"{seconds}s";

            timer.RemoveFromClassList("email-timer--urgent");
            if (seconds <= 10)
                timer.AddToClassList("email-timer--urgent");
        }

        private void UpdateItemState(VisualElement element, EmailInstance email)
        {
            bool isDone = email.IsCompleted || email.IsActedUpon;
            element.EnableInClassList("email-item--unread", !email.IsRead && !isDone && !email.IsExpired);
            element.EnableInClassList("email-item--expired", email.IsExpired && !isDone);
            element.EnableInClassList("email-item--done", isDone);

            var statusIcon = element.Q<Label>("status-icon");
            if (statusIcon != null)
            {
                statusIcon.RemoveFromClassList("status-icon--done");
                statusIcon.RemoveFromClassList("status-icon--expired");
                statusIcon.RemoveFromClassList("status-icon--pending");

                if (isDone)
                {
                    statusIcon.text = "\u2713";
                    statusIcon.AddToClassList("status-icon--done");
                }
                else if (email.IsExpired)
                {
                    statusIcon.text = "\u2717";
                    statusIcon.AddToClassList("status-icon--expired");
                }
                else
                {
                    statusIcon.text = "";
                    statusIcon.AddToClassList("status-icon--pending");
                }
            }

            var stateBadge = element.Q<Label>("state-badge");
            if (stateBadge != null)
            {
                stateBadge.RemoveFromClassList("done-badge");
                stateBadge.RemoveFromClassList("expired-badge");

                if (isDone)
                {
                    stateBadge.text = "SELESAI";
                    stateBadge.AddToClassList("done-badge");
                    stateBadge.style.display = DisplayStyle.Flex;
                }
                else if (email.IsExpired)
                {
                    stateBadge.text = "EXPIRED";
                    stateBadge.AddToClassList("expired-badge");
                    stateBadge.style.display = DisplayStyle.Flex;
                }
                else
                {
                    stateBadge.style.display = DisplayStyle.None;
                }
            }
        }

        private void SetPriorityClass(VisualElement dot, EmailPriority priority)
        {
            dot.RemoveFromClassList("priority-none");
            dot.RemoveFromClassList("priority-low");
            dot.RemoveFromClassList("priority-medium");
            dot.RemoveFromClassList("priority-high");
            dot.RemoveFromClassList("priority-critical");

            string className = priority switch
            {
                EmailPriority.Low => "priority-low",
                EmailPriority.Medium => "priority-medium",
                EmailPriority.High => "priority-high",
                EmailPriority.Critical => "priority-critical",
                _ => "priority-none"
            };
            dot.AddToClassList(className);
        }
    }
}
