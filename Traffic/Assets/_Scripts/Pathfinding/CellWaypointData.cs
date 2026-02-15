using System.Collections.Generic;

public class CellWaypointData
{
    public Dictionary<RoadDirection, List<TrafficWaypoint>> ExitWaypoints;
    public Dictionary<RoadDirection, List<TrafficWaypoint>> EntryWaypoints;
    public List<TrafficWaypoint> InternalWaypoints;
    public List<TrafficWaypoint> AllWaypoints;

    public CellWaypointData()
    {
        ExitWaypoints = new Dictionary<RoadDirection, List<TrafficWaypoint>>();
        EntryWaypoints = new Dictionary<RoadDirection, List<TrafficWaypoint>>();
        InternalWaypoints = new List<TrafficWaypoint>();
        AllWaypoints = new List<TrafficWaypoint>();

        foreach (RoadDirection dir in System.Enum.GetValues(typeof(RoadDirection)))
        {
            ExitWaypoints[dir] = new List<TrafficWaypoint>();
            EntryWaypoints[dir] = new List<TrafficWaypoint>();
        }
    }
}