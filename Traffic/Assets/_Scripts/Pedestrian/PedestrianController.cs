using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _waypointReachThreshold = 0.1f;
    private float _stopDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    // Path information
    public List<WaypointNode> Path { get; private set; }
    public WaypointNode CurrentWaypoint { get; private set; }
    public WaypointNode TargetWaypoint { get; private set; }

    private int _currentWaypointIndex = 0;
    private int _nextWaypointWithTrafficLightIndex = -1;
    private WaypointNode _nextWaypointWithTrafficLight = null;
    private bool _isMoving = false;

    public void Initialize(List<WaypointNode> path, WaypointNode target)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogError("Cannot initialize pedestrian with empty path!");
            return;
        }

        Path = new List<WaypointNode>(path);
        TargetWaypoint = target;
        CurrentWaypoint = path[0];
        _currentWaypointIndex = 0;
        _isMoving = true;

        // Position pedestrian at first waypoint
        transform.position = Path[0].Position;

        if (_showDebugInfo)
        {
            Debug.Log($"Pedestrian initialized with path of {Path.Count} waypoints");
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

        // check to see if we are within a couple of waypoints of a light
        if (_nextWaypointWithTrafficLightIndex != -1 && _nextWaypointWithTrafficLightIndex - _currentWaypointIndex <= 3)
        {
            // we are close to a light, if it is red and we are within half a vehicles length of the waypoint, stop
            if (!_nextWaypointWithTrafficLight.LaneNodeForTrafficLight.AssignedLight.IsRed())
            {
                return;
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

            if (Path[_currentWaypointIndex - 1].LaneNodeForTrafficLight != null && Path[_currentWaypointIndex - 1].LaneNodeForTrafficLight.AssignedLight != null)
            {
                // our last waypoint had a light, we have gone past it so lets see if there is a next light
                GetNextWaypointWithTrafficLight();
            }

            if (_showDebugInfo)
            {
                Debug.Log($"Pedestrian reached waypoint {_currentWaypointIndex}/{Path.Count}");
            }
        }
    }

    private void OnReachedTarget()
    {
        if (_showDebugInfo)
        {
            Debug.Log("Pedestrian reached target destination!");
        }

        _isMoving = false;

        // Request new target from PedestrianManager
        PedestrianManager.Instance.RequestNewTarget(this);
    }

    public void SetNewPath(List<WaypointNode> newPath, WaypointNode newTarget)
    {
        if (newPath == null || newPath.Count == 0)
        {
            Debug.LogError("Cannot set empty path!");
            return;
        }

        for (int i = newPath.Count; i < 0; i--)
        {
            if (newPath[i] != newTarget && newPath[i].Type == WaypointType.Exit)
                newPath.RemoveAt(i);
        }

        Path = new List<WaypointNode>(newPath);
        TargetWaypoint = newTarget;
        _currentWaypointIndex = 0;
        CurrentWaypoint = newPath[0];
        _isMoving = true;
        GetNextWaypointWithTrafficLight();

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

    private void GetNextWaypointWithTrafficLight()
    {
        _nextWaypointWithTrafficLight = null;
        _nextWaypointWithTrafficLightIndex = -1;

        for (int i = _currentWaypointIndex; i < Path.Count; i++)
        {
            if (_currentWaypointIndex > 0 && Path[i].LaneNodeForTrafficLight != null && Path[i].LaneNodeForTrafficLight.AssignedLight != null)
            {
                _nextWaypointWithTrafficLight = Path[i];
                _nextWaypointWithTrafficLightIndex = i;
                break;
            }
        }
    }
}