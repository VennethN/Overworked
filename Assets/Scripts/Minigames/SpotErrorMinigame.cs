using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.Minigames
{
    public class SpotErrorMinigame : IMinigame
    {
        public string MinigameId => "spot_error";
        public event Action<MinigameResult> OnCompleted;

        private struct ErrorPair
        {
            public string Correct;
            public string Wrong;
            public string Label;
        }

        private static readonly ErrorPair[] EasyPairs =
        {
            new() { Label = "Nama Karyawan", Correct = "Budi Santoso", Wrong = "Budi Santosa" },
            new() { Label = "No. Rekening", Correct = "1234567890", Wrong = "1234567980" },
            new() { Label = "Tanggal Lahir", Correct = "17-08-1995", Wrong = "17-08-1996" },
            new() { Label = "Email", Correct = "budi@sleepdep.co.id", Wrong = "budi@sleedep.co.id" },
            new() { Label = "No. Telepon", Correct = "0812-3456-7890", Wrong = "0812-3465-7890" },
            new() { Label = "Alamat", Correct = "Jl. Merdeka No. 45", Wrong = "Jl. Merdeka No. 54" },
            new() { Label = "Kode Pos", Correct = "12345", Wrong = "12354" },
            new() { Label = "NIK", Correct = "3275012508950001", Wrong = "3275012508950010" },
        };

        private static readonly ErrorPair[] MediumPairs =
        {
            new() { Label = "Invoice", Correct = "INV-2026-03847", Wrong = "INV-2026-03874" },
            new() { Label = "Total Bayar", Correct = "Rp 84.750.000", Wrong = "Rp 84.570.000" },
            new() { Label = "No. Kontrak", Correct = "KTR/SDS/2026/0891", Wrong = "KTR/SDS/2026/0819" },
            new() { Label = "NPWP", Correct = "09.847.235.1-012.000", Wrong = "09.847.235.1-021.000" },
            new() { Label = "Nama PT", Correct = "PT Maju Jaya Sentosa", Wrong = "PT Maju Jaya Santosa" },
            new() { Label = "No. Surat", Correct = "SE-034/HR/2026", Wrong = "SE-034/RH/2026" },
            new() { Label = "Jumlah Unit", Correct = "1.847 unit", Wrong = "1.874 unit" },
            new() { Label = "IBAN", Correct = "ID89 3700 0000 0532 0131 00", Wrong = "ID89 3700 0000 0532 0113 00" },
        };

        private static readonly ErrorPair[] HardPairs =
        {
            new() { Label = "Hash Dokumen", Correct = "a7f3b9c2d8e1f046", Wrong = "a7f3b9c2d8e1f064" },
            new() { Label = "Serial Number", Correct = "SN-KX7742-DELTA-09", Wrong = "SN-KX7742-DLETA-09" },
            new() { Label = "Koordinat", Correct = "-6.2088° S, 106.8456° E", Wrong = "-6.2088° S, 106.8465° E" },
            new() { Label = "MAC Address", Correct = "00:1A:2B:3C:4D:5E", Wrong = "00:1A:2B:3C:4E:5D" },
            new() { Label = "UUID", Correct = "550e8400-e29b-41d4-a716", Wrong = "550e8400-e29b-41d4-a761" },
            new() { Label = "Checksum", Correct = "CRC32: 0xAB12CD34", Wrong = "CRC32: 0xAB12DC34" },
            new() { Label = "IP Address", Correct = "192.168.14.237", Wrong = "192.168.14.273" },
            new() { Label = "Timestamp", Correct = "2026-03-20T14:32:07Z", Wrong = "2026-03-20T14:32:70Z" },
        };

        private readonly string _difficulty;
        private int _totalRounds;
        private int _currentRound;
        private int _correctCount;
        private float _timeLimit;
        private float _elapsed;
        private bool _finished;
        private float _startTime;
        private float _cooldownRemaining;
        private const float WRONG_COOLDOWN = 5.5f;

        private List<ErrorPair> _rounds = new();
        private bool _errorOnLeft; // true = left card has the error

        private Label _titleLabel;
        private Label _progressLabel;
        private Label _categoryLabel;
        private Label _timerLabel;
        private Label _feedbackLabel;
        private VisualElement _progressFill;
        private Button _leftCard;
        private Button _rightCard;
        private Label _leftText;
        private Label _rightText;
        private Label _referenceText;

        public SpotErrorMinigame(string difficulty)
        {
            _difficulty = difficulty ?? "medium";
        }

        public void BuildUI(VisualElement container)
        {
            container.Clear();

            ErrorPair[] pool;
            switch (_difficulty)
            {
                case "easy":
                    _totalRounds = 3;
                    _timeLimit = 20f;
                    pool = EasyPairs;
                    break;
                case "hard":
                    _totalRounds = 6;
                    _timeLimit = 25f;
                    pool = HardPairs;
                    break;
                default:
                    _totalRounds = 4;
                    _timeLimit = 22f;
                    pool = MediumPairs;
                    break;
            }

            // Shuffle and pick rounds
            var shuffled = new List<ErrorPair>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            _rounds.Clear();
            for (int i = 0; i < Mathf.Min(_totalRounds, shuffled.Count); i++)
                _rounds.Add(shuffled[i]);

            // Root
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
            card.style.paddingTop = 24;
            card.style.paddingBottom = 24;
            card.style.paddingLeft = 32;
            card.style.paddingRight = 32;
            card.style.width = 480;
            card.style.alignItems = Align.Center;

            _titleLabel = new Label("Cari Kesalahan");
            _titleLabel.style.fontSize = 22;
            _titleLabel.style.color = new Color(0.95f, 0.5f, 0.2f, 1f);
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.marginBottom = 4;
            card.Add(_titleLabel);

            var instruction = new Label("Cocokkan dengan data asli. Klik yang TIDAK COCOK:");
            instruction.style.fontSize = 13;
            instruction.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            instruction.style.marginBottom = 12;
            card.Add(instruction);

            _progressLabel = new Label($"Data 1 / {_totalRounds}");
            _progressLabel.style.fontSize = 13;
            _progressLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _progressLabel.style.marginBottom = 8;
            card.Add(_progressLabel);

            _categoryLabel = new Label("");
            _categoryLabel.style.fontSize = 14;
            _categoryLabel.style.color = new Color(0.7f, 0.7f, 0.85f, 1f);
            _categoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _categoryLabel.style.marginBottom = 6;
            card.Add(_categoryLabel);

            // Reference: the correct data shown as read-only
            var refRow = new VisualElement();
            refRow.style.flexDirection = FlexDirection.Row;
            refRow.style.alignItems = Align.Center;
            refRow.style.justifyContent = Justify.Center;
            refRow.style.marginBottom = 12;
            refRow.style.paddingTop = 8;
            refRow.style.paddingBottom = 8;
            refRow.style.paddingLeft = 14;
            refRow.style.paddingRight = 14;
            refRow.style.backgroundColor = new Color(0.15f, 0.35f, 0.2f, 0.4f);
            refRow.style.borderTopLeftRadius = 6;
            refRow.style.borderTopRightRadius = 6;
            refRow.style.borderBottomLeftRadius = 6;
            refRow.style.borderBottomRightRadius = 6;
            refRow.style.borderTopWidth = 1;
            refRow.style.borderBottomWidth = 1;
            refRow.style.borderLeftWidth = 1;
            refRow.style.borderRightWidth = 1;
            refRow.style.borderTopColor = new Color(0.3f, 0.85f, 0.45f, 0.4f);
            refRow.style.borderBottomColor = new Color(0.3f, 0.85f, 0.45f, 0.4f);
            refRow.style.borderLeftColor = new Color(0.3f, 0.85f, 0.45f, 0.4f);
            refRow.style.borderRightColor = new Color(0.3f, 0.85f, 0.45f, 0.4f);

            var refLabel = new Label("Data Asli: ");
            refLabel.style.fontSize = 12;
            refLabel.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);
            refLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            refRow.Add(refLabel);

            _referenceText = new Label("");
            _referenceText.style.fontSize = 15;
            _referenceText.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);
            _referenceText.style.unityFontStyleAndWeight = FontStyle.Bold;
            refRow.Add(_referenceText);

            card.Add(refRow);

            // Two side-by-side cards
            var cardsRow = new VisualElement();
            cardsRow.style.flexDirection = FlexDirection.Row;
            cardsRow.style.justifyContent = Justify.Center;
            cardsRow.style.marginBottom = 16;
            cardsRow.style.width = Length.Percent(100);

            _leftCard = new Button(() => OnCardClicked(true));
            _leftCard.text = "";
            StyleDataCard(_leftCard);
            _leftText = new Label("");
            _leftText.style.fontSize = 16;
            _leftText.style.color = Color.white;
            _leftText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _leftText.style.whiteSpace = WhiteSpace.Normal;
            _leftCard.Add(_leftText);
            cardsRow.Add(_leftCard);

            var spacer = new VisualElement();
            spacer.style.width = 16;
            cardsRow.Add(spacer);

            _rightCard = new Button(() => OnCardClicked(false));
            _rightCard.text = "";
            StyleDataCard(_rightCard);
            _rightText = new Label("");
            _rightText.style.fontSize = 16;
            _rightText.style.color = Color.white;
            _rightText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _rightText.style.whiteSpace = WhiteSpace.Normal;
            _rightCard.Add(_rightText);
            cardsRow.Add(_rightCard);

            card.Add(cardsRow);

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
            _progressFill.style.backgroundColor = new Color(0.95f, 0.5f, 0.2f, 1f);
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

        private void StyleDataCard(Button btn)
        {
            btn.style.flexGrow = 1;
            btn.style.paddingTop = 16;
            btn.style.paddingBottom = 16;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.backgroundColor = new Color(0.06f, 0.08f, 0.16f, 1f);
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;
            btn.style.borderTopColor = new Color(0.2f, 0.25f, 0.4f, 1f);
            btn.style.borderBottomColor = new Color(0.2f, 0.25f, 0.4f, 1f);
            btn.style.borderLeftColor = new Color(0.2f, 0.25f, 0.4f, 1f);
            btn.style.borderRightColor = new Color(0.2f, 0.25f, 0.4f, 1f);
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.Center;
            btn.style.minHeight = 50;
        }

        public void Start()
        {
            _elapsed = 0f;
            _finished = false;
            _currentRound = 0;
            _correctCount = 0;
            _cooldownRemaining = 0f;
            _startTime = Time.time;
            ShowCurrentRound();
        }

        public void Tick(float deltaTime)
        {
            if (_finished) return;

            _elapsed += deltaTime;

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= deltaTime;
                if (_cooldownRemaining <= 0f)
                {
                    _cooldownRemaining = 0f;
                    _feedbackLabel.text = "";
                    SetCardsEnabled(true);
                    ResetCardBorders();
                }
            }

            float remaining = _timeLimit - _elapsed;
            if (remaining <= 0f)
            {
                _finished = true;
                _feedbackLabel.text = "Waktu habis!";
                _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
                _progressFill.style.width = Length.Percent(0);
                _timerLabel.text = "Waktu: 0.0s";
                OnCompleted?.Invoke(new MinigameResult { Success = false, CompletionTime = _elapsed });
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

        private void ShowCurrentRound()
        {
            if (_currentRound >= _rounds.Count) return;

            var pair = _rounds[_currentRound];
            _errorOnLeft = UnityEngine.Random.value > 0.5f;

            _categoryLabel.text = pair.Label;
            _progressLabel.text = $"Data {_currentRound + 1} / {_totalRounds}";
            _referenceText.text = pair.Correct;

            _leftText.text = _errorOnLeft ? pair.Wrong : pair.Correct;
            _rightText.text = _errorOnLeft ? pair.Correct : pair.Wrong;

            ResetCardBorders();
        }

        private void OnCardClicked(bool clickedLeft)
        {
            if (_finished || _cooldownRemaining > 0f) return;

            bool clickedError = (clickedLeft == _errorOnLeft);

            if (clickedError)
            {
                _correctCount++;
                _feedbackLabel.text = "Benar! Kesalahan ditemukan!";
                _feedbackLabel.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);

                // Highlight the wrong one in red
                var errorCard = clickedLeft ? _leftCard : _rightCard;
                errorCard.style.borderTopColor = new Color(0.91f, 0.27f, 0.38f, 1f);
                errorCard.style.borderBottomColor = new Color(0.91f, 0.27f, 0.38f, 1f);
                errorCard.style.borderLeftColor = new Color(0.91f, 0.27f, 0.38f, 1f);
                errorCard.style.borderRightColor = new Color(0.91f, 0.27f, 0.38f, 1f);

                _currentRound++;
                if (_currentRound >= _rounds.Count)
                {
                    _finished = true;
                    float completionTime = Time.time - _startTime;
                    OnCompleted?.Invoke(new MinigameResult { Success = true, CompletionTime = completionTime });
                }
                else
                {
                    // Brief pause then show next
                    SetCardsEnabled(false);
                    _leftCard.schedule.Execute(() =>
                    {
                        SetCardsEnabled(true);
                        ShowCurrentRound();
                        _feedbackLabel.text = "";
                    }).ExecuteLater(400);
                }
            }
            else
            {
                _feedbackLabel.text = "Salah! Itu data yang benar. Tunggu...";
                _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);

                // Highlight clicked card in yellow (wrong pick)
                var wrongPick = clickedLeft ? _leftCard : _rightCard;
                wrongPick.style.borderTopColor = new Color(0.95f, 0.7f, 0.2f, 1f);
                wrongPick.style.borderBottomColor = new Color(0.95f, 0.7f, 0.2f, 1f);
                wrongPick.style.borderLeftColor = new Color(0.95f, 0.7f, 0.2f, 1f);
                wrongPick.style.borderRightColor = new Color(0.95f, 0.7f, 0.2f, 1f);

                _cooldownRemaining = WRONG_COOLDOWN;
                SetCardsEnabled(false);
            }
        }

        private void SetCardsEnabled(bool enabled)
        {
            _leftCard?.SetEnabled(enabled);
            _rightCard?.SetEnabled(enabled);
            if (_leftCard != null) _leftCard.style.opacity = enabled ? 1f : 0.5f;
            if (_rightCard != null) _rightCard.style.opacity = enabled ? 1f : 0.5f;
        }

        private void ResetCardBorders()
        {
            var neutral = new Color(0.2f, 0.25f, 0.4f, 1f);
            foreach (var c in new[] { _leftCard, _rightCard })
            {
                if (c == null) continue;
                c.style.borderTopColor = neutral;
                c.style.borderBottomColor = neutral;
                c.style.borderLeftColor = neutral;
                c.style.borderRightColor = neutral;
            }
        }

        public void Cleanup()
        {
            _finished = true;
        }
    }
}
