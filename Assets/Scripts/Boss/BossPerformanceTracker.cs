using System;
using System.Collections.Generic;
using UnityEngine;

public class BossPerformanceTracker : MonoBehaviour
{
    [Serializable]
    public class PerformanceLog
    {
        public string personalityName;
        public string metric;
        public float value;
        public float timestamp;
    }

    [Header("Settings")]
    public int maxLogEntries = 100;

    [Header("Performance Data")]
    public List<PerformanceLog> performanceLogs = new List<PerformanceLog>();

    private BossPersonalitySystem personalitySystem;

    private void Awake()
    {
        personalitySystem = GetComponent<BossPersonalitySystem>();
    }

    public void LogPerformance(string personalityName, string metric, float value)
    {
        // Add new log entry
        performanceLogs.Add(new PerformanceLog
        {
            personalityName = personalityName,
            metric = metric,
            value = value,
            timestamp = Time.time
        });

        // Trim oldest entries if exceeding max
        if (performanceLogs.Count > maxLogEntries)
        {
            performanceLogs.RemoveAt(0);
        }
    }

    // Get cumulative performance stats for a specific personality
    public Dictionary<string, float> GetPersonalityStats(string personalityName)
    {
        Dictionary<string, float> stats = new Dictionary<string, float>
        {
            { "damage_dealt", 0f },
            { "damage_taken", 0f },
            { "usage_count", 0f }
        };

        HashSet<float> uniqueTimestamps = new HashSet<float>();

        foreach (PerformanceLog log in performanceLogs)
        {
            if (log.personalityName == personalityName)
            {
                if (log.metric == "damage_dealt")
                {
                    stats["damage_dealt"] += log.value;
                }
                else if (log.metric == "damage_taken")
                {
                    stats["damage_taken"] += log.value;
                }

                uniqueTimestamps.Add(log.timestamp);
            }
        }

        stats["usage_count"] = uniqueTimestamps.Count;

        return stats;
    }

    // Get cumulative stats for all personalities
    public Dictionary<string, Dictionary<string, float>> GetAllStats()
    {
        Dictionary<string, Dictionary<string, float>> allStats = new Dictionary<string, Dictionary<string, float>>();

        // First collect all personality names
        HashSet<string> personalities = new HashSet<string>();
        foreach (PerformanceLog log in performanceLogs)
        {
            personalities.Add(log.personalityName);
        }

        // Then calculate stats for each
        foreach (string personality in personalities)
        {
            allStats[personality] = GetPersonalityStats(personality);
        }

        return allStats;
    }

    // Clear all logs
    public void ClearLogs()
    {
        performanceLogs.Clear();
    }
}

public class RLDataPersistence : MonoBehaviour
{
    [Header("Save Settings")]
    public string saveKey = "BossPersonalityData";
    public bool autoSaveOnExit = true;

    [Serializable]
    private class SaveData
    {
        public List<PersonalitySaveData> personalities = new List<PersonalitySaveData>();
        public int version = 1;
        public string timestamp;
    }

    [Serializable]
    private class PersonalitySaveData
    {
        public string name;
        public float selectionProbability;
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnExit)
        {
            BossPersonalitySystem personalitySystem = GetComponent<BossPersonalitySystem>();
            if (personalitySystem != null)
            {
                SavePersonalityData(personalitySystem.personalities);
            }
        }
    }

    // Save personality data to PlayerPrefs
    public void SavePersonalityData(List<PersonalityProfile> personalities)
    {
        SaveData saveData = new SaveData();
        saveData.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (PersonalityProfile profile in personalities)
        {
            saveData.personalities.Add(new PersonalitySaveData
            {
                name = profile.name,
                selectionProbability = profile.selectionProbability
            });
        }

        // Convert to JSON and save
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[RLData] Saved {personalities.Count} personality profiles to PlayerPrefs");
    }

    // Load personality data from PlayerPrefs
    public List<PersonalityProfile> LoadPersonalityData()
    {
        try
        {
            if (!PlayerPrefs.HasKey(saveKey))
            {
                Debug.Log("[RLData] No saved personality data found");
                return new List<PersonalityProfile>();
            }

            string json = PlayerPrefs.GetString(saveKey);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[RLData] Saved data is empty");
                return new List<PersonalityProfile>();
            }

            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                Debug.LogWarning("[RLData] Failed to parse saved personality data - null SaveData");
                return new List<PersonalityProfile>();
            }

            if (saveData.personalities == null || saveData.personalities.Count == 0)
            {
                Debug.LogWarning("[RLData] Saved personality data contains no personalities");
                return new List<PersonalityProfile>();
            }

            List<PersonalityProfile> loadedProfiles = new List<PersonalityProfile>();

            foreach (PersonalitySaveData saveProfile in saveData.personalities)
            {
                if (saveProfile == null || string.IsNullOrEmpty(saveProfile.name))
                {
                    Debug.LogWarning("[RLData] Skipping invalid personality in saved data");
                    continue;
                }

                loadedProfiles.Add(new PersonalityProfile
                {
                    name = saveProfile.name,
                    selectionProbability = Mathf.Clamp(saveProfile.selectionProbability, 0.01f, 1f)
                });
            }

            Debug.Log($"[RLData] Loaded {loadedProfiles.Count} personality profiles from save data ({saveData.timestamp})");

            return loadedProfiles;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RLData] Error loading personality data: {e.Message}");
            return new List<PersonalityProfile>();
        }
    }

    // Delete saved personality data
    public void DeleteSavedData()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
            Debug.Log("[RLData] Deleted saved personality data");
        }
    }
}