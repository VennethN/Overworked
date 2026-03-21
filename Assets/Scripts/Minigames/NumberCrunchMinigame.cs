using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.Minigames
{
    public class NumberCrunchMinigame : IMinigame
    {
        public string MinigameId => "number_crunch";
        public event Action<MinigameResult> OnCompleted;

        private readonly string _difficulty;
        private int _totalProblems;
        private int _currentProblem;
        private int _correctAnswer;
        private float _timeLimit;
        private float _elapsed;
        private bool _finished;
        private float _startTime;
        private float _cooldownRemaining;
        private const float WRONG_COOLDOWN = 1.0f;

        private Label _problemLabel;
        private Label _progressLabel;
        private Label _timerLabel;
        private Label _feedbackLabel;
        private VisualElement _progressFill;
        private VisualElement _choicesContainer;

        public NumberCrunchMinigame(string difficulty)
        {
            _difficulty = difficulty ?? "medium";
        }

        public void BuildUI(VisualElement container)
        {
            container.Clear();

            switch (_difficulty)
            {
                case "easy":
                    _totalProblems = 1;
                    _timeLimit = 10f;
                    break;
                case "hard":
                    _totalProblems = 2;
                    _timeLimit = 12f;
                    break;
                default:
                    _totalProblems = 3;
                    _timeLimit = 13f;
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
            card.style.paddingTop = 28;
            card.style.paddingBottom = 28;
            card.style.paddingLeft = 36;
            card.style.paddingRight = 36;
            card.style.width = 420;
            card.style.alignItems = Align.Center;

            var title = new Label("Hitung Cepat");
            title.style.fontSize = 22;
            title.style.color = new Color(0.95f, 0.7f, 0.2f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            card.Add(title);

            _progressLabel = new Label($"Soal 1 / {_totalProblems}");
            _progressLabel.style.fontSize = 13;
            _progressLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _progressLabel.style.marginBottom = 16;
            card.Add(_progressLabel);

            _problemLabel = new Label("");
            _problemLabel.style.fontSize = 32;
            _problemLabel.style.color = Color.white;
            _problemLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _problemLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _problemLabel.style.marginBottom = 20;
            _problemLabel.style.paddingTop = 12;
            _problemLabel.style.paddingBottom = 12;
            _problemLabel.style.paddingLeft = 20;
            _problemLabel.style.paddingRight = 20;
            _problemLabel.style.backgroundColor = new Color(0.06f, 0.08f, 0.16f, 1f);
            _problemLabel.style.borderTopLeftRadius = 8;
            _problemLabel.style.borderTopRightRadius = 8;
            _problemLabel.style.borderBottomLeftRadius = 8;
            _problemLabel.style.borderBottomRightRadius = 8;
            _problemLabel.style.width = Length.Percent(100);
            card.Add(_problemLabel);

            _choicesContainer = new VisualElement();
            _choicesContainer.style.flexDirection = FlexDirection.Row;
            _choicesContainer.style.flexWrap = Wrap.Wrap;
            _choicesContainer.style.justifyContent = Justify.Center;
            _choicesContainer.style.marginBottom = 16;
            card.Add(_choicesContainer);

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
            _progressFill.style.backgroundColor = new Color(0.95f, 0.7f, 0.2f, 1f);
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
            _currentProblem = 0;
            _cooldownRemaining = 0f;
            _startTime = Time.time;
            GenerateProblem();
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
                    SetChoicesEnabled(true);
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

        private void GenerateProblem()
        {
            int a, b;
            string op;

            switch (_difficulty)
            {
                case "easy":
                    a = UnityEngine.Random.Range(1, 20);
                    b = UnityEngine.Random.Range(1, 20);
                    op = "+";
                    _correctAnswer = a + b;
                    break;
                case "hard":
                    int opType = UnityEngine.Random.Range(0, 3);
                    if (opType == 0)
                    {
                        a = UnityEngine.Random.Range(10, 100);
                        b = UnityEngine.Random.Range(10, 100);
                        op = "+";
                        _correctAnswer = a + b;
                    }
                    else if (opType == 1)
                    {
                        a = UnityEngine.Random.Range(20, 100);
                        b = UnityEngine.Random.Range(1, a);
                        op = "-";
                        _correctAnswer = a - b;
                    }
                    else
                    {
                        a = UnityEngine.Random.Range(2, 15);
                        b = UnityEngine.Random.Range(2, 15);
                        op = "\u00d7";
                        _correctAnswer = a * b;
                    }
                    break;
                default: // medium
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        a = UnityEngine.Random.Range(5, 50);
                        b = UnityEngine.Random.Range(5, 50);
                        op = "+";
                        _correctAnswer = a + b;
                    }
                    else
                    {
                        a = UnityEngine.Random.Range(10, 50);
                        b = UnityEngine.Random.Range(1, a);
                        op = "-";
                        _correctAnswer = a - b;
                    }
                    break;
            }

            _problemLabel.text = $"{a} {op} {b} = ?";
            _progressLabel.text = $"Soal {_currentProblem + 1} / {_totalProblems}";

            // Generate choices
            _choicesContainer.Clear();
            int correctSlot = UnityEngine.Random.Range(0, 4);
            for (int i = 0; i < 4; i++)
            {
                int value;
                if (i == correctSlot)
                {
                    value = _correctAnswer;
                }
                else
                {
                    int offset = UnityEngine.Random.Range(1, 10) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
                    value = _correctAnswer + offset;
                    if (value == _correctAnswer) value += 1;
                }

                int capturedValue = value;
                var btn = new Button(() => OnChoiceClicked(capturedValue));
                btn.text = value.ToString();
                btn.style.fontSize = 20;
                btn.style.width = 80;
                btn.style.height = 48;
                btn.style.marginLeft = 6;
                btn.style.marginRight = 6;
                btn.style.marginBottom = 8;
                btn.style.backgroundColor = new Color(0.18f, 0.22f, 0.36f, 1f);
                btn.style.color = Color.white;
                btn.style.borderTopLeftRadius = 8;
                btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8;
                btn.style.borderBottomRightRadius = 8;
                btn.style.borderTopWidth = 0;
                btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0;
                btn.style.borderRightWidth = 0;
                _choicesContainer.Add(btn);
            }
        }

        private void OnChoiceClicked(int value)
        {
            if (_finished || _cooldownRemaining > 0f) return;

            if (value == _correctAnswer)
            {
                _currentProblem++;
                if (_currentProblem >= _totalProblems)
                {
                    _finished = true;
                    float completionTime = Time.time - _startTime;
                    OnCompleted?.Invoke(new MinigameResult { Success = true, CompletionTime = completionTime });
                }
                else
                {
                    _feedbackLabel.text = "Benar!";
                    _feedbackLabel.style.color = new Color(0.3f, 0.85f, 0.45f, 1f);
                    GenerateProblem();
                }
            }
            else
            {
                _feedbackLabel.text = "Salah! Tunggu...";
                _feedbackLabel.style.color = new Color(0.91f, 0.27f, 0.38f, 1f);
                _cooldownRemaining = WRONG_COOLDOWN;
                SetChoicesEnabled(false);
            }
        }

        private void SetChoicesEnabled(bool enabled)
        {
            if (_choicesContainer == null) return;
            foreach (var child in _choicesContainer.Children())
            {
                if (child is Button btn)
                {
                    btn.SetEnabled(enabled);
                    btn.style.opacity = enabled ? 1f : 0.4f;
                }
            }
        }

        public void Cleanup()
        {
            _finished = true;
        }
    }
}
