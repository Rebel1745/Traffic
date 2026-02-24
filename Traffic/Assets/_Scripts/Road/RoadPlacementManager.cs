using UnityEngine;

public class RoadPlacementManager : MonoBehaviour
{
    public static RoadPlacementManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to simulation state changes
        SimulationManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from simulation state changes
        SimulationManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameStateContext newState)
    {
        // Enable road placement features when entering Roads state
        if (newState.SimulationState == SimulationState.Roads)
        {
            EnableRoadPlacement();
        }
        else
        {
            DisableRoadPlacement();
        }
    }

    private void EnableRoadPlacement()
    {
        // Enable visual feedback
        GridVisualiser.Instance?.EnableRoadPlacementVisuals();
    }

    private void DisableRoadPlacement()
    {
        // Disable visual feedback
        GridVisualiser.Instance?.DisableRoadPlacementVisuals();
    }
}