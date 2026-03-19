using Overworked.Email;
using Overworked.Email.Data;

namespace Overworked.Actions
{
    public struct ReplyResult
    {
        public bool IsCorrect;
        public int ScoreChange;
        public string FeedbackText;
    }

    public class ReplyAction
    {
        public ReplyResult ProcessReply(EmailInstance email, int choiceIndex)
        {
            ReplyOption[] options = email.Definition.replyOptions;

            if (options == null || choiceIndex < 0 || choiceIndex >= options.Length)
            {
                return new ReplyResult
                {
                    IsCorrect = false,
                    ScoreChange = 0,
                    FeedbackText = "Invalid reply."
                };
            }

            ReplyOption option = options[choiceIndex];
            return new ReplyResult
            {
                IsCorrect = option.isCorrect,
                ScoreChange = option.scoreModifier,
                FeedbackText = option.isCorrect ? "Good call!" : "That wasn't right..."
            };
        }
    }
}
