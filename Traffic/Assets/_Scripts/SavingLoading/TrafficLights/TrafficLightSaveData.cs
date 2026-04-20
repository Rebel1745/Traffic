[System.Serializable]
public class TrafficLightSaveData
{
    public string LightWaypointNodeId;  // Which waypoint this light is on
    public string Label;
    public bool IsCopyOfLight;
    public RoadDirection LightPosition;
    public float GreenDuration;
    public float YellowDuration;
    public float RedDuration;
    public float RedOverlapDuration;
    public string OriginalLabel;
    public float OriginalRedDuration;
    public float OriginalYellowDuration;
    public float OriginalGreenDuration;
    public float OriginalRedOverlapDuration;
}