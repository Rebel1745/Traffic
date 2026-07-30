using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExitVehicleGoal : Goal
{
    private VehicleMovement _vm;

    public ExitVehicleGoal(AgentController vehicle, string goalName)
    {
        GoalName = goalName;

        _vm = vehicle.GetComponent<VehicleMovement>();
    }

    public override void Initialise(AgentController agent)
    {
        PedestrianManager.Instance.ReParentPedestrian(agent);
        agent.ShowHideAgent(true);

        // we need to get the alight waypoint from the parking space waypoint using its id from the relationship manager
        EntityId alightId = RelationshipManager.Instance.GetAlight(_vm.CurrentWaypoint.Id).First();
        if (!alightId.IsValid)
        {
            Debug.LogError("An alight Id for this parking space cannot be found");
            return;
        }

        WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightId);

        List<WaypointNode> path = new()
        {
            _vm.CurrentWaypoint, alightWaypoint
        };

        agent.Mover.SetPath(path);
    }

    public override void OnArrived(AgentController agent)
    {
    }
}
