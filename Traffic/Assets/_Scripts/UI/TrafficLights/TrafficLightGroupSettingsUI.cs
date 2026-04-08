using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLightGroupSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject _trafficLightListItemPrefab;
    [SerializeField] private Transform _lightListContainer;
    private List<TrafficLight> _lightListCopy;

    public void LoadSettings(TrafficLightGroupController group)
    {
        _lightListCopy = group.GetLightsCopy();

        foreach (Transform child in _lightListContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < _lightListCopy.Count; i++)
        {
            var item = Instantiate(_trafficLightListItemPrefab, _lightListContainer);
            var entry = item.GetComponent<TrafficLightListItem>();

            entry.SetDetails(_lightListCopy[i]);
        }
    }
}
