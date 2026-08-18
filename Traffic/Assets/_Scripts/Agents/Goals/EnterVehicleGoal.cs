using System.Collections.Generic;
using UnityEngine;

public class EnterVehicleGoal : Goal
{
    public override string GoalType => "EnterVehicle";

    private AgentController _vehicle;
    private PedestrianMovement _pm;

    public EnterVehicleGoal(AgentController vehicle, string goalName = "Entering vehicle")
    {
        GoalName = goalName;
        _vehicle = vehicle;
    }

    public override void Initialise(AgentController agent)
    {
        VehicleMovement vm = _vehicle.GetComponent<VehicleMovement>();
        _pm = agent.GetComponent<PedestrianMovement>();

        WaypointNode currentNode = _pm.CurrentWaypoint;

        List<WaypointNode> path = new()
        {
            currentNode, vm.CurrentWaypoint
        };

        agent.Mover.SetPath(path);
    }

    public override void OnArrived(AgentController agent)
    {
        agent.transform.parent = _vehicle.transform;
        agent.ShowHideAgent(false);
        _pm.SetCurrentVehicle(_vehicle);
    }

    public override string SaveData()
    {
        EnterVehicleGoalSaveData data = new() { VehicleId = _vehicle.Id.ToString() };
        return JsonUtility.ToJson(data);
    }
}
