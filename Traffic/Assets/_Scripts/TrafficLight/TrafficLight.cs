[System.Serializable]
public class TrafficLight
{
    public TrafficLightController Light;
    public string Label;
    public RoadDirection LightPosition;
    public float GreenDuration;
    public float YellowDuration;
    public float RedDuration;
    public float RedOverlapDuration;
    public string Details { get { return "Light: " + Label + " Green: " + GreenDuration + " Yellow: " + YellowDuration + " Red: " + RedDuration + " Overlap: " + RedOverlapDuration; } }
    public string OriginalLabel;
    public float OriginalRedDuration;
    public float OriginalYellowDuration;
    public float OriginalGreenDuration;
    public float OriginalRedOverlapDuration;

    public TrafficLight Clone()
    {
        return new TrafficLight
        {
            Light = this.Light,
            Label = this.Label,
            LightPosition = this.LightPosition,
            GreenDuration = this.GreenDuration,
            YellowDuration = this.YellowDuration,
            RedDuration = this.RedDuration,
            RedOverlapDuration = this.RedOverlapDuration,
            OriginalGreenDuration = this.GreenDuration,
            OriginalYellowDuration = this.YellowDuration,
            OriginalRedDuration = this.RedDuration,
            OriginalRedOverlapDuration = this.RedOverlapDuration
        };
    }

    public void ResetToDefaults()
    {
        Label = OriginalLabel;
        GreenDuration = OriginalGreenDuration;
        YellowDuration = OriginalYellowDuration;
        RedDuration = OriginalRedDuration;
        RedOverlapDuration = OriginalRedOverlapDuration;
    }
}