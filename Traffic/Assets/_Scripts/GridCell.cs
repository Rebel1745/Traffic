using System.Collections.Generic;
using UnityEngine;

public class GridCell
{
    public Vector3Int Position;
    public CellType CellType;
    public RoadType RoadType;
    public LaneData LaneData; // Add this
}

public class LaneData
{
    public List<LaneSegment> Lanes = new List<LaneSegment>();
}

public class LaneSegment
{
    public Vector3 StartWaypoint;
    public Vector3 EndWaypoint;
    public RoadDirection Direction; // Which way this lane flows
    public List<LaneConnection> OutgoingConnections = new List<LaneConnection>();
}

public class LaneConnection
{
    public LaneSegment TargetLane;
    public float Cost; // For A* (usually distance)
}

public enum RoadDirection
{
    North, East, South, West
}

public enum CellType
{
    Empty,
    Road
}