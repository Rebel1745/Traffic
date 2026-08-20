using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleManager : MonoBehaviour, ISaveable
{
    public static VehicleManager Instance { get; private set; }

    public string SaveKey => "Vehicles";

    [SerializeField] private GameObject[] _vehiclePrefabs;

    private Dictionary<EntityId, AgentController> _allVehicles = new();

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
        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.UnregisterSaveable(this);
        InputManager.OnLeftClickPressed -= HandleLeftClickPressed;
    }

    private void HandleLeftClickPressed(Vector2 screenPosition)
    {
        // Only spawn vehicles when simulation is running
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Vehicles)
            return;

        //AddAndRegisterVehicle();

        // Get a random valid spawn location
        // WaypointNode startWaypoint = GetRandomVehicleWaypoint(WaypointType.VehicleParking);
        // if (startWaypoint == null)
        // {
        //     Debug.LogWarning("No valid spawn location found!");
        //     return;
        // }

        // // Get a random valid target
        // //WaypointNode targetWaypoint = FindValidTarget(startWaypoint);
        // WaypointNode targetWaypoint = GetRandomVehicleWaypoint(WaypointType.VehicleEntryExit);
        // if (targetWaypoint == null)
        // {
        //     Debug.LogWarning("No valid target found for spawn location!");
        //     return;
        // }

        // VehicleSpawner.Instance.SpawnVehicle(startWaypoint, targetWaypoint);
    }

    public AgentController AddAndRegisterVehicle(EntityId id, WaypointNode spawnWaypoint, WaypointNode targetWaypoint)
    {
        // 1. Generate the ID
        if (id.Equals(EntityId.None))
            id = EntityId.New();

        // 2. Instantiate the GameObject
        // You can use Object.Instantiate with a prefab
        GameObject vehiclePrefab = _vehiclePrefabs[Random.Range(0, _vehiclePrefabs.Length)];
        Vector3 spawnLocation = Utils.GetVectorWithSetHeight(spawnWaypoint.Position, 0.2f);
        GameObject vehicle = Instantiate(vehiclePrefab, spawnLocation, Quaternion.identity, transform);
        Vector3 lookDirection = Vector3.back;
        vehicle.transform.rotation = Quaternion.LookRotation(lookDirection);
        AgentController vc = vehicle.GetComponent<AgentController>();

        // 3. Assign the ID to the controller
        vc.Initialise(AgentType.Vehicle, id, spawnWaypoint, targetWaypoint);

        // 4. Register in the dictionary
        _allVehicles[id] = vc;

        return vc;
    }

    public void GoToRandomWaypoint(AgentController agent)
    {
        VehicleMovement pm = agent.GetComponent<VehicleMovement>();

        WaypointNode randomNode = FindValidTarget(pm.CurrentWaypoint, type: WaypointType.Entry);
        string name = "Drive to random node at " + randomNode.Position;

        if (randomNode != null)
            agent.AddGoal(new DriveToWaypointGoal(randomNode, name));
        else Debug.LogWarning("No random location found");
    }

    public void GoHome(AgentController agent)
    {
        VehicleMovement vm = agent.GetComponent<VehicleMovement>();
        EntityId waypointId = RelationshipManager.Instance.GetHomeParkingSpotForVehicle(agent.Id).First();

        if (!waypointId.IsValid) Debug.LogError("Home building not found");

        WaypointNode parkingSpot = VehicleWaypointManager.Instance.GetWaypointFromId(waypointId);

        List<WaypointNode> newPath = AStarPathfinder.FindPath(vm.CurrentWaypoint, parkingSpot);

        if (newPath == null || newPath.Count == 0) Debug.LogError("Path to home node not found");

        string name = "Driven home to " + parkingSpot.Position;

        agent.InterruptAndAddGoal(new DriveToWaypointGoal(parkingSpot, name));
    }

    public void GoToRandomCarParkingSpace(AgentController agent)
    {
        List<EntityId> carParks = BuildingManager.Instance.GetBuildingsByType(BuildingSubState.CarPark);

        if (carParks.Count == 0) Debug.LogError("No car parks found!");

        BuildingCarPark carPark = BuildingManager.Instance.GetBuilding(carParks.First()) as BuildingCarPark;

        WaypointNode parkingSpot = carPark.GetRandomEmptyParkingSpot();

        agent.AddGoal(new DriveToWaypointGoal(parkingSpot, "Driving to random parking spot"));
    }

    public WaypointNode FindValidTarget(WaypointNode startWaypoint, WaypointType type = WaypointType.None, int maxAttempts = 10)
    {
        // Try to find a valid target
        for (int i = 0; i < maxAttempts; i++)
        {
            WaypointNode candidate = GetRandomVehicleWaypoint(type);
            if (candidate == startWaypoint) continue;

            // Check if path exists
            List<WaypointNode> path = AStarPathfinder.FindPath(startWaypoint, candidate);
            if (path != null && path.Count > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    public WaypointNode GetRandomVehicleWaypoint(WaypointType type)
    {
        List<WaypointNode> allWaypoints = VehicleWaypointManager.Instance.GetAllWaypoints();
        List<WaypointNode> specificNodes = allWaypoints;

        if (type != WaypointType.None)
            specificNodes = allWaypoints.Where(w => w.Type == type).ToList();

        if (specificNodes.Count == 0)
        {
            Debug.LogWarning($"No waypoints of type {type} found");
            return null;
        }

        return specificNodes[Random.Range(0, specificNodes.Count)];
    }

    public AgentController GetVehicle(EntityId entityId)
        => _allVehicles[entityId];

    public AgentController GetVehicle(string id)
        => GetVehicle(EntityId.FromString(id));

    public EntityId GetVehiclesHomeSpotId(EntityId vehicleId)
        => RelationshipManager.Instance.GetHomeParkingSpotForVehicle(vehicleId).FirstOrDefault();

    public void PopulateSaveData(GameSaveData saveData)
    {
        saveData.Vehicles = new();

        foreach (AgentController agent in _allVehicles.Values)
        {
            VehicleMovement vm = agent.GetComponent<VehicleMovement>();

            VehicleSaveData vehicle = new()
            {
                Id = agent.Id.ToString(),
                CurrentWaypointId = vm.CurrentWaypoint?.Id.ToString(),
                TargetWaypointId = vm.TargetWaypoint?.Id.ToString()
            };

            saveData.Vehicles.Add(vehicle);
        }
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.Vehicles == null)
        {
            Debug.LogWarning("[VehicleManager] No Vehicle data in save file.");
            return;
        }

        // clear and delete all Vehicle game objects from the world
        _allVehicles.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        foreach (VehicleSaveData v in saveData.Vehicles)
        {
            EntityId vId = EntityId.FromString(v.Id);
            WaypointNode currentWaypoint = VehicleWaypointManager.Instance.GetWaypointFromId(v.CurrentWaypointId);
            WaypointNode targetWaypoint = VehicleWaypointManager.Instance.GetWaypointFromId(v.TargetWaypointId);

            AddAndRegisterVehicle(vId, currentWaypoint, targetWaypoint);
        }
    }
}