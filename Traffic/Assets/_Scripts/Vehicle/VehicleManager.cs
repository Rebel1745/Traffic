using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    public static VehicleManager Instance { get; private set; }

    [Header("Vehicle Settings")]
    [SerializeField] private GameObject vehiclePrefab;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo;

    private List<VehicleController> activeVehicles = new List<VehicleController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(3))
        {
            SpawnVehicle();
        }
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

        // Spawn vehicle
        GameObject vehicleObj = Instantiate(vehiclePrefab, startWaypoint.Position, Quaternion.identity);
        VehicleController vehicle = vehicleObj.GetComponent<VehicleController>();

        if (vehicle != null)
        {
            // Calculate initial rotation to face next waypoint
            if (path.Count > 1)
            {
                Vector3 direction = (path[0].Position - path[0].Position).normalized;
                vehicleObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            vehicle.Initialize(path, targetWaypoint);
            activeVehicles.Add(vehicle);
            Debug.Log($"Vehicle spawned at {startWaypoint.Position} with target {targetWaypoint.Position}");
        }
        else
        {
            Debug.LogError("Vehicle prefab does not have VehicleController component!");
            Destroy(vehicleObj);
        }
    }

    public WaypointNode GetRandomEntryWaypoint()
    {
        var allWaypoints = WaypointManager.Instance.GetAllWaypoints();
        var entryWaypoints = allWaypoints.Where(w => w.Type == WaypointType.Entry).ToList();

        if (entryWaypoints.Count == 0)
            return null;

        return entryWaypoints[Random.Range(0, entryWaypoints.Count)];
    }

    public WaypointNode FindValidTarget(WaypointNode startWaypoint, int maxAttempts = 10)
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

    public void RequestNewTarget(VehicleController vehicle)
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
            WaypointNode newTarget = FindValidTarget(vehicle.CurrentWaypoint);

            if (newTarget != null)
            {
                List<WaypointNode> newPath = AStarPathfinder.FindPath(vehicle.CurrentWaypoint, newTarget);

                if (newPath != null && newPath.Count > 0)
                {
                    vehicle.SetNewPath(newPath, newTarget);
                    Debug.Log($"New target assigned to vehicle: {newTarget.Position}");
                    return;
                }
            }

            attempts++;
        }

        // Failed to find a valid target after 3 attempts
        Debug.LogWarning($"Failed to find valid target for vehicle after {maxAttempts} attempts. Destroying vehicle.");
        RemoveVehicle(vehicle);
    }

    public void RemoveVehicle(VehicleController vehicle)
    {
        if (activeVehicles.Contains(vehicle))
        {
            activeVehicles.Remove(vehicle);
        }
        Destroy(vehicle.gameObject);
    }

    public void RecalculateAllVehiclePaths()
    {
        Debug.Log($"Recalculating paths for {activeVehicles.Count} vehicles...");

        List<VehicleController> vehiclesToRemove = new List<VehicleController>();

        foreach (var vehicle in activeVehicles)
        {
            if (vehicle == null || vehicle.CurrentWaypoint == null || vehicle.TargetWaypoint == null)
            {
                vehiclesToRemove.Add(vehicle);
                continue;
            }

            // Try to find a new path to the same target
            List<WaypointNode> newPath = AStarPathfinder.FindPath(vehicle.CurrentWaypoint, vehicle.TargetWaypoint);

            if (newPath != null && newPath.Count > 0)
            {
                vehicle.SetNewPath(newPath, vehicle.TargetWaypoint);
                Debug.Log($"Path recalculated for vehicle at {vehicle.transform.position}");
            }
            else
            {
                // Can't reach current target, try to find a new one
                Debug.LogWarning($"Vehicle at {vehicle.transform.position} can no longer reach target. Finding new target...");
                RequestNewTarget(vehicle);
            }
        }

        // Clean up invalid vehicles
        foreach (var vehicle in vehiclesToRemove)
        {
            RemoveVehicle(vehicle);
        }

        Debug.Log("Path recalculation complete.");
    }

    public void RecheckAllVehiclePaths()
    {
        Debug.Log($"Rechecking paths for {activeVehicles.Count} vehicles...");

        List<VehicleController> vehiclesToRemove = new List<VehicleController>();

        foreach (var vehicle in activeVehicles)
        {
            if (vehicle == null)
            {
                vehiclesToRemove.Add(vehicle);
                continue;
            }

            // Check if current path is still valid
            if (!vehicle.IsPathValid())
            {
                Debug.LogWarning($"Vehicle at {vehicle.transform.position} has invalid path. Recalculating...");

                if (vehicle.CurrentWaypoint != null && vehicle.TargetWaypoint != null)
                {
                    List<WaypointNode> newPath = AStarPathfinder.FindPath(vehicle.CurrentWaypoint, vehicle.TargetWaypoint);

                    if (newPath != null && newPath.Count > 0)
                    {
                        vehicle.SetNewPath(newPath, vehicle.TargetWaypoint);
                    }
                    else
                    {
                        RequestNewTarget(vehicle);
                    }
                }
                else
                {
                    vehiclesToRemove.Add(vehicle);
                }
            }
        }

        // Clean up invalid vehicles
        foreach (var vehicle in vehiclesToRemove)
        {
            RemoveVehicle(vehicle);
        }

        Debug.Log("Path recheck complete.");
    }

    public int GetActiveVehicleCount()
    {
        return activeVehicles.Count;
    }

    private void OnDrawGizmos()
    {
        if (activeVehicles == null || !showDebugInfo) return;

        // Draw vehicle paths
        foreach (var vehicle in activeVehicles)
        {
            if (vehicle != null && vehicle.Path != null && vehicle.Path.Count > 1)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < vehicle.Path.Count - 1; i++)
                {
                    Gizmos.DrawLine(vehicle.Path[i].Position, vehicle.Path[i + 1].Position);
                }
            }
        }
    }
}