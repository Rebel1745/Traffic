using UnityEngine;

public class WalkToFrontDoorGoal : Goal
{
    public WalkToFrontDoorGoal(string goalName = "Walk to front door")
    {
        GoalName = goalName;
    }

    public override void Initialise(AgentController agent)
    {
        WaypointNode homeNode = PedestrianManager.Instance.GetHomeWaypoint(agent);

        agent.AddGoalAfterCurrent(new WalkToWaypointGoal(homeNode, "Walking to home waypoint"));
        agent.OnMovementFinished();
    }

    public override void OnArrived(AgentController agent)
    {

    }
}
