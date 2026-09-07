using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightManager : MonoBehaviour, ISaveable
{
    public static TrafficLightManager Instance { get; private set; }

    [SerializeField] private GameObject _trafficLightPrefab;
    public GameObject TrafficLightPrefab { get { return _trafficLightPrefab; } }
    [SerializeField] private float _redDuration = 2f;
    [SerializeField] private float _yellowDuration = 1f;
    [SerializeField] private float _greenDuration = 3f;
    [SerializeField] private float _allRedDuration = 0.5f;
    [SerializeField] private float _pedestrianCrossingDuration = 3f;

    public string SaveKey => "TrafficLights";

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

    private void Start()
    {
        SaveManager.Instance.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance.UnregisterSaveable(this);
    }

    public void PlaceTrafficLightsInCell(GridCell cell)
    {
        List<WaypointNode> validWaypoints = GetValidWaypointsForSubState(cell);

        if (validWaypoints.Count == 0)
        {
            Debug.LogWarning("No valid waypoints found for traffic lights");
            return;
        }

        WaypointNode lastWaypoint = null;

        // Confirm all previewed lights for this cell
        foreach (WaypointNode waypoint in validWaypoints)
        {
            if (waypoint.AssignedLight != null)
                continue;

            lastWaypoint = waypoint;

            if (!cell.HasTrafficLights)
                PlaceLightAtWaypoint(waypoint);
        }

        // if there are already traffic lights, load the settings screen then bail
        if (cell.HasTrafficLights)
        {
            UIManager.Instance.LoadTrafficLightGroupDetails(FindGroupForWaypoint(lastWaypoint));
            return;
        }

        cell.HasTrafficLights = true;

        // update the road markings if we have created a ped x-ing
        if (FindGroupForWaypoint(lastWaypoint).GroupType == TrafficLightGroupType.PedestrianCrossing)
        {
            cell.SetCustomUVs(RoadMarkingUVs.GetUVsForPedestrianCrossing(cell.RoadDirection));
            RoadMeshRenderer.Instance.UpdateRoadMesh(false);
        }

        // hand off to the traffic light settings UI to allow for light timings and order to be changed
        UIManager.Instance.LoadTrafficLightGroupDetails(FindGroupForWaypoint(lastWaypoint));
    }

    public void PlaceLightAtWaypoint(WaypointNode waypoint)
    {
        if (waypoint == null || waypoint.AssignedLight != null)
            return;

        GameObject lightObj = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);
        TrafficLightController light = lightObj.GetComponent<TrafficLightController>();

        if (waypoint.PedestiranOnlyTrafficLight)
            light.SetPedestrianOnlyLight();

        light.AssignedWaypoint = waypoint;
        waypoint.LaneNodeForTrafficLight.AssignedLight = light;

        TrafficLightGroupController group = FindOrCreateGroupForWaypoint(waypoint);
        lightObj.transform.parent = group.gameObject.transform;
        lightObj.transform.rotation = Quaternion.Euler(0, GetYRotationFromLightPosition(waypoint.LightPosition), 0);

        // Register with default timings (can be adjusted via UI later)
        group.RegisterLight(light, waypoint.LightPosition, waypoint.LightPosition.ToString(), _greenDuration, _yellowDuration, _redDuration, _allRedDuration, _pedestrianCrossingDuration, waypoint.LightPosition.ToString(), _greenDuration, _yellowDuration, _redDuration, _allRedDuration, _pedestrianCrossingDuration);

        //Debug.Log($"Confirmed traffic light at {waypoint.Id}");
    }

    private float GetYRotationFromLightPosition(RoadDirection lightPosition)
    {
        float rotation = 0;

        switch (lightPosition)
        {
            case RoadDirection.None:
                rotation = 0;
                break;
            case RoadDirection.North:
                rotation = 270;
                break;
            case RoadDirection.South:
                rotation = 90;
                break;
            case RoadDirection.East:
                rotation = 180;
                break;
            case RoadDirection.West:
                rotation = 0;
                break;
            case RoadDirection.NorthWest:
                rotation = 90;
                break;
            case RoadDirection.NorthEast:
                rotation = 180;
                break;
            case RoadDirection.SouthWest:
                rotation = 0;
                break;
            case RoadDirection.SouthEast:
                rotation = 270;
                break;
        }

        return rotation;
    }

    public void RemoveTrafficLightGroupFromCell(GridCell cell)
    {
        TrafficLightGroupController group = FindGroupForCell(cell);

        // if we don't have a group, bail
        if (group == null) return;

        if (group.GroupType == TrafficLightGroupType.PedestrianCrossing)
        {
            cell.RemoveCustomUVs();
            RoadMeshRenderer.Instance.UpdateRoadMesh(false);
        }

        // we have the group, remove it from the groups list
        _allGroups.Remove(group);

        Destroy(group.gameObject);

        cell.HasTrafficLights = false;

        UIManager.Instance.CloseUIDetailsWindow();
    }

    public void ReconfigureNeighbouringTrafficLights(GridCell cell)
    {
        // we need to see if any cells neighbouring this removed cell have lights, if they do, we may need to change the number of lights in them
        List<GridCell> neighbours = GridManager.Instance.GetCellRoadNeighbours(cell);
        GridCell neighbour;

        if (neighbours == null || neighbours.Count == 0) return;

        for (int i = 0; i < neighbours.Count; i++)
        {
            neighbour = neighbours[i];
            TrafficLightGroupController group = FindGroupForCell(neighbour);

            // if we don't have a group, bail - no groups = no lights
            if (group == null) continue;

            // if the cell is now a dead end remove the lights
            if (neighbour.RoadType == RoadType.DeadEnd)
            {
                Debug.Log("Removing lights in dead end, they shouldn't be here");
                RemoveTrafficLightGroupFromCell(neighbour);
                continue;
            }

            // if we have 4 lights at a T-junction, or 3 lights at a crossroad, remove the lights and re-add them
            // TODO: the below state changing just to place traffic lights seems a bit hacky. Maybe try a cleaner way?
            if (
                neighbour.RoadType == RoadType.TJunction && group.RoadLightCount == 4 ||
                neighbour.RoadType == RoadType.Crossroads && group.RoadLightCount == 3
            )
            {
                if (neighbour.RoadType == RoadType.TJunction)
                    Debug.Log("Replacing crossroad lights with T-junction lights");
                else
                    Debug.Log("Replacing T-junction lights with crossroad lights");

                RemoveTrafficLightGroupFromCell(neighbour);
                // save the current state
                GameStateContext currentContext = SimulationManager.Instance.CurrentState;
                // change it to the traffic light juntion state
                SimulationManager.Instance.SetTrafficLightSubState(TrafficLightSubState.AddJunctionLights, false);
                // place the lights
                PlaceTrafficLightsInCell(neighbour);
                // revert to previous state
                SimulationManager.Instance.SetState(currentContext);
                continue;
            }

            // if we have 3 lights and we are not a T-juntion, remove the lights
            if (neighbour.RoadType != RoadType.TJunction && group.RoadLightCount == 3)
            {
                Debug.Log("Removing T-junction lights");
                RemoveTrafficLightGroupFromCell(neighbour);
                continue;
            }
        }

    }

    private TrafficLightGroupController FindGroupForCell(GridCell cell)
    {
        TrafficLightGroupController group = null;
        List<WaypointNode> nodes = VehicleWaypointManager.Instance.GetCellWaypoints(cell);

        if (nodes == null || nodes.Count == 0) return null;

        // find the group from the cell
        foreach (WaypointNode node in nodes)
        {
            group = FindGroupForWaypoint(node);
            if (group != null) break;
        }

        return group;
    }

    public TrafficLightGroupController FindGroupForWaypoint(WaypointNode waypoint)
    {
        // Paired crossing lights
        if (waypoint.PairedCrossingWaypoint != null)
        {
            foreach (TrafficLightGroupController group in _allGroups)
            {
                if (group.IsForWaypoint(waypoint) || group.IsForWaypoint(waypoint.PairedCrossingWaypoint))
                    return group;
            }
        }

        // Junction lights
        if (waypoint.ParentCell.RoadType == RoadType.TJunction ||
            waypoint.ParentCell.RoadType == RoadType.Crossroads)
        {
            foreach (TrafficLightGroupController group in _allGroups)
            {
                if (group.IsForCell(waypoint.ParentCell))
                    return group;
            }
        }

        // Fallback — group by cell
        foreach (TrafficLightGroupController group in _allGroups)
        {
            if (group.IsForCell(waypoint.ParentCell))
                return group;
        }

        return null;
    }

    private TrafficLightGroupController FindOrCreateGroupForWaypoint(WaypointNode waypoint)
    {
        // Paired crossing lights
        if (waypoint.PairedCrossingWaypoint != null)
        {
            foreach (TrafficLightGroupController group in _allGroups)
            {
                if (group.IsForWaypoint(waypoint) || group.IsForWaypoint(waypoint.PairedCrossingWaypoint))
                    return group;
            }

            return CreateGroup(
                $"LightGroup_Crossing",
                waypoint.ParentCell.Position,
                TrafficLightGroupType.PedestrianCrossing
            );
        }

        // Junction lights
        if (waypoint.ParentCell.RoadType == RoadType.TJunction ||
            waypoint.ParentCell.RoadType == RoadType.Crossroads)
        {
            foreach (TrafficLightGroupController group in _allGroups)
            {
                if (group.IsForCell(waypoint.ParentCell))
                    return group;
            }

            return CreateGroup(
                $"LightGroup_Junction",
                waypoint.ParentCell.Position,
                TrafficLightGroupType.Junction
            );
        }

        // Fallback — group by cell
        foreach (TrafficLightGroupController group in _allGroups)
        {
            if (group.IsForCell(waypoint.ParentCell))
                return group;
        }

        return CreateGroup(
            $"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.z}",
            waypoint.ParentCell.Position,
            TrafficLightGroupType.Junction
        );
    }

    private TrafficLightGroupController CreateGroup(string name, Vector3 position, TrafficLightGroupType groupType)
    {
        GameObject groupObj = new GameObject(name);
        groupObj.transform.position = position;
        groupObj.transform.parent = this.transform;
        TrafficLightGroupController newGroup = groupObj.AddComponent<TrafficLightGroupController>();
        newGroup.Id = EntityId.New();
        newGroup.GroupType = groupType;
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

    public List<WaypointNode> GetValidWaypointsForSubState(GridCell cell)
    {
        TrafficLightSubState subState = SimulationManager.Instance.CurrentState.TrafficLightSubState;

        // Filter waypoints by type and substate
        return VehicleWaypointManager.Instance.GetCellWaypoints(cell).FindAll(w =>
        {
            if (w.Type != WaypointType.TrafficLightLocation)
                return false;

            return subState switch
            {
                TrafficLightSubState.AddJunctionLights =>
                    cell.RoadType == RoadType.TJunction || cell.RoadType == RoadType.Crossroads,
                TrafficLightSubState.AddPedestrianCrossings =>
                    cell.RoadType == RoadType.Straight,
                _ => false
            };
        });
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        TrafficLightsSaveData trafficLight = new TrafficLightsSaveData();

        foreach (TrafficLightGroupController group in _allGroups)
        {
            TrafficLightGroupSaveData groupData = new TrafficLightGroupSaveData
            {
                Id = group.Id.ToString(),
                JunctionName = group.JunctionName,
                GroupType = group.GroupType,
                Lights = new List<TrafficLightSaveData>()
            };

            foreach (TrafficLight light in group.Lights)
            {
                if (light.Light?.AssignedWaypoint == null)
                    continue;

                groupData.Lights.Add(new TrafficLightSaveData
                {
                    LightWaypointNodeId = light.Light.AssignedWaypoint.Id.ToString(),
                    Label = light.Label,
                    IsCopyOfLight = light.IsCopyOfLight,
                    LightPosition = light.LightPosition,
                    GreenDuration = light.GreenDuration,
                    YellowDuration = light.YellowDuration,
                    RedDuration = light.RedDuration,
                    AllRedDuration = light.AllRedDuration,
                    PedestrianCrossingDuration = light.PedestrianCrossingDuration,
                    OriginalLabel = light.OriginalLabel,
                    OriginalGreenDuration = light.OriginalGreenDuration,
                    OriginalYellowDuration = light.OriginalYellowDuration,
                    OriginalRedDuration = light.OriginalRedDuration,
                    OriginalAllRedDuration = light.OriginalAllRedDuration,
                    OriginalPedestrianCrossingDuration = light.OriginalPedestrianCrossingDuration
                });
            }

            trafficLight.Groups.Add(groupData);
        }

        saveData.TrafficLights = trafficLight;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.TrafficLights == null)
        {
            Debug.LogWarning("[TrafficLightManager] No traffic light data in save file.");
            return;
        }

        // clear the groups and delete all traffic light game objects from the world
        _allGroups.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        foreach (TrafficLightGroupSaveData groupData in saveData.TrafficLights.Groups)
        {
            // Create group GameObject
            GameObject groupObj = new GameObject($"LightGroup_{groupData.JunctionName}");
            groupObj.transform.position = Vector3.zero;  // Will be set later
            groupObj.transform.parent = this.transform;
            TrafficLightGroupController group = groupObj.AddComponent<TrafficLightGroupController>();

            // Set group properties
            group.SetupGroup(groupData.GroupType);
            group.Id = EntityId.FromString(groupData.Id);
            group.UpdateJunctionName(groupData.JunctionName);

            // Create lights for each phase
            foreach (TrafficLightSaveData light in groupData.Lights)
            {
                WaypointNode waypoint = VehicleWaypointManager.Instance.GetWaypointFromId(light.LightWaypointNodeId);

                if (waypoint == null)
                {
                    Debug.LogWarning($"[TrafficLightManager] Waypoint {light.LightWaypointNodeId} not found for traffic light.");
                    continue;
                }

                // Create light prefab at waypoint position
                GameObject lightObj = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);
                lightObj.transform.rotation = Quaternion.Euler(0, GetYRotationFromLightPosition(waypoint.LightPosition), 0);
                TrafficLightController newLight = lightObj.GetComponent<TrafficLightController>();
                newLight.AssignedWaypoint = waypoint;

                // Assign to waypoint
                waypoint.LaneNodeForTrafficLight.AssignedLight = newLight;

                // Register in group
                group.RegisterLight(newLight, light.LightPosition, light.LightPosition.ToString(), light.GreenDuration, light.YellowDuration, light.RedDuration, light.AllRedDuration, light.PedestrianCrossingDuration, light.OriginalLabel, light.OriginalGreenDuration, light.OriginalYellowDuration, light.OriginalRedDuration, light.OriginalAllRedDuration, light.OriginalPedestrianCrossingDuration);

                // Set group position to first light's position (or junction center)
                if (groupObj.transform.position == Vector3.zero)
                {
                    groupObj.transform.position = waypoint.Position;
                }

                lightObj.transform.parent = groupObj.transform;
            }

            _allGroups.Add(group);
        }

        Debug.Log($"[TrafficLightManager] Loaded {saveData.TrafficLights.Groups.Count} traffic light groups.");
    }
}

public enum LightState { Red, Yellow, Green }