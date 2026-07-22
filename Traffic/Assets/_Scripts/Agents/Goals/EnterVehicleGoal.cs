using System.Collections.Generic;
using UnityEngine;

public class EnterVehicleGoal : Goal
{
    private WaypointNode _currentNode;
    private AgentController _vehicle;

    public EnterVehicleGoal(WaypointNode currentNode, AgentController vehicle, string goalName)
    {
        GoalName = goalName;
        _currentNode = currentNode;
        _vehicle = vehicle;
    }

    public override void Initialise(AgentController agent)
    {
        VehicleMovement vm = _vehicle.GetComponent<VehicleMovement>();

        List<WaypointNode> path = new()
        {
            _currentNode, vm.CurrentWaypoint
        };

        agent.Mover.SetPath(path);
    }

    public override void OnArrived(AgentController agent)
    {
        agent.transform.parent = _vehicle.transform;
        agent.ShowHideAgent(false);
    }
}
