using System.Collections.Generic;
using UnityEngine;

public static class RoadMarkingUVs
{
    // Define UV coordinates for each road type (as fractions of the atlas)
    // Assuming a 4x2 grid layout
    // NOTE: The UV's listed are not in a logical order, they have been divised using trial and error
    // This means that there must be some breakdown in logic in my mesh creation code but I just want to get everything working and forget about it

    private static readonly Vector2[] _straightRoadDefault = new[]
    {
        new Vector2(0.0f, 0.5f),
        new Vector2(0.25f, 0.5f),
        new Vector2(0.25f, 1.0f),
        new Vector2(0.0f, 1.0f)
    };

    private static readonly Vector2[] _cornerRoadDefault = new[]
    {
        new Vector2(0.25f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 1.0f),
        new Vector2(0.25f, 1.0f)
    };

    private static readonly Vector2[] _tJunctionDefault = new[]
    {
        new Vector2(0.75f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 1.0f),
        new Vector2(0.75f, 1.0f),
    };

    private static readonly Vector2[] _crossroadDefault = new[]
    {
        new Vector2(1.0f, 0.5f),
        new Vector2(0.75f, 0.5f),
        new Vector2(0.75f, 1.0f),
        new Vector2(1.0f, 1.0f)
    };

    private static readonly Vector2[] _deadEndDefault = new[]
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.25f, 0.0f),
        new Vector2(0.25f, 0.5f),
        new Vector2(0.0f, 0.5f)
    };

    private static readonly Vector2[] _blankRoad = new[]
    {
        new Vector2(0.75f, 0.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 0.5f),
        new Vector2(0.75f, 0.5f)
    };

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
        { (RoadType.TJunction, RoadDirection.West), _tJunctionDefault },
        { (RoadType.TJunction, RoadDirection.North), new[]
            {
                _tJunctionDefault[3],
                _tJunctionDefault[0],
                _tJunctionDefault[1],
                _tJunctionDefault[2],
            } },
        { (RoadType.TJunction, RoadDirection.South), new[]
            {
                _tJunctionDefault[1],
                _tJunctionDefault[2],
                _tJunctionDefault[3],
                _tJunctionDefault[0]
            } },
        { (RoadType.TJunction, RoadDirection.East), new[]
            {
                _tJunctionDefault[2],
                _tJunctionDefault[3],
                _tJunctionDefault[0],
                _tJunctionDefault[1]
            } },
        { (RoadType.Crossroads, RoadDirection.None), _crossroadDefault },
        { (RoadType.DeadEnd, RoadDirection.West), _deadEndDefault },
        { (RoadType.DeadEnd, RoadDirection.East), new[]
            {
                _deadEndDefault[2],
                _deadEndDefault[3],
                _deadEndDefault[0],
                _deadEndDefault[1],
            }
        },
        { (RoadType.DeadEnd, RoadDirection.North), new[]
            {
                _deadEndDefault[3],
                _deadEndDefault[0],
                _deadEndDefault[1],
                _deadEndDefault[2],
            }
        },
        { (RoadType.DeadEnd, RoadDirection.South), new[]
            {
                _deadEndDefault[1],
                _deadEndDefault[2],
                _deadEndDefault[3],
                _deadEndDefault[0]
            }
        },
        { (RoadType.Empty, RoadDirection.None), _blankRoad }
    };

    public static Vector2[] GetUVsForRoadType(RoadType roadType, RoadDirection roadDirection)
    {
        return UVMappings.TryGetValue((roadType, roadDirection), out var uvs) ? uvs : _blankRoad;
    }
}
