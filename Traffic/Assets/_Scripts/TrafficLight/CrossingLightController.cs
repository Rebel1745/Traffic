using UnityEngine;

public class CrossingLightController : MonoBehaviour
{
    public LightState CurrentState { get; private set; } = LightState.Red;

    [Header("Material for each light")]
    [SerializeField] private Material _redLightOn;
    [SerializeField] private Material _redLightOff;
    [SerializeField] private Material _greenLightOn;
    [SerializeField] private Material _greenLightOff;

    [SerializeField] private Renderer _redLightRen;
    [SerializeField] private Renderer _greenLightRen;

    public void SetState(LightState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    public bool IsGreen() => CurrentState == LightState.Green;
    public bool IsRed() => CurrentState == LightState.Red;

    private void UpdateVisuals()
    {
        switch (CurrentState)
        {
            case LightState.Red:
                _redLightRen.material = _redLightOn;
                _greenLightRen.material = _greenLightOff;
                break;
            case LightState.Green:
                _redLightRen.material = _redLightOff;
                _greenLightRen.material = _greenLightOn;
                break;

        }
    }
}
