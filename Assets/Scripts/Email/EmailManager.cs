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

        // Always loaded regardless of Inspector config — follow-up emails depend on this
        private static readonly string[] RequiredPaths = {
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
            // Ensure required paths (like responses) are always loaded even if missing from Inspector
            _database.LoadFromResources(RequiredPaths);

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

            var instance = new EmailInstance(definition, Time.time);
            _inbox.Add(instance);
            GameEvents.FireEmailReceived(instance);
        }

        public void ScheduleFollowUp(FollowUp followUp)
        {
            if (followUp == null || string.IsNullOrEmpty(followUp.emailId))
            {
                Debug.Log($"[FollowUp Debug] ScheduleFollowUp called with null/empty followUp");
                return;
            }

            Debug.Log($"[FollowUp Debug] Scheduling '{followUp.emailId}' in {followUp.delaySeconds}s");

            EmailDefinition def = _database.GetById(followUp.emailId);
            if (def == null)
            {
                Debug.LogWarning($"[FollowUp Debug] FAILED: email '{followUp.emailId}' not found in database! Check emails_responses.json");
                return;
            }

            Debug.Log($"[FollowUp Debug] Found in database, starting coroutine for '{followUp.emailId}'");
            StartCoroutine(DeliverFollowUpAfterDelay(def, followUp.delaySeconds));
        }

        private IEnumerator DeliverFollowUpAfterDelay(EmailDefinition definition, float delay)
        {
            Debug.Log($"[FollowUp Debug] Waiting {delay}s to deliver '{definition.id}'...");
            yield return new WaitForSeconds(delay);
            Debug.Log($"[FollowUp Debug] Delivering '{definition.id}' NOW");
            ReceiveEmail(definition);
        }

        public void OpenEmail(string instanceId)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email == null) return;

            _actionHandler.HandleOpen(email);

            // Buffer story flag on read (flushed to disk on day completion)
            if (!string.IsNullOrEmpty(email.Definition.setFlagOnRead))
                Core.SaveManager.AddPendingFlag(email.Definition.setFlagOnRead);
        }

        public ReplyResult ReplyToEmail(string instanceId, int replyIndex)
        {
            EmailInstance email = FindEmail(instanceId);
            if (email == null)
                return new ReplyResult { IsCorrect = false, ScoreChange = 0, FeedbackText = "Email not found." };

            ReplyResult result = _actionHandler.HandleReply(email, replyIndex);

            // Process reply option: follow-up and story flag
            ReplyOption[] options = email.Definition.replyOptions;
            if (options != null && replyIndex >= 0 && replyIndex < options.Length)
            {
                var chosen = options[replyIndex];
                Debug.Log($"[FollowUp Debug] Reply on '{email.Definition.id}' idx={replyIndex} hasFollowUp={chosen.followUp != null} followUpId={(chosen.followUp != null ? chosen.followUp.emailId : "null")}");
                ScheduleFollowUp(chosen.followUp);

                // Buffer story flag (flushed to disk on day completion)
                if (!string.IsNullOrEmpty(chosen.setFlag))
                    Core.SaveManager.AddPendingFlag(chosen.setFlag);
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
