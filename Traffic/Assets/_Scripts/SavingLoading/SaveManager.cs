using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "traffic_sim_save.json";
    private readonly List<ISaveable> _saveables = new();

    // Define load order priorities
    private enum LoadPriority
    {
        Grid = 0,
        Waypoints = 1,
        TrafficLights = 2
    }

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterSaveable(ISaveable saveable)
    {
        if (!_saveables.Contains(saveable))
            _saveables.Add(saveable);
    }

    public void UnregisterSaveable(ISaveable saveable)
    {
        _saveables.Remove(saveable);
    }

    public void Save()
    {
        var saveData = new GameSaveData
        {
            saveVersion = "1.0",
            saveDate = DateTime.UtcNow.ToString("o")
        };

        foreach (var saveable in _saveables)
            saveable.PopulateSaveData(saveData);

        string json = JsonUtility.ToJson(saveData, prettyPrint: true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"[SaveManager] Saved to {SaveFilePath}");
    }

    public void Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning("[SaveManager] No save file found.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        var saveData = JsonUtility.FromJson<GameSaveData>(json);

        // Load in specific order
        LoadByKey(saveData, "Grid");
        LoadByKey(saveData, "Waypoints");
        LoadByKey(saveData, "TrafficLights");

        Debug.Log("[SaveManager] Loaded successfully.");
    }

    private void LoadByKey(GameSaveData saveData, string key)
    {
        var saveable = _saveables.Find(s => s.SaveKey == key);
        if (saveable != null)
        {
            saveable.LoadFromSaveData(saveData);
            Debug.Log($"[SaveManager] Loaded {key}");
        }
    }

    public bool SaveExists() => File.Exists(SaveFilePath);
}