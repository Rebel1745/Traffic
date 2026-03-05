using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private List<ButtonGroup> _topLevelGroups;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        foreach (ButtonGroup group in _topLevelGroups)
            group.OnSelectionChanged += _ => OnAnyTopLevelGroupChanged(group);
    }

    private void OnAnyTopLevelGroupChanged(ButtonGroup activeGroup)
    {
        // Deselect all other top-level groups
        foreach (ButtonGroup group in _topLevelGroups)
        {
            if (group != activeGroup)
                group.Deselect();
        }
    }

    public void SaveGame()
    {
        SaveManager.Instance.Save();
    }

    public void LoadGame()
    {
        SaveManager.Instance.Load();
    }
}