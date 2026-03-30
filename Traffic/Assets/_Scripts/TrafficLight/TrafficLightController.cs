using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public enum LightState { Red, Yellow, Green }

    public LightState CurrentState { get; private set; } = LightState.Red;

    public WaypointNode AssignedWaypoint { get; set; }

    [Header("Material for each light")]
    [SerializeField] private Material _redLight;
    [SerializeField] private Material _yellowLight;
    [SerializeField] private Material _greenLight;

    [SerializeField] private Renderer _ren;

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
                _ren.material = _redLight;
                break;
            case LightState.Yellow:
                _ren.material = _yellowLight;
                break;
            case LightState.Green:
                _ren.material = _greenLight;
                break;

        }
    }
}