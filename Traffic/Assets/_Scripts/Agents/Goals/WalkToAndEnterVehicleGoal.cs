using System;
using UnityEngine;

public class WalkToAndEnterVehicleGoal : Goal
{
    public override string GoalType => "WalkToAndEnterVehicle";

    private AgentController _agent;
    private AgentController _vehicle;
    private VehicleMovement _vm;

    public WalkToAndEnterVehicleGoal(string goalName = "Walking to and entering vehicle", AgentController vehicle = null)
    {
        GoalName = goalName;
        _vehicle = vehicle;
    }

    public override void Initialise(AgentController agent)
    {
        _agent = agent;

        // if we don't have a specified vehicle, see if the person has a realtionship with one
        if (_vehicle == null)
        {
            // find a vehicle
            EntityId vehicleId = PedestrianManager.Instance.GetPersonsVehicle(agent.Id);

            if (!vehicleId.IsValid)
            {
                Debug.LogError("No vehicle found");
                return;
            }

            // get the vehicle
            _vehicle = VehicleManager.Instance.GetVehicle(vehicleId);

            if (_vehicle == null)
            {
                Debug.LogError($"No vehicle found in the manager with Id {vehicleId}");
                return;
            }
        }

        // we now have a vehicle, see if it is moving
        _vm = _vehicle.GetComponent<VehicleMovement>();

        if (_vm.IsMoving)
        {
            // if moving, subscribe to the movement finished event, then continue with the rest of the goal
            _vm.OnArrivedAtDestination += OnVehicleArrived;
        }
        else
        {
            WalkToVehicle();
        }
    }

    public override void OnArrived(AgentController agent)
    {
    }

    private void WalkToVehicle()
    {
        // we have a vehicle, it is not moving, check if it has an alight waypoint next to it
        EntityId currentSpotId = _vm.CurrentWaypoint.Id;

        if (!currentSpotId.IsValid)
        {
            Debug.LogError("Current spot is not valid! What is going on here?");
            return;
        }

        EntityId alightWaypointId = PedestrianManager.Instance.GetAlightWaypointId(currentSpotId);

        if (!alightWaypointId.IsValid)
        {
            Debug.LogError("Alight waypoint not valid");
            return;
        }

        WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightWaypointId);

        if (alightWaypoint == null)
        {
            Debug.LogError("Alight waypoint not found in pedestrian waypoints");
            return;
        }

        // add a goal to enter the vehicle (this goal order is reversed as each goal is added after the current goal)
        _agent.AddGoalAfterCurrent(new EnterVehicleGoal(_vehicle, "Enter vehicle"));
        // set a new goal for the alight waypoint
        _agent.AddGoalAfterCurrent(new WalkToWaypointGoal(alightWaypoint, "Walking to alight waypoint"));

        // set this goal as finished
        _agent.OnMovementFinished();
    }

    private void OnVehicleArrived()
    {
        _vm.OnArrivedAtDestination -= OnVehicleArrived;
        WalkToVehicle();
    }

    public override string SaveData()
    {
        if (_vehicle != null)
        {
            WalkToAndEnterVehicleGoalSaveData data = new() { VehicleId = _vehicle.Id.ToString() };
            return JsonUtility.ToJson(data);
        }

        return "";
    }
}
