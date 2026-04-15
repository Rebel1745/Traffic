using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TrafficLightGroupSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private GameObject _trafficLightListItemPrefab;
    [SerializeField] private Transform _lightListContainer;

    private TrafficLightGroupController _group;
    private List<TrafficLight> _lightListCopy;
    private int _selectedIndex;

    [Header(("Junction Name"))]
    [SerializeField] private TMP_Text _junctionNameText;
    [SerializeField] private Button _editJunctionNameButton;
    [SerializeField] private TMP_InputField _junctionNameInput;
    [SerializeField] private Button _updateJunctionNameButton;
    [SerializeField] private Button _cancelUpdateJunctionNameButton;

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

        // setup junction naming
        _junctionNameText.text = group.JunctionName;
        _junctionNameInput.text = group.JunctionName;

        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(GetGroupCentrePoint());

        RefreshLightList();
        ClearSettingsPanel();
    }

    private Vector3 GetGroupCentrePoint()
    {
        Vector3 total = Vector3.zero;

        foreach (TrafficLight light in _lightListCopy)
        {
            total += light.Light.transform.position;
        }

        return total / _lightListCopy.Count;
    }

    public void Close()
    {
        uiPanel.SetActive(false);
        CameraFollow.Instance.StopFollowing();
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
                onMoveDown: () => MoveLight(capturedIndex, capturedIndex + 1),
                onCopy: () => CopyLight(capturedIndex),
                onRemove: () => RemoveLight(capturedIndex));
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

    public void OnEditJunctionNameClicked()
    {
        _junctionNameInput.text = _junctionNameText.text;
        ShowHideJunctionNameButtons(true);
    }

    public void OnUpdateJunctionNameClicked()
    {
        _junctionNameText.text = _junctionNameInput.text;
        ShowHideJunctionNameButtons(false);
    }

    public void OnCancelUpdateJunctionNameClicked()
    {
        _junctionNameInput.text = _junctionNameText.text;
        ShowHideJunctionNameButtons(false);
    }

    private void ShowHideJunctionNameButtons(bool show)
    {
        _junctionNameInput.gameObject.SetActive(show);
        _updateJunctionNameButton.gameObject.SetActive(show);
        _cancelUpdateJunctionNameButton.gameObject.SetActive(show);
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

    private void CopyLight(int index)
    {
        TrafficLight lightCopy = _lightListCopy[index].Clone();
        lightCopy.IsCopyOfLight = true;
        _lightListCopy.Add(lightCopy);

        RefreshLightList();
    }

    private void RemoveLight(int index)
    {
        _lightListCopy.RemoveAt(index);

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
        _group.UpdateJunctionName(_junctionNameInput.text);
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

        _junctionNameText.text = _group.JunctionName;
        _junctionNameInput.text = _group.JunctionName;
    }

    private float ParseFloatClamped(string input, float min, float max)
    {
        return float.TryParse(input, out float val)
            ? Mathf.Clamp(val, min, max)
            : min;
    }
}
