using System.Collections.Generic;
using UnityEngine;

public class GridCell
{
    public Vector3Int Position;
    public CellType CellType;
    public RoadType RoadType;
    public RoadDirection RoadDirection;
    public string CellInfo { get { return "Cell (" + Position.x + ", " + Position.z + ") Type: " + RoadType.ToString(); } }
}

public enum CellType
{
    Empty,
    Road
}

public enum RoadType
{
    Empty, // no road in the cell
    Single, // single square of road, surrounded on all four sides by pavement
    DeadEnd, // road only connected to one road, pavement on three sides
    Straight, // standard straight road, pavement on two sides
    Corner, // joins two perpendicular roads
    TJunction, // joins three straight roads with a perpendicular road in the middle
    Crossroads // joins four straight roads
}

public enum RoadDirection
{
    North,
    South,
    East,
    West
}