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
            public bool ShouldApprove;
        }

        // Rule: "Tolak dokumen di atas Rp 50 juta"
        private static readonly Document[] EasyDocs =
        {
            new() { Description = "Pembelian ATK Kantor", Amount = "Rp 2.500.000", ShouldApprove = true },
            new() { Description = "Sewa Gedung Tahunan", Amount = "Rp 120.000.000", ShouldApprove = false },
            new() { Description = "Laptop Karyawan Baru", Amount = "Rp 15.000.000", ShouldApprove = true },
            new() { Description = "Renovasi Lantai 3", Amount = "Rp 85.000.000", ShouldApprove = false },
            new() { Description = "Langganan Software", Amount = "Rp 8.400.000", ShouldApprove = true },
            new() { Description = "Mobil Operasional", Amount = "Rp 250.000.000", ShouldApprove = false },
            new() { Description = "Training Karyawan", Amount = "Rp 12.000.000", ShouldApprove = true },
            new() { Description = "Catering Meeting", Amount = "Rp 3.750.000", ShouldApprove = true },
        };

        // Rule: "Setujui hanya dari Divisi IT atau Finance"
        private static readonly Document[] MediumDocs =
        {
            new() { Description = "Server Rack Baru\nDivisi: IT", Amount = "Rp 45.000.000", ShouldApprove = true },
            new() { Description = "Meja Kerja Ergonomis\nDivisi: HR", Amount = "Rp 18.000.000", ShouldApprove = false },
            new() { Description = "Audit Software\nDivisi: Finance", Amount = "Rp 22.000.000", ShouldApprove = true },
            new() { Description = "Banner Event\nDivisi: Marketing", Amount = "Rp 5.000.000", ShouldApprove = false },
            new() { Description = "Firewall License\nDivisi: IT", Amount = "Rp 35.000.000", ShouldApprove = true },
            new() { Description = "Pelatihan Sales\nDivisi: Sales", Amount = "Rp 15.000.000", ShouldApprove = false },
            new() { Description = "Payroll System Update\nDivisi: Finance", Amount = "Rp 28.000.000", ShouldApprove = true },
            new() { Description = "AC Ruang Meeting\nDivisi: GA", Amount = "Rp 12.000.000", ShouldApprove = false },
            new() { Description = "Cloud Backup\nDivisi: IT", Amount = "Rp 18.500.000", ShouldApprove = true },
            new() { Description = "Dekorasi Lobby\nDivisi: GA", Amount = "Rp 8.000.000", ShouldApprove = false },
        };

        // Rule: "Tolak jika tanggal expired atau jumlah > Rp 30jt"
        private static readonly Document[] HardDocs =
        {
            new() { Description = "Kontrak Vendor\nExp: 2026-12-31", Amount = "Rp 25.000.000", ShouldApprove = true },
            new() { Description = "Sewa Printer\nExp: 2025-06-15", Amount = "Rp 8.000.000", ShouldApprove = false }, // expired
            new() { Description = "Lisensi Antivirus\nExp: 2027-01-01", Amount = "Rp 42.000.000", ShouldApprove = false }, // too high
            new() { Description = "Domain Hosting\nExp: 2026-09-30", Amount = "Rp 5.500.000", ShouldApprove = true },
            new() { Description = "Asuransi Gedung\nExp: 2024-12-31", Amount = "Rp 18.000.000", ShouldApprove = false }, // expired
            new() { Description = "UPS Backup\nExp: 2027-06-15", Amount = "Rp 28.000.000", ShouldApprove = true },
            new() { Description = "Konsultan Pajak\nExp: 2026-08-20", Amount = "Rp 55.000.000", ShouldApprove = false }, // too high
            new() { Description = "SSL Certificate\nExp: 2027-03-01", Amount = "Rp 3.200.000", ShouldApprove = true },
            new() { Description = "Maintenance AC\nExp: 2025-01-31", Amount = "Rp 12.000.000", ShouldApprove = false }, // expired
            new() { Description = "Monitor 4K\nExp: 2026-11-15", Amount = "Rp 15.000.000", ShouldApprove = true },
        };

        private readonly string _difficulty;
        private int _totalDocs;
        private int _currentDoc;
        private int _correctCount;
        private float _timeLimit;
        private float _elapsed;
        private bool _finished;
        private float _startTime;
        private float _cooldownRemaining;
        private const float WRONG_COOLDOWN = 1.0f;

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

            Document[] pool;
            switch (_difficulty)
            {
                case "easy":
                    _totalDocs = 4;
                    _timeLimit = 20f;
                    pool = EasyDocs;
                    _ruleText = "ATURAN: Tolak jika > Rp 50.000.000";
                    break;
                case "hard":
                    _totalDocs = 7;
                    _timeLimit = 25f;
                    pool = HardDocs;
                    _ruleText = "ATURAN: Tolak jika tanggal expired (< 2026) ATAU > Rp 30.000.000";
                    break;
                default:
                    _totalDocs = 5;
                    _timeLimit = 22f;
                    pool = MediumDocs;
                    _ruleText = "ATURAN: Setujui HANYA dari Divisi IT atau Finance";
                    break;
            }

            var shuffled = new List<Document>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            _docs.Clear();
            for (int i = 0; i < Mathf.Min(_totalDocs, shuffled.Count); i++)
                _docs.Add(shuffled[i]);

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
