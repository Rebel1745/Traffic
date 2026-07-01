public abstract class Goal
{
    public WaypointNode Target;
    public bool RequiresMovement { get; protected set; }
    public string GoalName;

    protected Goal(WaypointNode target, string name = "Goal", bool requiresMovement = true)
    {
        Target = target;
        GoalName = name;
        RequiresMovement = requiresMovement;
    }

    public abstract void OnArrived(AgentController agent);
}