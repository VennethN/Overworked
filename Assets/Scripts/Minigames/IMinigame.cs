using System;
using UnityEngine.UIElements;

namespace Overworked.Minigames
{
    public struct MinigameResult
    {
        public bool Success;
        public float CompletionTime;
    }

    public interface IMinigame
    {
        string MinigameId { get; }
        void BuildUI(VisualElement container);
        void Start();
        void Tick(float deltaTime);
        void Cleanup();
        event Action<MinigameResult> OnCompleted;
    }
}
