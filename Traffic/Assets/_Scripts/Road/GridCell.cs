using System.Collections.Generic;
using UnityEngine;

public class GridCell
{
    public Vector3Int Position;
    public CellType CellType;
    public RoadType RoadType;
    public LaneData LaneData;
    public string CellInfo { get { return "Cell (" + Position.x + ", " + Position.z + ") Type: " + RoadType.ToString() + " " + LaneData.LaneInfo; } }
}

public enum CellType
{
    Empty,
    Road
}