using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    [Header("References")]
    public RoadGraph RoadGraph;
    public GameObject VehiclePrefab;

    [Header("Spawn Settings")]
    public int MaxVehicles = 50;
    public float SpawnInterval = 2f;
    public float MinSpawnDistance = 10f; // Minimum distance between vehicles on spawn

    [Header("Vehicle Settings")]
    public float MinSpeed = 8f;
    public float MaxSpeed = 15f;

    private List<VehicleController> activeVehicles = new List<VehicleController>();
    private float spawnTimer = 0f;

    void Update()
    {
        // Auto-spawn vehicles
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= SpawnInterval && activeVehicles.Count < MaxVehicles)
        {
            SpawnRandomVehicle();
            spawnTimer = 0f;
        }

        // Clean up null vehicles
        activeVehicles.RemoveAll(v => v == null);
    }

    /// <summary>
    /// Spawn a vehicle on a random lane
    /// </summary>
    public VehicleController SpawnRandomVehicle()
    {
        if (RoadGraph == null || RoadGraph.Roads.Count == 0)
        {
            Debug.LogWarning("No roads available to spawn vehicle");
            return null;
        }

        // Pick a random road
        Road randomRoad = RoadGraph.Roads[Random.Range(0, RoadGraph.Roads.Count)];

        // Pick a random direction (A to B or B to A)
        List<Lane> lanes = Random.value > 0.5f ? randomRoad.LanesAtoB : randomRoad.LanesBtoA;

        if (lanes.Count == 0)
        {
            Debug.LogWarning("Selected road has no lanes");
            return null;
        }

        // Pick a random lane
        Lane randomLane = lanes[Random.Range(0, lanes.Count)];

        // Check if lane is clear enough to spawn
        if (!IsLaneClearForSpawn(randomLane))
        {
            return null; // Try again next time
        }

        return SpawnVehicleOnLane(randomLane, 0);
    }

    /// <summary>
    /// Spawn a vehicle on a specific lane at a specific waypoint
    /// </summary>
    public VehicleController SpawnVehicleOnLane(Lane lane, int waypointIndex = 0)
    {
        if (lane == null || lane.Waypoints.Count == 0)
        {
            Debug.LogWarning("Invalid lane for spawning");
            return null;
        }

        // Clamp waypoint index
        waypointIndex = Mathf.Clamp(waypointIndex, 0, lane.Waypoints.Count - 1);

        // Instantiate vehicle
        GameObject vehicleObj = Instantiate(VehiclePrefab, lane.Waypoints[waypointIndex], Quaternion.identity);
        VehicleController vehicle = vehicleObj.GetComponent<VehicleController>();

        if (vehicle == null)
        {
            Debug.LogError("Vehicle prefab must have VehicleController component");
            Destroy(vehicleObj);
            return null;
        }

        // Setup vehicle
        vehicle.CurrentLane = lane;
        vehicle.CurrentWaypointIndex = waypointIndex;
        vehicle.Speed = Random.Range(MinSpeed, MaxSpeed);
        vehicle.TargetSpeed = vehicle.Speed;

        // Set initial rotation to face the next waypoint
        if (waypointIndex < lane.Waypoints.Count - 1)
        {
            Vector3 direction = (lane.Waypoints[waypointIndex + 1] - lane.Waypoints[waypointIndex]).normalized;
            vehicleObj.transform.rotation = Quaternion.LookRotation(direction);
        }

        activeVehicles.Add(vehicle);

        Debug.Log($"Spawned vehicle on lane {lane.LaneID} at waypoint {waypointIndex}");

        return vehicle;
    }

    /// <summary>
    /// Spawn a vehicle on a specific road
    /// </summary>
    public VehicleController SpawnVehicleOnRoad(Road road, bool aToB = true, int laneIndex = 0)
    {
        if (road == null)
        {
            Debug.LogWarning("Invalid road for spawning");
            return null;
        }

        List<Lane> lanes = aToB ? road.LanesAtoB : road.LanesBtoA;

        if (laneIndex < 0 || laneIndex >= lanes.Count)
        {
            Debug.LogWarning($"Lane index {laneIndex} out of range. Road has {lanes.Count} lanes in this direction");
            return null;
        }

        return SpawnVehicleOnLane(lanes[laneIndex], 0);
    }

    /// <summary>
    /// Check if a lane is clear enough at the start to spawn a vehicle
    /// </summary>
    private bool IsLaneClearForSpawn(Lane lane)
    {
        Vector3 spawnPosition = lane.Waypoints[0];

        foreach (VehicleController vehicle in activeVehicles)
        {
            if (vehicle.CurrentLane == lane)
            {
                // Check if vehicle is too close to spawn point
                float distance = Vector3.Distance(vehicle.transform.position, spawnPosition);
                if (distance < MinSpawnDistance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Spawn a vehicle at a specific intersection
    /// </summary>
    public VehicleController SpawnVehicleAtIntersection(Intersection intersection, int roadIndex = 0)
    {
        if (intersection == null || intersection.ConnectedRoads.Count == 0)
        {
            Debug.LogWarning("Invalid intersection or no connected roads");
            return null;
        }

        roadIndex = Mathf.Clamp(roadIndex, 0, intersection.ConnectedRoads.Count - 1);
        Road road = intersection.ConnectedRoads[roadIndex];

        // Determine which direction to spawn (away from intersection)
        bool aToB = road.IntersectionA == intersection;

        return SpawnVehicleOnRoad(road, aToB, 0);
    }

    /// <summary>
    /// Remove a vehicle from the simulation
    /// </summary>
    public void RemoveVehicle(VehicleController vehicle)
    {
        if (vehicle != null)
        {
            activeVehicles.Remove(vehicle);
            Destroy(vehicle.gameObject);
        }
    }

    /// <summary>
    /// Clear all vehicles
    /// </summary>
    public void ClearAllVehicles()
    {
        foreach (VehicleController vehicle in activeVehicles)
        {
            if (vehicle != null)
            {
                Destroy(vehicle.gameObject);
            }
        }
        activeVehicles.Clear();
    }

    /// <summary>
    /// Get all active vehicles
    /// </summary>
    public List<VehicleController> GetActiveVehicles()
    {
        return new List<VehicleController>(activeVehicles);
    }
}