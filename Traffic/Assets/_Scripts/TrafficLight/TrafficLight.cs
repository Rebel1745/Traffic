[System.Serializable]
public class TrafficLight
{
    public TrafficLightController Light;
    public float GreenDuration = 10f;
    public float YellowDuration = 3f;
    public float RedDuration = 10f;
    public float RedOverlapDuration = 1f;
}