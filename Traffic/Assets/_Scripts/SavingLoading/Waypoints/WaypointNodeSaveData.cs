using System.Collections.Generic;

[System.Serializable]
public class WaypointNodeSaveData
{
    public string id;
    public float x;
    public float z;
    public WaypointType type;
    public int parentCellX;
    public int parentCellZ;
    public List<WaypointConnectionSaveData> connections = new();
    public string pairedCrossingWaypointId;
    public string laneNodeForTrafficLightId;
}