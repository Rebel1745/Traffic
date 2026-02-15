using System.Collections.Generic;
using UnityEngine;

public class TrafficWaypoint
{
    public Vector3 Position;
    public List<TrafficWaypoint> Connections;
    public int GridX, GridZ; // Cell coordinates
    public int LaneIndex; // Which lane this waypoint belongs to

    public TrafficWaypoint(Vector3 position, int gridX, int gridZ, int laneIndex)
    {
        Position = position;
        GridX = gridX;
        GridZ = gridZ;
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