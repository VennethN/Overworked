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
        /// <summary>
        /// When non-empty, random spawns (spawn_rules intervals/events) pick only from these email ids,
        /// still filtered by each rule's type and tags. Omitted or empty = use availableEmailPools as before.
        /// </summary>
        public string[] spawnEmailIds;
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
