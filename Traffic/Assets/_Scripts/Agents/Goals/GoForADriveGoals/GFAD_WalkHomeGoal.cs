using UnityEngine;

public class GFAD_WalkHomeGoal : Goal
{
    public GFAD_WalkHomeGoal(WaypointNode target, string name) : base(target, name, requiresMovement: true)
    {
    }

    public override void OnArrived(AgentController agent)
    {
        Debug.Log("We're home, smile and wave");
    }
}
