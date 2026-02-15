using System.Collections.Generic;
using UnityEngine;

public class TrafficWaypoint
{
    public Vector3 Position;
    public List<TrafficWaypoint> Connections;
    public int GridX, GridY; // Cell coordinates
    public int LaneIndex; // Which lane this waypoint belongs to

    public TrafficWaypoint(Vector3 position, int gridX, int gridY, int laneIndex)
    {
        Position = position;
        GridX = gridX;
        GridY = gridY;
        LaneIndex = laneIndex;
        Connections = new List<TrafficWaypoint>();
    }

    public void AddConnection(TrafficWaypoint target)
    {
        if (!Connections.Contains(target))
        {
            Connections.Add(target);
        }
    }
}