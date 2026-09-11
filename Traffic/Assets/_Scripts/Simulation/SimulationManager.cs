using UnityEngine;
using System;

public class SimulationManager : MonoBehaviour
{
    // Singleton
    public static SimulationManager Instance { get; private set; }

    public GameStateContext CurrentState { get; private set; }

    private float _simulationSpeed;

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

    public void SetSimulationSpeed(float speed)
    {
        _simulationSpeed = speed;

        Time.timeScale = speed;
    }

    // State management
    public void SetState(GameStateContext newState)
    {
        CurrentState = newState;
    }

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

    public void SetRoadSubState(RoadSubState subState, bool triggerStateChange = true)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.Roads,
            RoadSubState = subState,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = TrafficLightSubState.None
        };

        if (triggerStateChange)
            OnStateChanged?.Invoke(CurrentState);
    }

    public void SetVehicleSubState(VehicleSubState subState, bool triggerStateChange = true)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.Vehicles,
            RoadSubState = RoadSubState.None,
            VehicleSubState = subState,
            TrafficLightSubState = TrafficLightSubState.None,
            PedestrianSubState = PedestrianSubState.None
        };

        if (triggerStateChange)
            OnStateChanged?.Invoke(CurrentState);
    }

    public void SetTrafficLightSubState(TrafficLightSubState subState, bool triggerStateChange = true)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.TrafficLights,
            RoadSubState = RoadSubState.None,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = subState,
            PedestrianSubState = PedestrianSubState.None
        };

        if (triggerStateChange)
            OnStateChanged?.Invoke(CurrentState);
    }

    public void SetPedestrianSubState(PedestrianSubState subState, bool triggerStateChange = true)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.Pedestrians,
            RoadSubState = RoadSubState.None,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = TrafficLightSubState.None,
            PedestrianSubState = subState
        };

        if (triggerStateChange)
            OnStateChanged?.Invoke(CurrentState);
    }

    public void SetBuildingSubState(BuildingSubState subState, bool triggerStateChange = true)
    {
        CurrentState = new GameStateContext
        {
            SimulationState = SimulationState.Buildings,
            RoadSubState = RoadSubState.None,
            VehicleSubState = VehicleSubState.None,
            TrafficLightSubState = TrafficLightSubState.None,
            PedestrianSubState = PedestrianSubState.None,
            BuildingSubState = subState
        };

        if (triggerStateChange)
            OnStateChanged?.Invoke(CurrentState);
    }
}