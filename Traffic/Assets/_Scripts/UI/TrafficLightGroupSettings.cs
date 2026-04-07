using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLightGroupSettings : MonoBehaviour
{
    private List<TrafficLight> _lights;

    public void LoadSettings(TrafficLightGroupController group)
    {
        _lights = group.Lights.ToList();

        foreach (TrafficLight light in _lights)
        {
            Debug.Log(light.Details);
        }
    }
}
