#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Overworked.Core;
using Overworked.Scoring;
using Overworked.Story;
using Overworked.Story.Data;

namespace Overworked.UI
{
    /// <summary>
    /// Editor-only debug menu. Press F1 to toggle.
    /// Stripped from builds via #if UNITY_EDITOR.
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private bool _visible;
        private VisualElement _panel;
        private StoryCollection _storyData;

        private void Start()
        {
            var asset = Resources.Load<TextAsset>("Data/Story/story_data");
            if (asset != null)
                _storyData = JsonUtility.FromJson<StoryCollection>(asset.text);
        }

        private void Update()
        {
            // Ctrl + Shift + D to toggle debug menu
            if (Keyboard.current != null
                && Keyboard.current.ctrlKey.isPressed
                && Keyboard.current.shiftKey.isPressed
                && Keyboard.current.dKey.wasPressedThisFrame)
                Toggle();
        }

        private void Toggle()
        {
            if (_visible)
                Hide();
            else
                Show();
        }

        private void Show()
        {
            _visible = true;
            var root = uiDocument.rootVisualElement;

            _panel = new VisualElement();
            _panel.pickingMode = PickingMode.Position;
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0;
            _panel.style.top = 0;
            _panel.style.right = 0;
            _panel.style.bottom = 0;
            _panel.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            _panel.style.alignItems = Align.Center;
            _panel.style.justifyContent = Justify.Center;

            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.15f, 0.1f, 0.2f, 1f);
            container.style.borderTopLeftRadius = 10;
            container.style.borderTopRightRadius = 10;
            container.style.borderBottomLeftRadius = 10;
            container.style.borderBottomRightRadius = 10;
            container.style.paddingTop = 20;
            container.style.paddingBottom = 20;
            container.style.paddingLeft = 24;
            container.style.paddingRight = 24;
            container.style.width = 360;
            container.style.maxHeight = 500;

            var title = new Label("DEBUG MENU (Ctrl+Shift+D)");
            title.style.fontSize = 16;
            title.style.color = new Color(1f, 0.4f, 1f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            container.Add(title);

            var hint = new Label("Editor only — not in builds");
            hint.style.fontSize = 9;
            hint.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            hint.style.marginBottom = 16;
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            container.Add(hint);

            var scroll = new ScrollView();
            scroll.style.maxHeight = 400;

            // --- Endings ---
            AddSection(scroll, "ENDINGS");
            AddButton(scroll, "Ending: Survive", new Color(0.376f, 0.647f, 0.98f, 1f), () => PlayEnding(EndingResolver.ENDING_SURVIVE));
            AddButton(scroll, "Ending: Breakdown", new Color(0.973f, 0.443f, 0.443f, 1f), () => PlayEnding(EndingResolver.ENDING_BREAKDOWN));
            AddButton(scroll, "Ending: Resign", new Color(0.973f, 0.682f, 0.275f, 1f), () => PlayEnding(EndingResolver.ENDING_RESIGN));
            AddButton(scroll, "Ending: Secret", new Color(0.29f, 0.87f, 0.5f, 1f), () => PlayEnding(EndingResolver.ENDING_SECRET));

            // --- Day Dialogues ---
            AddSection(scroll, "DAY DIALOGUES");
            if (_storyData?.days != null)
            {
                foreach (var day in _storyData.days)
                {
                    int dayNum = day.dayNumber;
                    if (day.preDialogue != null && day.preDialogue.Length > 0)
                        AddButton(scroll, $"Day {dayNum} Pre-Dialogue", new Color(0.4f, 0.6f, 0.9f, 1f), () => PlayDialogue(day.preDialogue));
                    if (day.postDialogue?.pass != null && day.postDialogue.pass.Length > 0)
                        AddButton(scroll, $"Day {dayNum} Post-Pass", new Color(0.3f, 0.8f, 0.5f, 1f), () => PlayDialogue(day.postDialogue.pass));
                    if (day.postDialogue?.fail != null && day.postDialogue.fail.Length > 0)
                        AddButton(scroll, $"Day {dayNum} Post-Fail", new Color(0.9f, 0.4f, 0.4f, 1f), () => PlayDialogue(day.postDialogue.fail));
                }
            }

            // --- Flags ---
            AddSection(scroll, "STORY FLAGS");
            AddButton(scroll, "Set: helped_dika_d3", new Color(0.5f, 0.7f, 0.5f, 1f), () => SetFlag("helped_dika_d3"));
            AddButton(scroll, "Set: helped_dika_d5", new Color(0.5f, 0.7f, 0.5f, 1f), () => SetFlag("helped_dika_d5"));
            AddButton(scroll, "Set: read_evidence_d6", new Color(0.5f, 0.7f, 0.5f, 1f), () => SetFlag("read_evidence_d6"));
            AddButton(scroll, "Set: read_blacklist_d7", new Color(0.5f, 0.7f, 0.5f, 1f), () => SetFlag("read_blacklist_d7"));
            AddButton(scroll, "Set: read_burnout_d7", new Color(0.5f, 0.7f, 0.5f, 1f), () => SetFlag("read_burnout_d7"));
            AddButton(scroll, "Set: forwarded_evidence_d7", new Color(0.5f, 0.7f, 0.5f, 1f), () => SetFlag("forwarded_evidence_d7"));
            AddButton(scroll, "Set: chose_resign_d5", new Color(0.9f, 0.7f, 0.4f, 1f), () => SetFlag("chose_resign_d5"));
            AddButton(scroll, "Set: confirmed_resign_d6", new Color(0.9f, 0.7f, 0.4f, 1f), () => SetFlag("confirmed_resign_d6"));
            AddButton(scroll, "Print All Flags", new Color(0.6f, 0.6f, 0.8f, 1f), PrintFlags);
            AddButton(scroll, "Clear All Flags", new Color(0.9f, 0.3f, 0.3f, 1f), ClearFlags);

            // --- Save ---
            AddSection(scroll, "SAVE DATA");
            AddButton(scroll, "Unlock All Days", new Color(0.6f, 0.8f, 0.6f, 1f), UnlockAllDays);
            AddButton(scroll, "Set Score +100", new Color(0.6f, 0.8f, 0.6f, 1f), () => AddDebugScore(100));
            AddButton(scroll, "Set Score -50", new Color(0.9f, 0.5f, 0.5f, 1f), () => AddDebugScore(-50));
            AddButton(scroll, "Print Save Data", new Color(0.6f, 0.6f, 0.8f, 1f), PrintSaveData);
            AddButton(scroll, "Full Reset", new Color(0.9f, 0.3f, 0.3f, 1f), () =>
            {
                SaveManager.ResetSave();
                Debug.Log("[Debug] Save fully reset.");
            });

            container.Add(scroll);

            // Close
            var closeBtn = new Button(Hide);
            closeBtn.text = "Close (Ctrl+Shift+D)";
            closeBtn.style.marginTop = 12;
            closeBtn.style.paddingTop = 8;
            closeBtn.style.paddingBottom = 8;
            closeBtn.style.paddingLeft = 20;
            closeBtn.style.paddingRight = 20;
            closeBtn.style.fontSize = 12;
            closeBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.4f, 1f);
            closeBtn.style.color = Color.white;
            closeBtn.style.borderTopWidth = 0;
            closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0;
            closeBtn.style.borderRightWidth = 0;
            closeBtn.style.borderTopLeftRadius = 5;
            closeBtn.style.borderTopRightRadius = 5;
            closeBtn.style.borderBottomLeftRadius = 5;
            closeBtn.style.borderBottomRightRadius = 5;
            container.Add(closeBtn);

            _panel.Add(container);
            root.Add(_panel);
        }

        private void Hide()
        {
            _visible = false;
            _panel?.RemoveFromHierarchy();
            _panel = null;
        }

        private void AddSection(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.style.fontSize = 11;
            label.style.color = new Color(0.7f, 0.5f, 0.9f, 1f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = 1;
            label.style.marginTop = 12;
            label.style.marginBottom = 6;
            parent.Add(label);
        }

        private void AddButton(VisualElement parent, string text, Color color, System.Action onClick)
        {
            var btn = new Button(() => { onClick?.Invoke(); });
            btn.text = text;
            btn.style.fontSize = 11;
            btn.style.paddingTop = 6;
            btn.style.paddingBottom = 6;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.marginBottom = 3;
            btn.style.backgroundColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 1f);
            btn.style.color = color;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(color.r, color.g, color.b, 0.3f);
            btn.style.borderBottomColor = new Color(color.r, color.g, color.b, 0.3f);
            btn.style.borderLeftColor = new Color(color.r, color.g, color.b, 0.3f);
            btn.style.borderRightColor = new Color(color.r, color.g, color.b, 0.3f);
            btn.style.borderTopLeftRadius = 4;
            btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4;
            btn.style.borderBottomRightRadius = 4;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            parent.Add(btn);
        }

        // --- Actions ---

        private void PlayEnding(string endingType)
        {
            Hide();
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager == null) return;

            var epilogue = EndingResolver.GetEpilogueDialogue(endingType);
            uiManager.ShowDialogue(epilogue, () => uiManager.HideDialogue());
            Debug.Log($"[Debug] Playing ending: {endingType}");
        }

        private void PlayDialogue(DialogueLine[] lines)
        {
            Hide();
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager == null) return;

            uiManager.ShowDialogue(lines, () => uiManager.HideDialogue());
            Debug.Log($"[Debug] Playing dialogue ({lines.Length} lines)");
        }

        private void SetFlag(string flag)
        {
            var save = SaveManager.Load();
            save.SetFlag(flag);
            SaveManager.Save(save);
            Debug.Log($"[Debug] Flag set: {flag}");
        }

        private void PrintFlags()
        {
            var save = SaveManager.Load();
            if (save.storyFlags.Count == 0)
            {
                Debug.Log("[Debug] No flags set.");
                return;
            }
            Debug.Log($"[Debug] Flags ({save.storyFlags.Count}): {string.Join(", ", save.storyFlags)}");
        }

        private void ClearFlags()
        {
            var save = SaveManager.Load();
            save.storyFlags.Clear();
            SaveManager.Save(save);
            Debug.Log("[Debug] All flags cleared.");
        }

        private void UnlockAllDays()
        {
            var save = SaveManager.Load();
            save.lastCompletedDay = 7;
            SaveManager.Save(save);
            Debug.Log("[Debug] All days unlocked (lastCompletedDay = 7).");
        }

        private void AddDebugScore(int amount)
        {
            if (ScoreManager.Instance != null)
            {
                // Access score directly for debug
                var field = typeof(ScoreManager).GetField("_score", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var score = (ScoreData)field.GetValue(ScoreManager.Instance);
                    score.totalScore += amount;
                    field.SetValue(ScoreManager.Instance, score);
                    Debug.Log($"[Debug] Score adjusted by {amount}, total: {score.totalScore}");
                }
            }
            else
            {
                Debug.Log("[Debug] ScoreManager not available.");
            }
        }

        private void PrintSaveData()
        {
            var save = SaveManager.Load();
            Debug.Log($"[Debug] SaveData:\n" +
                $"  playerName: {save.playerName}\n" +
                $"  lastCompletedDay: {save.lastCompletedDay}\n" +
                $"  arcadeHighScore: {save.arcadeHighScore}\n" +
                $"  dayScores: {save.dayScores.Count} entries\n" +
                $"  storyFlags: [{string.Join(", ", save.storyFlags)}]\n" +
                $"  endingsUnlocked: [{string.Join(", ", save.endingsUnlocked)}]");
        }
    }
}
#endif
