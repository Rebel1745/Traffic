using System.Collections.Generic;
using UnityEngine;

public class LaneData
{
    public List<LaneSegment> Lanes = new List<LaneSegment>();
    public string LaneInfo
    {
        get
        {
            string info = "" + Lanes.Count + " Lanes. ";

            foreach (var lane in Lanes)
            {
                info += lane.SegmentInfo + ". ";
            }

            return info;
        }
    }
}
