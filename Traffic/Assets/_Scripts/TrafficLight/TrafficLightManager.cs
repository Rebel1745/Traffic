using System.Collections.Generic;
using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    [SerializeField] private GameObject trafficLightPrefab;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SimulationManager SimulationManager; // Reference to your state manager

    private List<TrafficLightGroupController> _allGroups = new();

    private void Update()
    {
        // Only process input if we're in TrafficLights state
        if (SimulationManager.CurrentState.SimulationState != SimulationState.TrafficLights)
            return;

        if (Input.GetMouseButtonDown(0)) // Left click = place
            PlaceLightAtMouse();
        else if (Input.GetMouseButtonDown(1)) // Right click = remove
            RemoveLightAtMouse();
    }

    private void PlaceLightAtMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Waypoint")))
        {
            WaypointNode waypoint = hit.collider.GetComponent<WaypointNode>();
            if (waypoint == null || waypoint.Type != WaypointType.TrafficLightLocation)
            {
                Debug.LogWarning("Cannot place light here — not a TrafficLightLocation waypoint.");
                return;
            }

            // Validate based on current substate
            TrafficLightSubState currentSubState = SimulationManager.CurrentState.TrafficLightSubState;

            if (currentSubState == TrafficLightSubState.AddJunctionLights)
            {
                // Only allow placement on T-junctions and crossroads
                if (waypoint.ParentCell.RoadType != RoadType.TJunction &&
                    waypoint.ParentCell.RoadType != RoadType.Crossroads)
                {
                    Debug.LogWarning("Junction lights can only be placed on T-junctions and crossroads.");
                    return;
                }
            }
            else if (currentSubState == TrafficLightSubState.AddPedestrianCrossings)
            {
                // Only allow placement on straight roads
                if (waypoint.ParentCell.RoadType != RoadType.Straight)
                {
                    Debug.LogWarning("Pedestrian crossing lights can only be placed on straight roads.");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("Invalid traffic light substate.");
                return;
            }

            if (waypoint.AssignedLight != null)
            {
                Debug.LogWarning("Light already exists here.");
                return;
            }

            // Spawn light
            GameObject lightObj = Instantiate(trafficLightPrefab, waypoint.Position, Quaternion.identity);
            TrafficLightController light = lightObj.GetComponent<TrafficLightController>();
            waypoint.AssignedLight = light;

            // Find or create group
            TrafficLightGroupController group = FindOrCreateGroupForWaypoint(waypoint);
            group.RegisterLight(light);

            Debug.Log($"Placed {currentSubState} traffic light at {waypoint.Id}");
        }
    }

    private void RemoveLightAtMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Waypoint")))
        {
            WaypointNode waypoint = hit.collider.GetComponent<WaypointNode>();
            if (waypoint?.AssignedLight != null)
            {
                // Remove from group
                TrafficLightGroupController group = FindGroupForLight(waypoint.AssignedLight);
                if (group != null)
                    group.RemoveLight(waypoint.AssignedLight);

                // Destroy light
                Destroy(waypoint.AssignedLight.gameObject);
                waypoint.AssignedLight = null;

                Debug.Log("Removed traffic light.");
            }
        }
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