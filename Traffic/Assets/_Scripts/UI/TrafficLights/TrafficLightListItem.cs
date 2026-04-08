using UnityEngine;
using TMPro;

public class TrafficLightListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _lightNameText;
    [SerializeField] private TMP_Text _redDurationText;
    [SerializeField] private TMP_Text _greenDurationText;
    [SerializeField] private TMP_Text _yellowDurationText;
    [SerializeField] private TMP_Text _allRedDurationText;

    public void SetDetails(TrafficLight light)
    {
        _lightNameText.text = light.Label;
        _redDurationText.text = light.RedDuration.ToString();
        _yellowDurationText.text = light.YellowDuration.ToString();
        _greenDurationText.text = light.GreenDuration.ToString();
        _allRedDurationText.text = light.RedOverlapDuration.ToString();
    }
}
