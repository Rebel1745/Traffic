using UnityEngine;

[System.Serializable]
public class GridCellSaveData
{
    public int X;
    public int Z;
    public CellType CellType;
    public RoadType RoadType;
    public RoadDirection RoadDirection;
    public bool HasCustomUVs;
    public Vector2[] CustomUVs;
    public bool HasTrafficLights;
}
