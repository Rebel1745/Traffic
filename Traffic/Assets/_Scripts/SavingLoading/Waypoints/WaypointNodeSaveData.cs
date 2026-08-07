using System.Collections.Generic;

[System.Serializable]
public class WaypointNodeSaveData
{
    public string Id;
    public float X;
    public float Z;
    public List<WaypointConnectionSaveData> Connections = new();
    public int ParentCellX;
    public int ParentCellZ;
    public WaypointType Type;
    public WaypointNetworkType NetworkType;
    public string PairedCrossingWaypointId;
    public string LaneNodeForTrafficLightId;
    public RoadDirection LightPosition;
}