using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance { get; private set; }

    // The Core Data Structure
    // Key: Relationship Type (e.g., "Resident")
    // Value: Dictionary of SourceID -> List of TargetIDs (Many-to-Many support)
    private Dictionary<RelationshipType, Dictionary<EntityId, List<EntityId>>> _relationships = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Add a relationship. Automatically creates lists if they don't exist.
    /// </summary>
    public void AddRelationship(RelationshipType type, EntityId source, EntityId target)
    {
        if (!_relationships.ContainsKey(type))
            _relationships[type] = new Dictionary<EntityId, List<EntityId>>();

        var map = _relationships[type];

        if (!map.ContainsKey(source))
            map[source] = new List<EntityId>();

        // Prevent duplicates
        if (!map[source].Contains(target))
            map[source].Add(target);
    }

    /// <summary>
    /// Remove a specific relationship.
    /// </summary>
    public void RemoveRelationship(RelationshipType type, EntityId source, EntityId target)
    {
        if (!_relationships.TryGetValue(type, out var map) || !map.TryGetValue(source, out var targets))
            return;

        targets.Remove(target);

        // Cleanup empty lists to save memory
        if (targets.Count == 0)
            map.Remove(source);
    }

    /// <summary>
    /// Remove ALL relationships for a source entity (Critical for cleanup when objects are destroyed).
    /// </summary>
    public void RemoveAllRelationships(EntityId source)
    {
        foreach (var kvp in _relationships)
        {
            if (kvp.Value.ContainsKey(source))
                kvp.Value.Remove(source);
        }
    }

    /// <summary>
    /// Query: Get all targets for a source (e.g., Who lives in this building?)
    /// </summary>
    public List<EntityId> GetTargets(RelationshipType type, EntityId source)
    {
        if (_relationships.TryGetValue(type, out var map) && map.TryGetValue(source, out var targets))
            return new List<EntityId>(targets); // Return copy to prevent modification
        return new List<EntityId>();
    }

    /// <summary>
    /// Query: Get all sources that point to a target (e.g., Which cars are parked at this building?)
    /// Note: This is slower O(N) unless you add a reverse index.
    /// </summary>
    public List<EntityId> GetSources(RelationshipType type, EntityId target)
    {
        var results = new List<EntityId>();
        if (_relationships.TryGetValue(type, out var map))
        {
            foreach (var kvp in map)
            {
                if (kvp.Value.Contains(target))
                    results.Add(kvp.Key);
            }
        }
        return results;
    }
}