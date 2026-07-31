using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VehicleMovement : MonoBehaviour, IMovable
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _waypointReachThreshold = 0.1f;
    [SerializeField] private float _lookAheadDistance = 0.2f;
    [SerializeField] private LayerMask _whatIsVehicle;
    private Collider _vehicleCollider;
    private float _stopDistance;

    // pathfinding
    private List<WaypointNode> _currentPath = new();
    private int _currentWaypointIndex = 0;
    private int _nextWaypointWithTrafficLightIndex = -1;
    private WaypointNode _nextWaypointWithTrafficLight = null;
    private bool _isMoving = false;
    public bool IsMoving => _isMoving;
    private float _roadHeight = 0f;

    private WaypointNode _currentWaypoint;
    public WaypointNode CurrentWaypoint => _currentWaypoint;

    public event Action OnArrivedAtDestination;

    private void Start()
    {
        _vehicleCollider = GetComponent<Collider>();
        _stopDistance = _vehicleCollider.bounds.extents.z;
        _roadHeight = RoadMeshRenderer.Instance.GetRoadHeight();
    }

    private void Update()
    {
        if (!_isMoving || _currentPath == null || _currentPath.Count == 0)
            return;

        MoveTowardsNextWaypoint();
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

        // NOT SURE WHY THIS IS HERE. CHECK IF IT WORKS WHEN REMOVED
        for (int i = path.Count; i < 0; i--)
        {
            if (path[i].Type == WaypointType.Exit)
                path.RemoveAt(i);
        }

        _currentPath = new List<WaypointNode>(path);
        _currentWaypointIndex = 0;
        _isMoving = true;

        GetNextWaypointWithTrafficLight();
    }

    private void MoveTowardsNextWaypoint()
    {
        if (_currentWaypointIndex >= _currentPath.Count)
        {
            // Reached the end of the path
            Stop();
            return;
        }

        // check to make sure a vehicle is not too close in front of this one
        // if it is, stop
        // NOTE: this is not performant for a large number of vehicles, change to something better in the future
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _stopDistance + _lookAheadDistance, _whatIsVehicle))
        {
            return; // Wait one frame
        }

        WaypointNode targetWaypoint = _currentPath[_currentWaypointIndex];
        Vector3 targetPosition = Utils.GetVectorWithSetHeight(targetWaypoint.Position, _roadHeight);

        // check to see if we are within a couple of waypoints of a light
        if (_nextWaypointWithTrafficLightIndex != -1 && _nextWaypointWithTrafficLightIndex - _currentWaypointIndex <= 3)
        {
            // we are close to a light, if it is red and we are within half a vehicles length of the waypoint, stop
            if (_nextWaypointWithTrafficLight.AssignedLight.IsRed() && Utils.GetDistanceWithSetHeight(transform.position, _nextWaypointWithTrafficLight.Position, 0f) <= _stopDistance)
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
        float distance = Utils.GetDistanceWithSetHeight(transform.position, targetPosition, 0f);
        if (distance < _waypointReachThreshold)
        {
            _currentWaypoint = targetWaypoint;
            _currentWaypointIndex++;

            if (_currentPath[_currentWaypointIndex - 1].AssignedLight != null)
            {
                // our last waypoint had a light, we have gone past it so lets see if there is a next light
                GetNextWaypointWithTrafficLight();
            }
        }
    }

    private void GetNextWaypointWithTrafficLight()
    {
        _nextWaypointWithTrafficLight = null;
        _nextWaypointWithTrafficLightIndex = -1;

        for (int i = _currentWaypointIndex; i < _currentPath.Count; i++)
        {
            if (_currentPath[i].AssignedLight != null)
            {
                _nextWaypointWithTrafficLight = _currentPath[i];
                _nextWaypointWithTrafficLightIndex = i;
                break;
            }
        }
    }

    public void Stop(bool forceStop = false)
    {
        _currentWaypoint = _currentPath.Last();

        // if we have just parked, flip the car
        if (_currentWaypoint.Type == WaypointType.VehicleParking)
            transform.eulerAngles = new Vector3(0, Utils.SnapToOppositeEulerAngle(transform.eulerAngles.y), 0);


        _isMoving = false;
        OnArrivedAtDestination?.Invoke();
    }


}
