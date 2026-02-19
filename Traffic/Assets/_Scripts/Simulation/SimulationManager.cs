using UnityEngine;
using System;

public class SimulationManager : MonoBehaviour
{
    // Singleton
    public static SimulationManager Instance { get; private set; }

    // Events
    public static event Action<SimulationState> OnStateChanged;
    public static event Action<float> OnSimulationSpeedChanged;

    // State
    public SimulationState CurrentState { get; private set; } = SimulationState.PlacingRoads;

    // Speed
    [SerializeField, Range(0f, 5f)] private float simulationSpeed = 1f;
    public float SimulationSpeed => simulationSpeed;

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
        SetSimulationSpeed(simulationSpeed);
    }

    // State management
    public void SetState(SimulationState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);

        // Pause Unity's time when simulation is paused
        Time.timeScale = (CurrentState == SimulationState.Paused) ? 0f : simulationSpeed;

        Debug.Log($"Simulation state changed to: {CurrentState}");
    }

    public bool IsInState(SimulationState state) => CurrentState == state;

    // Speed management
    public void SetSimulationSpeed(float speed)
    {
        simulationSpeed = Mathf.Clamp(speed, 0f, 5f);

        // Don't change timescale if paused
        if (CurrentState != SimulationState.Paused)
            Time.timeScale = simulationSpeed;

        OnSimulationSpeedChanged?.Invoke(simulationSpeed);
    }

    public void TogglePause()
    {
        if (CurrentState == SimulationState.Paused)
            SetState(SimulationState.Running);
        else
            SetState(SimulationState.Paused);
    }

    // Convenience methods for placement modes
    public void StartPlacingRoads() => SetState(SimulationState.PlacingRoads);
    public void StartPlacingTrafficLights() => SetState(SimulationState.PlacingTrafficLights);
    public void StartPlacingBuildings() => SetState(SimulationState.PlacingBuildings);
    public void ReturnToRunning() => SetState(SimulationState.Running);
}

public enum SimulationState
{
    Empty,
    Running,
    Paused,
    PlacingRoads,
    PlacingTrafficLights,
    PlacingBuildings
}