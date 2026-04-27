using UnityEngine;
using System.Collections.Generic;

public class VehicleSpawner : MonoBehaviour
{
    public static VehicleSpawner Instance { get; private set; }

    [SerializeField] private GameObject[] _vehiclePrefabs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnVehicle(WaypointNode startWaypoint, WaypointNode targetWaypoint)
    {
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

    private GameObject GetRandomVehiclePrefab()
    {
        return _vehiclePrefabs[Random.Range(0, _vehiclePrefabs.Length)];
    }
}