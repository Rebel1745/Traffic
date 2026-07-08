public struct RelationshipType
{
    public string Name; // e.g., "Resident", "ParkedAt", "WorksAt"

    public RelationshipType(string name) => Name = name;

    public static readonly RelationshipType Resident = new("Resident"); // person -> building
    public static readonly RelationshipType Driver = new("Driver"); // person -> vehicle
    public static readonly RelationshipType CurrentParkingSpot = new("CurrentParkingSpot"); // vehicle -> current parking spot (waypoint)
    public static readonly RelationshipType HomeBuilding = new("HomeBuilding"); // vehicle -> building
    public static readonly RelationshipType HomeParkingSpot = new("HomeParkingSpot"); // vehicle -> home parking spot (waypoint)
    public static readonly RelationshipType AlightsAt = new("AlightsAt"); // parking spot (waypoint) -> pedestrian vehicle entry / exit (waypoint)
}