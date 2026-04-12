using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TrafficLightGroupSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private GameObject _trafficLightListItemPrefab;
    [SerializeField] private Transform _lightListContainer;
    //[SerializeField] private TMP_Text junctionNameText;

    private TrafficLightGroupController _group;
    private List<TrafficLight> _lightListCopy;
    private int _selectedIndex;

    [Header("Selected Light Settings")]
    [SerializeField] private TMP_Text _selectedLightLabel;
    [SerializeField] private TMP_InputField _labelInput;
    [SerializeField] private TMP_InputField _greenInput;
    [SerializeField] private TMP_InputField _yellowInput;
    [SerializeField] private TMP_InputField _redInput;
    [SerializeField] private TMP_InputField _allRedInput;

    [Header("Buttons")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _resetButton;

    public void LoadSettings(TrafficLightGroupController group)
    {
        _group = group;
        _lightListCopy = group.GetLightsCopy();
        _selectedIndex = -1;

        // When I get around to implementing a junction name, set it here
        //junctionNameText.text = group.name;
        uiPanel.SetActive(true);

        RefreshLightList();
        ClearSettingsPanel();
    }

    public void Close()
    {
        uiPanel.SetActive(false);
        _group = null;
    }

    private void RefreshLightList()
    {
        foreach (Transform child in _lightListContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < _lightListCopy.Count; i++)
        {
            int capturedIndex = i;
            GameObject item = Instantiate(_trafficLightListItemPrefab, _lightListContainer);
            TrafficLightListItem entry = item.GetComponent<TrafficLightListItem>();

            entry.SetDetails(this, _lightListCopy[capturedIndex],
                onEdit: () => LoadLightSettings(capturedIndex),
                onMoveUp: () => MoveLight(capturedIndex, capturedIndex - 1),
                onMoveDown: () => MoveLight(capturedIndex, capturedIndex + 1));
        }
    }

    private void ClearSettingsPanel()
    {
        _selectedIndex = -1;
        _selectedLightLabel.text = "Select a light";
        _labelInput.text = _yellowInput.text = _greenInput.text = _redInput.text = _allRedInput.text = "";
    }

    public void LoadLightSettings(int index)
    {
        TrafficLight light = _lightListCopy[index];
        _selectedIndex = index;

        _selectedLightLabel.text = light.Label;
        _labelInput.text = light.Label;
        _redInput.text = light.RedDuration.ToString("F1");
        _yellowInput.text = light.YellowDuration.ToString("F1");
        _greenInput.text = light.GreenDuration.ToString("F1");
        _allRedInput.text = light.RedOverlapDuration.ToString("F1");
    }

    private void MoveLight(int from, int to)
    {
        if (to < 0 || to >= _lightListCopy.Count) return;

        SaveCurrentEditsToWorkingCopy();

        var item = _lightListCopy[from];
        _lightListCopy.RemoveAt(from);
        _lightListCopy.Insert(to, item);

        // Keep selection following the moved item
        if (_selectedIndex == from) _selectedIndex = to;

        RefreshLightList();
    }

    private void SaveCurrentEditsToWorkingCopy()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _lightListCopy.Count) return;

        var s = _lightListCopy[_selectedIndex];
        s.Label = _labelInput.text;
        s.GreenDuration = ParseFloatClamped(_greenInput.text, 0.5f, 300f);
        s.YellowDuration = ParseFloatClamped(_yellowInput.text, 0.5f, 30f);
        s.RedDuration = ParseFloatClamped(_redInput.text, 0.5f, 300f);
        s.RedOverlapDuration = ParseFloatClamped(_allRedInput.text, 0f, 30f);
        _selectedLightLabel.text = _labelInput.text;
    }

    // --- Buttons ---

    // Updates the individual light details of the copy
    public void OnUpdateClicked()
    {
        SaveCurrentEditsToWorkingCopy();
        RefreshLightList();
    }

    // Discards any changes made to the light list copy
    public void OnDiscardClicked()
    {
        ClearSettingsPanel();
    }

    // Applies the changes to the light group
    public void OnApplyClicked()
    {
        SaveCurrentEditsToWorkingCopy();
        _group.ApplySettings(_lightListCopy);
        Close();
    }

    // Discards all the changes to the light group
    public void OnCancelClicked() => Close();

    // Resets all of the changes to the light group to the defaults when each light was created
    public void OnResetClicked()
    {
        // Re-fetch defaults from the group's actual current state
        // (assumes your TrafficLightController has a ResetToDefaults method)
        foreach (var light in _group.Lights)
            light.ResetToDefaults();

        _lightListCopy = _group.GetLightsCopy();
        RefreshLightList();
        if (_selectedIndex >= 0)
            LoadLightSettings(_selectedIndex);
    }

    private float ParseFloatClamped(string input, float min, float max)
    {
        return float.TryParse(input, out float val)
            ? Mathf.Clamp(val, min, max)
            : min;
    }
}
