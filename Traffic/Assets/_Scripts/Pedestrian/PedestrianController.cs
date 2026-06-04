using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _waypointReachThreshold = 0.1f;

    [Header("Animation")]
    private PedestrianAnimationController _animController;

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    // Path information
    public List<WaypointNode> Path { get; private set; }
    public WaypointNode CurrentWaypoint { get; private set; }
    public WaypointNode TargetWaypoint { get; private set; }

    private int _currentWaypointIndex = 0;
    private bool _isMoving = false;
    private bool _isCrossing = false;
    private float _currentSpeed = 0f;
    private float _roadHeight = 0f;
    private float _pavementHeight = 0f;
    private float _currentHeight = 0f;

    private void Awake()
    {
        _animController = GetComponentInChildren<PedestrianAnimationController>();
    }

    private void Start()
    {
        _roadHeight = RoadMeshRenderer.Instance.GetRoadHeight();
        _pavementHeight = RoadMeshRenderer.Instance.GetPavementHeight();
    }

    private void Update()
    {
        if (!_isMoving || Path == null || Path.Count == 0)
            return;

        MoveTowardsNextWaypoint();
    }

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
        _animController.SetAnimation(PedestrianAnimationType.Walk);

        // Position pedestrian at first waypoint
        transform.position = Path[0].Position;

        if (_showDebugInfo)
        {
            Debug.Log($"Pedestrian initialized with path of {Path.Count} waypoints");
        }
    }

    private void MoveTowardsNextWaypoint()
    {
        if (_currentWaypointIndex >= Path.Count)
        {
            // Reached the end of the path
            OnReachedTarget();
            return;
        }

        _currentHeight = _isCrossing ? _roadHeight : _pavementHeight;

        WaypointNode targetWaypoint = Path[_currentWaypointIndex];
        Vector3 targetPosition = Utils.GetVectorWithSetHeight(targetWaypoint.Position, _currentHeight);

        // Rotate towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        // does our target have a light on it?
        if (targetWaypoint.LaneNodeForTrafficLight != null &&
             targetWaypoint.LaneNodeForTrafficLight.AssignedLight != null)
        {
            // is this the first light of the crossing? (i.e. is there another light after this one?)
            if (_currentWaypointIndex < Path.Count - 1 &&
                Path[_currentWaypointIndex + 1].LaneNodeForTrafficLight != null &&
                Path[_currentWaypointIndex + 1].LaneNodeForTrafficLight.AssignedLight != null)
            {
                // is it red and are we close enough to stop at it and wait?
                if (targetWaypoint.LaneNodeForTrafficLight.AssignedLight.IsCrossingRed &&
                    Utils.GetDistanceWithSetHeight(transform.position, targetPosition, 0) <= 0.2f)
                {
                    _animController.SetAnimation(PedestrianAnimationType.Idle);
                    _currentSpeed = 0f;
                }
                // we just walkin'
                else
                {
                    _animController.SetAnimation(PedestrianAnimationType.Walk);
                    _currentSpeed = _moveSpeed;
                }
            }
            // we are currently crossing the road, is the pedestrian light red?
            else if (_isCrossing && targetWaypoint.LaneNodeForTrafficLight.AssignedLight.IsCrossingRed)
            {
                // if so, run!
                _animController.SetAnimation(PedestrianAnimationType.Run);
                _currentSpeed = _moveSpeed * 2f;
            }
            // we just walkin'
            else
            {
                _animController.SetAnimation(PedestrianAnimationType.Walk);
                _currentSpeed = _moveSpeed;
            }
        }
        // we just walkin'
        else
        {
            _animController.SetAnimation(PedestrianAnimationType.Walk);
            _currentSpeed = _moveSpeed;
        }

        // Move towards target
        // imediately get to the correct height
        transform.position = Utils.GetVectorWithSetHeight(transform.position, _currentHeight);
        // then move to the new x,z
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _currentSpeed * Time.deltaTime);

        // Check if reached waypoint
        float distance = Utils.GetDistanceWithSetHeight(transform.position, targetPosition, 0);
        if (distance < _waypointReachThreshold)
        {
            // if we have reached a crossing point, and the next waypoint is a crossing point, we are crossing
            if (targetWaypoint.Type == WaypointType.PedestrianRoadCrossing &&
                _currentWaypointIndex < Path.Count - 1 &&
                Path[_currentWaypointIndex + 1].Type == WaypointType.PedestrianRoadCrossing
            )
            {
                _isCrossing = true;
            }
            else _isCrossing = false;

            CurrentWaypoint = targetWaypoint;
            _currentWaypointIndex++;

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

        bool newTargetIsDoorway = Path.Count > 0 && Path.Last().Type != WaypointType.BuildingDoor;

        // Request new target from PedestrianManager
        PedestrianManager.Instance.RequestNewTarget(this, newTargetIsDoorway);
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
}