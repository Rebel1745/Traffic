using UnityEngine;

public class DriveToRandomGoal : Goal
{
    public DriveToRandomGoal(WaypointNode target, string name) : base(target, name: name, requiresMovement: true)
    {
    }

    public override void OnArrived(AgentController agent)
    {
        //Debug.Log($"{agent.name} arrived at random spot. Smile and wave boys, smile and wave.");
    }
}
