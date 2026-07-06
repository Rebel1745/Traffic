using UnityEngine;

public class WalkToRandomGoal : Goal
{
    public WalkToRandomGoal(WaypointNode target, string name) : base(target, name: name, requiresMovement: true)
    {
    }

    public override void OnArrived(AgentController agent)
    {
        WaypointNode randomNode = PedestrianManager.Instance.GetRandomWaypoint(agent, WaypointType.PedestrianWalkway);
        string name = "Walk to random node at " + randomNode.Position;

        agent.AddGoal(new WalkToRandomGoal(randomNode, name));

        Debug.Log($"{agent.name} arrived at random spot. Move on.");
    }
}
