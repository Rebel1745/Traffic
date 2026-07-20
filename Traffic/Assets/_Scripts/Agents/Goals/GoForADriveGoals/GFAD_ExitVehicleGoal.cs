using UnityEngine;

public class GFAD_ExitVehicleGoal : Goal
{
    private AgentController _person;

    public GFAD_ExitVehicleGoal(WaypointNode target, AgentController person, string name)
    {
        _person = person;
    }

    public override void Initialise(AgentController agent)
    {

    }

    public override void OnArrived(AgentController agent)
    {
        WaypointNode homeWaypoint = PedestrianManager.Instance.GetHomeWaypoint(agent);

        agent.InterruptAndAddGoal(new GFAD_WalkHomeGoal(homeWaypoint, "Walking Home"));
    }
}
