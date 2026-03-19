using Overworked.Core;
using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.Actions
{
    public class EmailActionHandler
    {
        private readonly ReplyAction _replyAction = new();
        private readonly TaskAction _taskAction;

        public EmailActionHandler(TaskRegistry taskRegistry)
        {
            _taskAction = new TaskAction(taskRegistry);
        }

        public void HandleOpen(EmailInstance email)
        {
            email.IsRead = true;
            GameEvents.FireEmailOpened(email);

            // Auto-complete tasks that resolve on open
            if (email.Definition.parsedType == EmailType.Task
                && email.Definition.taskTrigger != null
                && email.Definition.taskTrigger.autoCompleteOnOpen)
            {
                HandleTaskComplete(email);
            }
        }

        public ReplyResult HandleReply(EmailInstance email, int choiceIndex)
        {
            ReplyResult result = _replyAction.ProcessReply(email, choiceIndex);
            email.IsActedUpon = true;

            if (result.IsCorrect)
                email.IsCompleted = true;

            GameEvents.FireEmailReplied(email, result);
            return result;
        }

        public void HandleDelete(EmailInstance email)
        {
            email.IsActedUpon = true;
            GameEvents.FireEmailDeleted(email);
        }

        public void HandleTaskComplete(EmailInstance email)
        {
            bool success = _taskAction.StartTask(email);
            email.IsActedUpon = true;

            if (success)
            {
                email.IsCompleted = true;
                GameEvents.FireTaskCompleted(email);
            }
            else
            {
                GameEvents.FireTaskFailed(email);
            }
        }
    }
}
