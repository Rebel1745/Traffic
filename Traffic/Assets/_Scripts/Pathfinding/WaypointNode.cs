using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaypointNode
{
    public string Id { get; set; }
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

    public WaypointNode(Vector3 position, GridCell parentCell, WaypointType type, WaypointNetworkType networkType = WaypointNetworkType.Vehicle, WaypointNode laneNode = null, RoadDirection lightPos = RoadDirection.None)
    {
        Id = System.Guid.NewGuid().ToString();
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
    PropertyEntryExit, // the entry/exit point of a property (maybe not needed)
    VehicleParking,
    VehicleEntryExit // where a person gets in/out a vehicle in a garage
}

public enum WaypointNetworkType
{
    Vehicle,
    Pedestrian
}