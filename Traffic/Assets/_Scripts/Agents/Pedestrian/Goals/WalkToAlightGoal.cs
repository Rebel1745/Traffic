using UnityEngine;

public class WalkToAlightGoal : Goal
{
    public WalkToAlightGoal(WaypointNode target, string name) : base(target, name: name, requiresMovement: true)
    {
        Debug.Log(name);
    }

    public override void OnArrived(AgentController agent)
    {
        Debug.Log($"{agent.name} got to the alight point. Smile and wave boys, smile and wave.");
    }
}
