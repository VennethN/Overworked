using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Overworked.Actions;
using Overworked.Core;
using Overworked.Email.Data;
using Overworked.Minigames;

namespace Overworked.Email
{
    public class EmailManager : MonoBehaviour
    {
        public static EmailManager Instance { get; private set; }

        [SerializeField] private int maxInboxSize = 30;
        [SerializeField] private string[] emailJsonPaths = {
            "Data/Emails/emails_general",
            "Data/Emails/emails_hr",
            "Data/Emails/emails_spam",
            "Data/Emails/emails_responses"
        };

        private readonly List<EmailInstance> _inbox = new();
        private EmailDatabase _database;
        private EmailActionHandler _actionHandler;
        private TaskRegistry _taskRegistry;
        private MinigameRegistry _minigameRegistry;

        public IReadOnlyList<EmailInstance> Inbox => _inbox;
        public EmailDatabase Database => _database;
        public int UnreadCount => _inbox.Count(e => !e.IsRead);
        public int ActiveCount => _inbox.Count(e => !e.IsExpired && !e.IsCompleted);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _database = new EmailDatabase();
            _database.LoadFromResources(emailJsonPaths);

            _taskRegistry = new TaskRegistry();
            _actionHandler = new EmailActionHandler(_taskRegistry);
            _minigameRegistry = new MinigameRegistry();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _inbox.Count - 1; i >= 0; i--)
            {
                EmailInstance email = _inbox[i];
                if (email.IsExpired || email.IsCompleted) continue;

                bool wasAlive = !email.IsExpired;
                email.Tick(dt);

                if (wasAlive && email.IsExpired)
                {
                    ExpireEmail(email);
                }
            }
        }

        public void ReceiveEmail(EmailDefinition definition)
        {
            if (_inbox.Count >= maxInboxSize)
            {
                Debug.LogWarning("EmailManager: Inbox full, cannot receive more emails.");
                return;
            }

            var instance = new EmailInstance(definition, Time.time);
            _inbox.Add(instance);
            GameEvents.FireEmailReceived(instance);
        }

        public void ScheduleFollowUp(FollowUp followUp)
        {
            if (followUp == null || string.IsNullOrEmpty(followUp.emailId)) return;

            EmailDefinition def = _database.GetById(followUp.emailId);
            if (def == null)
            {
                Debug.LogWarning($"EmailManager: Follow-up email '{followUp.emailId}' not found in database.");
                return;
            }

            StartCoroutine(DeliverFollowUpAfterDelay(def, followUp.delaySeconds));
        }

        private IEnumerator DeliverFollowUpAfterDelay(EmailDefinition definition, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReceiveEmail(definition);
        }

        public void OpenEmail(string instanceId)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email == null) return;

            _actionHandler.HandleOpen(email);
        }

        public ReplyResult ReplyToEmail(string instanceId, int replyIndex)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email == null)
                return new ReplyResult { IsCorrect = false, ScoreChange = 0, FeedbackText = "Email not found." };

            ReplyResult result = _actionHandler.HandleReply(email, replyIndex);

            // Schedule follow-up if the chosen reply has one
            ReplyOption[] options = email.Definition.replyOptions;
            if (options != null && replyIndex >= 0 && replyIndex < options.Length)
            {
                ScheduleFollowUp(options[replyIndex].followUp);
            }

            return result;
        }

        public void DeleteEmail(string instanceId)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email == null) return;

            _actionHandler.HandleDelete(email);
            _inbox.Remove(email);
        }

        public void CompleteTask(string instanceId)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email == null) return;

            _actionHandler.HandleTaskComplete(email);

            // Schedule follow-up if the task trigger has one
            if (email.Definition.taskTrigger?.followUp != null)
            {
                ScheduleFollowUp(email.Definition.taskTrigger.followUp);
            }
        }

        public IMinigame GetMinigame(string instanceId)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email?.Definition.taskTrigger?.minigameId == null) return null;

            var trigger = email.Definition.taskTrigger;
            return _minigameRegistry.Create(trigger.minigameId, trigger.minigameDifficulty);
        }

        public void LoadAdditionalEmails(string[] paths)
        {
            _database.LoadFromResources(paths);
        }

        public void ClearInbox()
        {
            StopAllCoroutines();
            _inbox.Clear();
        }

        private void ExpireEmail(EmailInstance email)
        {
            GameEvents.FireEmailExpired(email);

            // Schedule follow-up on expiry if defined
            if (email.Definition.expiredFollowUp != null)
            {
                ScheduleFollowUp(email.Definition.expiredFollowUp);
            }
        }

        private EmailInstance FindEmail(string instanceId)
        {
            for (int i = 0; i < _inbox.Count; i++)
            {
                if (_inbox[i].InstanceId == instanceId)
                    return _inbox[i];
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
