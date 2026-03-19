using System.Collections.Generic;
using UnityEngine;

public static class RoadMarkingUVs
{

    private static readonly Vector2[] _straightRoadDefault = new[]
    {
        new Vector2(0.0f, 0f),
        new Vector2(0.25f, 0f),
        new Vector2(0.25f, 1.0f),
        new Vector2(0.0f, 1.0f)
    };

    private static readonly Vector2[] _cornerRoadDefault = new[]
    {
        new Vector2(0.25f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0.5f, 1.0f),
        new Vector2(0.25f, 1.0f)
    };

    private static readonly Vector2[] _blankRoad = new[]
    {
        new Vector2(0.5f, 0.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1f),
        new Vector2(0.5f, 1f)
    };

    // Define UV coordinates for each road type (as fractions of the atlas)
    // Assuming a 2x1 grid layout

    private static readonly Dictionary<(RoadType, RoadDirection), Vector2[]> UVMappings = new()
    {
        { (RoadType.Straight, RoadDirection.WestEast), _straightRoadDefault },
        { (RoadType.Straight, RoadDirection.NorthSouth), new[]
            {
                _straightRoadDefault[1],
                _straightRoadDefault[2],
                _straightRoadDefault[3],
                _straightRoadDefault[0]
            }
        },
        { (RoadType.Corner, RoadDirection.NorthWest), _cornerRoadDefault},
        { (RoadType.Corner, RoadDirection.SouthWest), new[]
            {
                _cornerRoadDefault[1],
                _cornerRoadDefault[2],
                _cornerRoadDefault[3],
                _cornerRoadDefault[0]
            }
        },
        { (RoadType.Corner, RoadDirection.NorthEast), new[]
            {
                _cornerRoadDefault[3],
                _cornerRoadDefault[0],
                _cornerRoadDefault[1],
                _cornerRoadDefault[2]
            }
        },
        { (RoadType.Corner, RoadDirection.SouthEast), new[]
            {
                _cornerRoadDefault[2],
                _cornerRoadDefault[3],
                _cornerRoadDefault[0],
                _cornerRoadDefault[1]
            }
        },
        { (RoadType.Empty, RoadDirection.None), _blankRoad }
    };

    public static Vector2[] GetUVsForRoadType(RoadType roadType, RoadDirection roadDirection)
    {
        Debug.Log(roadType + " - " + roadDirection);
        return UVMappings.TryGetValue((roadType, roadDirection), out var uvs) ? uvs : _blankRoad;
    }
}
