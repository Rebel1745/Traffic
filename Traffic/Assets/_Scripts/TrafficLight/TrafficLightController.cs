using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public enum LightState { Red, Yellow, Green }

    public LightState CurrentState { get; private set; } = LightState.Red;

    public WaypointNode AssignedWaypoint { get; set; }

    [Header("Material for each light")]
    [SerializeField] private Material _redLightOn;
    [SerializeField] private Material _redLightOff;
    [SerializeField] private Material _yellowLightOn;
    [SerializeField] private Material _yellowLightOff;
    [SerializeField] private Material _greenLightOn;
    [SerializeField] private Material _greenLightOff;

    [SerializeField] private Renderer _redLightRen;
    [SerializeField] private Renderer _yellowLightRen;
    [SerializeField] private Renderer _greenLightRen;

    public void SetState(LightState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    public bool IsGreen() => CurrentState == LightState.Green;
    public bool IsYellow() => CurrentState == LightState.Yellow;
    public bool IsRed() => CurrentState == LightState.Red;

    private void UpdateVisuals()
    {
        switch (CurrentState)
        {
            case LightState.Red:
                _redLightRen.material = _redLightOn;
                _yellowLightRen.material = _yellowLightOff;
                _greenLightRen.material = _greenLightOff;
                break;
            case LightState.Yellow:
                _redLightRen.material = _redLightOff;
                _yellowLightRen.material = _yellowLightOn;
                _greenLightRen.material = _greenLightOff;
                break;
            case LightState.Green:
                _redLightRen.material = _redLightOff;
                _yellowLightRen.material = _yellowLightOff;
                _greenLightRen.material = _greenLightOn;
                break;

        }
    }
}