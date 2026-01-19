using System.Collections.Generic;
using UnityEngine;

public class Lane
{
    public int LaneID;
    public List<Vector3> Waypoints;
    public Intersection StartIntersection;
    public Intersection EndIntersection;
    public float SpeedLimit = 50f;
    public List<Lane> ConnectedLanes;

    public Lane(int id, Intersection start, Intersection end)
    {
        LaneID = id;
        StartIntersection = start;
        EndIntersection = end;
        ConnectedLanes = new List<Lane>();
        Waypoints = new List<Vector3>();
    }
}