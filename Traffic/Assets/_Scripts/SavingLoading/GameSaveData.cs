using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public string SaveVersion = "1.0";
    public string SaveDate;
    public GridSaveData Grid;
    public WaypointSaveData VehicleWaypoints;
    public WaypointSaveData PedestrianWaypoints;
    public TrafficLightsSaveData TrafficLights;
}