using System;

namespace Overworked.Scoring
{
    [Serializable]
    public struct ScoreData
    {
        public int totalScore;
        public int correctReplies;
        public int wrongReplies;
        public int tasksCompleted;
        public int tasksFailed;
        public int emailsExpired;
        public int spamDeleted;
        public int spamReplied;
        public int highestStreak;
    }
}
