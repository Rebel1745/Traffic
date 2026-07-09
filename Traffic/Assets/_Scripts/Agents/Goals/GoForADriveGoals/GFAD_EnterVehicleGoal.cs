using UnityEngine;

public class GFAD_EnterVehicleGoal : Goal
{
    private AgentController _vehicle;

    public GFAD_EnterVehicleGoal(WaypointNode target, AgentController vehicle, string name) : base(target, name, requiresMovement: true)
    {
        _vehicle = vehicle;
    }

    public override void OnArrived(AgentController agent)
    {
        agent.transform.parent = _vehicle.transform;
        agent.ShowHideAgent(false);
    }
}
