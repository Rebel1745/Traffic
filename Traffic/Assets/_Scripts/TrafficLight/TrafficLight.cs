[System.Serializable]
public class TrafficLight
{
    public TrafficLightController Light;
    public string Label;
    public RoadDirection LightPosition;
    public float GreenDuration = 10f;
    public float YellowDuration = 3f;
    public float RedDuration = 10f;
    public float RedOverlapDuration = 1f;
    public string Details { get { return "Light: " + Label + " Green: " + GreenDuration + " Yellow: " + YellowDuration + " Red: " + RedDuration + " Overlap: " + RedOverlapDuration; } }

    public TrafficLight Clone()
    {
        return new TrafficLight
        {
            Label = this.Label,
            GreenDuration = this.GreenDuration,
            YellowDuration = this.YellowDuration,
            RedDuration = this.RedDuration,
            RedOverlapDuration = this.RedOverlapDuration
        };
    }
}