using Overworked.Email.Data;

namespace Overworked.Email
{
    public class EmailInstance
    {
        public string InstanceId { get; }
        public EmailDefinition Definition { get; }
        public float TimeRemaining { get; set; }
        public bool IsRead { get; set; }
        public bool IsExpired { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsActedUpon { get; set; }
        public float ReceivedAtGameTime { get; }

        public bool CanExpire => Definition.expirationSeconds > 0f;

        public EmailInstance(EmailDefinition definition, float gameTime)
        {
            InstanceId = System.Guid.NewGuid().ToString();
            Definition = definition;
            TimeRemaining = definition.expirationSeconds;
            ReceivedAtGameTime = gameTime;
        }

        public void Tick(float deltaTime)
        {
            if (!CanExpire || IsExpired || IsCompleted || IsActedUpon)
                return;

            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsExpired = true;
            }
        }
    }
}
