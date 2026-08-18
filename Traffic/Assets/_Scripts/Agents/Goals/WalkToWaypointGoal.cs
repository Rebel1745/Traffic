using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class WalkToWaypointGoal : Goal
{
    private WaypointNode _target;

    public override string GoalType => "WalkToWaypoint";

    public WalkToWaypointGoal(WaypointNode target, string goalName = "Walking to waypoint")
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

    public override string SaveData()
    {
        WalkToWaypointGoalSaveData data = new() { TargetId = _target.Id.ToString() };
        return JsonUtility.ToJson(data);
    }
}
