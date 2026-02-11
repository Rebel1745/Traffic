using UnityEngine;
using System.Collections.Generic;
using System;

public class LanePathfinder
{
    public List<LaneSegment> FindPath(LaneSegment start, LaneSegment goal)
    {
        var openSet = new PriorityQueue<PathNode>();
        var cameFrom = new Dictionary<LaneSegment, LaneSegment>();
        var gScore = new Dictionary<LaneSegment, float>();
        var fScore = new Dictionary<LaneSegment, float>();

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        openSet.Enqueue(new PathNode(start, fScore[start]));

        while (!openSet.IsEmpty())
        {
            PathNode currentNode = openSet.Dequeue();
            LaneSegment current = currentNode.Lane;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (var connection in current.OutgoingConnections)
            {
                LaneSegment neighbor = connection.TargetLane;
                float tentativeGScore = gScore[current] + connection.Cost;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);

                    if (!openSet.Contains(new PathNode(neighbor, fScore[neighbor])))
                        openSet.Enqueue(new PathNode(neighbor, fScore[neighbor]));
                }
            }
        }

        return null; // No path found
    }

    private float Heuristic(LaneSegment from, LaneSegment to)
    {
        return Vector3.Distance(from.EndWaypoint, to.EndWaypoint);
    }

    private List<LaneSegment> ReconstructPath(Dictionary<LaneSegment, LaneSegment> cameFrom, LaneSegment current)
    {
        var path = new List<LaneSegment> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }
}

public class PathNode : IComparable<PathNode>
{
    public LaneSegment Lane;
    public float FScore;

    public PathNode(LaneSegment lane, float fScore)
    {
        Lane = lane;
        FScore = fScore;
    }

    public int CompareTo(PathNode other)
    {
        if (other == null)
            return 1;

        // Lower fScore has higher priority (comes first)
        return FScore.CompareTo(other.FScore);
    }
}