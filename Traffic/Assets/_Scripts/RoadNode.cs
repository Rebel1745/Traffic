using UnityEngine;
using System.Collections.Generic;

public class RoadNode
{
    public Vector3 position;
    public List<RoadSegment> connectedSegments = new List<RoadSegment>();
    public NodeType nodeType;

    public RoadNode(Vector3 pos)
    {
        position = pos;
    }

    public void UpdateNodeType()
    {
        int connectionCount = connectedSegments.Count;

        if (connectionCount == 1)
        {
            nodeType = NodeType.DeadEnd;
        }
        else if (connectionCount == 2)
        {
            // Check if both segments have same direction
            if (connectedSegments[0].direction == connectedSegments[1].direction)
            {
                nodeType = NodeType.Straight;
            }
            else
            {
                nodeType = NodeType.Corner;
            }
        }
        else if (connectionCount == 3)
        {
            nodeType = NodeType.TJunction;
        }
        else if (connectionCount >= 4)
        {
            nodeType = NodeType.Crossroad;
        }
    }
}

public enum NodeType
{
    DeadEnd,      // 1 connection
    Straight,     // 2 connections, same direction
    Corner,       // 2 connections, different directions
    TJunction,    // 3 connections
    Crossroad     // 4 connections
}