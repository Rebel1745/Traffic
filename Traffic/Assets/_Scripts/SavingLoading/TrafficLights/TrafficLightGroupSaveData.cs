using System.Collections.Generic;

[System.Serializable]
public class TrafficLightGroupSaveData
{
    public string Id;  // Unique ID for this traffic light group
    public string JunctionName;
    public TrafficLightGroupType GroupType;
    public List<TrafficLightSaveData> Lights = new();
}