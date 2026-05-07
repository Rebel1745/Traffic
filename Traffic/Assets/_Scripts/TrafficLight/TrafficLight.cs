[System.Serializable]
public class TrafficLight
{
    public TrafficLightController Light;
    public string Label;
    public bool IsCopyOfLight; // this is false for a created junction light, true for a copy of the light used in the order functionality of the group settings
    public RoadDirection LightPosition;
    public float GreenDuration;
    public float YellowDuration;
    public float RedDuration;
    public float AllRedDuration;
    public float PedestrianCrossingDuration;
    public string OriginalLabel;
    public float OriginalRedDuration;
    public float OriginalYellowDuration;
    public float OriginalGreenDuration;
    public float OriginalAllRedDuration;
    public float OriginalPedestrianCrossingDuration;

    public TrafficLight Clone()
    {
        return new TrafficLight
        {
            Light = this.Light,
            Label = this.Label,
            IsCopyOfLight = this.IsCopyOfLight,
            LightPosition = this.LightPosition,
            GreenDuration = this.GreenDuration,
            YellowDuration = this.YellowDuration,
            RedDuration = this.RedDuration,
            AllRedDuration = this.AllRedDuration,
            PedestrianCrossingDuration = this.PedestrianCrossingDuration,
            OriginalGreenDuration = this.GreenDuration,
            OriginalYellowDuration = this.YellowDuration,
            OriginalRedDuration = this.RedDuration,
            OriginalAllRedDuration = this.AllRedDuration
        };
    }

    public void ResetToDefaults()
    {
        Label = OriginalLabel;
        GreenDuration = OriginalGreenDuration;
        YellowDuration = OriginalYellowDuration;
        RedDuration = OriginalRedDuration;
        AllRedDuration = OriginalAllRedDuration;
        PedestrianCrossingDuration = OriginalPedestrianCrossingDuration;
    }
}