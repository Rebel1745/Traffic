using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class TrafficLightListItem : MonoBehaviour
{
    private TrafficLightGroupSettingsUI _settingsUI;
    [SerializeField] private TMP_Text _lightNameText;
    [SerializeField] private TMP_Text _pedestrianCrossingDurationText;
    [SerializeField] private TMP_Text _greenDurationText;
    [SerializeField] private TMP_Text _yellowDurationText;
    [SerializeField] private TMP_Text _allRedDurationText;
    [SerializeField] private Button _editButton;
    [SerializeField] private Button _moveUpButton;
    [SerializeField] private Button _moveDownButton;
    [SerializeField] private Button _copyButton;
    [SerializeField] private Button _removeButton;

    public void SetDetails(TrafficLightGroupSettingsUI settings, TrafficLight light, UnityAction onEdit, UnityAction onMoveUp, UnityAction onMoveDown, UnityAction onCopy, UnityAction onRemove)
    {
        _settingsUI = settings;
        _lightNameText.text = light.Label;
        _pedestrianCrossingDurationText.text = light.PedestrianCrossingDuration.ToString();
        _yellowDurationText.text = light.YellowDuration.ToString();
        _greenDurationText.text = light.GreenDuration.ToString();
        _allRedDurationText.text = light.AllRedDuration.ToString();

        _editButton.onClick.RemoveAllListeners();
        _editButton.onClick.AddListener(onEdit);

        _moveUpButton.onClick.RemoveAllListeners();
        _moveUpButton.onClick.AddListener(onMoveUp);

        _moveDownButton.onClick.RemoveAllListeners();
        _moveDownButton.onClick.AddListener(onMoveDown);

        if (!light.IsCopyOfLight)
        {
            _copyButton.onClick.RemoveAllListeners();
            _copyButton.onClick.AddListener(onCopy);
            _copyButton.gameObject.SetActive(true);
        }
        else _copyButton.gameObject.SetActive(false);

        if (light.IsCopyOfLight)
        {
            _removeButton.onClick.RemoveAllListeners();
            _removeButton.onClick.AddListener(onRemove);
            _removeButton.gameObject.SetActive(true);
        }
        else _removeButton.gameObject.SetActive(false);
    }
}
