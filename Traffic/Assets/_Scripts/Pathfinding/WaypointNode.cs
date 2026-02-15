using System.Collections.Generic;
using UnityEngine;

public class WaypointNode
{
    public Vector3 Position { get; set; }
    public List<WaypointConnection> Connections { get; set; }
    public GridCell ParentCell { get; set; }
    public WaypointType Type { get; set; }

    public WaypointNode(Vector3 position, GridCell parentCell, WaypointType type)
    {
        Position = position;
        ParentCell = parentCell;
        Type = type;
        Connections = new List<WaypointConnection>();
    }
}

public enum WaypointType
{
    Entry,
    Exit,
    Midpoint,
    UTurn
}