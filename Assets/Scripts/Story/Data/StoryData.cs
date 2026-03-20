using System;

namespace Overworked.Story.Data
{
    [Serializable]
    public class StoryCollection
    {
        public DayDefinition[] days;
    }

    [Serializable]
    public class DayDefinition
    {
        public int dayNumber;
        public string title;
        public float dayLengthSeconds;
        public int scoreGoal;
        public float difficulty;
        public DialogueLine[] preDialogue;
        public PostDayDialogue postDialogue;
        public string spawnRulesOverride;
        public ScriptedEmail[] scriptedEmails;
        public string specialEmailJsonPath;
        public string[] availableEmailPools;
        public int unlockedAfterDay;
    }

    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string avatar;
        public string text;
    }

    [Serializable]
    public class PostDayDialogue
    {
        public DialogueLine[] pass;
        public DialogueLine[] fail;
    }

    [Serializable]
    public class ScriptedEmail
    {
        public string emailId;
        public float triggerAtSeconds;
    }
}
