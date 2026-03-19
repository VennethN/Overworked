using System;

namespace Overworked.Email.Data
{
    [Serializable]
    public enum EmailType { Reply, Task, Spam, Info }

    [Serializable]
    public enum EmailPriority { None, Low, Medium, High, Critical }

    [Serializable]
    public enum EmailCategory { Utama, Pekerjaan, Promosi, Sosial }

    [Serializable]
    public class FollowUp
    {
        public string emailId;
        public float delaySeconds;
    }

    [Serializable]
    public class ReplyOption
    {
        public string text;
        public bool isCorrect;
        public int scoreModifier;
        public FollowUp followUp;
    }

    [Serializable]
    public class TaskTrigger
    {
        public string taskId;
        public string description;
        public bool autoCompleteOnOpen;
        public float timeoutSeconds;
        public FollowUp followUp;
    }

    [Serializable]
    public class EmailDefinition
    {
        public string id;
        public string sender;
        public string senderAddress;
        public string department;
        public string subject;
        public string body;
        public string type;
        public string priority;
        public string category;
        public float expirationSeconds;
        public string[] tags;
        public ReplyOption[] replyOptions;
        public TaskTrigger taskTrigger;
        public FollowUp expiredFollowUp;

        [NonSerialized] public EmailType parsedType;
        [NonSerialized] public EmailPriority parsedPriority;
        [NonSerialized] public EmailCategory parsedCategory;

        public void ParseEnums()
        {
            parsedType = type?.ToLower() switch
            {
                "reply" => EmailType.Reply,
                "task" => EmailType.Task,
                "spam" => EmailType.Spam,
                "info" => EmailType.Info,
                _ => EmailType.Info
            };

            parsedPriority = priority?.ToLower() switch
            {
                "critical" => EmailPriority.Critical,
                "high" => EmailPriority.High,
                "medium" => EmailPriority.Medium,
                "low" => EmailPriority.Low,
                _ => EmailPriority.None
            };

            parsedCategory = category?.ToLower() switch
            {
                "pekerjaan" => EmailCategory.Pekerjaan,
                "promosi" => EmailCategory.Promosi,
                "sosial" => EmailCategory.Sosial,
                _ => EmailCategory.Utama
            };
        }
    }

    [Serializable]
    public class EmailCollection
    {
        public EmailDefinition[] emails;
    }
}
