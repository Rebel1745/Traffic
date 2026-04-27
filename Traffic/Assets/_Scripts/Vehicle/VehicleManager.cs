using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    public static VehicleManager Instance { get; private set; }

    private List<VehicleController> _activeVehicles = new List<VehicleController>();

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
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Vehicles)
            return;

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

        VehicleSpawner.Instance.SpawnVehicle(startWaypoint, targetWaypoint);
    }

    public void RegisterVehicle(VehicleController vehicle)
    {
        if (vehicle != null && !_activeVehicles.Contains(vehicle))
        {
            _activeVehicles.Add(vehicle);
        }
    }

    public void UnregisterVehicle(VehicleController vehicle)
    {
        if (_activeVehicles.Contains(vehicle))
        {
            _activeVehicles.Remove(vehicle);
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
                    //Debug.Log($"New target assigned to vehicle: {newTarget.Position}");
                    return;
                }
            }

            attempts++;
        }

        // Failed to find a valid target after 3 attempts
        Debug.LogWarning($"Failed to find valid target for vehicle after {maxAttempts} attempts. Destroying vehicle.");
        RemoveVehicle(vehicle);
    }

    public WaypointNode FindValidTarget(WaypointNode startWaypoint, int maxAttempts = 10)
    {
        var allWaypoints = RoadWaypointManager.Instance.GetAllWaypoints();
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

    public WaypointNode GetRandomEntryWaypoint()
    {
        var allWaypoints = RoadWaypointManager.Instance.GetAllWaypoints();
        var entryWaypoints = allWaypoints.Where(w => w.Type == WaypointType.Entry).ToList();

        if (entryWaypoints.Count == 0)
        {
            Debug.LogWarning("No waypoints");
            return null;
        }

        return entryWaypoints[Random.Range(0, entryWaypoints.Count)];
    }

    public void RemoveVehicle(VehicleController vehicle)
    {
        if (_activeVehicles.Contains(vehicle))
        {
            _activeVehicles.Remove(vehicle);
        }
        Destroy(vehicle.gameObject);
    }
}