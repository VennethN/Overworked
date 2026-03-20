using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.Minigames
{
    public class TypingTestMinigame : IMinigame
    {
        public string MinigameId => "typing_test";
        public event Action<MinigameResult> OnCompleted;

        private static readonly string[] PhrasesEasy =
        {
            "laporan sudah selesai",
            "rapat jam dua siang",
            "kirim email ke bos",
            "deadline hari jumat",
            "budget disetujui"
        };

        private static readonly string[] PhrasesMedium =
        {
            "tolong review dokumen kontrak vendor",
            "meeting dengan klien pukul tiga sore",
            "laporan keuangan kuartal tiga sudah siap",
            "persetujuan anggaran departemen IT",
            "jadwal training karyawan baru senin"
        };

        private static readonly string[] PhrasesHard =
        {
            "segera kirim laporan pengeluaran ke bagian keuangan sebelum jam lima",
            "konfirmasi kehadiran rapat darurat direksi besok pagi jam delapan",
            "persetujuan kontrak vendor baru senilai seratus dua puluh juta rupiah",
            "koordinasi dengan tim keamanan IT untuk investigasi login mencurigakan",
            "pengajuan cuti karyawan perlu tanda tangan manajer dan direktur"
        };

        private readonly string _difficulty;
        private string _targetPhrase;
        private float _timeLimit;
        private float _elapsed;
        private bool _finished;

        private Label _phraseLabel;
        private TextField _inputField;
        private Label _timerLabel;
        private Label _feedbackLabel;
        private VisualElement _progressFill;
        private float _startTime;

        public TypingTestMinigame(string difficulty)
        {
            _difficulty = difficulty ?? "medium";
        }

        public void BuildUI(VisualElement container)
        {
            container.Clear();

            // Pick phrase based on difficulty
            string[] pool;
            switch (_difficulty)
            {
                case "easy":
                    pool = PhrasesEasy;
                    _timeLimit = 15f;
                    break;
                case "hard":
                    pool = PhrasesHard;
                    _timeLimit = 20f;
                    break;
                default:
                    pool = PhrasesMedium;
                    _timeLimit = 18f;
                    break;
            }
            _targetPhrase = pool[UnityEngine.Random.Range(0, pool.Length)];

            // Root wrapper
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
            card.style.width = 500;
            card.style.alignItems = Align.Center;

            // Title
            var title = new Label("Tes Mengetik");
            title.style.fontSize = 22;
            title.style.color = new Color(0.4f, 0.65f, 1f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            card.Add(title);

            var instruction = new Label("Ketik kalimat di bawah ini dengan benar:");
            instruction.style.fontSize = 13;
            instruction.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            instruction.style.marginBottom = 16;
            card.Add(instruction);

            // Target phrase
            _phraseLabel = new Label(_targetPhrase);
            _phraseLabel.style.fontSize = 18;
            _phraseLabel.style.color = Color.white;
            _phraseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _phraseLabel.style.whiteSpace = WhiteSpace.Normal;
            _phraseLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _phraseLabel.style.marginBottom = 16;
            _phraseLabel.style.paddingLeft = 12;
            _phraseLabel.style.paddingRight = 12;
            _phraseLabel.style.paddingTop = 10;
            _phraseLabel.style.paddingBottom = 10;
            _phraseLabel.style.backgroundColor = new Color(0.06f, 0.08f, 0.16f, 1f);
            _phraseLabel.style.borderTopLeftRadius = 6;
            _phraseLabel.style.borderTopRightRadius = 6;
            _phraseLabel.style.borderBottomLeftRadius = 6;
            _phraseLabel.style.borderBottomRightRadius = 6;
            card.Add(_phraseLabel);

            // Input field
            _inputField = new TextField();
            _inputField.style.width = Length.Percent(100);
            _inputField.style.fontSize = 16;
            _inputField.style.marginBottom = 12;
            _inputField.RegisterValueChangedCallback(OnInputChanged);
            card.Add(_inputField);

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
            _progressFill.style.backgroundColor = new Color(0.4f, 0.65f, 1f, 1f);
            _progressFill.style.borderTopLeftRadius = 3;
            _progressFill.style.borderTopRightRadius = 3;
            _progressFill.style.borderBottomLeftRadius = 3;
            _progressFill.style.borderBottomRightRadius = 3;
            progressBg.Add(_progressFill);
            card.Add(progressBg);

            // Timer text
            _timerLabel = new Label($"Waktu: {_timeLimit:F1}s");
            _timerLabel.style.fontSize = 14;
            _timerLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _timerLabel.style.marginBottom = 8;
            card.Add(_timerLabel);

            // Feedback
            _feedbackLabel = new Label("");
            _feedbackLabel.style.fontSize = 14;
            _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
            card.Add(_feedbackLabel);

            root.Add(card);
            container.Add(root);
        }

        public void Start()
        {
            _elapsed = 0f;
            _finished = false;
            _startTime = Time.time;
            _inputField?.Focus();
        }

        public void Tick(float deltaTime)
        {
            if (_finished) return;

            _elapsed += deltaTime;
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

            // Color shift as time runs low
            if (ratio < 0.3f)
                _progressFill.style.backgroundColor = new Color(0.91f, 0.27f, 0.38f, 1f);
            else if (ratio < 0.6f)
                _progressFill.style.backgroundColor = new Color(0.95f, 0.7f, 0.2f, 1f);
        }

        private void OnInputChanged(ChangeEvent<string> evt)
        {
            if (_finished) return;

            string input = evt.newValue?.Trim() ?? "";
            if (string.Equals(input, _targetPhrase, StringComparison.OrdinalIgnoreCase))
            {
                _finished = true;
                float completionTime = Time.time - _startTime;
                _feedbackLabel.text = "Benar!";
                _feedbackLabel.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);
                OnCompleted?.Invoke(new MinigameResult { Success = true, CompletionTime = completionTime });
            }
        }

        public void Cleanup()
        {
            _finished = true;
        }
    }
}
