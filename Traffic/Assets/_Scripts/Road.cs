using System.Collections.Generic;
using UnityEngine;

public class Road
{
    public int RoadID;
    public Intersection IntersectionA;
    public Intersection IntersectionB;
    public int LaneCount = 2;
    public List<Lane> LanesAtoB;
    public List<Lane> LanesBtoA;

    public Road(int id, Intersection a, Intersection b, int lanes = 2)
    {
        RoadID = id;
        IntersectionA = a;
        IntersectionB = b;
        LaneCount = lanes;
        LanesAtoB = new List<Lane>();
        LanesBtoA = new List<Lane>();
    }
}