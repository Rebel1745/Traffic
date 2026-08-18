using UnityEngine;

public class WalkToFrontDoorGoal : Goal
{
    public override string GoalType => "WalkToFrontDoor";

    private WaypointNode _target;

    public WalkToFrontDoorGoal(string goalName = "Walk to front door")
    {
        GoalName = goalName;
    }

    public override void Initialise(AgentController agent)
    {
        _target = PedestrianManager.Instance.GetHomeWaypoint(agent);

        if (_target == null)
        {
            Debug.LogError("Home node is null!");
            return;
        }

        agent.OnMovementFinished();
    }

    public override void OnArrived(AgentController agent)
    {
        agent.AddGoalAfterCurrent(new WalkToWaypointGoal(_target, "Walking to home waypoint"));

    }

    public override string SaveData()
    {
        return "";
    }
}
