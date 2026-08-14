using UnityEngine;
using System.Collections.Generic;

public class WalkToWaypointGoal : Goal
{
    private WaypointNode _target;

    public WalkToWaypointGoal(WaypointNode target, string goalName)
    {
        GoalName = goalName;
        _target = target;
    }

    public override void Initialise(AgentController agent)
    {
        List<WaypointNode> path = AStarPathfinder.FindPath(agent.Mover.CurrentWaypoint, _target);

        if (path != null && path.Count > 0)
        {
            agent.Mover.SetPath(path);
        }
        else
        {
            Debug.LogWarning($"[{agent.gameObject.name}] No path found for goal: {GoalName}");
            path = new() { agent.Mover.CurrentWaypoint, _target };
            agent.Mover.SetPath(path);
        }
    }

    public override void OnArrived(AgentController agent)
    {

    }
}
