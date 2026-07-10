using UnityEngine;

public class GFAD_EnterVehicleGoal : Goal
{
    private AgentController _vehicle;
    private WaypointNode _target;

    public GFAD_EnterVehicleGoal(WaypointNode target, AgentController vehicle, string name) : base(target, name, requiresMovement: true)
    {
        _vehicle = vehicle;
        _target = target;
    }

    public override void OnArrived(AgentController agent)
    {
        agent.transform.parent = _vehicle.transform;
        agent.ShowHideAgent(false);

        agent.InterruptAndAddGoal(new GFAD_DriveAroundRandomlyGoal(_target, _vehicle, "Drive around randomly"));
    }
}
