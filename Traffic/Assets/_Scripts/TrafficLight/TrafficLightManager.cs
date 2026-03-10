using System.Collections.Generic;
using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    public static TrafficLightManager Instance { get; private set; }

    [SerializeField] private GameObject _trafficLightPrefab;
    public GameObject TrafficLightPrefab { get { return _trafficLightPrefab; } }
    [SerializeField] private Camera _mainCamera;

    private List<TrafficLightGroupController> _allGroups = new();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Only process input if we're in TrafficLights state
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.TrafficLights)
            return;
    }

    public void PlaceLightAtWaypoint(WaypointNode waypoint)
    {
        if (waypoint == null || waypoint.AssignedLight != null)
            return;

        GameObject lightObj = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);
        TrafficLightController light = lightObj.GetComponent<TrafficLightController>();
        waypoint.AssignedLight = light;

        TrafficLightGroupController group = FindOrCreateGroupForWaypoint(waypoint);
        group.RegisterLight(light);

        Debug.Log($"Confirmed traffic light at {waypoint.Id}");
    }

    public void RemoveLightAtWaypoint(WaypointNode waypoint)
    {
        if (waypoint?.AssignedLight == null)
            return;

        TrafficLightGroupController group = FindGroupForLight(waypoint.AssignedLight);
        if (group != null)
            group.RemoveLight(waypoint.AssignedLight);

        Destroy(waypoint.AssignedLight.gameObject);
        waypoint.AssignedLight = null;

        Debug.Log($"Removed traffic light at {waypoint.Id}");
    }

    private TrafficLightGroupController FindOrCreateGroupForWaypoint(WaypointNode waypoint)
    {
        GameObject groupObj;
        TrafficLightGroupController newGroup;

        // For paired crossing lights (straight roads only)
        if (waypoint.PairedCrossingWaypoint != null)
        {
            // Look for an existing group that contains either waypoint
            foreach (TrafficLightGroupController group in _allGroups)
            {
                if (group.IsForWaypoint(waypoint) || group.IsForWaypoint(waypoint.PairedCrossingWaypoint))
                {
                    return group;
                }
            }

            // Create a new group for this crossing pair
            groupObj = new GameObject($"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.y}_Crossing");
            groupObj.transform.position = waypoint.Position;
            newGroup = groupObj.AddComponent<TrafficLightGroupController>();
            _allGroups.Add(newGroup);

            // Add both lights to the group
            newGroup.RegisterLight(waypoint.AssignedLight);
            if (waypoint.PairedCrossingWaypoint.AssignedLight != null)
                newGroup.RegisterLight(waypoint.PairedCrossingWaypoint.AssignedLight);

            return newGroup;
        }

        // For junctions (T-junction, crossroads)
        if (waypoint.ParentCell.RoadType == RoadType.TJunction || waypoint.ParentCell.RoadType == RoadType.Crossroads)
        {
            // Group all lights on this junction cell
            foreach (TrafficLightGroupController group in _allGroups)
            {
                if (group.IsForCell(waypoint.ParentCell))
                    return group;
            }

            // Create a new group for this junction
            groupObj = new GameObject($"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.y}_Junction");
            groupObj.transform.position = waypoint.ParentCell.Position;
            newGroup = groupObj.AddComponent<TrafficLightGroupController>();
            _allGroups.Add(newGroup);
            return newGroup;
        }

        // For corners and dead ends — group by parent cell
        foreach (TrafficLightGroupController group in _allGroups)
        {
            if (group.IsForCell(waypoint.ParentCell))
                return group;
        }

        // Create a new group for this road cell
        groupObj = new GameObject($"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.y}");
        groupObj.transform.position = waypoint.ParentCell.Position;
        newGroup = groupObj.AddComponent<TrafficLightGroupController>();
        _allGroups.Add(newGroup);
        return newGroup;
    }

    private TrafficLightGroupController FindGroupForLight(TrafficLightController light)
    {
        foreach (TrafficLightGroupController group in _allGroups)
        {
            if (group.ContainsLight(light))
                return group;
        }
        return null;
    }
}