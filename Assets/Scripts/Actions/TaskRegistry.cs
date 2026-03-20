using System.Collections.Generic;
using UnityEngine;
using Overworked.Email;

namespace Overworked.Actions
{
    public interface ITaskHandler
    {
        void Execute(EmailInstance email);
        bool IsComplete { get; }
        int ScoreValue { get; }
    }

    /// <summary>
    /// Simple task handler that completes immediately on execution (button press).
    /// Used as default for all task types until more complex handlers are needed.
    /// </summary>
    public class SimpleTaskHandler : ITaskHandler
    {
        public int ScoreValue { get; }
        public bool IsComplete { get; private set; }

        public SimpleTaskHandler(int scoreValue = 15)
        {
            ScoreValue = scoreValue;
        }

        public void Execute(EmailInstance email)
        {
            IsComplete = true;
        }
    }

    public class TaskRegistry
    {
        private readonly Dictionary<string, System.Func<ITaskHandler>> _handlerFactories = new();

        public TaskRegistry()
        {
            // Register default task handlers — all simple for now
            Register("approve_budget", () => new SimpleTaskHandler(20));
            Register("approve_restart", () => new SimpleTaskHandler(30));
            Register("file_report", () => new SimpleTaskHandler(15));
            Register("approve_equipment", () => new SimpleTaskHandler(15));
            Register("approve_vacation", () => new SimpleTaskHandler(10));
            Register("forward_complaint", () => new SimpleTaskHandler(20));
        }

        public void Register(string taskId, System.Func<ITaskHandler> handlerFactory)
        {
            _handlerFactories[taskId] = handlerFactory;
        }

        public bool Execute(string taskId, EmailInstance email)
        {
            if (!_handlerFactories.TryGetValue(taskId, out var factory))
            {
                Debug.LogWarning($"TaskRegistry: No handler registered for task '{taskId}'");
                return false;
            }

            ITaskHandler handler = factory();
            handler.Execute(email);
            return handler.IsComplete;
        }

        public int GetScoreValue(string taskId)
        {
            if (!_handlerFactories.TryGetValue(taskId, out var factory))
                return 0;

            return factory().ScoreValue;
        }
    }
}
