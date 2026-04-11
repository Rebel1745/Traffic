using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class TrafficLightListItem : MonoBehaviour
{
    private TrafficLightGroupSettingsUI _settingsUI;
    [SerializeField] private TMP_Text _lightNameText;
    [SerializeField] private TMP_Text _redDurationText;
    [SerializeField] private TMP_Text _greenDurationText;
    [SerializeField] private TMP_Text _yellowDurationText;
    [SerializeField] private TMP_Text _allRedDurationText;
    [SerializeField] private Button _editButton;
    [SerializeField] private Button _moveUpButton;
    [SerializeField] private Button _moveDownButton;

    public void SetDetails(TrafficLightGroupSettingsUI settings, TrafficLight light, UnityAction onEdit, UnityAction onMoveUp, UnityAction onMoveDown)
    {
        _settingsUI = settings;
        _lightNameText.text = light.Label;
        _redDurationText.text = light.RedDuration.ToString();
        _yellowDurationText.text = light.YellowDuration.ToString();
        _greenDurationText.text = light.GreenDuration.ToString();
        _allRedDurationText.text = light.RedOverlapDuration.ToString();

        _editButton.onClick.RemoveAllListeners();
        _editButton.onClick.AddListener(onEdit);

        _moveUpButton.onClick.RemoveAllListeners();
        _moveUpButton.onClick.AddListener(onMoveUp);

        _moveDownButton.onClick.RemoveAllListeners();
        _moveDownButton.onClick.AddListener(onMoveDown);
    }
}
