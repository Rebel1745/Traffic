using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianManager : MonoBehaviour
{
    public static PedestrianManager Instance { get; private set; }

    private List<PedestrianController> _activePedestrians = new List<PedestrianController>();

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

        // Get a random valid spawn location
        WaypointNode startWaypoint = GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway);
        //WaypointNode startWaypoint = GetRandomPedestrianWaypoint(WaypointType.InsideBuilding);
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

        PedestrianSpawner.Instance.SpawnPedestrian(startWaypoint, targetWaypoint);
    }

    public void RegisterPedestrian(PedestrianController pedestrian)
    {
        if (pedestrian != null && !_activePedestrians.Contains(pedestrian))
        {
            _activePedestrians.Add(pedestrian);
        }
    }

    public void UnregisterPedestrian(PedestrianController pedestrian)
    {
        if (_activePedestrians.Contains(pedestrian))
        {
            _activePedestrians.Remove(pedestrian);
        }
    }

    public void RequestNewTarget(PedestrianController pedestrian, WaypointType previousTargetType)
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

            if (previousTargetType != WaypointType.InsideBuilding)
                newTarget = GetRandomPedestrianWaypoint(WaypointType.InsideBuilding);

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
        Removepedestrian(pedestrian);
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

    public void Removepedestrian(PedestrianController pedestrian)
    {
        if (_activePedestrians.Contains(pedestrian))
        {
            _activePedestrians.Remove(pedestrian);
        }
        Destroy(pedestrian.gameObject);
    }
}
