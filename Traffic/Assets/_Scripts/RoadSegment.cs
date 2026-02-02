using UnityEngine;

public class RoadSegment
{
    public Vector3 startPos;
    public Vector3 endPos;
    public RoadNode startNode;
    public RoadNode endNode;
    public RoadDirection direction;
    public int lanesPerDirection = 1; // For future expansion

    public float Length => Vector3.Distance(startPos, endPos);

    public RoadSegment(Vector3 start, Vector3 end, RoadDirection dir)
    {
        startPos = start;
        endPos = end;
        direction = dir;
    }
}

public enum RoadDirection
{
    NorthSouth,
    EastWest
}
