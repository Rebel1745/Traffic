using UnityEngine;

public class GridCell
{
    public Vector3 Position;
    public CellType CellType;
    public RoadType RoadType;
}

public enum CellType
{
    Empty,
    Road
}