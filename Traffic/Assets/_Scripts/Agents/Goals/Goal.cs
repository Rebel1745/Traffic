public abstract class Goal
{
    public string GoalName { get; set; } = "Unnamed Goal";
    public abstract string GoalType { get; }

    // Called immediately before the goal starts executing.
    // Used to calculate paths, set targets, or subscribe to events.
    public abstract void Initialise(AgentController agent);

    // Called when the goal finishes (arrival, timer, or event).
    // Used for cleanup or conditional logic (e.g., "Check Fuel").
    public abstract void OnArrived(AgentController agent);

    // called when the goal data needs to be saved
    public abstract string SaveData();
}