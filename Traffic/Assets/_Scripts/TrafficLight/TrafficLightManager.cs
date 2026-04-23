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
    [SerializeField] private float _redOverlapDuration = 0.5f;

    public string SaveKey => "TrafficLights";

    private List<TrafficLightGroupController> _allGroups = new();
    private bool _subscribedToSaveManager;

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
        if (!_subscribedToSaveManager)
            TryToSubscribeToSaveManager();
    }

    private void OnEnable()
    {
        TryToSubscribeToSaveManager();
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
            _subscribedToSaveManager = false;
        }
    }

    private void TryToSubscribeToSaveManager()
    {
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.RegisterSaveable(this);
        _subscribedToSaveManager = true;
    }

    public void PlaceLightAtWaypoint(WaypointNode waypoint)
    {
        if (waypoint == null || waypoint.AssignedLight != null)
            return;

        GameObject lightObj = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);
        TrafficLightController light = lightObj.GetComponent<TrafficLightController>();

        light.AssignedWaypoint = waypoint;
        waypoint.LaneNodeForTrafficLight.AssignedLight = light;

        TrafficLightGroupController group = FindOrCreateGroupForWaypoint(waypoint);
        lightObj.transform.parent = group.gameObject.transform;

        // Register with default timings (can be adjusted via UI later)
        group.RegisterLight(light, waypoint.LightPosition, waypoint.LightPosition.ToString(), _greenDuration, _yellowDuration, _redDuration, _redOverlapDuration, waypoint.LightPosition.ToString(), _greenDuration, _yellowDuration, _redDuration, _redOverlapDuration);

        //Debug.Log($"Confirmed traffic light at {waypoint.Id}");
    }

    public void RemoveTrafficLightGroupFromCell(GridCell cell)
    {
        TrafficLightGroupController group = null;

        // find the group from the cell
        foreach (WaypointNode node in RoadWaypointManager.Instance.GetCellWaypoints(cell))
        {
            group = FindGroupForWaypoint(node);
            if (group != null) break;
        }

        // we have the group, remove it from the groups list
        if (group == null) { Debug.LogError("Can't find group"); return; }

        _allGroups.Remove(group);

        Destroy(group.gameObject);

        cell.HasTrafficLights = false;

        UIManager.Instance.CloseTrafficLightGroupDetails();
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
        newGroup.Id = System.Guid.NewGuid().ToString();
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

    public void PopulateSaveData(GameSaveData saveData)
    {
        var trafficLight = new TrafficLightsSaveData();

        foreach (var group in _allGroups)
        {
            var groupData = new TrafficLightGroupSaveData
            {
                Id = group.Id,
                JunctionName = group.JunctionName,
                GroupType = group.GroupType,
                Lights = new List<TrafficLightSaveData>()
            };

            foreach (var light in group.Lights)
            {
                if (light.Light?.AssignedWaypoint == null)
                    continue;

                groupData.Lights.Add(new TrafficLightSaveData
                {
                    LightWaypointNodeId = light.Light.AssignedWaypoint.Id,
                    Label = light.Label,
                    IsCopyOfLight = light.IsCopyOfLight,
                    LightPosition = light.LightPosition,
                    GreenDuration = light.GreenDuration,
                    YellowDuration = light.YellowDuration,
                    RedDuration = light.RedDuration,
                    RedOverlapDuration = light.RedOverlapDuration,
                    OriginalLabel = light.OriginalLabel,
                    OriginalGreenDuration = light.OriginalGreenDuration,
                    OriginalYellowDuration = light.OriginalYellowDuration,
                    OriginalRedDuration = light.OriginalRedDuration,
                    OriginalRedOverlapDuration = light.OriginalRedOverlapDuration
                });
            }

            trafficLight.Groups.Add(groupData);
        }

        saveData.trafficLights = trafficLight;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.trafficLights == null)
        {
            Debug.LogWarning("[TrafficLightManager] No traffic light data in save file.");
            return;
        }

        _allGroups.Clear();

        var nodeLookup = RoadWaypointManager.Instance.GetAllWaypointLookup();  // You'll need to expose this method

        foreach (var groupData in saveData.trafficLights.Groups)
        {
            // Create group GameObject
            GameObject groupObj = new GameObject($"LightGroup_{groupData.JunctionName}");
            groupObj.transform.position = Vector3.zero;  // Will be set later
            groupObj.transform.parent = this.transform;
            TrafficLightGroupController group = groupObj.AddComponent<TrafficLightGroupController>();

            // Set group properties
            group.SetupGroup(groupData.GroupType);
            group.Id = groupData.Id;
            group.UpdateJunctionName(groupData.JunctionName);

            // Create lights for each phase
            foreach (TrafficLightSaveData light in groupData.Lights)
            {
                if (!nodeLookup.TryGetValue(light.LightWaypointNodeId, out var waypoint))
                {
                    Debug.LogWarning($"[TrafficLightManager] Waypoint {light.LightWaypointNodeId} not found for traffic light.");
                    continue;
                }

                // Create light prefab at waypoint position
                GameObject lightObj = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);
                TrafficLightController newLight = lightObj.GetComponent<TrafficLightController>();
                newLight.AssignedWaypoint = waypoint;

                // Assign to waypoint
                waypoint.LaneNodeForTrafficLight.AssignedLight = newLight;

                // Register in group
                group.RegisterLight(newLight, light.LightPosition, light.LightPosition.ToString(), light.GreenDuration, light.YellowDuration, light.RedDuration, light.RedOverlapDuration, light.OriginalLabel, light.OriginalGreenDuration, light.OriginalYellowDuration, light.OriginalRedDuration, light.OriginalRedOverlapDuration);

                // Set group position to first light's position (or junction center)
                if (groupObj.transform.position == Vector3.zero)
                {
                    groupObj.transform.position = waypoint.Position;
                }

                lightObj.transform.parent = groupObj.transform;
            }

            _allGroups.Add(group);
        }

        Debug.Log($"[TrafficLightManager] Loaded {saveData.trafficLights.Groups.Count} traffic light groups.");
    }
}