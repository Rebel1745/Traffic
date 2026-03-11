using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLightGroupController : MonoBehaviour
{
    [Header("Group Settings")]
    [SerializeField] private TrafficLightGroupType _groupType = TrafficLightGroupType.Junction;
    public string Id { get; set; }

    // Exposed for UI integration
    public TrafficLightGroupType GroupType
    {
        get => _groupType;
        set
        {
            _groupType = value;
            RestartCycle();
        }
    }

    [SerializeField] private List<TrafficLight> _lights = new();

    public IReadOnlyList<TrafficLight> Lights => _lights;
    public int CurrentLightIndex => _currentLightIndex;

    private int _currentLightIndex = 0;
    private Coroutine _cycleCoroutine;

    private void Start()
    {
        InitialiseLights();
        RestartCycle();
    }

    public void SetupGroup(TrafficLightGroupType type)
    {
        Id = System.Guid.NewGuid().ToString();
        GroupType = type;
    }

    private void InitialiseLights()
    {
        foreach (TrafficLight light in _lights)
            light.Light.SetState(TrafficLightController.LightState.Red);
    }

    // -------------------------------------------------------------------------
    // Cycle Control
    // -------------------------------------------------------------------------

    public void RestartCycle()
    {
        if (_cycleCoroutine != null)
            StopCoroutine(_cycleCoroutine);

        InitialiseLights();
        _currentLightIndex = 0;

        if (_lights.Count == 0)
            return;

        _cycleCoroutine = _groupType switch
        {
            TrafficLightGroupType.Junction => StartCoroutine(CycleJunction()),
            TrafficLightGroupType.PedestrianCrossing => StartCoroutine(CyclePedestrianCrossing()),
            _ => StartCoroutine(CycleJunction())
        };
    }

    // -------------------------------------------------------------------------
    // Junction Cycle — one light green at a time, others red
    // -------------------------------------------------------------------------

    private IEnumerator CycleJunction()
    {
        while (true)
        {
            if (_lights.Count == 0)
                yield break;

            TrafficLight current = _lights[_currentLightIndex];

            // Green light
            current.Light.SetState(TrafficLightController.LightState.Green);
            yield return new WaitForSeconds(current.GreenDuration);

            // Yellow light
            current.Light.SetState(TrafficLightController.LightState.Yellow);
            yield return new WaitForSeconds(current.YellowDuration);

            // Red light (with individual duration)
            current.Light.SetState(TrafficLightController.LightState.Red);
            yield return new WaitForSeconds(current.RedDuration);

            // All-red safety buffer (all lights red)
            yield return new WaitForSeconds(current.RedOverlapDuration);

            // Move to next light
            _currentLightIndex = (_currentLightIndex + 1) % _lights.Count;
        }
    }

    // -------------------------------------------------------------------------
    // Pedestrian Crossing Cycle — all lights same colour, shared timings
    // -------------------------------------------------------------------------

    private IEnumerator CyclePedestrianCrossing()
    {
        // Use the first light's timings to drive the whole group
        while (true)
        {
            if (_lights.Count == 0)
                yield break;

            TrafficLight primary = _lights[0];

            // All lights green
            SetAllLights(TrafficLightController.LightState.Green);
            yield return new WaitForSeconds(primary.GreenDuration);

            // All lights yellow
            SetAllLights(TrafficLightController.LightState.Yellow);
            yield return new WaitForSeconds(primary.YellowDuration);

            // All lights red (using primary's red duration)
            SetAllLights(TrafficLightController.LightState.Red);
            yield return new WaitForSeconds(primary.RedDuration);

            // All-red overlap (safety buffer)
            yield return new WaitForSeconds(primary.RedOverlapDuration);
        }
    }

    private void SetAllLights(TrafficLightController.LightState state)
    {
        foreach (TrafficLight light in _lights)
            light.Light.SetState(state);
    }

    // -------------------------------------------------------------------------
    // Registration & Removal
    // -------------------------------------------------------------------------

    public void RegisterLight(TrafficLightController light, float greenDuration = 10f, float yellowDuration = 3f, float redDuration = 10f, float redOverlapDuration = 2f)
    {
        _lights.Add(new TrafficLight
        {
            Light = light,
            GreenDuration = greenDuration,
            YellowDuration = yellowDuration,
            RedDuration = redDuration,
            RedOverlapDuration = redOverlapDuration
        });
    }

    public void RemoveLight(TrafficLightController light)
    {
        _lights.RemoveAll(p => p.Light == light);

        if (_lights.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // Clamp index in case we removed the current light
        _currentLightIndex = Mathf.Clamp(_currentLightIndex, 0, _lights.Count - 1);
    }

    // -------------------------------------------------------------------------
    // Helpers for TrafficLightManager
    // -------------------------------------------------------------------------

    public bool ContainsLight(TrafficLightController light) =>
        _lights.Any(p => p.Light == light);

    public bool IsForCell(GridCell cell) =>
    _lights.Any(p => p.Light != null &&
                     p.Light.AssignedWaypoint?.ParentCell == cell);

    public bool IsForWaypoint(WaypointNode waypoint) =>
        _lights.Any(p => p.Light != null &&
                         p.Light.AssignedWaypoint == waypoint);
}

public enum TrafficLightGroupType
{
    Junction,            // Lights cycle one at a time (only one green at a time)
    PedestrianCrossing   // All lights in the group are the same colour at the same time
}