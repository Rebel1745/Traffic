using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    public static VehicleManager Instance { get; private set; }

    [SerializeField] private GameObject[] _vehiclePrefabs;

    private Dictionary<EntityId, VehicleController> _allVehicles = new();

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

    private VehicleController AddAndRegisterVehicle()
    {
        return AddAndRegisterVehicle(GetRandomVehicleWaypoint(WaypointType.None));
    }

    public VehicleController AddAndRegisterVehicle(WaypointNode spawnWaypoint)
    {
        // 1. Generate the ID
        EntityId newId = EntityId.New();

        // 2. Instantiate the GameObject
        // You can use Object.Instantiate with a prefab
        GameObject vehiclePrefab = _vehiclePrefabs[Random.Range(0, _vehiclePrefabs.Length)];
        Vector3 spawnLocation = Utils.GetVectorWithSetHeight(spawnWaypoint.Position, 0.2f);
        //Vector3 lookDirection = (Utils.GetVectorWithSetHeight(Camera.main.transform.position, 0.2f) - spawnLocation).normalized;
        Vector3 lookDirection = Vector3.back;
        GameObject vehicle = Instantiate(vehiclePrefab, spawnLocation, Quaternion.identity, transform);
        vehicle.transform.rotation = Quaternion.LookRotation(lookDirection);
        VehicleController vc = vehicle.GetComponent<VehicleController>();

        // 3. Assign the ID to the controller
        vc.Initialise(newId, spawnWaypoint);

        // 4. Register in the dictionary
        _allVehicles[newId] = vc;

        // 5. Hook into the Destroy event to auto-cleanup
        // (See Step C below)

        return vc;
    }

    public void RequestNewTarget(VehicleController vehicle, WaypointType previousTargetType)
    {
        if (vehicle == null || vehicle.CurrentWaypoint == null)
        {
            Debug.LogWarning("Invalid vehicle or current waypoint!");
            return;
        }

        int attempts = 0;
        int maxAttempts = 3;

        while (attempts < maxAttempts)
        {
            WaypointNode newTarget = null;

            if (previousTargetType != WaypointType.VehicleParking)
                newTarget = GetRandomVehicleWaypoint(WaypointType.VehicleParking);

            if (newTarget == null) newTarget = FindValidTarget(vehicle.CurrentWaypoint);

            if (newTarget != null)
            {
                List<WaypointNode> newPath = AStarPathfinder.FindPath(vehicle.CurrentWaypoint, newTarget);

                if (newPath != null && newPath.Count > 0)
                {
                    vehicle.SetNewPath(newPath, newTarget);
                    //Debug.Log($"New target assigned to vehicle: {newTarget.Position}");
                    return;
                }
            }
            Debug.Log($"Path to {newTarget.NetworkType} {newTarget.Type} {newTarget.Position} not found");
            attempts++;
        }

        // Failed to find a valid target after 3 attempts
        Debug.LogWarning($"Failed to find valid target for vehicle after {maxAttempts} attempts. Destroying vehicle.");
        //RemoveVehicle(vehicle);
    }

    public WaypointNode FindValidTarget(WaypointNode startWaypoint, int maxAttempts = 10)
    {
        var allWaypoints = RoadWaypointManager.Instance.GetAllWaypoints();
        var entryWaypoints = allWaypoints.Where(w => w != startWaypoint && w.Type != WaypointType.TrafficLightLocation).ToList();

        if (entryWaypoints.Count == 0)
            return null;

        // Try to find a valid target
        for (int i = 0; i < maxAttempts; i++)
        {
            WaypointNode candidate = entryWaypoints[Random.Range(0, entryWaypoints.Count)];

            // Check if path exists
            List<WaypointNode> path = AStarPathfinder.FindPath(startWaypoint, candidate);
            if (path != null && path.Count > 0)
            {
                return candidate;
            }
            Debug.Log($"Path to {candidate.NetworkType} {candidate.Type} {candidate.ParentCell.Position} not found");
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
}