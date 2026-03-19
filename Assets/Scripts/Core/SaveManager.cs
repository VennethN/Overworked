using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Overworked.Core
{
    [Serializable]
    public class SaveData
    {
        public int lastCompletedDay;
        public int arcadeHighScore;
        public List<DaySaveEntry> dayScores = new();

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
        private const byte SAVE_VERSION = 1;
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "overworked.sav");

        private static SaveData _cached;

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
                if (version != SAVE_VERSION)
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
    }
}
