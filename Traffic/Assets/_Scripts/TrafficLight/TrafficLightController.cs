using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public LightState CurrentState { get; private set; } = LightState.Red;

    public WaypointNode AssignedWaypoint { get; set; }

    [Header("Material for each light")]
    [SerializeField] private Material _redLightOn;
    [SerializeField] private Material _redLightOff;
    [SerializeField] private Material _yellowLightOn;
    [SerializeField] private Material _yellowLightOff;
    [SerializeField] private Material _greenLightOn;
    [SerializeField] private Material _greenLightOff;

    [Header("Light Object Renderers")]
    [SerializeField] private Renderer _redLightRen;
    [SerializeField] private Renderer _yellowLightRen;
    [SerializeField] private Renderer _greenLightRen;

    [Header("Main Light")]
    private bool _isPedestrianOnlyLight = false;
    public bool IsPedestrianOnlyLight => _isPedestrianOnlyLight;
    [SerializeField] private Transform _mainLight;

    [Header("Pedestrian Crossing Lights")]
    [SerializeField] private Transform _parallelCrossingLight;
    [SerializeField] private Transform _perpendicularCrossingLight;
    private List<CrossingLightController> _crossingLightControllers;

    public void InitialiseLight(TrafficLightGroupType groupType)
    {
        if (groupType == TrafficLightGroupType.PedestrianCrossing)
            _perpendicularCrossingLight.gameObject.SetActive(false);

        _crossingLightControllers = GetComponentsInChildren<CrossingLightController>().ToList();

        foreach (CrossingLightController light in _crossingLightControllers)
            light.SetState(LightState.Red);
    }

    public void SetState(LightState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    public void SetCrossingLightsState(LightState state)
    {
        foreach (CrossingLightController light in _crossingLightControllers)
            light.SetState(state);
    }

    public bool IsGreen() => CurrentState == LightState.Green;
    public bool IsYellow() => CurrentState == LightState.Yellow;
    public bool IsRed() => CurrentState == LightState.Red;

    // to check the crossing lights, just the first in the list has to be checked as they will all be the same
    public bool IsCrossingRed => _crossingLightControllers.Count > 0 && _crossingLightControllers[0].IsRed();
    public bool IsCrossingGreen => _crossingLightControllers.Count > 0 && _crossingLightControllers[0].IsGreen();

    private void UpdateVisuals()
    {
        switch (CurrentState)
        {
            case LightState.Red:
                _redLightRen.material = _redLightOn;
                _yellowLightRen.material = _yellowLightOff;
                _greenLightRen.material = _greenLightOff;
                break;
            case LightState.Yellow:
                _redLightRen.material = _redLightOff;
                _yellowLightRen.material = _yellowLightOn;
                _greenLightRen.material = _greenLightOff;
                break;
            case LightState.Green:
                _redLightRen.material = _redLightOff;
                _yellowLightRen.material = _yellowLightOff;
                _greenLightRen.material = _greenLightOn;
                break;
        }
    }

    public void SetPedestrianOnlyLight()
    {
        _isPedestrianOnlyLight = true;
        _mainLight.gameObject.SetActive(false);
        _parallelCrossingLight.gameObject.SetActive(false);
    }
}