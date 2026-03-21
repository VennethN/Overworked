using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.Minigames
{
    public class ApprovalRushMinigame : IMinigame
    {
        public string MinigameId => "approval_rush";
        public event Action<MinigameResult> OnCompleted;

        private struct Document
        {
            public string Description;
            public string Amount;
            public long AmountValue;
            public bool ShouldApprove;
        }

        private static readonly string[] ItemNames =
        {
            "Pembelian ATK Kantor", "Sewa Gedung Tahunan", "Laptop Karyawan Baru",
            "Renovasi Lantai 3", "Langganan Software", "Mobil Operasional",
            "Training Karyawan", "Catering Meeting", "Server Rack Baru",
            "Meja Kerja Ergonomis", "Audit Software", "Firewall License",
            "Payroll System Update", "AC Ruang Meeting", "Cloud Backup",
            "Monitor 4K", "Printer Laser", "Kursi Ergonomis",
            "Lisensi Antivirus", "Domain Hosting", "UPS Backup",
            "SSL Certificate", "Konsultan Pajak", "Maintenance AC",
            "Sewa Printer", "Dekorasi Lobby", "Kontrak Vendor",
        };

        private static readonly string[] Divisions =
        {
            "IT", "Finance", "HR", "Marketing", "Sales", "GA", "Legal", "Engineering"
        };

        private static string FormatRupiah(long val)
        {
            return $"Rp {val:N0}".Replace(",", ".");
        }

        private struct RuleConfig
        {
            public string Operator; // ">", ">=", "<", "<="
            public long Threshold;
            public string RuleText;
        }

        private static readonly RuleConfig[] EasyRules =
        {
            new() { Operator = ">", Threshold = 50_000_000, RuleText = "Tolak jika > Rp 50.000.000" },
            new() { Operator = ">", Threshold = 30_000_000, RuleText = "Tolak jika > Rp 30.000.000" },
            new() { Operator = ">=", Threshold = 25_000_000, RuleText = "Tolak jika >= Rp 25.000.000" },
            new() { Operator = "<", Threshold = 10_000_000, RuleText = "Tolak jika < Rp 10.000.000" },
            new() { Operator = "<=", Threshold = 15_000_000, RuleText = "Tolak jika <= Rp 15.000.000" },
            new() { Operator = ">", Threshold = 75_000_000, RuleText = "Tolak jika > Rp 75.000.000" },
            new() { Operator = ">=", Threshold = 40_000_000, RuleText = "Tolak jika >= Rp 40.000.000" },
        };

        private static bool ShouldReject(string op, long amount, long threshold)
        {
            return op switch
            {
                ">" => amount > threshold,
                ">=" => amount >= threshold,
                "<" => amount < threshold,
                "<=" => amount <= threshold,
                _ => false
            };
        }

        private void GenerateEasyDocs(int count)
        {
            var rule = EasyRules[UnityEngine.Random.Range(0, EasyRules.Length)];
            _ruleText = $"ATURAN: {rule.RuleText}";
            _docs.Clear();

            for (int i = 0; i < count + 4; i++)
            {
                long val = UnityEngine.Random.Range(1, 200) * 500_000L;
                bool reject = ShouldReject(rule.Operator, val, rule.Threshold);
                _docs.Add(new Document
                {
                    Description = ItemNames[UnityEngine.Random.Range(0, ItemNames.Length)],
                    Amount = FormatRupiah(val),
                    AmountValue = val,
                    ShouldApprove = !reject
                });
            }

            // Shuffle and trim
            for (int i = _docs.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_docs[i], _docs[j]) = (_docs[j], _docs[i]);
            }
            while (_docs.Count > count) _docs.RemoveAt(_docs.Count - 1);
        }

        private void GenerateMediumDocs(int count)
        {
            // Pick 2 "approved" divisions randomly
            var shuffledDiv = new List<string>(Divisions);
            for (int i = shuffledDiv.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffledDiv[i], shuffledDiv[j]) = (shuffledDiv[j], shuffledDiv[i]);
            }
            string div1 = shuffledDiv[0], div2 = shuffledDiv[1];
            _ruleText = $"ATURAN: Setujui HANYA dari Divisi {div1} atau {div2}";
            _docs.Clear();

            for (int i = 0; i < count + 4; i++)
            {
                string div = Divisions[UnityEngine.Random.Range(0, Divisions.Length)];
                long val = UnityEngine.Random.Range(1, 100) * 1_000_000L;
                bool approved = (div == div1 || div == div2);
                _docs.Add(new Document
                {
                    Description = $"{ItemNames[UnityEngine.Random.Range(0, ItemNames.Length)]}\nDivisi: {div}",
                    Amount = FormatRupiah(val),
                    AmountValue = val,
                    ShouldApprove = approved
                });
            }

            for (int i = _docs.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_docs[i], _docs[j]) = (_docs[j], _docs[i]);
            }
            while (_docs.Count > count) _docs.RemoveAt(_docs.Count - 1);
        }

        private void GenerateHardDocs(int count)
        {
            var rule = EasyRules[UnityEngine.Random.Range(0, EasyRules.Length)];
            _ruleText = $"ATURAN: Tolak jika expired (< 2026) ATAU {rule.RuleText.Replace("Tolak jika ", "")}";
            _docs.Clear();

            for (int i = 0; i < count + 4; i++)
            {
                long val = UnityEngine.Random.Range(1, 120) * 1_000_000L;
                int year = UnityEngine.Random.Range(2024, 2028);
                int month = UnityEngine.Random.Range(1, 13);
                string expDate = $"{year}-{month:D2}-15";
                bool expired = year < 2026;
                bool overBudget = ShouldReject(rule.Operator, val, rule.Threshold);

                _docs.Add(new Document
                {
                    Description = $"{ItemNames[UnityEngine.Random.Range(0, ItemNames.Length)]}\nExp: {expDate}",
                    Amount = FormatRupiah(val),
                    AmountValue = val,
                    ShouldApprove = !expired && !overBudget
                });
            }

            for (int i = _docs.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_docs[i], _docs[j]) = (_docs[j], _docs[i]);
            }
            while (_docs.Count > count) _docs.RemoveAt(_docs.Count - 1);
        }

        private readonly string _difficulty;
        private int _totalDocs;
        private int _currentDoc;
        private int _correctCount;
        private float _timeLimit;
        private float _elapsed;
        private bool _finished;
        private float _startTime;
        private float _cooldownRemaining;
        private const float WRONG_COOLDOWN = 2.0f;

        private List<Document> _docs = new();
        private string _ruleText;

        private Label _ruleLabel;
        private Label _progressLabel;
        private Label _docDescription;
        private Label _docAmount;
        private Label _timerLabel;
        private Label _feedbackLabel;
        private VisualElement _progressFill;
        private Button _approveBtn;
        private Button _rejectBtn;

        public ApprovalRushMinigame(string difficulty)
        {
            _difficulty = difficulty ?? "medium";
        }

        public void BuildUI(VisualElement container)
        {
            container.Clear();

            switch (_difficulty)
            {
                case "easy":
                    _totalDocs = 3;
                    _timeLimit = 20f;
                    GenerateEasyDocs(_totalDocs);
                    break;
                case "hard":
                    _totalDocs = 3;
                    _timeLimit = 25f;
                    GenerateHardDocs(_totalDocs);
                    break;
                default:
                    _totalDocs = 3;
                    _timeLimit = 22f;
                    GenerateMediumDocs(_totalDocs);
                    break;
            }

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
            card.style.width = 440;
            card.style.alignItems = Align.Center;

            var title = new Label("Approval Rush");
            title.style.fontSize = 22;
            title.style.color = new Color(0.3f, 0.85f, 0.95f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            card.Add(title);

            // Rule display (always visible)
            _ruleLabel = new Label(_ruleText);
            _ruleLabel.style.fontSize = 12;
            _ruleLabel.style.color = new Color(0.95f, 0.7f, 0.2f, 1f);
            _ruleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _ruleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _ruleLabel.style.whiteSpace = WhiteSpace.Normal;
            _ruleLabel.style.marginBottom = 12;
            _ruleLabel.style.paddingTop = 6;
            _ruleLabel.style.paddingBottom = 6;
            _ruleLabel.style.paddingLeft = 10;
            _ruleLabel.style.paddingRight = 10;
            _ruleLabel.style.backgroundColor = new Color(0.95f, 0.7f, 0.2f, 0.1f);
            _ruleLabel.style.borderTopLeftRadius = 4;
            _ruleLabel.style.borderTopRightRadius = 4;
            _ruleLabel.style.borderBottomLeftRadius = 4;
            _ruleLabel.style.borderBottomRightRadius = 4;
            card.Add(_ruleLabel);

            _progressLabel = new Label($"Dokumen 1 / {_totalDocs}");
            _progressLabel.style.fontSize = 13;
            _progressLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _progressLabel.style.marginBottom = 12;
            card.Add(_progressLabel);

            // Document card
            var docCard = new VisualElement();
            docCard.style.width = Length.Percent(100);
            docCard.style.paddingTop = 16;
            docCard.style.paddingBottom = 16;
            docCard.style.paddingLeft = 16;
            docCard.style.paddingRight = 16;
            docCard.style.backgroundColor = new Color(0.06f, 0.08f, 0.16f, 1f);
            docCard.style.borderTopLeftRadius = 8;
            docCard.style.borderTopRightRadius = 8;
            docCard.style.borderBottomLeftRadius = 8;
            docCard.style.borderBottomRightRadius = 8;
            docCard.style.marginBottom = 16;
            docCard.style.alignItems = Align.Center;

            _docDescription = new Label("");
            _docDescription.style.fontSize = 15;
            _docDescription.style.color = Color.white;
            _docDescription.style.unityTextAlign = TextAnchor.MiddleCenter;
            _docDescription.style.whiteSpace = WhiteSpace.Normal;
            _docDescription.style.marginBottom = 8;
            docCard.Add(_docDescription);

            _docAmount = new Label("");
            _docAmount.style.fontSize = 20;
            _docAmount.style.color = new Color(0.3f, 0.85f, 0.95f, 1f);
            _docAmount.style.unityFontStyleAndWeight = FontStyle.Bold;
            docCard.Add(_docAmount);

            card.Add(docCard);

            // Approve / Reject buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.Center;
            btnRow.style.marginBottom = 16;

            _approveBtn = new Button(() => OnDecision(true));
            _approveBtn.text = "SETUJU";
            _approveBtn.style.fontSize = 16;
            _approveBtn.style.paddingTop = 12;
            _approveBtn.style.paddingBottom = 12;
            _approveBtn.style.paddingLeft = 28;
            _approveBtn.style.paddingRight = 28;
            _approveBtn.style.marginRight = 12;
            _approveBtn.style.backgroundColor = new Color(0.15f, 0.68f, 0.38f, 1f);
            _approveBtn.style.color = Color.white;
            _approveBtn.style.borderTopLeftRadius = 8;
            _approveBtn.style.borderTopRightRadius = 8;
            _approveBtn.style.borderBottomLeftRadius = 8;
            _approveBtn.style.borderBottomRightRadius = 8;
            _approveBtn.style.borderTopWidth = 0;
            _approveBtn.style.borderBottomWidth = 0;
            _approveBtn.style.borderLeftWidth = 0;
            _approveBtn.style.borderRightWidth = 0;
            _approveBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnRow.Add(_approveBtn);

            _rejectBtn = new Button(() => OnDecision(false));
            _rejectBtn.text = "TOLAK";
            _rejectBtn.style.fontSize = 16;
            _rejectBtn.style.paddingTop = 12;
            _rejectBtn.style.paddingBottom = 12;
            _rejectBtn.style.paddingLeft = 28;
            _rejectBtn.style.paddingRight = 28;
            _rejectBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
            _rejectBtn.style.color = Color.white;
            _rejectBtn.style.borderTopLeftRadius = 8;
            _rejectBtn.style.borderTopRightRadius = 8;
            _rejectBtn.style.borderBottomLeftRadius = 8;
            _rejectBtn.style.borderBottomRightRadius = 8;
            _rejectBtn.style.borderTopWidth = 0;
            _rejectBtn.style.borderBottomWidth = 0;
            _rejectBtn.style.borderLeftWidth = 0;
            _rejectBtn.style.borderRightWidth = 0;
            _rejectBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnRow.Add(_rejectBtn);

            card.Add(btnRow);

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
            _progressFill.style.backgroundColor = new Color(0.3f, 0.85f, 0.95f, 1f);
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
            _currentDoc = 0;
            _correctCount = 0;
            _cooldownRemaining = 0f;
            _startTime = Time.time;
            ShowCurrentDoc();
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
                    SetButtonsEnabled(true);
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
                bool success = _correctCount > _docs.Count / 2;
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

        private void ShowCurrentDoc()
        {
            if (_currentDoc >= _docs.Count) return;

            var doc = _docs[_currentDoc];
            _docDescription.text = doc.Description;
            _docAmount.text = doc.Amount;
            _progressLabel.text = $"Dokumen {_currentDoc + 1} / {_totalDocs}";
        }

        private void OnDecision(bool approved)
        {
            if (_finished || _cooldownRemaining > 0f || _currentDoc >= _docs.Count) return;

            bool correct = (approved == _docs[_currentDoc].ShouldApprove);

            if (correct)
            {
                _correctCount++;
                _feedbackLabel.text = approved ? "Disetujui! Benar!" : "Ditolak! Benar!";
                _feedbackLabel.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);

                _currentDoc++;
                if (_currentDoc >= _docs.Count)
                {
                    _finished = true;
                    float completionTime = Time.time - _startTime;
                    OnCompleted?.Invoke(new MinigameResult { Success = true, CompletionTime = completionTime });
                }
                else
                {
                    ShowCurrentDoc();
                }
            }
            else
            {
                string shouldHave = _docs[_currentDoc].ShouldApprove ? "Setuju" : "Tolak";
                _feedbackLabel.text = $"Salah! Harusnya: {shouldHave}. Tunggu...";
                _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
                _cooldownRemaining = WRONG_COOLDOWN;
                SetButtonsEnabled(false);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _approveBtn?.SetEnabled(enabled);
            _rejectBtn?.SetEnabled(enabled);
            if (_approveBtn != null) _approveBtn.style.opacity = enabled ? 1f : 0.4f;
            if (_rejectBtn != null) _rejectBtn.style.opacity = enabled ? 1f : 0.4f;
        }

        public void Cleanup()
        {
            _finished = true;
        }
    }
}
