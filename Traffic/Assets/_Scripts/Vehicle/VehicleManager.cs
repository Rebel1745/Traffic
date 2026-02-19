using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    public static VehicleManager Instance { get; private set; }

    private List<VehicleController> activeVehicles = new List<VehicleController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterVehicle(VehicleController vehicle)
    {
        if (vehicle != null && !activeVehicles.Contains(vehicle))
        {
            activeVehicles.Add(vehicle);
        }
    }

    public void UnregisterVehicle(VehicleController vehicle)
    {
        if (activeVehicles.Contains(vehicle))
        {
            activeVehicles.Remove(vehicle);
        }
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

    public List<VehicleController> GetActiveVehicles()
    {
        return new List<VehicleController>(activeVehicles);
    }
}