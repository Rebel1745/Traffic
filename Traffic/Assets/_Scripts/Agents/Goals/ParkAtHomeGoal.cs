using UnityEngine;

public class ParkAtHomeGoal : Goal
{
    public ParkAtHomeGoal(WaypointNode target, string name)
    {
    }

    public override void Initialise(AgentController agent)
    {

    }

    public override void OnArrived(AgentController agent)
    {
        Debug.Log($"{agent.name} arrived home. Smile and wave boys, smile and wave.");
    }
}
