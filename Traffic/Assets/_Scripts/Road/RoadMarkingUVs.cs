using System.Collections.Generic;
using UnityEngine;

public static class RoadMarkingUVs
{
    // Define UV coordinates for each road type (as fractions of the atlas)
    // Assuming a 2x1 grid layout

    private static readonly Dictionary<RoadType, Vector2[]> UVMappings = new()
    {
        { RoadType.Straight, new[] {
            new Vector2(0.0f, 0f),   // tbl
            new Vector2(0.25f, 0f),  // tbr
            new Vector2(0.25f, 1.0f),// ttr
            new Vector2(0.0f, 1.0f)  // ttl
        }},
        { RoadType.Corner, new[] {
            new Vector2(0.25f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 1.0f),
            new Vector2(0.25f, 1.0f)
        }},
        { RoadType.Empty, new[] {
            new Vector2(0.5f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1f),
            new Vector2(0.5f, 1f)
        }}
    };

    public static Vector2[] GetUVsForRoadType(RoadType roadType)
    {
        return UVMappings.TryGetValue(roadType, out var uvs) ? uvs : UVMappings[RoadType.Empty];
    }
}
