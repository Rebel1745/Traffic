public struct RelationshipType
{
    public string Name; // e.g., "Resident", "ParkedAt", "WorksAt"

    public RelationshipType(string name) => Name = name;

    public static readonly RelationshipType Resident = new("Resident");
    public static readonly RelationshipType Driver = new("Driver");
    public static readonly RelationshipType ParksAt = new("ParksAt");
    // Add new types here without changing the core logic
}