using System.Collections.Generic;
using UnityEngine;

public class TrafficLightManager : MonoBehaviour, ISaveable
{
    public static TrafficLightManager Instance { get; private set; }

    [SerializeField] private GameObject _trafficLightPrefab;
    public GameObject TrafficLightPrefab { get { return _trafficLightPrefab; } }

    public string SaveKey => "TrafficLights";

    [SerializeField] private Camera _mainCamera;

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
        group.RegisterLight(light, greenDuration: 10f, yellowDuration: 3f, redDuration: 10f, redOverlapDuration: 2f);

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
        waypoint.LaneNodeForTrafficLight.AssignedLight = null;

        Debug.Log($"Removed traffic light at {waypoint.Id}");
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
                $"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.y}_Crossing",
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
                $"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.y}_Junction",
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
            $"LightGroup_{waypoint.ParentCell.Position.x}_{waypoint.ParentCell.Position.y}",
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
                id = group.Id,
                groupType = group.GroupType,
                lights = new List<TrafficLightSaveData>()
            };

            foreach (var light in group.Lights)
            {
                if (light.Light?.AssignedWaypoint == null)
                    continue;

                groupData.lights.Add(new TrafficLightSaveData
                {
                    lightWaypointNodeId = light.Light.AssignedWaypoint.Id,
                    greenDuration = light.GreenDuration,
                    yellowDuration = light.YellowDuration,
                    redDuration = light.RedDuration,
                    redOverlapDuration = light.RedOverlapDuration
                });
            }

            trafficLight.groups.Add(groupData);
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

        var nodeLookup = WaypointManager.Instance.GetAllWaypointLookup();  // You'll need to expose this method

        foreach (var groupData in saveData.trafficLights.groups)
        {
            // Create group GameObject
            GameObject groupObj = new GameObject($"LightGroup_{groupData.id}");
            groupObj.transform.position = Vector3.zero;  // Will be set later
            groupObj.transform.parent = this.transform;
            TrafficLightGroupController group = groupObj.AddComponent<TrafficLightGroupController>();

            // Set group properties
            group.SetupGroup(groupData.groupType);
            group.Id = groupData.id;

            // Create lights for each phase
            foreach (TrafficLightSaveData light in groupData.lights)
            {
                if (!nodeLookup.TryGetValue(light.lightWaypointNodeId, out var waypoint))
                {
                    Debug.LogWarning($"[TrafficLightManager] Waypoint {light.lightWaypointNodeId} not found for traffic light.");
                    continue;
                }

                // Create light prefab at waypoint position
                GameObject lightObj = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);
                TrafficLightController newLight = lightObj.GetComponent<TrafficLightController>();
                newLight.AssignedWaypoint = waypoint;

                // Assign to waypoint
                waypoint.LaneNodeForTrafficLight.AssignedLight = newLight;

                // Register in group
                group.RegisterLight(newLight, light.greenDuration, light.yellowDuration, light.redDuration, light.redOverlapDuration);

                // Set group position to first light's position (or junction center)
                if (groupObj.transform.position == Vector3.zero)
                {
                    groupObj.transform.position = waypoint.Position;
                }

                lightObj.transform.parent = groupObj.transform;
            }

            _allGroups.Add(group);
        }

        Debug.Log($"[TrafficLightManager] Loaded {saveData.trafficLights.groups.Count} traffic light groups.");
    }
}