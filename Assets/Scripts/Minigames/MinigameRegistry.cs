using System;
using System.Collections.Generic;

namespace Overworked.Minigames
{
    public class MinigameRegistry
    {
        private readonly Dictionary<string, Func<string, IMinigame>> _factories = new();

        public MinigameRegistry()
        {
            Register("typing_test", difficulty => new TypingTestMinigame(difficulty));
            Register("number_crunch", difficulty => new NumberCrunchMinigame(difficulty));
            Register("inbox_sort", difficulty => new InboxSortMinigame(difficulty));
            Register("spot_error", difficulty => new SpotErrorMinigame(difficulty));
            Register("approval_rush", difficulty => new ApprovalRushMinigame(difficulty));
        }

        public void Register(string minigameId, Func<string, IMinigame> factory)
        {
            _factories[minigameId] = factory;
        }

        public IMinigame Create(string minigameId, string difficulty)
        {
            if (_factories.TryGetValue(minigameId, out var factory))
                return factory(difficulty ?? "medium");
            return null;
        }
    }
}
