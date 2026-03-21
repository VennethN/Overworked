using UnityEngine;
using UnityEngine.UIElements;
using Overworked.Actions;
using Overworked.Core;
using Overworked.Email;
using Overworked.Minigames;
using Overworked.Email.Data;
using Overworked.Scoring;
using Overworked.Story;
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
        [SerializeField] private UIScaleController uiScaleController;

        private VisualElement _inboxSlot;
        private VisualElement _detailSlot;
        private VisualElement _hudSlot;
        private VisualElement _gameoverSlot;
        private VisualElement _modeselectSlot;
        private VisualElement _dialogueSlot;
        private VisualElement _daysummarySlot;
        private VisualElement _minigameSlot;
        private VisualElement _settingsSlot;
        private VisualElement _sidebar;
        private VisualElement _mainContent;

        private InboxController _inbox;
        private EmailDetailController _detail;
        private HUDController _hud;
        private ModeSelectController _modeSelect;

        private VisualElement _uiRoot;
        private VisualElement _docRoot;
        private bool _isLightMode = true;

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
            _docRoot = root;
            _uiRoot = root.Q("root");

            _inboxSlot = root.Q("inbox-slot");
            _detailSlot = root.Q("detail-slot");
            _hudSlot = root.Q("hud-slot");
            _gameoverSlot = root.Q("gameover-slot");
            _modeselectSlot = root.Q("modeselect-slot");
            _dialogueSlot = root.Q("dialogue-slot");
            _daysummarySlot = root.Q("daysummary-slot");
            _minigameSlot = root.Q("minigame-slot");
            _settingsSlot = root.Q("settings-slot");
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

            // Theme toggle — dark-mode class is added when NOT in light mode
            _hud.OnThemeToggleClicked += ToggleTheme;
            _hud.OnSettingsClicked += ToggleSettings;
            ApplyThemeToAll(!_isLightMode);
            _hud.UpdateThemeButtonLabel(_isLightMode);

            // Subscribe to events
            GameEvents.OnEmailReceived += OnEmailReceivedJuice;
            GameEvents.OnEmailExpired += OnEmailExpiredJuice;
            GameEvents.OnEmailDeleted += _ => { RefreshInbox(); RefreshHUD(); };
            GameEvents.OnEmailReplied += OnEmailRepliedJuice;
            GameEvents.OnTaskCompleted += OnTaskCompletedJuice;
            GameEvents.OnTaskFailed += OnTaskFailedJuice;
            GameEvents.OnGameOver += OnGameOverHandler;


            // Global click sounds — click for general, select for buttons
            _docRoot.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is Button)
                    Audio.SFXManager.Instance?.PlaySelect();
                else
                    Audio.SFXManager.Instance?.PlayClick();
            }, TrickleDown.TrickleDown);
        }

        private bool _scoreSubscribed;

        private void Update()
        {
            if (EmailManager.Instance == null) return;

            // Late-subscribe to ScoreManager (may not exist during OnEnable)
            if (!_scoreSubscribed && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
                _scoreSubscribed = true;
                Debug.Log("[ScoreUI] Subscribed to ScoreManager.OnScoreChanged");
            }

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

            // Responsive: hide sidebar on narrow screens (mobile)
            UpdateResponsiveLayout();

            // Escape key toggles pause/settings
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                ToggleSettings();
        }

        public void ShowModeSelect()
        {
            // Discard any flags set during an incomplete day
            SaveManager.DiscardPendingFlags();

            // Hide game UI
            _mainContent.style.display = DisplayStyle.None;
            _hudSlot.style.display = DisplayStyle.None;
            HideGameOver();

            // Show mode select overlay
            _modeselectSlot.style.display = DisplayStyle.Flex;
            _modeSelect = new ModeSelectController(_modeselectSlot, OnArcadeSelected, OnStoryDaySelected, ShowSettings);
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

            // Discard any leftover pending flags from a previous incomplete attempt
            SaveManager.DiscardPendingFlags();

            // Load day-specific emails if defined
            if (!string.IsNullOrEmpty(_pendingDay.specialEmailJsonPath))
                EmailManager.Instance?.LoadAdditionalEmails(new[] { _pendingDay.specialEmailJsonPath });

            // Start the day first (this clears inbox + coroutines)
            GameManager.Instance?.StartStoryDay(
                _pendingDay.dayLengthSeconds,
                _pendingDay.difficulty,
                _pendingDay.spawnRulesOverride,
                _pendingDay.availableEmailPools,
                _pendingDay.spawnEmailTags,
                _pendingDay.spawnEmailIds);

            // Schedule scripted emails AFTER StartGame so ClearInbox doesn't kill them
            if (_pendingDay.scriptedEmails != null)
            {
                var save = SaveManager.Load();
                foreach (var scripted in _pendingDay.scriptedEmails)
                {
                    // Check flag gates
                    if (!string.IsNullOrEmpty(scripted.requireFlag) && !save.HasFlag(scripted.requireFlag))
                        continue;
                    if (!string.IsNullOrEmpty(scripted.excludeFlag) && save.HasFlag(scripted.excludeFlag))
                        continue;

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

        public void ShowInbox(bool resetScroll = false)
        {
            _inboxSlot.style.display = DisplayStyle.Flex;
            _detailSlot.style.display = DisplayStyle.None;
            if (resetScroll)
                _inbox?.ClearAll();
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

            if (GameManager.Instance?.CurrentMode != GameMode.Story)
            {
                var restartBtn = CreateOverlayButton("Main Lagi", new Color(0.91f, 0.27f, 0.38f, 1f),
                    () => GameManager.Instance?.StartArcade());
                btnRow.Add(restartBtn);

                var spacerBtn = new VisualElement();
                spacerBtn.style.width = 8;
                btnRow.Add(spacerBtn);
            }

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
            ApplyThemeToAll(!_isLightMode);
            _hud?.UpdateThemeButtonLabel(_isLightMode);
        }

        private void ApplyThemeToAll(bool dark)
        {
            // Apply to document root
            _docRoot?.EnableInClassList("dark-mode", dark);

            // Also apply to all template containers so their scoped :root styles get overridden
            if (_docRoot != null)
            {
                _docRoot.Query(className: null).ForEach(el =>
                {
                    if (el is TemplateContainer)
                        el.EnableInClassList("dark-mode", dark);
                });
            }
        }

        // --- Settings Panel ---

        private bool _settingsOpen;

        private void ToggleSettings()
        {
            if (_settingsOpen)
                HideSettings();
            else
                ShowSettings();
        }

        private void ShowSettings()
        {
            bool isPlaying = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

            // Pause game if playing
            if (isPlaying)
                GameManager.Instance.PauseGame();

            _settingsOpen = true;
            _settingsSlot.Clear();
            _settingsSlot.style.display = DisplayStyle.Flex;

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
            container.style.width = 260;

            var title = new Label(isPlaying ? "PAUSED" : "SETTINGS");
            title.style.fontSize = 18;
            title.style.color = new Color(0.376f, 0.647f, 0.98f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            container.Add(title);

            // UI Scale row: [ - ]  1.0x  [ + ]
            float currentScale = uiScaleController != null ? uiScaleController.CurrentScale : 1f;

            var scaleLabel = new Label("UI Scale");
            scaleLabel.style.fontSize = 11;
            scaleLabel.style.color = new Color(0.63f, 0.63f, 0.69f, 1f);
            scaleLabel.style.marginBottom = 8;
            container.Add(scaleLabel);

            var scaleRow = new VisualElement();
            scaleRow.style.flexDirection = FlexDirection.Row;
            scaleRow.style.alignItems = Align.Center;
            scaleRow.style.justifyContent = Justify.Center;
            scaleRow.style.width = Length.Percent(100);
            scaleRow.style.marginBottom = 12;

            var scaleValue = new Label($"{currentScale:F1}x");
            scaleValue.style.fontSize = 14;
            scaleValue.style.color = Color.white;
            scaleValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            scaleValue.style.width = 48;
            scaleValue.style.unityTextAlign = TextAnchor.MiddleCenter;

            var minusBtn = CreateOverlayButton("-", new Color(0.235f, 0.306f, 0.416f, 1f), () =>
            {
                float step = uiScaleController != null ? uiScaleController.scaleStep : 0.1f;
                uiScaleController?.SetScale(uiScaleController.CurrentScale - step);
            });
            minusBtn.style.width = 36;
            minusBtn.style.paddingLeft = 0;
            minusBtn.style.paddingRight = 0;
            minusBtn.style.fontSize = 14;

            var plusBtn = CreateOverlayButton("+", new Color(0.235f, 0.306f, 0.416f, 1f), () =>
            {
                float step = uiScaleController != null ? uiScaleController.scaleStep : 0.1f;
                uiScaleController?.SetScale(uiScaleController.CurrentScale + step);
            });
            plusBtn.style.width = 36;
            plusBtn.style.paddingLeft = 0;
            plusBtn.style.paddingRight = 0;
            plusBtn.style.fontSize = 14;

            scaleRow.Add(minusBtn);
            scaleRow.Add(scaleValue);
            scaleRow.Add(plusBtn);

            // Keep value label in sync with keyboard shortcuts
            if (uiScaleController != null)
            {
                uiScaleController.OnScaleChanged += newScale =>
                {
                    if (_settingsOpen)
                        scaleValue.text = $"{newScale:F1}x";
                };
            }

            container.Add(scaleRow);

            // Hint for keyboard shortcut
            var hint = new Label("Ctrl +/- to adjust");
            hint.style.fontSize = 9;
            hint.style.color = new Color(0.4f, 0.4f, 0.47f, 1f);
            hint.style.marginBottom = 12;
            container.Add(hint);

            // Resume / Quit buttons (only during gameplay)
            if (isPlaying)
            {
                var resumeBtn = CreateOverlayButton("Lanjut", new Color(0.29f, 0.87f, 0.5f, 1f), HideSettings);
                resumeBtn.style.marginTop = 16;
                resumeBtn.style.width = Length.Percent(100);
                container.Add(resumeBtn);

                var quitBtn = CreateOverlayButton("Keluar ke Menu", new Color(0.85f, 0.25f, 0.25f, 1f), () =>
                {
                    HideSettings();
                    GameManager.Instance?.ReturnToMenu();
                });
                quitBtn.style.marginTop = 8;
                quitBtn.style.width = Length.Percent(100);
                container.Add(quitBtn);
            }
            else
            {
                // Close button (menu-only)
                var closeBtn = CreateOverlayButton("Close", new Color(0.376f, 0.647f, 0.98f, 1f), HideSettings);
                closeBtn.style.marginTop = 8;
                container.Add(closeBtn);
            }

            overlay.Add(container);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            _settingsSlot.Add(overlay);

            overlay.schedule.Execute(() => overlay.AddToClassList("overlay--visible"));
        }

        private void HideSettings()
        {
            _settingsOpen = false;

            // Resume game if it was paused
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused)
                GameManager.Instance.ResumeGame();

            // Clean up the callback
            if (uiScaleController != null)
                uiScaleController.OnScaleChanged = null;

            var overlay = _settingsSlot.Q(className: "overlay");
            if (overlay != null)
            {
                overlay.RemoveFromClassList("overlay--visible");
                overlay.schedule.Execute(() =>
                {
                    _settingsSlot.Clear();
                    _settingsSlot.style.display = DisplayStyle.None;
                }).ExecuteLater(250);
            }
            else
            {
                _settingsSlot.Clear();
                _settingsSlot.style.display = DisplayStyle.None;
            }
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

        // --- Responsive Layout ---
        private bool _isMobileLayout;

        private void UpdateResponsiveLayout()
        {
            bool narrow = Screen.width < 800;
            if (narrow == _isMobileLayout) return;
            _isMobileLayout = narrow;

            // On mobile: stack HUD items to prevent overlap
            var hudContainer = _hudSlot?.Q("hud-container");
            if (hudContainer != null)
            {
                hudContainer.style.flexWrap = narrow ? Wrap.Wrap : Wrap.NoWrap;
                hudContainer.style.justifyContent = narrow ? Justify.Center : Justify.SpaceBetween;
                hudContainer.style.paddingTop = narrow ? 4 : 6;
                hudContainer.style.paddingBottom = narrow ? 4 : 6;
            }
        }

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
            Audio.SFXManager.Instance?.PlayNewEmail();
            UIEffects.Pop(_hudSlot);
        }

        private void OnScoreChanged(int delta)
        {
            Debug.Log($"[ScoreUI] OnScoreChanged fired! delta={delta} docRoot={_docRoot != null}");

            string text = delta > 0 ? $"+{delta}" : $"{delta}";
            Color color = delta > 0
                ? new Color(0.29f, 0.87f, 0.5f, 1f)
                : new Color(0.97f, 0.44f, 0.44f, 1f);

            float w = Screen.width;
            float h = Screen.height;
            UIEffects.FloatingText(_docRoot, text, color, new Vector2(w / 2f, h / 3f));
        }

        private void OnEmailExpiredJuice(EmailInstance _)
        {
            RefreshInbox();
            RefreshHUD();
            Audio.SFXManager.Instance?.PlayEmailExpire();
            UIEffects.Shake(_uiRoot, 4f, 4);
            UIEffects.VignetteFlash(_uiRoot, new Color(0.97f, 0.27f, 0.27f, 0.6f), 300);
        }

        private void OnEmailRepliedJuice(EmailInstance _, ReplyResult result)
        {
            RefreshHUD();
            if (result.ScoreChange > 0)
            {
                Audio.SFXManager.Instance?.PlaySuccess();
                UIEffects.VignetteFlash(_uiRoot, new Color(0.29f, 0.87f, 0.5f, 0.4f), 250);
            }
            else if (result.ScoreChange < 0)
            {
                Audio.SFXManager.Instance?.PlayFail();
                UIEffects.Shake(_uiRoot, 5f, 5);
                UIEffects.VignetteFlash(_uiRoot, new Color(0.97f, 0.27f, 0.27f, 0.5f), 300);
            }
        }

        private void OnTaskCompletedJuice(EmailInstance _)
        {
            RefreshHUD();
            Audio.SFXManager.Instance?.PlaySuccess();
            UIEffects.VignetteFlash(_uiRoot, new Color(0.29f, 0.87f, 0.5f, 0.4f), 250);
            UIEffects.Pop(_hudSlot, 1.05f);
        }

        private void OnTaskFailedJuice(EmailInstance _)
        {
            RefreshHUD();
            Audio.SFXManager.Instance?.PlayFail();
            UIEffects.Shake(_uiRoot, 6f, 5);
            UIEffects.VignetteFlash(_uiRoot, new Color(0.97f, 0.27f, 0.27f, 0.6f), 400);
        }

        private void OnGameOverHandler(ScoreData finalScore)
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentMode != GameMode.Story) return;
            if (_pendingDay == null)
            {
                ShowGameOver(finalScore);
                return;
            }

            // Flush buffered flags to disk — day completed
            SaveManager.FlushPendingFlags();

            // Save day score — always unlock next day so the story progresses
            var save = SaveManager.Load();
            bool passed = finalScore.totalScore >= _pendingDay.scoreGoal;
            save.SetBestScore(_pendingDay.dayNumber, finalScore.totalScore);
            if (_pendingDay.dayNumber > save.lastCompletedDay)
                save.lastCompletedDay = _pendingDay.dayNumber;
            SaveManager.Save(save);

            // Check for special ending on day 6 (resign) or day 7 (all others)
            if (_pendingDay.dayNumber == 6 && save.HasFlag("confirmed_resign_d6"))
            {
                // Mark story as complete so day select shows "CERITA SELESAI"
                save.lastCompletedDay = 7;
                SaveManager.Save(save);
                ShowEndingDialogue(EndingResolver.Resolve(save, _storyData), finalScore);
                return;
            }

            if (_pendingDay.dayNumber == 7)
            {
                // Count failed days for breakdown check
                int failedDays = 0;
                if (_storyData?.days != null)
                {
                    foreach (var d in _storyData.days)
                    {
                        if (d.dayNumber > 7) continue;
                        if (save.GetBestScore(d.dayNumber) < d.scoreGoal) failedDays++;
                    }
                }

                string ending = EndingResolver.Resolve(save, _storyData);
                ShowEndingDialogue(ending, finalScore);
                return;
            }

            // Normal day end: show pass/fail dialogue then results
            DialogueLine[] lines = passed ? _pendingDay.postDialogue?.pass : _pendingDay.postDialogue?.fail;
            if (lines != null && lines.Length > 0)
            {
                ShowDialogue(lines, () =>
                {
                    HideDialogue();
                    ShowGameOver(finalScore);
                });
            }
            else
            {
                ShowGameOver(finalScore);
            }
        }

        private void ShowEndingDialogue(string endingType, ScoreData finalScore)
        {
            // Record ending as achievement
            var save = SaveManager.Load();
            save.UnlockEnding(endingType);
            SaveManager.Save(save);

            DialogueLine[] epilogue = EndingResolver.GetEpilogueDialogue(endingType);
            ShowDialogue(epilogue, () =>
            {
                HideDialogue();
                ShowGameOver(finalScore);
            });
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
                _activeMinigame.Cleanup();
                _activeMinigame = null;
                _minigameSlot.Clear();
                _minigameSlot.style.display = DisplayStyle.None;

                if (result.Success)
                    EmailManager.Instance?.CompleteTask(_minigameEmailInstanceId);

                _minigameEmailInstanceId = null;
                ShowInbox();
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
