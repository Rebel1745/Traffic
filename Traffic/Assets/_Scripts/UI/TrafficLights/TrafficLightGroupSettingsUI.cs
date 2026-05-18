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
    [SerializeField] private Transform _trafficLightList;
    [SerializeField] private Transform _trafficLightDetails;
    [SerializeField] private TMP_Text _lightEditText;

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
    [SerializeField] private Transform _lightLabelLayout;
    [SerializeField] private Transform _redLayout;
    [SerializeField] private Transform _allRedLayout;
    [SerializeField] private Transform _pedestrianCrossingLayout;
    [SerializeField] private TMP_InputField _labelInput;
    [SerializeField] private TMP_InputField _greenInput;
    [SerializeField] private TMP_InputField _yellowInput;
    [SerializeField] private TMP_InputField _redInput;
    [SerializeField] private TMP_InputField _allRedInput;
    [SerializeField] private TMP_InputField _pedestrianCrossingInput;

    [Header("Pedestrian Crossing Light Details")]
    [SerializeField] private TMP_Text _redTimeText;
    [SerializeField] private TMP_Text _yellowTimeText;
    [SerializeField] private TMP_Text _greenTimeText;

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

        if (group.GroupType == TrafficLightGroupType.Junction)
        {
            _trafficLightList.gameObject.SetActive(true);
            _lightLabelLayout.gameObject.SetActive(true);
            _redLayout.gameObject.SetActive(false);
            _allRedLayout.gameObject.SetActive(true);
            _pedestrianCrossingLayout.gameObject.SetActive(true);
            _trafficLightDetails.gameObject.SetActive(false);

            RefreshLightList();
            ClearSettingsPanel();
        }
        else
        {
            _trafficLightList.gameObject.SetActive(false);
            _lightLabelLayout.gameObject.SetActive(false);
            _redLayout.gameObject.SetActive(true);
            _allRedLayout.gameObject.SetActive(false);
            _pedestrianCrossingLayout.gameObject.SetActive(false);
            _trafficLightDetails.gameObject.SetActive(true);

            LoadLightSettings(0);
            LoadTrafficLightDetails();
        }
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

            if (_lightListCopy[capturedIndex].Light.IsPedestrianOnlyLight) continue;

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

        if (_group.GroupType == TrafficLightGroupType.Junction)
            _lightEditText.text = "Select Light to Edit";
        else
            _lightEditText.text = "Edit Light Timings";

        _labelInput.text = _yellowInput.text = _greenInput.text = _redInput.text = _allRedInput.text = _pedestrianCrossingInput.text = "";
    }

    public void LoadLightSettings(int index)
    {
        TrafficLight light = _lightListCopy[index];
        _selectedIndex = index;

        _lightEditText.text = "Editing Light: " + light.Label;

        _labelInput.text = light.Label;
        _redInput.text = light.RedDuration.ToString("F1");
        _yellowInput.text = light.YellowDuration.ToString("F1");
        _greenInput.text = light.GreenDuration.ToString("F1");
        _allRedInput.text = light.AllRedDuration.ToString("F1");
        _pedestrianCrossingInput.text = light.PedestrianCrossingDuration.ToString("F1");
    }

    private void LoadTrafficLightDetails()
    {
        _redTimeText.text = _lightListCopy[0].RedDuration.ToString("F1");
        _yellowTimeText.text = _lightListCopy[0].YellowDuration.ToString("F1");
        _greenTimeText.text = _lightListCopy[0].GreenDuration.ToString("F1");
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
        _lightEditText.text = "Editing Light: " + s.Label;

        // all red and pedestrian crossing duration are the same for all lights in the group
        foreach (TrafficLight light in _lightListCopy)
        {
            light.AllRedDuration = ParseFloatClamped(_allRedInput.text, 0f, 30f);
            light.PedestrianCrossingDuration = ParseFloatClamped(_pedestrianCrossingInput.text, 0f, 30f);
        }
    }

    // --- Buttons ---

    // Updates the individual light details of the copy
    public void OnUpdateClicked()
    {
        if (_group.GroupType == TrafficLightGroupType.Junction)
        {
            SaveCurrentEditsToWorkingCopy();
            RefreshLightList();
        }
        else
        {
            // for a pedestrian crossing, save both lights with the same timings
            for (int i = 0; i <= 1; i++)
            {
                var s = _lightListCopy[i];
                s.GreenDuration = ParseFloatClamped(_greenInput.text, 0.5f, 300f);
                s.YellowDuration = ParseFloatClamped(_yellowInput.text, 0.5f, 30f);
                s.RedDuration = ParseFloatClamped(_redInput.text, 0.5f, 300f);
            }

            LoadTrafficLightDetails();
        }
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
