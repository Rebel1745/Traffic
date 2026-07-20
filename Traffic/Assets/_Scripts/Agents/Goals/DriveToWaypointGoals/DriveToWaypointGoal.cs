using UnityEngine;
using System.Collections.Generic;

public class DriveToWaypointGoal : Goal
{
    private WaypointNode _target;

    public DriveToWaypointGoal(WaypointNode target, string goalName)
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
            // Handle failure (retry, pick new goal, etc.)
        }
    }

    public override void OnArrived(AgentController agent)
    {
    }
}
