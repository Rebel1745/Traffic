using UnityEngine;
using System.Collections.Generic;
using System;

public class AgentController : MonoBehaviour, ISelectableObject
{
    [Header("Identity")]
    public EntityId Id { get; private set; }
    private AgentType _agentType;
    public AgentType AgentType => _agentType;
    [SerializeField] private Vector3 _cameraFocusOffset; // the offset to apply to the camera that looks at the object when it is selected
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] private Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    private IMovable _mover;
    public IMovable Mover => _mover;
    private Queue<Goal> _goalQueue = new Queue<Goal>();

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
        if (_goalQueue.Count == 0) return;

        Goal finishedGoal = _goalQueue.Dequeue();
        Debug.Log($"{gameObject.name} finished: {finishedGoal.GoalName}");

        // first, let the goal handle its specific logic (e.g., wait, trigger event, or add next goal)
        finishedGoal.OnArrived(this);

        // next check if there is any goal waiting to be executed
        // This handles the case where:
        // A) The goal added a new one immediately (e.g., chain reaction)
        // B) The goal did nothing, but a user interrupted and added one to the queue
        if (_goalQueue.Count > 0)
        {
            StartGoal(_goalQueue.Peek());
        }
        else
        {
            // Truly done
            Debug.Log($"{gameObject.name} has finished all goals.");
        }
    }

    public void AddGoal(Goal goal)
    {
        _goalQueue.Enqueue(goal);

        // If this is the first goal, start it immediately
        if (_goalQueue.Count == 1)
        {
            StartGoal(goal);
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
        _goalQueue.Enqueue(goal);
        StartGoal(goal);
    }

    private void StartGoal(Goal goal)
    {
        // Calculate path from current position to goal target
        // You might need a PathfindingSystem that takes the type of mover into account
        // e.g., PathfindingSystem.FindPath(_mover.CurrentPosition, goal.Target, MoverType.Vehicle)
        List<WaypointNode> path = AStarPathfinder.FindPath(_mover.CurrentWaypoint, goal.Target);

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
        }
    }
}

public enum AgentType
{
    None,
    Person,
    Vehicle
}