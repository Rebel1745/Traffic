using System.Collections.Generic;

[System.Serializable]
public class TrafficLightGroupSaveData
{
    public string id;  // Unique ID for this traffic light group
    public TrafficLightGroupType groupType;
    public List<TrafficLightSaveData> lights = new();
}