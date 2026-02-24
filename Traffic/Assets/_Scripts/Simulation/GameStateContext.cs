[System.Serializable]
public struct GameStateContext
{
    public SimulationState SimulationState;
    public RoadSubState RoadSubState;
    public VehicleSubState VehicleSubState;
    public TrafficLightSubState TrafficLightSubState;

    // Convenience method to check the full state
    public bool Is(SimulationState main, RoadSubState road) =>
        SimulationState == main && RoadSubState == road;

    public bool Is(SimulationState main, VehicleSubState vehicle) =>
        SimulationState == main && VehicleSubState == vehicle;

    public bool Is(SimulationState main, TrafficLightSubState trafficLight) =>
        SimulationState == main && TrafficLightSubState == trafficLight;
}

public enum SimulationState
{
    None,
    Running,
    Paused,
    Roads,
    Vehicles,
    TrafficLights
}

public enum RoadSubState
{
    None,
    PlaceRoad,
    DeleteRoad,
    EditRoad
}

public enum VehicleSubState
{
    None,
    SpawnVehicle,
    DeleteVehicle
}

public enum TrafficLightSubState
{
    None,
    PlaceTrafficLight,
    DeleteTrafficLight,
    EditTrafficLight
}