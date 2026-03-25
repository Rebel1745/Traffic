using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _waypointReachThreshold = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    // Path information
    public List<WaypointNode> Path { get; private set; }
    public WaypointNode CurrentWaypoint { get; private set; }
    public WaypointNode TargetWaypoint { get; private set; }

    private int _currentWaypointIndex = 0;
    private bool _isMoving = false;

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
        _currentWaypointIndex = 0;
        _isMoving = true;

        // Position vehicle at first waypoint
        transform.position = Path[0].Position;

        if (_showDebugInfo)
        {
            Debug.Log($"Vehicle initialized with path of {Path.Count} waypoints");
        }
    }

    private void Update()
    {
        if (!_isMoving || Path == null || Path.Count == 0)
            return;

        MoveTowardsNextWaypoint();
    }

    private void MoveTowardsNextWaypoint()
    {
        if (_currentWaypointIndex >= Path.Count)
        {
            // Reached the end of the path
            OnReachedTarget();
            return;
        }

        WaypointNode targetWaypoint = Path[_currentWaypointIndex];
        Vector3 targetPosition = targetWaypoint.Position;

        if (targetWaypoint.AssignedLight != null)
        {
            if (!targetWaypoint.AssignedLight.IsGreen())
            {
                return; // Wait one frame
            }
        }

        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

        // Rotate towards target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        // Check if reached waypoint
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < _waypointReachThreshold)
        {
            CurrentWaypoint = targetWaypoint;
            _currentWaypointIndex++;

            if (_showDebugInfo)
            {
                Debug.Log($"Vehicle reached waypoint {_currentWaypointIndex}/{Path.Count}");
            }
        }
    }

    private void OnReachedTarget()
    {
        if (_showDebugInfo)
        {
            Debug.Log("Vehicle reached target destination!");
        }

        _isMoving = false;

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
        _currentWaypointIndex = 0;
        CurrentWaypoint = newPath[0];
        _isMoving = true;

        if (_showDebugInfo)
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
                if (_showDebugInfo)
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
        _isMoving = false;
    }

    public void ResumeVehicle()
    {
        if (Path != null && Path.Count > 0)
        {
            _isMoving = true;
        }
    }
}