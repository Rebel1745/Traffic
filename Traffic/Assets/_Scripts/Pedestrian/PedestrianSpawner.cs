using UnityEngine;
using System.Collections.Generic;

public class PedestrianSpawner : MonoBehaviour
{
    public static PedestrianSpawner Instance { get; private set; }

    [SerializeField] private GameObject[] _pedestrianPrefabs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnPedestrian(WaypointNode startWaypoint, WaypointNode targetWaypoint)
    {
        // Find path
        List<WaypointNode> path = AStarPathfinder.FindPath(startWaypoint, targetWaypoint);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("No path found between spawn and target!");
            return;
        }

        GameObject pedestrianPrefab = GetRandomPedestrianPrefab();
        // Spawn pedestrian
        GameObject pedestrianObj = Instantiate(pedestrianPrefab, startWaypoint.Position, Quaternion.identity);
        PedestrianController pedestrian = pedestrianObj.GetComponent<PedestrianController>();

        if (pedestrian != null)
        {
            // Calculate initial rotation to face next waypoint
            if (path.Count > 1)
            {
                Vector3 direction = (path[1].Position - path[0].Position).normalized;
                pedestrianObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            pedestrian.Initialize(path, targetWaypoint);
            //PedestrianManager.Instance.RegisterPedestrian(pedestrian);
            Debug.Log($"pedestrian spawned at {startWaypoint.Position} with target {targetWaypoint.Position}");
        }
        else
        {
            Debug.LogError("pedestrian prefab does not have pedestrianController component!");
            Destroy(pedestrianObj);
        }
    }

    private GameObject GetRandomPedestrianPrefab()
    {
        return _pedestrianPrefabs[Random.Range(0, _pedestrianPrefabs.Length)];
    }
}