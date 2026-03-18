using UnityEngine;

public class GridCell
{
    public Vector3Int Position;
    public CellType CellType;
    public RoadType RoadType;
    public RoadDirection RoadDirection;
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
    North, // used for t junction (direction corresponds to the turnoff road direction, e.g. the classic T layout would be South)
    South, // used for t junction
    East, // used for t junction
    West, // used for t junction
    NorthSouth, // used for straight roads
    WestEast,  // used for straight roads
    NorthEast, // Used for corners
    NorthWest, // Used for corners
    SouthEast, // Used for corners
    SouthWest // Used for corners
}