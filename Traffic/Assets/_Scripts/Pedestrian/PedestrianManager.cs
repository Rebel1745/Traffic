using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianManager : MonoBehaviour
{
    public static PedestrianManager Instance { get; private set; }

    [SerializeField] private GameObject[] _pedestrianPrefabs;

    private Dictionary<EntityId, PedestrianController> _allPedestrians = new();

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
        // Only spawn Pedestrians when simulation is running
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Pedestrians)
            return;

        //AddAndRegisterPerson();

        // Get a random valid spawn location
        // WaypointNode startWaypoint = GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway);
        // //WaypointNode startWaypoint = GetRandomPedestrianWaypoint(WaypointType.InsideBuilding);
        // if (startWaypoint == null)
        // {
        //     Debug.LogWarning("No valid spawn location found!");
        //     return;
        // }

        // // Get a random valid target
        // WaypointNode targetWaypoint = FindValidTarget(startWaypoint);
        // if (targetWaypoint == null)
        // {
        //     Debug.LogWarning("No valid target found for spawn location!");
        //     return;
        // }

        // PedestrianSpawner.Instance.SpawnPedestrian(startWaypoint, targetWaypoint);
    }

    private PedestrianController AddAndRegisterPerson()
    {
        return AddAndRegisterPerson(GetRandomPedestrianWaypoint(WaypointType.None));
    }

    public PedestrianController AddAndRegisterPerson(WaypointNode spawnWaypoint)
    {
        // 1. Generate the ID
        EntityId newId = EntityId.New();

        // 2. Instantiate the GameObject
        // You can use Object.Instantiate with a prefab
        GameObject pedestrianPrefab = _pedestrianPrefabs[Random.Range(0, _pedestrianPrefabs.Length)];
        Vector3 spawnLocation = Utils.GetVectorWithSetHeight(spawnWaypoint.Position, 0.2f);
        //Vector3 lookDirection = (Utils.GetVectorWithSetHeight(Camera.main.transform.position, 0.2f) - spawnLocation).normalized;
        Vector3 lookDirection = Vector3.back;
        GameObject pedestrian = Instantiate(pedestrianPrefab, spawnLocation, Quaternion.identity, transform);
        pedestrian.transform.rotation = Quaternion.LookRotation(lookDirection);
        PedestrianController pc = pedestrian.GetComponent<PedestrianController>();

        // 3. Assign the ID to the controller
        pc.Initialise(newId, spawnWaypoint);

        // 4. Register in the dictionary
        _allPedestrians[newId] = pc;

        // 5. Hook into the Destroy event to auto-cleanup
        // (See Step C below)

        return pc;
    }

    public void GoToRandomWaypoint(PedestrianController pc)
    {
        RequestNewTarget(pc);
    }

    public void GoHome(PedestrianController pc)
    {

    }

    public void RequestNewTarget(PedestrianController pedestrian, WaypointType previousTargetType = WaypointType.None)
    {
        if (pedestrian == null || pedestrian.CurrentWaypoint == null)
        {
            Debug.LogWarning("Invalid pedestrian or current waypoint!");
            return;
        }

        int attempts = 0;
        int maxAttempts = 3;

        while (attempts < maxAttempts)
        {
            WaypointNode newTarget = null;

            // if (previousTargetType != WaypointType.InsideBuilding)
            //     newTarget = GetRandomPedestrianWaypoint(WaypointType.InsideBuilding);

            if (newTarget == null) newTarget = FindValidTarget(pedestrian.CurrentWaypoint);

            if (newTarget != null)
            {
                List<WaypointNode> newPath = AStarPathfinder.FindPath(pedestrian.CurrentWaypoint, newTarget);

                if (newPath != null && newPath.Count > 0)
                {
                    pedestrian.SetNewPath(newPath, newTarget);
                    //Debug.Log($"New target assigned to pedestrian: {newTarget.Position}");
                    return;
                }
            }

            attempts++;
        }

        // Failed to find a valid target after 3 attempts
        Debug.LogWarning($"Failed to find valid target for pedestrian after {maxAttempts} attempts. Destroying pedestrian.");
        Destroy(pedestrian.gameObject);
    }

    public WaypointNode FindValidTarget(WaypointNode startWaypoint, int maxAttempts = 10)
    {
        var allWaypoints = PedestrianWaypointManager.Instance.GetAllWaypoints();

        if (allWaypoints.Count == 0)
            return null;

        // Try to find a valid target
        for (int i = 0; i < maxAttempts; i++)
        {
            WaypointNode candidate = allWaypoints[Random.Range(0, allWaypoints.Count)];

            // Check if path exists
            List<WaypointNode> path = AStarPathfinder.FindPath(startWaypoint, candidate);
            if (path != null && path.Count > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    public WaypointNode GetRandomPedestrianWaypoint(WaypointType type)
    {
        List<WaypointNode> allWaypoints = PedestrianWaypointManager.Instance.GetAllWaypoints();
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
