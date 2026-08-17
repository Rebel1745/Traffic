using UnityEngine;
using System.Collections.Generic;

public struct RelationshipType
{
    public string Name; // e.g., "Resident", "ParkedAt", "WorksAt"
    public static readonly RelationshipType None = new RelationshipType("");

    private static readonly Dictionary<string, RelationshipType> _registry = new();

    public RelationshipType(string name)
    {
        Name = name;
        // Register itself immediately upon creation
        if (!string.IsNullOrEmpty(name))
        {
            // Check if already registered to avoid Overwrite errors if static fields are reloaded
            if (!_registry.ContainsKey(name))
            {
                _registry[name] = this;
            }
        }
    }

    public static RelationshipType FromName(string name)
    {
        if (_registry.TryGetValue(name, out var type))
            return type;

        Debug.LogWarning($"Unknown relationship type: '{name}'");
        return None;
    }

    // Standard Equals/GetHashCode
    public override bool Equals(object obj) => obj is RelationshipType other && Name == other.Name;
    public override int GetHashCode() => Name?.GetHashCode() ?? 0;

    public static readonly RelationshipType Resident = new("Resident"); // person -> building
    public static readonly RelationshipType Driver = new("Driver"); // person -> vehicle
    //public static readonly RelationshipType CurrentParkingSpot = new("CurrentParkingSpot"); // vehicle -> current parking spot (waypoint) (do we need this as the current parking spot should just be the CurrentWaypoint of the vehicle movement class)
    public static readonly RelationshipType HomeBuilding = new("HomeBuilding"); // vehicle -> building
    public static readonly RelationshipType HomeParkingSpot = new("HomeParkingSpot"); // vehicle -> home parking spot (waypoint)
    public static readonly RelationshipType BuildingParkingSpot = new("BuildingParkingSpot"); // building -> parking spot (waypoint) (the parking spots for the building)
    public static readonly RelationshipType AlightsAt = new("AlightsAt"); // parking spot (waypoint) -> pedestrian vehicle entry / exit (waypoint)
}