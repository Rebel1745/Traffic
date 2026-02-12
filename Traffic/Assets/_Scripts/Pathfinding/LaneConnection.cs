using UnityEngine;

public class LaneConnection
{
    public LaneSegment TargetLane;
    public float Cost; // For A* (usually distance)
    public string ConnectionInfo
    {
        get
        {
            string info = "Target Lane: " + TargetLane.SegmentName;

            return info;
        }
    }
}
