using System.Collections.Generic;
using UnityEngine;

public class RoadGraph : MonoBehaviour
{
    public List<Road> Roads = new List<Road>();
    public List<Intersection> Intersections = new List<Intersection>();
    public Dictionary<int, Intersection> IntersectionMap = new Dictionary<int, Intersection>();

    [Header("Lane Settings")]
    public float LaneWidth = 3f;
    public int WaypointsPerLane = 10;

    private int roadIDCounter = 0;
    private int laneIDCounter = 0;

    /// <summary>
    /// Create an intersection at the specified position
    /// </summary>
    public Intersection CreateIntersection(Vector3 position)
    {
        int id = Intersections.Count;
        Intersection intersection = new Intersection(id, position);
        Intersections.Add(intersection);
        IntersectionMap[id] = intersection;
        return intersection;
    }

    /// <summary>
    /// Create a road between two intersections with the specified number of lanes per direction
    /// </summary>
    public Road CreateRoad(Intersection startIntersection, Intersection endIntersection, int laneCount = 2)
    {
        Road road = new Road(roadIDCounter++, startIntersection, endIntersection, laneCount);

        // Create lanes for both directions
        CreateLanesForDirection(road, startIntersection, endIntersection, laneCount, road.LanesAtoB);
        CreateLanesForDirection(road, endIntersection, startIntersection, laneCount, road.LanesBtoA);

        Roads.Add(road);
        startIntersection.ConnectedRoads.Add(road);
        endIntersection.ConnectedRoads.Add(road);

        return road;
    }

    /// <summary>
    /// Create lanes for a specific direction on a road
    /// </summary>
    private void CreateLanesForDirection(Road road, Intersection start, Intersection end, int laneCount, List<Lane> laneList)
    {
        for (int i = 0; i < laneCount; i++)
        {
            Lane lane = new Lane(laneIDCounter++, start, end);
            lane.Waypoints = GenerateWaypoints(start.Position, end.Position, i, laneCount);
            laneList.Add(lane);
        }
    }

    /// <summary>
    /// Generate waypoints along a lane with proper offset for lane positioning
    /// </summary>
    private List<Vector3> GenerateWaypoints(Vector3 start, Vector3 end, int laneIndex, int totalLanes)
    {
        List<Vector3> waypoints = new List<Vector3>();

        Vector3 direction = (end - start).normalized;
        float roadLength = Vector3.Distance(start, end);

        // Calculate lane offset (perpendicular to road direction)
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        float laneOffset = (laneIndex - (totalLanes - 1) * 0.5f) * LaneWidth;
        Vector3 offset = perpendicular * laneOffset;

        // Generate waypoints along the lane
        for (int i = 0; i < WaypointsPerLane; i++)
        {
            float t = i / (float)(WaypointsPerLane - 1);
            Vector3 position = Vector3.Lerp(start, end, t) + offset;
            waypoints.Add(position);
        }

        return waypoints;
    }

    /// <summary>
    /// Add a connection between two lanes (for turns at intersections)
    /// </summary>
    public void AddLaneConnection(Lane fromLane, Lane toLane)
    {
        if (fromLane == null || toLane == null)
        {
            Debug.LogWarning("Attempted to connect null lanes");
            return;
        }

        if (!fromLane.ConnectedLanes.Contains(toLane))
        {
            fromLane.ConnectedLanes.Add(toLane);
        }
    }

    /// <summary>
    /// Automatically connect all valid lanes at an intersection
    /// </summary>
    public void ConnectAllLanes(Intersection intersection)
    {
        List<Lane> incomingLanes = GetIncomingLanes(intersection);
        List<Lane> outgoingLanes = GetOutgoingLanes(intersection);

        foreach (Lane incoming in incomingLanes)
        {
            foreach (Lane outgoing in outgoingLanes)
            {
                // Don't connect a lane back to the road it came from
                if (incoming.StartIntersection != outgoing.EndIntersection)
                {
                    AddLaneConnection(incoming, outgoing);
                }
            }
        }

        Debug.Log($"Connected {incomingLanes.Count} incoming lanes to {outgoingLanes.Count} outgoing lanes at intersection {intersection.IntersectionID}");
    }

    /// <summary>
    /// Get all lanes entering an intersection
    /// </summary>
    public List<Lane> GetIncomingLanes(Intersection intersection)
    {
        List<Lane> incomingLanes = new List<Lane>();

        foreach (Road road in intersection.ConnectedRoads)
        {
            if (road.IntersectionB == intersection)
            {
                incomingLanes.AddRange(road.LanesAtoB);
            }
            if (road.IntersectionA == intersection)
            {
                incomingLanes.AddRange(road.LanesBtoA);
            }
        }

        return incomingLanes;
    }

    /// <summary>
    /// Get all lanes leaving an intersection
    /// </summary>
    public List<Lane> GetOutgoingLanes(Intersection intersection)
    {
        List<Lane> outgoingLanes = new List<Lane>();

        foreach (Road road in intersection.ConnectedRoads)
        {
            if (road.IntersectionA == intersection)
            {
                outgoingLanes.AddRange(road.LanesAtoB);
            }
            if (road.IntersectionB == intersection)
            {
                outgoingLanes.AddRange(road.LanesBtoA);
            }
        }

        return outgoingLanes;
    }

    /// <summary>
    /// Get a lane by its ID
    /// </summary>
    public Lane GetLaneByID(int laneID)
    {
        foreach (Road road in Roads)
        {
            foreach (Lane lane in road.LanesAtoB)
            {
                if (lane.LaneID == laneID) return lane;
            }
            foreach (Lane lane in road.LanesBtoA)
            {
                if (lane.LaneID == laneID) return lane;
            }
        }
        return null;
    }

    /// <summary>
    /// Clear all roads and intersections
    /// </summary>
    public void Clear()
    {
        Roads.Clear();
        Intersections.Clear();
        IntersectionMap.Clear();
        roadIDCounter = 0;
        laneIDCounter = 0;
    }
}