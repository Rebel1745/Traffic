using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficLightGroupController : MonoBehaviour
{
    [System.Serializable]
    public class LightPhase
    {
        public TrafficLightController Light;
        public float GreenDuration = 10f;
        public float YellowDuration = 3f;
    }

    [SerializeField] private List<LightPhase> phases = new();
    [SerializeField] private float redOverlapDuration = 1f;

    private int _currentPhaseIndex = 0;
    private Coroutine _cycleCoroutine;

    private void Start()
    {
        foreach (var phase in phases)
            phase.Light.SetState(TrafficLightController.LightState.Red);

        _cycleCoroutine = StartCoroutine(CycleLights());
    }

    private IEnumerator CycleLights()
    {
        while (true)
        {
            if (phases.Count == 0)
            {
                // No lights left — stop cycling and destroy self
                StopCoroutine(_cycleCoroutine);
                Destroy(gameObject);
                yield break;
            }

            LightPhase current = phases[_currentPhaseIndex];

            current.Light.SetState(TrafficLightController.LightState.Green);
            yield return new WaitForSeconds(current.GreenDuration);

            current.Light.SetState(TrafficLightController.LightState.Yellow);
            yield return new WaitForSeconds(current.YellowDuration);

            current.Light.SetState(TrafficLightController.LightState.Red);
            yield return new WaitForSeconds(redOverlapDuration);

            _currentPhaseIndex = (_currentPhaseIndex + 1) % phases.Count;
        }
    }

    public void RegisterLight(TrafficLightController light, float greenDuration = 10f, float yellowDuration = 3f)
    {
        phases.Add(new LightPhase
        {
            Light = light,
            GreenDuration = greenDuration,
            YellowDuration = yellowDuration
        });
    }

    public void RemoveLight(TrafficLightController light)
    {
        phases.RemoveAll(p => p.Light == light);
        if (phases.Count == 0)
        {
            // No lights left — destroy group
            Destroy(gameObject);
        }
    }

    // Helper: Check if this group contains a specific light
    public bool ContainsLight(TrafficLightController light)
    {
        return phases.Any(p => p.Light == light);
    }

    public bool IsForWaypoint(WaypointNode waypoint)
    {
        return phases.Any(p => p.Light != null &&
                               p.Light.transform.parent?.GetComponent<WaypointNode>() == waypoint);
    }

    public bool IsForCell(GridCell cell)
    {
        return phases.Any(p => p.Light != null &&
                             p.Light.transform.parent?.GetComponent<WaypointNode>()?.ParentCell == cell);
    }
}