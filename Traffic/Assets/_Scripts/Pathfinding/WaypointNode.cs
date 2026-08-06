using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaypointNode
{
    public EntityId Id { get; set; }
    public Vector3 Position { get; set; }
    public List<WaypointConnection> Connections { get; set; }
    public GridCell ParentCell { get; set; }
    public WaypointType Type { get; set; }
    public WaypointNetworkType NetworkType { get; set; }
    public TrafficLightController AssignedLight { get; set; }
    public WaypointNode PairedCrossingWaypoint { get; set; }
    public string PairedCrossingWaypointId { get; set; }
    // below is the node that the vehicle will stop at if it has a traffic light
    public WaypointNode LaneNodeForTrafficLight { get; set; }
    public string LaneNodeForTrafficLightId { get; set; }
    public RoadDirection LightPosition { get; set; } // the cardinal position of the light e.g. top left of a junction would be NorthWest
    public bool PedestiranOnlyTrafficLight { get; set; } // the light is not for road users
    public WaypointNode PairedParkingSpotWaypoint { get; set; }
    public EntityId PairedParkingSpotWaypointId { get; set; }

    public WaypointNode(Vector3 position, GridCell parentCell, WaypointType type, WaypointNetworkType networkType = WaypointNetworkType.Vehicle, WaypointNode laneNode = null, RoadDirection lightPos = RoadDirection.None)
    {
        Id = EntityId.New();
        Position = position;
        ParentCell = parentCell;
        Type = type;
        NetworkType = networkType;
        Connections = new List<WaypointConnection>();
        AssignedLight = null;
        PairedCrossingWaypoint = null;
        LaneNodeForTrafficLight = laneNode;
        LightPosition = lightPos;
    }
}

public enum WaypointType
{
    None,
    Entry,
    Exit,
    Midpoint,
    UTurn,
    TrafficLightLocation,
    PedestrianWalkway,
    PedestrianRoadCrossing,
    InsideBuilding,
    BuildingDoor,
    PropertyWalkway,
    PropertyDriveway, // the waypoints that lead a vehicle to its parking space on a property
    PropertyEntryExit, // the combined entry/exit point of a property
    PropertyEntry, // the entry point of a property
    PropertyExit, // the exit point of a property
    VehicleParking,
    VehicleEntryExit, // where a person gets in/out a vehicle in a garage
    VehiclePropertyEntryExit, // if the entry and exit point is the same
    VehiclePropertyEntry,
    VehiclePropertyExit,
    PetrolStationPump
}

public enum WaypointNetworkType
{
    Vehicle,
    Pedestrian
}