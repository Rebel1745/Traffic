using UnityEngine;
using System.Collections.Generic;

public class VehicleController : MonoBehaviour
{
    [Header("References")]
    public GameObject vehiclePrefab;
    public RoadGrid roadGrid; // Reference to your road grid

    [Header("Settings")]
    public float spawnHeight = 0.5f;
    public int maxVehicles = 50;

    [Header("Debug")]
    public bool showDebugPaths = true;

    private List<Vehicle> activeVehicles = new List<Vehicle>();
    private LanePathfinder pathfinder;

    private void Start()
    {
        pathfinder = new LanePathfinder();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            SpawnVehicle();
        }
    }

    public void SpawnVehicle()
    {
        if (activeVehicles.Count >= maxVehicles)
        {
            Debug.LogWarning("Maximum number of vehicles reached!");
            return;
        }

        // Get a random road tile with lanes
        GridCell spawnCell = RoadGrid.Instance.GetRandomRoadCell();
        if (spawnCell == null || spawnCell.LaneData == null || spawnCell.LaneData.Lanes.Count == 0)
        {
            Debug.LogWarning("No valid road cells found for spawning!");
            return;
        }

        // Get a random lane from the spawn cell
        LaneSegment spawnLane = spawnCell.LaneData.Lanes[Random.Range(0, spawnCell.LaneData.Lanes.Count)];

        // Spawn the vehicle
        Vector3 spawnPosition = spawnLane.StartWaypoint + Vector3.up * spawnHeight;
        GameObject vehicleObj = Instantiate(vehiclePrefab, spawnPosition, Quaternion.identity);

        Vehicle vehicle = vehicleObj.GetComponent<Vehicle>();
        if (vehicle == null)
        {
            vehicle = vehicleObj.AddComponent<Vehicle>();
        }

        vehicle.Initialize(this, spawnLane, pathfinder, roadGrid);
        activeVehicles.Add(vehicle);

        Debug.Log($"Vehicle spawned at {spawnPosition}. Total vehicles: {activeVehicles.Count}");
    }

    public void RemoveVehicle(Vehicle vehicle)
    {
        activeVehicles.Remove(vehicle);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPaths || activeVehicles == null)
            return;

        foreach (var vehicle in activeVehicles)
        {
            if (vehicle != null)
            {
                vehicle.DrawDebugPath();
            }
        }
    }
}