using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public class PedestrianMovement : MonoBehaviour, IMovable
{
    [Header("Settings")][SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _waypointReachThreshold = 0.1f;

    [Header("References")]
    private PedestrianAnimationController _animController;
    private float _roadHeight;
    private float _pavementHeight;

    // State
    private List<WaypointNode> _currentPath = new();
    private int _currentWaypointIndex = 0;
    private bool _isMoving = false;
    private bool _isCrossing = false;
    private float _currentSpeed = 0f;

    private WaypointNode _currentWaypoint;
    public WaypointNode CurrentWaypoint => _currentWaypoint;

    public event Action OnArrivedAtDestination;

    private void Awake()
    {
        _animController = GetComponentInChildren<PedestrianAnimationController>();
    }

    private void Start()
    {
        // Keep your existing height logic here
        _roadHeight = RoadMeshRenderer.Instance.GetRoadHeight();
        _pavementHeight = RoadMeshRenderer.Instance.GetPavementHeight();
    }

    private void Update()
    {
        if (!_isMoving || _currentPath.Count == 0) return;

        MoveToNextWaypoint();
    }

    public void Initialise(WaypointNode spawnWaypoint)
    {
        _currentPath = new()
        {
            spawnWaypoint
        };
        _currentWaypointIndex = 0;
        _currentWaypoint = spawnWaypoint;
        _isMoving = false;

        _animController.SetAnimation(PedestrianAnimationType.Wave);
    }

    public void SetPath(List<WaypointNode> path)
    {
        if (path == null || path.Count == 0)
        {
            // Path is empty. We are effectively "at the destination" instantly.
            // Trigger the arrival event so the AgentController can pick the next goal.
            OnArrivedAtDestination?.Invoke();
            return;
        }

        _currentPath = new List<WaypointNode>(path);
        _currentWaypointIndex = 0;
        _isMoving = true;
        Debug.Log("SetPath");
    }

    public void Stop()
    {
        _isMoving = false;
        Debug.Log("Stop");
        _animController.SetAnimation(PedestrianAnimationType.Wave);
        OnArrivedAtDestination?.Invoke();
    }

    private void MoveToNextWaypoint()
    {
        if (_currentWaypointIndex >= _currentPath.Count)
        {
            Stop();
            return;
        }
        _animController.SetAnimation(PedestrianAnimationType.Walk);

        _currentWaypoint = _currentPath[_currentWaypointIndex];
        float targetHeight = _isCrossing ? _roadHeight : _pavementHeight;
        Vector3 targetPos = Utils.GetVectorWithSetHeight(_currentWaypoint.Position, targetHeight);

        // Rotation
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        }

        // Traffic Light Logic (Simplified for brevity - you can paste your full logic here)
        _currentSpeed = _moveSpeed; // Default
        if (IsTrafficLightRed(_currentWaypoint))
        {
            _animController.SetAnimation(PedestrianAnimationType.Idle);
            _currentSpeed = 0f;
        }
        else if (_isCrossing && IsTrafficLightRed(_currentWaypoint))
        {
            _animController.SetAnimation(PedestrianAnimationType.Run);
            _currentSpeed = _moveSpeed * 2f;
        }
        else
        {
            _animController.SetAnimation(PedestrianAnimationType.Walk);
        }

        // Movement
        transform.position = Utils.GetVectorWithSetHeight(transform.position, targetHeight);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, _currentSpeed * Time.deltaTime);

        // Check Arrival
        float dist = Utils.GetDistanceWithSetHeight(transform.position, targetPos, 0);
        if (dist < _waypointReachThreshold)
        {
            HandleWaypointReached(_currentWaypoint);
        }
    }

    private void HandleWaypointReached(WaypointNode node)
    {
        // Logic for crossing detection
        if (node.Type == WaypointType.PedestrianRoadCrossing &&
            _currentWaypointIndex < _currentPath.Count - 1 &&
            _currentPath[_currentWaypointIndex + 1].Type == WaypointType.PedestrianRoadCrossing)
        {
            _isCrossing = true;
        }
        else
        {
            _isCrossing = false;
        }

        _currentWaypointIndex++;

        // If we finished the path, the Stop() method will fire the event
        if (_currentWaypointIndex >= _currentPath.Count)
        {
            Stop();
        }
    }

    private bool IsTrafficLightRed(WaypointNode node)
    {
        // Paste your existing traffic light check logic here
        if (node.LaneNodeForTrafficLight != null && node.LaneNodeForTrafficLight.AssignedLight != null)
        {
            return node.LaneNodeForTrafficLight.AssignedLight.IsCrossingRed;
        }
        return false;
    }
}