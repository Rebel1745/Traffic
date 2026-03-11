using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public string saveVersion = "1.0";
    public string saveDate;
    public GridSaveData grid;
    public WaypointSaveData waypoints;
    public TrafficLightsSaveData trafficLights;
}