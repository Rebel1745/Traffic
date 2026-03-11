[System.Serializable]
public class TrafficLightSaveData
{
    public string lightWaypointNodeId;  // Which waypoint this light is on
    public float greenDuration;
    public float yellowDuration;
    public float redDuration;
    public float redOverlapDuration;
}