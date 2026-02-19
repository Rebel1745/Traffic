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

    private void OnEnable()
    {
        // Subscribe to simulation state changes
        SimulationManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from simulation state changes
        SimulationManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(SimulationState newState)
    {
        // Enable road placement features when entering PlacingRoads state
        if (newState == SimulationState.PlacingRoads)
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

        // Enable input for road placement
        InputManager.OnLeftClickPressed += HandleLeftClick;
        InputManager.OnMouseMoved += HandleMouseMoved;
    }

    private void DisableRoadPlacement()
    {
        // Disable visual feedback
        GridVisualiser.Instance?.DisableRoadPlacementVisuals();

        // Disable input for road placement
        InputManager.OnLeftClickPressed -= HandleLeftClick;
        InputManager.OnMouseMoved -= HandleMouseMoved;
    }

    private void HandleLeftClick(Vector2 screenPosition)
    {
        // This is now handled by GridVisualizer
        // We're keeping this method for potential future enhancements
    }

    private void HandleMouseMoved(Vector2 screenPosition)
    {
        // This is now handled by GridVisualizer
        // We're keeping this method for potential future enhancements
    }

    // Public methods for other systems to control road placement
    public void StartRoadPlacement()
    {
        SimulationManager.Instance.SetState(SimulationState.PlacingRoads);
    }

    public void StopRoadPlacement()
    {
        SimulationManager.Instance.SetState(SimulationState.Running);
    }

    public bool IsInRoadPlacementMode()
    {
        return SimulationManager.Instance.IsInState(SimulationState.PlacingRoads);
    }
}