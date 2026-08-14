using UnityEngine;
using System.Collections.Generic;

public class DriveToWaypointGoal : Goal
{
    private AgentController _vehicle;
    private VehicleMovement _vm;
    private WaypointNode _target;
    private AgentController _agent;

    public DriveToWaypointGoal(WaypointNode target, string goalName)
    {
        GoalName = goalName;
        _target = target;
    }

    public override void Initialise(AgentController agent)
    {
        _agent = agent;
        _vehicle = agent.GetComponent<PedestrianMovement>().CurrentVehicle;

        _vm = _vehicle.GetComponent<VehicleMovement>();

        _vm.OnArrivedAtDestination += OnVehicleArrived;

        List<WaypointNode> path = AStarPathfinder.FindPath(_vehicle.Mover.CurrentWaypoint, _target);

        if (path != null && path.Count > 0)
        {
            _vehicle.Mover.SetPath(path);
        }
        else
        {
            Debug.LogWarning($"[{_vehicle.gameObject.name}] No path found for goal: {GoalName}");
            path = new() { _vehicle.Mover.CurrentWaypoint, _target };
            _vehicle.Mover.SetPath(path);
        }
    }

    public override void OnArrived(AgentController agent)
    {
        _vm.OnArrivedAtDestination -= OnVehicleArrived;
    }

    private void OnVehicleArrived()
    {
        //OnArrived(_vehicle);
        if (_agent != _vehicle) _agent.OnMovementFinished();
    }
}
