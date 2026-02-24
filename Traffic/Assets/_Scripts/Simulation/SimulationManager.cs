using UnityEngine;
using System;

public class SimulationManager : MonoBehaviour
{
    // Singleton
    public static SimulationManager Instance { get; private set; }

    public GameStateContext CurrentState { get; private set; }

    // Events
    public event Action<GameStateContext> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetSimulationState(SimulationState.Running);
    }

    // State management
    public void SetSimulationState(SimulationState state)
    {
        // Reset all sub-states when changing main state
        CurrentState = new GameStateContext
        {
            SimulationState = state,
            RoadSubState = RoadSubState.None,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = TrafficLightSubState.None
        };

        OnStateChanged?.Invoke(CurrentState);
    }

    public void SetRoadSubState(RoadSubState subState)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.Roads,
            RoadSubState = subState,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = TrafficLightSubState.None
        };

        OnStateChanged?.Invoke(CurrentState);
    }

    public void SetVehicleSubState(VehicleSubState subState)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.Vehicles,
            RoadSubState = RoadSubState.None,
            VehicleSubState = subState,
            TrafficLightSubState = TrafficLightSubState.None
        };

        OnStateChanged?.Invoke(CurrentState);
    }

    public void SetTrafficLightSubState(TrafficLightSubState subState)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.TrafficLights,
            RoadSubState = RoadSubState.None,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = subState
        };

        OnStateChanged?.Invoke(CurrentState);
    }
}