using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Actions;
using Overworked.Core;
using Overworked.Email;
using Overworked.Scoring;

namespace Overworked.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset emailListItemTemplate;
        [SerializeField] private VisualTreeAsset inboxPanelTemplate;
        [SerializeField] private VisualTreeAsset detailPanelTemplate;
        [SerializeField] private VisualTreeAsset hudTemplate;

        private VisualElement _inboxSlot;
        private VisualElement _detailSlot;
        private VisualElement _hudSlot;
        private VisualElement _gameoverSlot;
        private VisualElement _sidebar;

        private InboxController _inbox;
        private EmailDetailController _detail;
        private HUDController _hud;

        private VisualElement _uiRoot;
        private bool _isLightMode;

        private float _timerUpdateAccumulator;
        private const float TIMER_UPDATE_INTERVAL = 0.25f;


        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            _uiRoot = root.Q("root");

            _inboxSlot = root.Q("inbox-slot");
            _detailSlot = root.Q("detail-slot");
            _hudSlot = root.Q("hud-slot");
            _gameoverSlot = root.Q("gameover-slot");
            _sidebar = root.Q("sidebar");

            // Instantiate templates into slots
            if (inboxPanelTemplate != null)
                _inboxSlot.Add(inboxPanelTemplate.Instantiate());

            if (detailPanelTemplate != null)
                _detailSlot.Add(detailPanelTemplate.Instantiate());

            if (hudTemplate != null)
                _hudSlot.Add(hudTemplate.Instantiate());

            // Create controllers
            _inbox = new InboxController(_inboxSlot, _sidebar, emailListItemTemplate, OnEmailClicked);
            _detail = new EmailDetailController(_detailSlot, OnBackClicked, OnReplyClicked, OnTaskClicked, OnDeleteClicked);
            _detail.SetReplyCallback(OnReplyChosen);
            _hud = new HUDController(_hudSlot);

            // Theme toggle
            _hud.OnThemeToggleClicked += ToggleTheme;
            _hud.UpdateThemeButtonLabel(_isLightMode);

            // Subscribe to events
            GameEvents.OnEmailReceived += _ => RefreshInbox();
            GameEvents.OnEmailExpired += _ => RefreshInbox();
            GameEvents.OnEmailDeleted += _ => RefreshInbox();
            GameEvents.OnEmailReplied += (_, _) => RefreshHUD();
            GameEvents.OnTaskCompleted += _ => RefreshHUD();
            GameEvents.OnTaskFailed += _ => RefreshHUD();

            ShowInbox();
        }

        private void Update()
        {
            if (EmailManager.Instance == null) return;

            _timerUpdateAccumulator += Time.deltaTime;
            if (_timerUpdateAccumulator >= TIMER_UPDATE_INTERVAL)
            {
                _timerUpdateAccumulator = 0f;
                _inbox?.UpdateTimers(EmailManager.Instance.Inbox);
                _detail?.UpdateExpiryBar();
            }

            // Update HUD timer
            if (GameManager.Instance != null)
                _hud?.UpdateTimer(GameManager.Instance.TimeRemaining);

            _hud?.UpdateEmailCount(EmailManager.Instance.ActiveCount);
        }

        public void ShowInbox()
        {
            _inboxSlot.style.display = DisplayStyle.Flex;
            _detailSlot.style.display = DisplayStyle.None;
            RefreshInbox();
        }

        public void ShowEmailDetail(EmailInstance email)
        {
            _inboxSlot.style.display = DisplayStyle.None;
            _detailSlot.style.display = DisplayStyle.Flex;
            _detail.ShowEmail(email);

            // Mark as read
            EmailManager.Instance?.OpenEmail(email.InstanceId);
        }

        public void ShowGameOver(ScoreData score)
        {
            _gameoverSlot.Clear();
            _gameoverSlot.style.display = DisplayStyle.Flex;

            var overlay = new VisualElement();
            overlay.AddToClassList("overlay");

            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.086f, 0.13f, 0.24f, 1f);
            container.style.borderTopLeftRadius = 12;
            container.style.borderTopRightRadius = 12;
            container.style.borderBottomLeftRadius = 12;
            container.style.borderBottomRightRadius = 12;
            container.style.paddingTop = 32;
            container.style.paddingBottom = 32;
            container.style.paddingLeft = 40;
            container.style.paddingRight = 40;
            container.style.alignItems = Align.Center;

            var title = new Label("GAME OVER");
            title.style.fontSize = 32;
            title.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 20;
            container.Add(title);

            var finalScore = new Label($"Final Score: {score.totalScore}");
            finalScore.style.fontSize = 24;
            finalScore.style.color = Color.white;
            finalScore.style.unityFontStyleAndWeight = FontStyle.Bold;
            finalScore.style.marginBottom = 20;
            container.Add(finalScore);

            AddStatLine(container, "Correct Replies", score.correctReplies.ToString());
            AddStatLine(container, "Wrong Replies", score.wrongReplies.ToString());
            AddStatLine(container, "Tasks Completed", score.tasksCompleted.ToString());
            AddStatLine(container, "Tasks Failed", score.tasksFailed.ToString());
            AddStatLine(container, "Emails Expired", score.emailsExpired.ToString());
            AddStatLine(container, "Spam Deleted", score.spamDeleted.ToString());
            AddStatLine(container, "Spam Replied To", score.spamReplied.ToString());
            AddStatLine(container, "Highest Streak", score.highestStreak.ToString());

            var restartBtn = new Button(() => GameManager.Instance?.StartGame());
            restartBtn.text = "Play Again";
            restartBtn.style.marginTop = 24;
            restartBtn.style.paddingTop = 10;
            restartBtn.style.paddingBottom = 10;
            restartBtn.style.paddingLeft = 32;
            restartBtn.style.paddingRight = 32;
            restartBtn.style.fontSize = 16;
            restartBtn.style.backgroundColor = new Color(0.91f, 0.27f, 0.38f, 1f);
            restartBtn.style.color = Color.white;
            restartBtn.style.borderTopWidth = 0;
            restartBtn.style.borderBottomWidth = 0;
            restartBtn.style.borderLeftWidth = 0;
            restartBtn.style.borderRightWidth = 0;
            restartBtn.style.borderTopLeftRadius = 6;
            restartBtn.style.borderTopRightRadius = 6;
            restartBtn.style.borderBottomLeftRadius = 6;
            restartBtn.style.borderBottomRightRadius = 6;
            container.Add(restartBtn);

            overlay.Add(container);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            _gameoverSlot.Add(overlay);

            // Fade in the overlay
            overlay.schedule.Execute(() => overlay.AddToClassList("overlay--visible"));
        }

        public void HideGameOver()
        {
            var overlay = _gameoverSlot.Q(className: "overlay");
            if (overlay != null)
            {
                overlay.RemoveFromClassList("overlay--visible");
                overlay.schedule.Execute(() =>
                {
                    _gameoverSlot.Clear();
                    _gameoverSlot.style.display = DisplayStyle.None;
                }).ExecuteLater(250);
            }
            else
            {
                _gameoverSlot.Clear();
                _gameoverSlot.style.display = DisplayStyle.None;
            }
        }

        private void AddStatLine(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.width = 300;
            row.style.marginBottom = 4;

            var lbl = new Label(label);
            lbl.style.fontSize = 14;
            lbl.style.color = new Color(0.63f, 0.63f, 0.69f, 1f);
            row.Add(lbl);

            var val = new Label(value);
            val.style.fontSize = 14;
            val.style.color = Color.white;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(val);

            parent.Add(row);
        }

        public void ToggleTheme()
        {
            _isLightMode = !_isLightMode;
            _uiRoot?.EnableInClassList("light-mode", _isLightMode);
            _hud?.UpdateThemeButtonLabel(_isLightMode);
        }

        private void RefreshInbox()
        {
            if (EmailManager.Instance != null)
                _inbox?.Refresh(EmailManager.Instance.Inbox);
            RefreshHUD();
        }

        private void RefreshHUD()
        {
            if (ScoreManager.Instance != null)
                _hud?.UpdateScore(ScoreManager.Instance.CurrentScore, ScoreManager.Instance.CurrentStreak);
        }

        // --- Callbacks ---

        private void OnEmailClicked(EmailInstance email)
        {
            ShowEmailDetail(email);
        }

        private void OnBackClicked()
        {
            ShowInbox();
        }

        private void OnReplyClicked()
        {
            // Reply toggle is handled inline by EmailDetailController
        }

        private void OnTaskClicked()
        {
            if (_detail?.CurrentEmail == null) return;
            EmailManager.Instance?.CompleteTask(_detail.CurrentEmail.InstanceId);
            ShowInbox();
        }

        private void OnDeleteClicked()
        {
            if (_detail?.CurrentEmail == null) return;
            string id = _detail.CurrentEmail.InstanceId;
            EmailManager.Instance?.DeleteEmail(id);
            ShowInbox();
        }

        private void OnReplyChosen(EmailInstance email, int choiceIndex)
        {
            if (EmailManager.Instance == null) return;

            EmailManager.Instance.ReplyToEmail(email.InstanceId, choiceIndex);
            ShowInbox();
        }

    }
}
