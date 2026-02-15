using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficPathfinder
{
    private class PathNode : IComparable<PathNode>
    {
        public TrafficWaypoint Waypoint;
        public PathNode Parent;
        public float GCost; // Cost from start
        public float HCost; // Heuristic cost to goal
        public float FCost => GCost + HCost;

        public PathNode(TrafficWaypoint waypoint, PathNode parent, float gCost, float hCost)
        {
            Waypoint = waypoint;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
        }

        public int CompareTo(PathNode other)
        {
            // Compare by F cost first, then by H cost for tie-breaking
            int fComparison = FCost.CompareTo(other.FCost);
            if (fComparison != 0)
                return fComparison;
            return HCost.CompareTo(other.HCost);
        }
    }

    /// <summary>
    /// Finds a path from start waypoint to goal waypoint using A* algorithm
    /// </summary>
    /// <param name="startWaypoint">The starting waypoint</param>
    /// <param name="goalWaypoint">The destination waypoint</param>
    /// <returns>List of waypoints representing the path, or empty list if no path found</returns>
    public static List<TrafficWaypoint> FindPath(TrafficWaypoint startWaypoint, TrafficWaypoint goalWaypoint)
    {
        if (startWaypoint == null || goalWaypoint == null)
        {
            Debug.LogError("Start or goal waypoint is null");
            return new List<TrafficWaypoint>();
        }

        if (startWaypoint == goalWaypoint)
        {
            return new List<TrafficWaypoint> { startWaypoint };
        }

        var openSet = new PriorityQueue<PathNode>();
        var closedSet = new HashSet<TrafficWaypoint>();
        var gScores = new Dictionary<TrafficWaypoint, float>();

        // Initialize start node
        float hCostStart = Vector3.Distance(startWaypoint.Position, goalWaypoint.Position);
        var startNode = new PathNode(startWaypoint, null, 0f, hCostStart);
        openSet.Enqueue(startNode);
        gScores[startWaypoint] = 0f;

        while (openSet.Count > 0)
        {
            PathNode currentNode = openSet.Dequeue();
            TrafficWaypoint current = currentNode.Waypoint;

            // Goal reached
            if (current == goalWaypoint)
            {
                return ReconstructPath(currentNode);
            }

            closedSet.Add(current);

            // Check all neighbors
            foreach (TrafficWaypoint neighbor in current.Connections)
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = currentNode.GCost + Vector3.Distance(current.Position, neighbor.Position);

                // If we've found a better path to this neighbor, or haven't visited it yet
                if (!gScores.ContainsKey(neighbor) || tentativeGCost < gScores[neighbor])
                {
                    gScores[neighbor] = tentativeGCost;
                    float hCost = Vector3.Distance(neighbor.Position, goalWaypoint.Position);
                    var neighborNode = new PathNode(neighbor, currentNode, tentativeGCost, hCost);
                    openSet.Enqueue(neighborNode);
                }
            }
        }

        // No path found
        Debug.LogWarning($"No path found from waypoint at {startWaypoint.Position} to {goalWaypoint.Position}");
        return new List<TrafficWaypoint>();
    }

    /// <summary>
    /// Finds a path from a starting cell to a goal cell
    /// </summary>
    /// <param name="startCell">The starting cell</param>
    /// <param name="goalCell">The destination cell</param>
    /// <param name="preferredLane">Optional preferred lane (0, 1, 2, etc.)</param>
    /// <returns>List of waypoints representing the path</returns>
    public static List<TrafficWaypoint> FindPathBetweenCells(GridCell startCell, GridCell goalCell, int preferredLane = -1)
    {
        if (startCell == null || goalCell == null || startCell.WaypointData.AllWaypoints.Count == 0 || goalCell.WaypointData.AllWaypoints.Count == 0)
        {
            Debug.LogError("Invalid cells or cells have no waypoints");
            return new List<TrafficWaypoint>();
        }

        // Select starting waypoint (prefer specified lane)
        TrafficWaypoint startWaypoint = SelectStartWaypoint(startCell, preferredLane);
        if (startWaypoint == null)
        {
            Debug.LogError("No valid start waypoint found");
            return new List<TrafficWaypoint>();
        }

        // Select goal waypoint (prefer specified lane)
        TrafficWaypoint goalWaypoint = SelectGoalWaypoint(goalCell, preferredLane);
        if (goalWaypoint == null)
        {
            Debug.LogError("No valid goal waypoint found");
            return new List<TrafficWaypoint>();
        }

        // Find path
        List<TrafficWaypoint> path = FindPath(startWaypoint, goalWaypoint);
        return path;
    }

    /// <summary>
    /// Selects a starting waypoint from the cell, optionally preferring a specific lane
    /// </summary>
    private static TrafficWaypoint SelectStartWaypoint(GridCell cell, int preferredLane)
    {
        // Return any available waypoint
        foreach (RoadDirection dir in Enum.GetValues(typeof(RoadDirection)))
        {
            if (cell.WaypointData.ExitWaypoints.ContainsKey(dir))
            {
                foreach (var waypoint in cell.WaypointData.ExitWaypoints[dir])
                {
                    if (waypoint.LaneIndex == preferredLane || preferredLane == -1)
                        return waypoint;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Selects a goal waypoint from the cell, optionally preferring a specific lane
    /// </summary>
    private static TrafficWaypoint SelectGoalWaypoint(GridCell cell, int preferredLane)
    {
        if (preferredLane >= 0 && cell.WaypointData.ExitWaypoints.ContainsKey(RoadDirection.North))
        {
            // Try to find a waypoint in the preferred lane
            foreach (var waypoint in cell.WaypointData.ExitWaypoints[RoadDirection.North])
            {
                if (waypoint.LaneIndex == preferredLane)
                    return waypoint;
            }
        }

        // Return any available waypoint
        foreach (RoadDirection dir in Enum.GetValues(typeof(RoadDirection)))
        {
            if (cell.WaypointData.ExitWaypoints.ContainsKey(dir))
            {
                foreach (var waypoint in cell.WaypointData.ExitWaypoints[dir])
                {
                    if (waypoint.LaneIndex == preferredLane || preferredLane == -1)
                        return waypoint;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Reconstructs the path from the goal node back to the start
    /// </summary>
    private static List<TrafficWaypoint> ReconstructPath(PathNode goalNode)
    {
        var path = new List<TrafficWaypoint>();
        PathNode current = goalNode;

        while (current != null)
        {
            path.Add(current.Waypoint);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Priority queue implementation for A* algorithm
    /// </summary>
    private class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> items = new List<T>();

        public int Count => items.Count;

        public void Enqueue(T item)
        {
            items.Add(item);
            items.Sort();
        }

        public T Dequeue()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Cannot dequeue from empty queue");

            T item = items[0];
            items.RemoveAt(0);
            return item;
        }
    }
}