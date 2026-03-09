using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public enum LightState { Red, Yellow, Green }

    public LightState CurrentState { get; private set; } = LightState.Red;

    [Header("Material for each light")]
    [SerializeField] private Material redLight;
    [SerializeField] private Material yellowLight;
    [SerializeField] private Material greenLight;

    [SerializeField] private Renderer bum;

    public void SetState(LightState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    public bool IsGreen() => CurrentState == LightState.Green;

    private void UpdateVisuals()
    {
        switch (CurrentState)
        {
            case LightState.Red:
                bum.material = redLight;
                break;
            case LightState.Yellow:
                bum.material = yellowLight;
                break;
            case LightState.Green:
                bum.material = greenLight;
                break;

        }
    }
}