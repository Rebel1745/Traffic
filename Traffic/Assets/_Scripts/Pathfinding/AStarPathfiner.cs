using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private class PathNode
    {
        public WaypointNode Waypoint { get; set; }
        public PathNode Parent { get; set; }
        public float GCost { get; set; } // Cost from start
        public float HCost { get; set; } // Heuristic cost to end
        public float FCost => GCost + HCost;

        public PathNode(WaypointNode waypoint, PathNode parent, float gCost, float hCost)
        {
            Waypoint = waypoint;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
        }
    }

    public static List<WaypointNode> FindPath(WaypointNode startWaypoint, WaypointNode endWaypoint)
    {
        if (startWaypoint == null || endWaypoint == null)
            return new List<WaypointNode>();

        // Check if start and end are in the same lane (same parent cell and compatible types)
        if (!AreWaypointsInSameLane(startWaypoint, endWaypoint))
            return new List<WaypointNode>();

        var openSet = new List<PathNode>();
        var closedSet = new HashSet<WaypointNode>();

        PathNode startNode = new PathNode(startWaypoint, null, 0f, CalculateHeuristic(startWaypoint, endWaypoint));
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Find node with lowest F cost
            int currentIndex = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < openSet[currentIndex].FCost)
                    currentIndex = i;
            }

            PathNode currentNode = openSet[currentIndex];

            // Goal reached
            if (currentNode.Waypoint == endWaypoint)
                return ReconstructPath(currentNode);

            openSet.RemoveAt(currentIndex);
            closedSet.Add(currentNode.Waypoint);

            // Check all connections from current waypoint
            foreach (var connection in currentNode.Waypoint.Connections)
            {
                WaypointNode neighbor = connection.TargetWaypoint;

                if (closedSet.Contains(neighbor))
                    continue;

                float newGCost = currentNode.GCost + connection.Cost;
                float hCost = CalculateHeuristic(neighbor, endWaypoint);

                // Check if this neighbor is already in open set
                PathNode existingNode = openSet.Find(n => n.Waypoint == neighbor);

                if (existingNode != null)
                {
                    // If we found a better path, update it
                    if (newGCost < existingNode.GCost)
                    {
                        existingNode.Parent = currentNode;
                        existingNode.GCost = newGCost;
                    }
                }
                else
                {
                    // Add new node to open set
                    PathNode newNode = new PathNode(neighbor, currentNode, newGCost, hCost);
                    openSet.Add(newNode);
                }
            }
        }

        // No path found
        return new List<WaypointNode>();
    }

    private static float CalculateHeuristic(WaypointNode from, WaypointNode to)
    {
        // Euclidean distance as heuristic
        return Vector3.Distance(from.Position, to.Position);
    }

    private static bool AreWaypointsInSameLane(WaypointNode start, WaypointNode end)
    {
        // Both waypoints must be Entry points (start of a lane)
        // This ensures vehicles start and end in the same lane direction
        if (start.Type != WaypointType.Entry || end.Type != WaypointType.Entry)
            return false;

        return true;
    }

    private static List<WaypointNode> ReconstructPath(PathNode endNode)
    {
        var path = new List<WaypointNode>();
        PathNode current = endNode;

        while (current != null)
        {
            path.Add(current.Waypoint);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }
}