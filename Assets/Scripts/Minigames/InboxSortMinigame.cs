using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.Minigames
{
    public class InboxSortMinigame : IMinigame
    {
        public string MinigameId => "inbox_sort";
        public event Action<MinigameResult> OnCompleted;

        private struct SortItem
        {
            public string Text;
            public string CorrectBin;
        }

        private static readonly SortItem[] AllItems =
        {
            new() { Text = "Laporan Keuangan Q3", CorrectBin = "Pekerjaan" },
            new() { Text = "Promo Diskon 50%!", CorrectBin = "Promosi" },
            new() { Text = "Undangan Makan Siang Tim", CorrectBin = "Sosial" },
            new() { Text = "URGENT: Server Down", CorrectBin = "Utama" },
            new() { Text = "Newsletter Bulanan", CorrectBin = "Promosi" },
            new() { Text = "Jadwal Rapat Direksi", CorrectBin = "Utama" },
            new() { Text = "Ulang Tahun Rekan Kerja", CorrectBin = "Sosial" },
            new() { Text = "Review Pull Request #42", CorrectBin = "Pekerjaan" },
            new() { Text = "Flash Sale Gadget!", CorrectBin = "Promosi" },
            new() { Text = "Pengajuan Cuti Karyawan", CorrectBin = "Pekerjaan" },
            new() { Text = "Acara Team Building Sabtu", CorrectBin = "Sosial" },
            new() { Text = "Alert Keamanan Login", CorrectBin = "Utama" },
            new() { Text = "Konfirmasi Vendor Baru", CorrectBin = "Pekerjaan" },
            new() { Text = "Gratis Ongkir Hari Ini", CorrectBin = "Promosi" },
            new() { Text = "Keluhan Pelanggan VIP", CorrectBin = "Utama" },
            new() { Text = "Kabar Pernikahan Kolega", CorrectBin = "Sosial" }
        };

        private static readonly string[] BinNames = { "Utama", "Pekerjaan", "Sosial", "Promosi" };

        private readonly string _difficulty;
        private readonly List<SortItem> _items = new();
        private int _currentIndex;
        private int _correctCount;
        private float _timeLimit;
        private float _elapsed;
        private bool _finished;
        private float _startTime;

        private Label _itemLabel;
        private Label _progressLabel;
        private Label _timerLabel;
        private Label _feedbackLabel;
        private VisualElement _progressFill;
        private VisualElement _binsContainer;

        public InboxSortMinigame(string difficulty)
        {
            _difficulty = difficulty ?? "medium";
        }

        public void BuildUI(VisualElement container)
        {
            container.Clear();

            int itemCount;
            switch (_difficulty)
            {
                case "easy":
                    itemCount = 4;
                    _timeLimit = 20f;
                    break;
                case "hard":
                    itemCount = 8;
                    _timeLimit = 25f;
                    break;
                default:
                    itemCount = 6;
                    _timeLimit = 22f;
                    break;
            }

            // Shuffle and pick items
            var shuffled = new List<SortItem>(AllItems);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            _items.Clear();
            for (int i = 0; i < Mathf.Min(itemCount, shuffled.Count); i++)
                _items.Add(shuffled[i]);

            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.style.backgroundColor = new Color(0, 0, 0, 0.85f);

            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.11f, 0.15f, 0.27f, 1f);
            card.style.borderTopLeftRadius = 12;
            card.style.borderTopRightRadius = 12;
            card.style.borderBottomLeftRadius = 12;
            card.style.borderBottomRightRadius = 12;
            card.style.paddingTop = 28;
            card.style.paddingBottom = 28;
            card.style.paddingLeft = 36;
            card.style.paddingRight = 36;
            card.style.width = 480;
            card.style.alignItems = Align.Center;

            var title = new Label("Sortir Inbox");
            title.style.fontSize = 22;
            title.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            card.Add(title);

            var instruction = new Label("Pilih kategori yang tepat untuk setiap item:");
            instruction.style.fontSize = 13;
            instruction.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            instruction.style.marginBottom = 16;
            card.Add(instruction);

            _progressLabel = new Label($"Item 1 / {_items.Count}");
            _progressLabel.style.fontSize = 13;
            _progressLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _progressLabel.style.marginBottom = 12;
            card.Add(_progressLabel);

            // Current item display
            _itemLabel = new Label("");
            _itemLabel.style.fontSize = 18;
            _itemLabel.style.color = Color.white;
            _itemLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _itemLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _itemLabel.style.marginBottom = 20;
            _itemLabel.style.paddingTop = 14;
            _itemLabel.style.paddingBottom = 14;
            _itemLabel.style.paddingLeft = 16;
            _itemLabel.style.paddingRight = 16;
            _itemLabel.style.backgroundColor = new Color(0.06f, 0.08f, 0.16f, 1f);
            _itemLabel.style.borderTopLeftRadius = 8;
            _itemLabel.style.borderTopRightRadius = 8;
            _itemLabel.style.borderBottomLeftRadius = 8;
            _itemLabel.style.borderBottomRightRadius = 8;
            _itemLabel.style.width = Length.Percent(100);
            card.Add(_itemLabel);

            // Bin buttons
            _binsContainer = new VisualElement();
            _binsContainer.style.flexDirection = FlexDirection.Row;
            _binsContainer.style.flexWrap = Wrap.Wrap;
            _binsContainer.style.justifyContent = Justify.Center;
            _binsContainer.style.marginBottom = 16;

            Color[] binColors =
            {
                new Color(0.91f, 0.27f, 0.38f, 1f), // Utama - red
                new Color(0.4f, 0.65f, 1f, 1f),      // Pekerjaan - blue
                new Color(0.3f, 0.85f, 0.45f, 1f),   // Sosial - green
                new Color(0.95f, 0.7f, 0.2f, 1f)     // Promosi - yellow
            };

            for (int i = 0; i < BinNames.Length; i++)
            {
                string binName = BinNames[i];
                Color color = binColors[i];
                var btn = new Button(() => OnBinClicked(binName));
                btn.text = binName;
                btn.style.fontSize = 15;
                btn.style.paddingTop = 10;
                btn.style.paddingBottom = 10;
                btn.style.paddingLeft = 20;
                btn.style.paddingRight = 20;
                btn.style.marginLeft = 4;
                btn.style.marginRight = 4;
                btn.style.marginBottom = 8;
                btn.style.backgroundColor = color;
                btn.style.color = Color.white;
                btn.style.borderTopLeftRadius = 8;
                btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8;
                btn.style.borderBottomRightRadius = 8;
                btn.style.borderTopWidth = 0;
                btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0;
                btn.style.borderRightWidth = 0;
                _binsContainer.Add(btn);
            }
            card.Add(_binsContainer);

            // Timer bar
            var progressBg = new VisualElement();
            progressBg.style.width = Length.Percent(100);
            progressBg.style.height = 6;
            progressBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 1f);
            progressBg.style.borderTopLeftRadius = 3;
            progressBg.style.borderTopRightRadius = 3;
            progressBg.style.borderBottomLeftRadius = 3;
            progressBg.style.borderBottomRightRadius = 3;
            progressBg.style.marginBottom = 8;

            _progressFill = new VisualElement();
            _progressFill.style.height = Length.Percent(100);
            _progressFill.style.width = Length.Percent(100);
            _progressFill.style.backgroundColor = new Color(0.3f, 0.85f, 0.45f, 1f);
            _progressFill.style.borderTopLeftRadius = 3;
            _progressFill.style.borderTopRightRadius = 3;
            _progressFill.style.borderBottomLeftRadius = 3;
            _progressFill.style.borderBottomRightRadius = 3;
            progressBg.Add(_progressFill);
            card.Add(progressBg);

            _timerLabel = new Label($"Waktu: {_timeLimit:F1}s");
            _timerLabel.style.fontSize = 14;
            _timerLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _timerLabel.style.marginBottom = 8;
            card.Add(_timerLabel);

            _feedbackLabel = new Label("");
            _feedbackLabel.style.fontSize = 14;
            _feedbackLabel.style.height = 20;
            card.Add(_feedbackLabel);

            root.Add(card);
            container.Add(root);
        }

        public void Start()
        {
            _elapsed = 0f;
            _finished = false;
            _currentIndex = 0;
            _correctCount = 0;
            _startTime = Time.time;
            ShowCurrentItem();
        }

        public void Tick(float deltaTime)
        {
            if (_finished) return;

            _elapsed += deltaTime;
            float remaining = _timeLimit - _elapsed;

            if (remaining <= 0f)
            {
                _finished = true;
                bool success = _correctCount >= _items.Count / 2;
                _feedbackLabel.text = success
                    ? $"Waktu habis! Benar: {_correctCount}/{_items.Count}"
                    : "Waktu habis!";
                _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
                _progressFill.style.width = Length.Percent(0);
                _timerLabel.text = "Waktu: 0.0s";
                OnCompleted?.Invoke(new MinigameResult { Success = success, CompletionTime = _elapsed });
                return;
            }

            float ratio = remaining / _timeLimit;
            _progressFill.style.width = Length.Percent(ratio * 100f);
            _timerLabel.text = $"Waktu: {remaining:F1}s";

            if (ratio < 0.3f)
                _progressFill.style.backgroundColor = new Color(0.91f, 0.27f, 0.38f, 1f);
            else if (ratio < 0.6f)
                _progressFill.style.backgroundColor = new Color(0.95f, 0.7f, 0.2f, 1f);
        }

        private void ShowCurrentItem()
        {
            if (_currentIndex >= _items.Count) return;
            _itemLabel.text = _items[_currentIndex].Text;
            _progressLabel.text = $"Item {_currentIndex + 1} / {_items.Count}";
        }

        private void OnBinClicked(string binName)
        {
            if (_finished || _currentIndex >= _items.Count) return;

            bool correct = _items[_currentIndex].CorrectBin == binName;
            if (correct)
            {
                _correctCount++;
                _feedbackLabel.text = "Benar!";
                _feedbackLabel.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);
            }
            else
            {
                _feedbackLabel.text = $"Salah! Yang benar: {_items[_currentIndex].CorrectBin}";
                _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
            }

            _currentIndex++;

            if (_currentIndex >= _items.Count)
            {
                _finished = true;
                bool success = _correctCount > _items.Count / 2;
                float completionTime = Time.time - _startTime;

                _itemLabel.text = $"Selesai! Benar: {_correctCount}/{_items.Count}";
                _feedbackLabel.text = success ? "Lulus!" : "Gagal!";
                _feedbackLabel.style.color = success
                    ? new Color(0.3f, 0.85f, 0.45f, 1f)
                    : new Color(0.91f, 0.27f, 0.38f, 1f);

                OnCompleted?.Invoke(new MinigameResult { Success = success, CompletionTime = completionTime });
            }
            else
            {
                ShowCurrentItem();
            }
        }

        public void Cleanup()
        {
            _finished = true;
        }
    }
}
