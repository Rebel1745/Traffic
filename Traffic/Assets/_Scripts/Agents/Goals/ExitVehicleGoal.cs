using System.Collections.Generic;
using UnityEngine;

public class ExitVehicleGoal : Goal
{
    private WaypointNode _targetNode;
    private AgentController _vehicle;
    private VehicleMovement _vm;

    public ExitVehicleGoal(AgentController vehicle, WaypointNode targetNode, string goalName)
    {
        GoalName = goalName;
        _targetNode = targetNode;
        _vehicle = vehicle;

        _vm = _vehicle.GetComponent<VehicleMovement>();
    }

    public override void Initialise(AgentController agent)
    {
        PedestrianManager.Instance.ReParentPedestrian(agent);
        agent.ShowHideAgent(true);

        List<WaypointNode> path = new()
        {
            _vm.CurrentWaypoint, _targetNode
        };

        agent.Mover.SetPath(path);
    }

    public override void OnArrived(AgentController agent)
    {
    }
}
