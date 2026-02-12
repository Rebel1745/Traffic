using System.Collections.Generic;
using UnityEngine;

public class LaneSegment
{
    public Vector3 StartWaypoint;
    public Vector3 EndWaypoint;
    public RoadDirection Direction; // Which way this lane flows
    public List<LaneConnection> OutgoingConnections = new List<LaneConnection>();
    public string SegmentName
    {
        get
        {
            return "Lane from: " + StartWaypoint + "  to: " + EndWaypoint;
        }
    }
    public string SegmentInfo
    {
        get
        {
            string info = OutgoingConnections.Count + " connections - ";
            int i = 1;

            foreach (var connection in OutgoingConnections)
            {
                info += "Connection " + i + " " + connection.ConnectionInfo;
                i++;
            }

            return info;
        }
    }
}

public enum RoadDirection
{
    North, East, South, West
}
