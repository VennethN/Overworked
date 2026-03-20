using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Actions;
using Overworked.Core;
using Overworked.Email;
using Overworked.Minigames;
using Overworked.Email.Data;
using Overworked.Scoring;
using Overworked.Story.Data;

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
        private VisualElement _modeselectSlot;
        private VisualElement _dialogueSlot;
        private VisualElement _daysummarySlot;
        private VisualElement _minigameSlot;
        private VisualElement _sidebar;
        private VisualElement _mainContent;

        private InboxController _inbox;
        private EmailDetailController _detail;
        private HUDController _hud;
        private ModeSelectController _modeSelect;

        private VisualElement _uiRoot;
        private bool _isLightMode;

        private float _timerUpdateAccumulator;
        private const float TIMER_UPDATE_INTERVAL = 0.25f;

        private IMinigame _activeMinigame;
        private string _minigameEmailInstanceId;

        // Stress vignette overlay
        private VisualElement _stressVignette;

        // Story data loaded once for day selection + StoryManager
        private StoryCollection _storyData;
        public StoryCollection StoryData => _storyData;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            _uiRoot = root.Q("root");

            _inboxSlot = root.Q("inbox-slot");
            _detailSlot = root.Q("detail-slot");
            _hudSlot = root.Q("hud-slot");
            _gameoverSlot = root.Q("gameover-slot");
            _modeselectSlot = root.Q("modeselect-slot");
            _dialogueSlot = root.Q("dialogue-slot");
            _daysummarySlot = root.Q("daysummary-slot");
            _minigameSlot = root.Q("minigame-slot");
            _sidebar = root.Q("sidebar");
            _mainContent = root.Q("main-content");

            // Create persistent stress vignette overlay
            _stressVignette = new VisualElement();
            _stressVignette.name = "stress-vignette";
            _stressVignette.pickingMode = PickingMode.Ignore;
            _stressVignette.style.position = Position.Absolute;
            _stressVignette.style.left = 0;
            _stressVignette.style.top = 0;
            _stressVignette.style.right = 0;
            _stressVignette.style.bottom = 0;
            _stressVignette.style.borderTopWidth = 0;
            _stressVignette.style.borderBottomWidth = 0;
            _stressVignette.style.borderLeftWidth = 0;
            _stressVignette.style.borderRightWidth = 0;
            _stressVignette.style.borderTopColor = new Color(0.94f, 0.2f, 0.2f, 0f);
            _stressVignette.style.borderBottomColor = new Color(0.94f, 0.2f, 0.2f, 0f);
            _stressVignette.style.borderLeftColor = new Color(0.94f, 0.2f, 0.2f, 0f);
            _stressVignette.style.borderRightColor = new Color(0.94f, 0.2f, 0.2f, 0f);
            _uiRoot.Add(_stressVignette);

            // Load story data
            var storyAsset = Resources.Load<TextAsset>("Data/Story/story_data");
            if (storyAsset != null)
                _storyData = JsonUtility.FromJson<StoryCollection>(storyAsset.text);

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
            GameEvents.OnEmailReceived += OnEmailReceivedJuice;
            GameEvents.OnEmailExpired += OnEmailExpiredJuice;
            GameEvents.OnEmailDeleted += _ => RefreshInbox();
            GameEvents.OnEmailReplied += OnEmailRepliedJuice;
            GameEvents.OnTaskCompleted += OnTaskCompletedJuice;
            GameEvents.OnTaskFailed += OnTaskFailedJuice;
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

            // Tick active minigame
            _activeMinigame?.Tick(Time.deltaTime);

            // Update HUD timer
            if (GameManager.Instance != null)
            {
                _hud?.UpdateTimer(GameManager.Instance.TimeRemaining);
                UpdateStressVignette(GameManager.Instance.TimeRemaining, GameManager.Instance.DayLength);
            }

            _hud?.UpdateEmailCount(EmailManager.Instance.ActiveCount);
        }

        public void ShowModeSelect()
        {
            // Hide game UI
            _mainContent.style.display = DisplayStyle.None;
            _hudSlot.style.display = DisplayStyle.None;
            HideGameOver();

            // Show mode select overlay
            _modeselectSlot.style.display = DisplayStyle.Flex;
            _modeSelect = new ModeSelectController(_modeselectSlot, OnArcadeSelected, OnStoryDaySelected);
        }

        public void HideModeSelect()
        {
            _modeselectSlot.Clear();
            _modeselectSlot.style.display = DisplayStyle.None;

            // Show game UI
            _mainContent.style.display = DisplayStyle.Flex;
            _hudSlot.style.display = DisplayStyle.Flex;
        }

        private void OnArcadeSelected()
        {
            HideModeSelect();
            GameManager.Instance?.StartArcade();
        }

        private DayDefinition _pendingDay;

        private void OnStoryDaySelected(int dayNumber)
        {
            if (_storyData?.days == null) return;

            DayDefinition day = null;
            foreach (var d in _storyData.days)
            {
                if (d.dayNumber == dayNumber) { day = d; break; }
            }
            if (day == null) return;

            _pendingDay = day;
            HideModeSelect();

            // Show pre-day dialogue if available
            if (day.preDialogue != null && day.preDialogue.Length > 0)
            {
                ShowDialogue(day.preDialogue, OnPreDialogueComplete);
            }
            else
            {
                StartPendingDay();
            }
        }

        private void OnPreDialogueComplete()
        {
            HideDialogue();
            StartPendingDay();
        }

        private void StartPendingDay()
        {
            if (_pendingDay == null) return;

            // Load day-specific emails if defined
            if (!string.IsNullOrEmpty(_pendingDay.specialEmailJsonPath))
                EmailManager.Instance?.LoadAdditionalEmails(new[] { _pendingDay.specialEmailJsonPath });

            // Start the day first (this clears inbox + coroutines)
            GameManager.Instance?.StartStoryDay(
                _pendingDay.dayLengthSeconds,
                _pendingDay.difficulty,
                _pendingDay.spawnRulesOverride,
                _pendingDay.availableEmailPools);

            // Schedule scripted emails AFTER StartGame so ClearInbox doesn't kill them
            if (_pendingDay.scriptedEmails != null)
            {
                foreach (var scripted in _pendingDay.scriptedEmails)
                {
                    EmailManager.Instance?.ScheduleFollowUp(new FollowUp
                    {
                        emailId = scripted.emailId,
                        delaySeconds = scripted.triggerAtSeconds
                    });
                }
            }
        }

        public void ShowDialogue(DialogueLine[] lines, System.Action onComplete)
        {
            // Hide game UI during dialogue
            _mainContent.style.display = DisplayStyle.None;
            _hudSlot.style.display = DisplayStyle.None;

            _dialogueSlot.style.display = DisplayStyle.Flex;
            new DialogueController(_dialogueSlot, lines, onComplete);
        }

        public void HideDialogue()
        {
            _dialogueSlot.Clear();
            _dialogueSlot.style.display = DisplayStyle.None;

            _mainContent.style.display = DisplayStyle.Flex;
            _hudSlot.style.display = DisplayStyle.Flex;
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
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            container.style.paddingTop = 22;
            container.style.paddingBottom = 22;
            container.style.paddingLeft = 28;
            container.style.paddingRight = 28;
            container.style.alignItems = Align.Center;

            var title = new Label("GAME OVER");
            title.style.fontSize = 22;
            title.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 14;
            container.Add(title);

            var finalScore = new Label($"Final Score: {score.totalScore}");
            finalScore.style.fontSize = 16;
            finalScore.style.color = Color.white;
            finalScore.style.unityFontStyleAndWeight = FontStyle.Bold;
            finalScore.style.marginBottom = 14;
            container.Add(finalScore);

            AddStatLine(container, "Correct Replies", score.correctReplies.ToString());
            AddStatLine(container, "Wrong Replies", score.wrongReplies.ToString());
            AddStatLine(container, "Tasks Completed", score.tasksCompleted.ToString());
            AddStatLine(container, "Tasks Failed", score.tasksFailed.ToString());
            AddStatLine(container, "Emails Expired", score.emailsExpired.ToString());
            AddStatLine(container, "Spam Deleted", score.spamDeleted.ToString());
            AddStatLine(container, "Spam Replied To", score.spamReplied.ToString());
            AddStatLine(container, "Highest Streak", score.highestStreak.ToString());

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = 16;

            var restartBtn = CreateOverlayButton("Main Lagi", new Color(0.91f, 0.27f, 0.38f, 1f),
                () => GameManager.Instance?.StartArcade());
            btnRow.Add(restartBtn);

            var spacer = new VisualElement();
            spacer.style.width = 8;
            btnRow.Add(spacer);

            var menuBtn = CreateOverlayButton("Menu", new Color(0.235f, 0.306f, 0.416f, 1f),
                () => GameManager.Instance?.ReturnToMenu());
            btnRow.Add(menuBtn);

            container.Add(btnRow);

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

        private Button CreateOverlayButton(string text, Color bgColor, System.Action onClick)
        {
            var btn = new Button(() => onClick?.Invoke());
            btn.text = text;
            btn.style.paddingTop = 7;
            btn.style.paddingBottom = 7;
            btn.style.paddingLeft = 22;
            btn.style.paddingRight = 22;
            btn.style.fontSize = 11;
            btn.style.backgroundColor = bgColor;
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = 4;
            btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4;
            btn.style.borderBottomRightRadius = 4;
            return btn;
        }

        private void AddStatLine(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.width = 210;
            row.style.marginBottom = 3;

            var lbl = new Label(label);
            lbl.style.fontSize = 10;
            lbl.style.color = new Color(0.63f, 0.63f, 0.69f, 1f);
            row.Add(lbl);

            var val = new Label(value);
            val.style.fontSize = 10;
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

        // --- Stress Vignette ---

        private void UpdateStressVignette(float timeRemaining, float dayLength)
        {
            if (_stressVignette == null || dayLength <= 0f) return;

            float ratio = timeRemaining / dayLength;
            // Start showing at 30% time remaining, max intensity at 0%
            float intensity = Mathf.Clamp01(1f - ratio / 0.3f);

            if (intensity <= 0f)
            {
                _stressVignette.style.borderTopWidth = 0;
                _stressVignette.style.borderBottomWidth = 0;
                _stressVignette.style.borderLeftWidth = 0;
                _stressVignette.style.borderRightWidth = 0;
                return;
            }

            // Border width and alpha scale with stress
            float borderWidth = Mathf.Lerp(0f, 5f, intensity);
            float alpha = Mathf.Lerp(0f, 0.5f, intensity);
            var color = new Color(0.94f, 0.2f, 0.2f, alpha);

            _stressVignette.style.borderTopWidth = borderWidth;
            _stressVignette.style.borderBottomWidth = borderWidth;
            _stressVignette.style.borderLeftWidth = borderWidth;
            _stressVignette.style.borderRightWidth = borderWidth;
            _stressVignette.style.borderTopColor = color;
            _stressVignette.style.borderBottomColor = color;
            _stressVignette.style.borderLeftColor = color;
            _stressVignette.style.borderRightColor = color;
        }

        // --- Juice Effects ---

        private void OnEmailReceivedJuice(EmailInstance _)
        {
            RefreshInbox();
            UIEffects.Pop(_hudSlot);
        }

        private void OnEmailExpiredJuice(EmailInstance _)
        {
            RefreshInbox();
            UIEffects.Shake(_uiRoot, 4f, 4);
            UIEffects.VignetteFlash(_uiRoot, new Color(0.97f, 0.27f, 0.27f, 0.6f), 300);
        }

        private void OnEmailRepliedJuice(EmailInstance _, ReplyResult result)
        {
            RefreshHUD();
            if (result.ScoreChange > 0)
            {
                UIEffects.VignetteFlash(_uiRoot, new Color(0.29f, 0.87f, 0.5f, 0.4f), 250);
                UIEffects.FloatingText(_uiRoot, $"+{result.ScoreChange}", new Color(0.29f, 0.87f, 0.5f, 1f),
                    new Vector2(_uiRoot.resolvedStyle.width / 2f, _uiRoot.resolvedStyle.height / 2f));
            }
            else if (result.ScoreChange < 0)
            {
                UIEffects.Shake(_uiRoot, 5f, 5);
                UIEffects.VignetteFlash(_uiRoot, new Color(0.97f, 0.27f, 0.27f, 0.5f), 300);
                UIEffects.FloatingText(_uiRoot, $"{result.ScoreChange}", new Color(0.97f, 0.44f, 0.44f, 1f),
                    new Vector2(_uiRoot.resolvedStyle.width / 2f, _uiRoot.resolvedStyle.height / 2f));
            }
        }

        private void OnTaskCompletedJuice(EmailInstance _)
        {
            RefreshHUD();
            UIEffects.VignetteFlash(_uiRoot, new Color(0.29f, 0.87f, 0.5f, 0.4f), 250);
            UIEffects.Pop(_hudSlot, 1.05f);
        }

        private void OnTaskFailedJuice(EmailInstance _)
        {
            RefreshHUD();
            UIEffects.Shake(_uiRoot, 6f, 5);
            UIEffects.VignetteFlash(_uiRoot, new Color(0.97f, 0.27f, 0.27f, 0.6f), 400);
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

            string instanceId = _detail.CurrentEmail.InstanceId;

            // Check if this task requires a minigame
            IMinigame minigame = EmailManager.Instance?.GetMinigame(instanceId);
            if (minigame != null)
            {
                ShowMinigame(minigame, instanceId);
            }
            else
            {
                EmailManager.Instance?.CompleteTask(instanceId);
                ShowInbox();
            }
        }

        private void ShowMinigame(IMinigame minigame, string emailInstanceId)
        {
            _activeMinigame = minigame;
            _minigameEmailInstanceId = emailInstanceId;

            _minigameSlot.Clear();
            _minigameSlot.style.display = DisplayStyle.Flex;

            minigame.BuildUI(_minigameSlot);
            minigame.OnCompleted += OnMinigameCompleted;
            minigame.Start();
        }

        private void OnMinigameCompleted(MinigameResult result)
        {
            if (_activeMinigame != null)
            {
                _activeMinigame.OnCompleted -= OnMinigameCompleted;
                // Delay cleanup so player can see the result
                _minigameSlot.schedule.Execute(() =>
                {
                    _activeMinigame?.Cleanup();
                    _activeMinigame = null;
                    _minigameSlot.Clear();
                    _minigameSlot.style.display = DisplayStyle.None;

                    if (result.Success)
                    {
                        EmailManager.Instance?.CompleteTask(_minigameEmailInstanceId);
                    }
                    _minigameEmailInstanceId = null;
                    ShowInbox();
                }).ExecuteLater(1500);
            }
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
