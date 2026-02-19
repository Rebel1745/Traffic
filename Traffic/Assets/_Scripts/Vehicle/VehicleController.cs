using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Movement Settings")][SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float waypointReachThreshold = 0.1f;

    [Header("Debug")][SerializeField] private bool showDebugInfo = true;

    // Path information
    public List<WaypointNode> Path { get; private set; }
    public WaypointNode CurrentWaypoint { get; private set; }
    public WaypointNode TargetWaypoint { get; private set; }

    private int currentWaypointIndex = 0;
    private bool isMoving = false;

    public void Initialize(List<WaypointNode> path, WaypointNode target)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogError("Cannot initialize vehicle with empty path!");
            return;
        }

        Path = new List<WaypointNode>(path);
        TargetWaypoint = target;
        CurrentWaypoint = path[0];
        currentWaypointIndex = 0;
        isMoving = true;

        // Position vehicle at first waypoint
        transform.position = Path[0].Position;

        if (showDebugInfo)
        {
            Debug.Log($"Vehicle initialized with path of {Path.Count} waypoints");
        }
    }

    private void Update()
    {
        if (!isMoving || Path == null || Path.Count == 0)
            return;

        MoveTowardsNextWaypoint();
    }

    private void MoveTowardsNextWaypoint()
    {
        if (currentWaypointIndex >= Path.Count)
        {
            // Reached the end of the path
            OnReachedTarget();
            return;
        }

        WaypointNode targetWaypoint = Path[currentWaypointIndex];
        Vector3 targetPosition = targetWaypoint.Position;

        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Rotate towards target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Check if reached waypoint
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < waypointReachThreshold)
        {
            CurrentWaypoint = targetWaypoint;
            currentWaypointIndex++;

            if (showDebugInfo)
            {
                Debug.Log($"Vehicle reached waypoint {currentWaypointIndex}/{Path.Count}");
            }
        }
    }

    private void OnReachedTarget()
    {
        if (showDebugInfo)
        {
            Debug.Log("Vehicle reached target destination!");
        }

        isMoving = false;

        // Request new target from VehicleManager
        VehicleManager.Instance.RequestNewTarget(this);
    }

    public void SetNewPath(List<WaypointNode> newPath, WaypointNode newTarget)
    {
        if (newPath == null || newPath.Count == 0)
        {
            Debug.LogError("Cannot set empty path!");
            return;
        }

        Path = new List<WaypointNode>(newPath);
        TargetWaypoint = newTarget;
        currentWaypointIndex = 0;
        CurrentWaypoint = newPath[0];
        isMoving = true;

        if (showDebugInfo)
        {
            Debug.Log($"New path set with {Path.Count} waypoints");
        }
    }

    public bool IsPathValid()
    {
        if (Path == null || Path.Count == 0)
            return false;

        // Check if all waypoints in the path still have valid connections
        for (int i = 0; i < Path.Count - 1; i++)
        {
            WaypointNode current = Path[i];
            WaypointNode next = Path[i + 1];

            // Check if connection still exists
            bool connectionExists = false;
            foreach (var connection in current.Connections)
            {
                if (connection.TargetWaypoint == next)
                {
                    connectionExists = true;
                    break;
                }
            }

            if (!connectionExists)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Connection broken between waypoint {i} and {i + 1}");
                }
                return false;
            }
        }

        return true;
    }

    public void StopVehicle()
    {
        isMoving = false;
    }

    public void ResumeVehicle()
    {
        if (Path != null && Path.Count > 0)
        {
            isMoving = true;
        }
    }
}