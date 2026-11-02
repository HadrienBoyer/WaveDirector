using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TappyTale
{
    [Serializable]
    public sealed class WaveSaveData
    {
        public string TimelineGuid;
        public int CurrentWaveIndex;
        public int TotalKills;
        public float TotalTimeSeconds;
        public long LastUpdatedUnixMs;
    }

    public static class WaveSaveSystem
    {
        private const string SaveFolderName = "TappyTale_WaveSaves";

        public static string GetSavePath(string timelineName, string timelineGuid)
        {
            string safeName = MakeSafeFileName(timelineName);
            string safeGuid = MakeSafeFileName(timelineGuid);
            string folder = Path.Combine(Application.persistentDataPath, SaveFolderName);
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"{safeName}_{safeGuid}.json");
        }

        public static bool TryLoad(string path, out WaveSaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<WaveSaveData>(json);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaveSaveSystem] Load failed: {ex.Message}");
                return false;
            }
        }

        public static void Save(string path, WaveSaveData data)
        {
            if (data == null) return;

            try
            {
                data.LastUpdatedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaveSaveSystem] Save failed: {ex.Message}");
            }
        }

        private static string MakeSafeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unnamed";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }

    [Serializable]
    public sealed class WaveAnalyticsEntry
    {
        public string TimelineGuid;
        public int WaveIndex;
        public float Difficulty;
        public int Kills;
        public float ClearTimeSeconds;
        public float DamageTaken;
        public int Deaths;
        public long TimestampUnixMs;
    }

    public static class WaveAnalyticsLogger
    {
        private const string AnalyticsFolderName = "TappyTale_WaveAnalytics";

        public static string GetAnalyticsPath(string timelineName, string timelineGuid)
        {
            string safeName = MakeSafeFileName(timelineName);
            string safeGuid = MakeSafeFileName(timelineGuid);
            string folder = Path.Combine(Application.persistentDataPath, AnalyticsFolderName);
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"{safeName}_{safeGuid}.jsonl");
        }

        public static void Append(string path, WaveAnalyticsEntry entry)
        {
            if (entry == null) return;

            try
            {
                entry.TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string json = JsonUtility.ToJson(entry, false);
                File.AppendAllText(path, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaveAnalyticsLogger] Append failed: {ex.Message}");
            }
        }

        private static string MakeSafeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unnamed";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}
