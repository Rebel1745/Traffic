using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaypointNode
{
    public string Id { get; set; }
    public Vector3 Position { get; set; }
    public List<WaypointConnection> Connections { get; set; }
    public GridCell ParentCell { get; set; }
    public WaypointType Type { get; set; }
    public TrafficLightController AssignedLight { get; set; }
    public WaypointNode PairedCrossingWaypoint { get; set; }
    public string PairedCrossingWaypointId { get; set; }
    // below is the node that the vehicle will stop at if it has a traffic light
    public WaypointNode LaneNodeForTrafficLight { get; set; }
    public string LaneNodeForTrafficLightId { get; set; }

    public WaypointNode(Vector3 position, GridCell parentCell, WaypointType type, WaypointNode laneNode = null)
    {
        Id = System.Guid.NewGuid().ToString();
        Position = position;
        ParentCell = parentCell;
        Type = type;
        Connections = new List<WaypointConnection>();
        AssignedLight = null;
        PairedCrossingWaypoint = null;
        LaneNodeForTrafficLight = laneNode;
    }
}

public enum WaypointType
{
    Entry,
    Exit,
    Midpoint,
    UTurn,
    TrafficLightLocation
}