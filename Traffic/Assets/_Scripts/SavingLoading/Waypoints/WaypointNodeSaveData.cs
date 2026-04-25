using System.Collections.Generic;

[System.Serializable]
public class WaypointNodeSaveData
{
    public string Id;
    public float X;
    public float Z;
    public WaypointType Type;
    public WaypointNetworkType NetworkType;
    public int ParentCellX;
    public int ParentCellZ;
    public List<WaypointConnectionSaveData> Connections = new();
    public string PairedCrossingWaypointId;
    public string LaneNodeForTrafficLightId;
    public RoadDirection LightPosition;
}