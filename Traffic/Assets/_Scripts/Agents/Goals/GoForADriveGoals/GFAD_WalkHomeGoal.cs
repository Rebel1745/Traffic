using UnityEngine;

public class GFAD_WalkHomeGoal : Goal
{
    public GFAD_WalkHomeGoal(WaypointNode target, string name)
    {
    }

    public override void Initialise(AgentController agent)
    {

    }

    public override void OnArrived(AgentController agent)
    {
        Debug.Log("We're home, smile and wave");
    }
}
