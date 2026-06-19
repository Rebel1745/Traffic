using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance { get; private set; }

    // Forward relationships: Source -> Targets (e.g., Building -> Residents)
    private Dictionary<RelationshipType, Dictionary<EntityId, List<EntityId>>> _forwardMaps = new();

    // Reverse relationships: Target -> Sources (e.g., Resident Person -> Home Building)
    private Dictionary<RelationshipType, Dictionary<EntityId, List<EntityId>>> _reverseMaps = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddRelationship(RelationshipType type, EntityId source, EntityId target)
    {
        // --- Update Forward Map (Source -> Target) ---
        if (!_forwardMaps.ContainsKey(type)) _forwardMaps[type] = new Dictionary<EntityId, List<EntityId>>();
        var forwardMap = _forwardMaps[type];

        if (!forwardMap.ContainsKey(source)) forwardMap[source] = new List<EntityId>();
        if (!forwardMap[source].Contains(target)) forwardMap[source].Add(target);

        // --- Update Reverse Map (Target -> Source) ---
        if (!_reverseMaps.ContainsKey(type)) _reverseMaps[type] = new Dictionary<EntityId, List<EntityId>>();
        var reverseMap = _reverseMaps[type];

        if (!reverseMap.ContainsKey(target)) reverseMap[target] = new List<EntityId>();
        if (!reverseMap[target].Contains(source)) reverseMap[target].Add(source);
    }

    public void RemoveRelationship(RelationshipType type, EntityId source, EntityId target)
    {
        // Remove from Forward
        if (_forwardMaps.TryGetValue(type, out var fMap) && fMap.TryGetValue(source, out var fTargets))
        {
            fTargets.Remove(target);
            if (fTargets.Count == 0) fMap.Remove(source);
        }

        // Remove from Reverse
        if (_reverseMaps.TryGetValue(type, out var rMap) && rMap.TryGetValue(target, out var rSources))
        {
            rSources.Remove(source);
            if (rSources.Count == 0) rMap.Remove(target);
        }
    }

    public void RemoveAllRelationships(EntityId source)
    {
        // 1. Remove as a SOURCE (Key) in Forward Maps
        // Example: Removing a Building. It was the key for "Residents".
        foreach (var kvp in _forwardMaps)
        {
            if (kvp.Value.ContainsKey(source))
            {
                kvp.Value.Remove(source);
            }
        }

        // 2. Remove as a TARGET (Value) in Reverse Maps
        // Example: Removing a Person. They were the value in "Home Buildings" lists.
        // We must check every relationship type to see if this source was a target.
        foreach (var kvp in _reverseMaps)
        {
            var map = kvp.Value;

            // We need to find which keys (Sources) have this 'source' in their value list
            // and remove it from those lists.
            // Note: We can't just remove the key because the key is the OTHER entity.

            // To avoid modifying the dictionary while iterating, we collect keys to update first
            var keysToUpdate = new List<EntityId>();

            foreach (var entry in map)
            {
                if (entry.Value.Contains(source))
                {
                    keysToUpdate.Add(entry.Key);
                }
            }

            // Now perform the removals
            foreach (var key in keysToUpdate)
            {
                if (map.TryGetValue(key, out var list))
                {
                    list.Remove(source);
                    // Optional: Clean up empty lists to save memory
                    if (list.Count == 0)
                    {
                        map.Remove(key);
                    }
                }
            }
        }
    }

    public List<EntityId> GetTargets(RelationshipType type, EntityId sourceId, bool reverse = false)
    {
        var map = reverse ? _reverseMaps : _forwardMaps;
        if (map.TryGetValue(type, out var dict) && dict.TryGetValue(sourceId, out var list))
            return new List<EntityId>(list);
        return new List<EntityId>();
    }

    // Forward: Building -> People
    public List<EntityId> GetResidents(EntityId buildingId)
        => GetTargets(RelationshipType.Resident, buildingId, reverse: false);

    // Reverse: Person -> Building
    public List<EntityId> GetHomeBuildings(EntityId personId)
        => GetTargets(RelationshipType.Resident, personId, reverse: true);
}