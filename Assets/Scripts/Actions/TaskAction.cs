using Overworked.Email;

namespace Overworked.Actions
{
    public class TaskAction
    {
        private readonly TaskRegistry _registry;

        public TaskAction(TaskRegistry registry)
        {
            _registry = registry;
        }

        public bool StartTask(EmailInstance email)
        {
            if (email.Definition.taskTrigger == null)
                return false;

            string taskId = email.Definition.taskTrigger.taskId;
            return _registry.Execute(taskId, email);
        }
    }
}
