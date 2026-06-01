[System.Serializable]
public struct GameStateContext
{
    public SimulationState SimulationState;
    public RoadSubState RoadSubState;
    public VehicleSubState VehicleSubState;
    public TrafficLightSubState TrafficLightSubState;
    public PedestrianSubState PedestrianSubState;
    public BuildingSubState BuildingSubState;
}

public enum SimulationState
{
    None,
    Running,
    Paused,
    Roads,
    Vehicles,
    TrafficLights,
    Pedestrians,
    Buildings
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
    AddJunctionLights,      // T-junctions and crossroads
    AddPedestrianCrossings  // Straight roads only
}

public enum PedestrianSubState
{
    None,
    SpawnPedestrian,
    DeletePedestrian
}

public enum BuildingSubState
{
    None,
    AddBuilding,
    DeleteBuilding
}