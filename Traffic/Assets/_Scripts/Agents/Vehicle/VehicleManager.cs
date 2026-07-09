using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    public static VehicleManager Instance { get; private set; }

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

    private void OnEnable()
    {
        // Subscribe to input events
        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
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

    private AgentController AddAndRegisterVehicle()
    {
        return AddAndRegisterVehicle(GetRandomVehicleWaypoint(WaypointType.None));
    }

    public AgentController AddAndRegisterVehicle(WaypointNode spawnWaypoint)
    {
        // 1. Generate the ID
        EntityId newId = EntityId.New();

        // 2. Instantiate the GameObject
        // You can use Object.Instantiate with a prefab
        GameObject vehiclePrefab = _vehiclePrefabs[Random.Range(0, _vehiclePrefabs.Length)];
        Vector3 spawnLocation = Utils.GetVectorWithSetHeight(spawnWaypoint.Position, 0.2f);
        //Vector3 lookDirection = (Utils.GetVectorWithSetHeight(Camera.main.transform.position, 0.2f) - spawnLocation).normalized;
        GameObject vehicle = Instantiate(vehiclePrefab, spawnLocation, Quaternion.identity, transform);
        Vector3 lookDirection = Vector3.back;
        vehicle.transform.rotation = Quaternion.LookRotation(lookDirection);
        AgentController vc = vehicle.GetComponent<AgentController>();

        // 3. Assign the ID to the controller
        vc.Initialise(AgentType.Vehicle, newId, spawnWaypoint);

        // 4. Register in the dictionary
        _allVehicles[newId] = vc;

        // 5. Hook into the Destroy event to auto-cleanup
        // (See Step C below)

        return vc;
    }

    public void GoToRandomWaypoint(AgentController agent)
    {
        VehicleMovement pm = agent.GetComponent<VehicleMovement>();

        WaypointNode randomNode = FindValidTarget(pm.CurrentWaypoint, type: WaypointType.Entry);
        string name = "Drive to random node at " + randomNode.Position;

        if (randomNode != null)
            agent.AddGoal(new DriveToRandomGoal(randomNode, name));
        else Debug.LogWarning("No random location found");
    }

    public void GoHome(AgentController agent)
    {
        VehicleMovement vm = agent.GetComponent<VehicleMovement>();
        EntityId waypointId = RelationshipManager.Instance.GetHomeParkingSpot(agent.Id).First();

        if (!waypointId.IsValid) Debug.LogError("Home building not found");

        WaypointNode parkingSpot = RoadWaypointManager.Instance.GetWaypointFromId(waypointId);

        List<WaypointNode> newPath = AStarPathfinder.FindPath(vm.CurrentWaypoint, parkingSpot);

        if (newPath == null || newPath.Count == 0) Debug.LogError("Path to home node not found");

        string name = "Driven home to " + parkingSpot.Position;

        agent.InterruptAndAddGoal(new ParkAtHomeGoal(parkingSpot, name));
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
        List<WaypointNode> allWaypoints = RoadWaypointManager.Instance.GetAllWaypoints();
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

    public EntityId GetVehiclesHomeSpotId(EntityId vehicleId)
        => RelationshipManager.Instance.GetHomeParkingSpot(vehicleId).FirstOrDefault();

    public EntityId GetVehiclesCurrentSpotId(EntityId vehicleId)
        => RelationshipManager.Instance.GetCurrentParkingSpot(vehicleId).FirstOrDefault();
}