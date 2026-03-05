using System.Collections.Generic;

[System.Serializable]
public class WaypointNodeSaveData
{
    public string id;
    public float x;
    public float z;
    public WaypointType type;
    public int parentCellX;     // Reference parent cell by grid position
    public int parentCellZ;
    public List<WaypointConnectionSaveData> connections = new();
}