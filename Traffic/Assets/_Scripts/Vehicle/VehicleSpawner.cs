using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class VehicleSpawner : MonoBehaviour
{
    public static VehicleSpawner Instance { get; private set; }

    [SerializeField] private GameObject[] vehiclePrefabs;

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
        InputManager.OnMiddleClickPressed += HandleMiddleClickPressed;
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        InputManager.OnMiddleClickPressed -= HandleMiddleClickPressed;
    }

    private void HandleMiddleClickPressed(Vector2 screenPosition)
    {
        // Only spawn vehicles when simulation is running
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Roads)
            return;

        SpawnVehicle();
    }

    public void SpawnVehicle()
    {
        // Get a random valid spawn location
        WaypointNode startWaypoint = GetRandomEntryWaypoint();
        if (startWaypoint == null)
        {
            Debug.LogWarning("No valid spawn location found!");
            return;
        }

        // Get a random valid target
        WaypointNode targetWaypoint = FindValidTarget(startWaypoint);
        if (targetWaypoint == null)
        {
            Debug.LogWarning("No valid target found for spawn location!");
            return;
        }

        // Find path
        List<WaypointNode> path = AStarPathfinder.FindPath(startWaypoint, targetWaypoint);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("No path found between spawn and target!");
            return;
        }

        GameObject vehiclePrefab = GetRandomVehiclePrefab();
        // Spawn vehicle
        GameObject vehicleObj = Instantiate(vehiclePrefab, startWaypoint.Position, Quaternion.identity);
        VehicleController vehicle = vehicleObj.GetComponent<VehicleController>();

        if (vehicle != null)
        {
            // Calculate initial rotation to face next waypoint
            if (path.Count > 1)
            {
                Vector3 direction = (path[1].Position - path[0].Position).normalized;
                vehicleObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            vehicle.Initialize(path, targetWaypoint);
            VehicleManager.Instance.RegisterVehicle(vehicle);
            Debug.Log($"Vehicle spawned at {startWaypoint.Position} with target {targetWaypoint.Position}");
        }
        else
        {
            Debug.LogError("Vehicle prefab does not have VehicleController component!");
            Destroy(vehicleObj);
        }
    }

    private WaypointNode GetRandomEntryWaypoint()
    {
        var allWaypoints = WaypointManager.Instance.GetAllWaypoints();
        var entryWaypoints = allWaypoints.Where(w => w.Type == WaypointType.Entry).ToList();

        if (entryWaypoints.Count == 0)
            return null;

        return entryWaypoints[Random.Range(0, entryWaypoints.Count)];
    }

    private WaypointNode FindValidTarget(WaypointNode startWaypoint, int maxAttempts = 10)
    {
        var allWaypoints = WaypointManager.Instance.GetAllWaypoints();
        var entryWaypoints = allWaypoints.Where(w => w.Type == WaypointType.Entry && w != startWaypoint).ToList();

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
        }

        return null;
    }

    private GameObject GetRandomVehiclePrefab()
    {
        return vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];
    }
}