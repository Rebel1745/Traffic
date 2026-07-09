using UnityEngine;
using System.Collections.Generic;
using System;

public class AgentController : MonoBehaviour, ISelectableObject
{
    [Header("Identity")]
    public EntityId Id { get; private set; }
    [SerializeField] GameObject _agentModel;
    private AgentType _agentType;
    public AgentType AgentType => _agentType;
    [SerializeField] private Vector3 _cameraFocusOffset; // the offset to apply to the camera that looks at the object when it is selected
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] private Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    private IMovable _mover;
    public IMovable Mover => _mover;
    //private Queue<Goal> _goalQueue = new Queue<Goal>();

    private LinkedList<Goal> _goalQueue = new LinkedList<Goal>();
    private Goal _currentGoal; // Track the active goal separately
    private LinkedListNode<Goal> _currentNode;

    private void Awake()
    {
        // Find the movement component (PedestrianMovement or VehicleMovement)
        _mover = GetComponent<IMovable>();

        if (_mover == null)
        {
            Debug.LogError($"[{gameObject.name}] AgentController requires a component implementing IMovable!");
            return;
        }

        // Subscribe to the movement completion event
        _mover.OnArrivedAtDestination += OnMovementFinished;
    }

    public void Initialise(AgentType type, EntityId id, WaypointNode startWaypoint)
    {
        Id = id;
        _agentType = type;
        _mover.Initialise(startWaypoint);
    }

    private void OnMovementFinished()
    {
        if (_currentGoal == null) return;

        // 1. Remove the current goal from the list
        _goalQueue.Remove(_currentNode);

        // 2. Ask the goal what to do next (if it has logic)
        // Note: With this new system, we might NOT want the goal to add the next one automatically.
        // We want the "AddGoalAfterCurrent" to handle the chaining.
        // So, OnArrived might just be empty or handle specific side effects.
        _currentGoal.OnArrived(this);

        // 3. Check if there is a next goal
        if (_goalQueue.Count > 0)
        {
            // The next goal is now at the head of the list
            Goal nextGoal = _goalQueue.First.Value;
            StartGoal(nextGoal);
        }
        else
        {
            _currentGoal = null;
            _currentNode = null;
        }
    }

    public void AddGoal(Goal goal)
    {
        _goalQueue.AddLast(goal);

        // If the queue was empty, start this goal immediately
        if (_goalQueue.Count == 1 && _currentGoal == null)
        {
            StartGoal(goal);
        }
    }

    public void AddGoalAfterCurrent(Goal goal)
    {
        if (_currentNode != null && _goalQueue.Count > 0)
        {
            // There is an active goal. Insert the new goal immediately after it.
            // This pushes all existing queued goals further down the list.
            _goalQueue.AddAfter(_currentNode, goal);

            Debug.Log($"{gameObject.name} Inserted {goal.GoalName} after current goal.");
        }
        else
        {
            // No active goal. Just add to the end.
            _goalQueue.AddLast(goal);

            // If this is the first goal, start it
            if (_goalQueue.Count == 1)
            {
                StartGoal(goal);
            }
        }
    }

    public void InterruptAndAddGoal(Goal goal)
    {
        Debug.Log($"{gameObject.name} Interrupting to: {goal.GoalName}");

        // Stop current movement immediately
        //_mover.Stop();

        // Clear all pending goals (Dance, Wait, etc. are gone)
        _goalQueue.Clear();

        // Add the new goal and start it
        _goalQueue.AddFirst(goal);
        StartGoal(goal);
    }

    private void StartGoal(Goal goal)
    {
        _currentGoal = goal;
        _currentNode = _goalQueue.First;

        List<WaypointNode> path = null;

        // Calculate path from current position to goal target
        if (_mover.CurrentWaypoint.NetworkType == goal.Target.NetworkType)
            path = AStarPathfinder.FindPath(_mover.CurrentWaypoint, goal.Target);
        else
        {
            // if the goal isn't on the same network as the current waypoint (e.g. moving from pedestrian into vehicle waypoints)
            if (_mover.CurrentWaypoint != goal.Target)
            {
                path = new()
                {
                    _mover.CurrentWaypoint,
                    goal.Target
                };
            }
        }

        if (path != null && path.Count > 0)
        {
            _mover.SetPath(path);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No path found for goal: {goal.GoalName}");
            // Handle failure (retry, pick new goal, etc.)
        }
    }

    // Optional: Keep your selection/UI logic here if needed for both types
    public void SelectObject()
    {
        switch (_agentType)
        {
            case AgentType.Person:
                UIManager.Instance.LoadPedestrianDetails(this);
                break;
            case AgentType.Vehicle:
                UIManager.Instance.LoadVehicleDetails(this);
                break;
        }
    }

    public void ShowHideAgent(bool show)
    {
        _agentModel.SetActive(show);
    }
}

public enum AgentType
{
    None,
    Person,
    Vehicle
}