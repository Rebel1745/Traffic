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

    public void Initialise(AgentType type, EntityId id, WaypointNode startWaypoint, WaypointNode targetWaypoint)
    {
        Id = id;
        _agentType = type;
        _mover.Initialise(startWaypoint, targetWaypoint);
    }

    public void OnMovementFinished()
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
        _mover.Stop(true);

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

        goal.Initialise(this);
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

    public List<GoalSaveData> SaveQueueToJson()
    {
        List<GoalSaveData> goalDataList = new();

        foreach (var goal in _goalQueue)
        {
            GoalSaveData data = new GoalSaveData
            {
                GoalType = goal.GoalType,
                Json = goal.SaveData()
            };
            goalDataList.Add(data);
        }

        return goalDataList;
    }

    public void LoadQueue(List<GoalSaveData> json)
    {
        _goalQueue.Clear();

        AgentController vehicle;
        WaypointNode targetNode;

        foreach (GoalSaveData data in json)
        {
            Goal newGoal = null;

            switch (data.GoalType)
            {
                case "DriveHome":
                    newGoal = new DriveHomeGoal();
                    break;
                case "DriveToAssignedPump":
                    DriveToAssignedPumpGoalSaveData pumpData = JsonUtility.FromJson<DriveToAssignedPumpGoalSaveData>(data.Json);
                    BuildingPetrolStation petrolStation = BuildingManager.Instance.GetBuilding(pumpData.PetrolStationId) as BuildingPetrolStation;
                    newGoal = new DriveToAssignedPumpGoal(petrolStation);
                    break;
                case "DriveToWaypoint":
                    DriveToWaypointGoalSaveData driveData = JsonUtility.FromJson<DriveToWaypointGoalSaveData>(data.Json);
                    targetNode = VehicleWaypointManager.Instance.GetWaypointFromId(driveData.TargetId);
                    newGoal = new DriveToWaypointGoal(targetNode, targetNode.Position.ToString());
                    break;
                case "EnterVehicle":
                    EnterVehicleGoalSaveData vehicleData = JsonUtility.FromJson<EnterVehicleGoalSaveData>(data.Json);
                    vehicle = VehicleManager.Instance.GetVehicle(vehicleData.VehicleId);
                    newGoal = new EnterVehicleGoal(vehicle);
                    break;
                case "ExitCarPark":
                    newGoal = new ExitCarParkGoal();
                    break;
                case "ExitVehicle":
                    newGoal = new ExitVehicleGoal();
                    break;
                case "ParkInCarPark":
                    ParkInCarParkGoalSaveData carParkData = JsonUtility.FromJson<ParkInCarParkGoalSaveData>(data.Json);
                    BuildingCarPark carPark = BuildingManager.Instance.GetBuilding(carParkData.CarParkId) as BuildingCarPark;
                    newGoal = new ParkInCarParkGoal(carPark);
                    break;
                case "Wait":
                    WaitGoalSaveData waitData = JsonUtility.FromJson<WaitGoalSaveData>(data.Json);
                    newGoal = new WaitGoal(waitData.WaitTime);
                    break;
                case "WalkToAndEnterVehicle":
                    if (data.Json != "")
                    {
                        WalkToAndEnterVehicleGoalSaveData walkToData = JsonUtility.FromJson<WalkToAndEnterVehicleGoalSaveData>(data.Json);
                        vehicle = VehicleManager.Instance.GetVehicle(walkToData.VehicleId);
                        newGoal = new WalkToAndEnterVehicleGoal(vehicle: vehicle);
                    }
                    else newGoal = new WalkToAndEnterVehicleGoal();
                    break;
                case "WalkToFrontDoor":
                    newGoal = new WalkToFrontDoorGoal();
                    break;
                case "WalkToWaypoint":
                    WalkToWaypointGoalSaveData waypoint = JsonUtility.FromJson<WalkToWaypointGoalSaveData>(data.Json);
                    targetNode = PedestrianWaypointManager.Instance.GetWaypointFromId(waypoint.TargetId);
                    newGoal = new WalkToWaypointGoal(targetNode);
                    break;
            }

            if (newGoal != null)
            {
                AddGoal(newGoal);
            }
        }
    }
}

public enum AgentType
{
    None,
    Person,
    Vehicle
}