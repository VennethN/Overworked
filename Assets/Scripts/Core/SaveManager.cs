using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Overworked.Core
{
    [Serializable]
    public class SaveData
    {
        public string playerName = "Pegawai Baru";
        public int lastCompletedDay;
        public int arcadeHighScore;
        public List<DaySaveEntry> dayScores = new();
        public List<string> storyFlags = new();
        public List<string> endingsUnlocked = new();

        public bool HasFlag(string flag) => storyFlags.Contains(flag);
        public void SetFlag(string flag) { if (!storyFlags.Contains(flag)) storyFlags.Add(flag); }
        public void RemoveFlag(string flag) => storyFlags.Remove(flag);

        public bool HasEnding(string ending) => endingsUnlocked.Contains(ending);
        public void UnlockEnding(string ending) { if (!endingsUnlocked.Contains(ending)) endingsUnlocked.Add(ending); }

        public void ResetStory()
        {
            lastCompletedDay = 0;
            dayScores.Clear();
            storyFlags.Clear();
            // endingsUnlocked intentionally kept — achievements are permanent
        }

        public int GetBestScore(int dayNumber)
        {
            for (int i = 0; i < dayScores.Count; i++)
            {
                if (dayScores[i].dayNumber == dayNumber)
                    return dayScores[i].bestScore;
            }
            return 0;
        }

        public void SetBestScore(int dayNumber, int score)
        {
            for (int i = 0; i < dayScores.Count; i++)
            {
                if (dayScores[i].dayNumber == dayNumber)
                {
                    if (score > dayScores[i].bestScore)
                        dayScores[i] = new DaySaveEntry { dayNumber = dayNumber, bestScore = score };
                    return;
                }
            }
            dayScores.Add(new DaySaveEntry { dayNumber = dayNumber, bestScore = score });
        }

        public bool IsDayUnlocked(int dayNumber, int unlockedAfterDay)
        {
            if (unlockedAfterDay <= 0) return true;
            return lastCompletedDay >= unlockedAfterDay;
        }
    }

    [Serializable]
    public struct DaySaveEntry
    {
        public int dayNumber;
        public int bestScore;
    }

    public static class SaveManager
    {
        private const byte SAVE_VERSION = 4;
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "overworked.sav");

        private static SaveData _cached;

        // Pending flags: buffered during gameplay, flushed to SaveData on day completion
        private static readonly List<string> PendingFlags = new();

        public static void AddPendingFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !PendingFlags.Contains(flag))
                PendingFlags.Add(flag);
        }

        public static bool HasPendingOrSavedFlag(string flag)
        {
            return PendingFlags.Contains(flag) || Load().HasFlag(flag);
        }

        public static void FlushPendingFlags()
        {
            if (PendingFlags.Count == 0) return;
            var save = Load();
            foreach (var flag in PendingFlags)
                save.SetFlag(flag);
            Save(save);
            PendingFlags.Clear();
        }

        public static void DiscardPendingFlags()
        {
            PendingFlags.Clear();
        }

        public static SaveData Load()
        {
            if (_cached != null) return _cached;

            if (!File.Exists(SavePath))
            {
                _cached = new SaveData();
                return _cached;
            }

            try
            {
                using var stream = new FileStream(SavePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(stream);

                byte version = reader.ReadByte();
                if (version < 1 || version > 4)
                {
                    Debug.LogWarning($"SaveManager: Unknown save version {version}, creating fresh save.");
                    _cached = new SaveData();
                    return _cached;
                }

                var data = new SaveData();
                data.lastCompletedDay = reader.ReadInt32();
                data.arcadeHighScore = reader.ReadInt32();

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int day = reader.ReadInt32();
                    int score = reader.ReadInt32();
                    data.dayScores.Add(new DaySaveEntry { dayNumber = day, bestScore = score });
                }

                if (version >= 2)
                {
                    data.playerName = reader.ReadString();
                }

                if (version >= 3)
                {
                    int flagCount = reader.ReadInt32();
                    for (int i = 0; i < flagCount; i++)
                        data.storyFlags.Add(reader.ReadString());
                }

                if (version >= 4)
                {
                    int endingCount = reader.ReadInt32();
                    for (int i = 0; i < endingCount; i++)
                        data.endingsUnlocked.Add(reader.ReadString());
                }

                _cached = data;
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveManager: Failed to load save file: {ex.Message}");
                _cached = new SaveData();
                return _cached;
            }
        }

        public static void Save(SaveData data)
        {
            _cached = data;

            try
            {
                using var stream = new FileStream(SavePath, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(stream);

                writer.Write(SAVE_VERSION);
                writer.Write(data.lastCompletedDay);
                writer.Write(data.arcadeHighScore);
                writer.Write(data.dayScores.Count);

                for (int i = 0; i < data.dayScores.Count; i++)
                {
                    writer.Write(data.dayScores[i].dayNumber);
                    writer.Write(data.dayScores[i].bestScore);
                }

                writer.Write(data.playerName);

                writer.Write(data.storyFlags.Count);
                for (int i = 0; i < data.storyFlags.Count; i++)
                    writer.Write(data.storyFlags[i]);

                writer.Write(data.endingsUnlocked.Count);
                for (int i = 0; i < data.endingsUnlocked.Count; i++)
                    writer.Write(data.endingsUnlocked[i]);

            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveManager: Failed to write save file: {ex.Message}");
            }
        }

        public static void InvalidateCache()
        {
            _cached = null;
        }

        public static void ResetSave()
        {
            _cached = new SaveData();

            if (File.Exists(SavePath))
            {
                try { File.Delete(SavePath); }
                catch (Exception ex) { Debug.LogWarning($"SaveManager: Failed to delete save: {ex.Message}"); }
            }

            Debug.Log("SaveManager: Save data reset.");
        }
    }
}
