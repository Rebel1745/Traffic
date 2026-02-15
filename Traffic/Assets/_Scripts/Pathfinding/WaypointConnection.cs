public class WaypointConnection
{
    public WaypointNode TargetWaypoint { get; set; }
    public float Cost { get; set; }

    public WaypointConnection(WaypointNode target, float cost)
    {
        TargetWaypoint = target;
        Cost = cost;
    }
}