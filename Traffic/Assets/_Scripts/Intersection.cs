using System.Collections.Generic;
using UnityEngine;

public class Intersection
{
    public int IntersectionID;
    public Vector3 Position;
    public List<Road> ConnectedRoads;

    public Intersection(int id, Vector3 pos)
    {
        IntersectionID = id;
        Position = pos;
        ConnectedRoads = new List<Road>();
    }
}

public enum TurnType { Straight, Left, Right, UTurn }
