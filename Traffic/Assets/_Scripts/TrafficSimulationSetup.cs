using UnityEngine;

[RequireComponent(typeof(RoadGraph))]
[RequireComponent(typeof(RoadVisualiser))]
public class TrafficSimulationSetup : MonoBehaviour
{
    [Header("Components")]
    private RoadGraph roadGraph;
    private RoadVisualiser visualiser;

    [Header("Network Type")]
    public NetworkType networkType = NetworkType.SimpleIntersection;

    [Header("Grid Settings (for Grid Network)")]
    public int gridSize = 3;
    public float gridSpacing = 20f;

    public enum NetworkType
    {
        SimpleIntersection,
        GridNetwork
    }

    void Start()
    {
        // Add or get components
        roadGraph = GetComponent<RoadGraph>();

        visualiser = GetComponent<RoadVisualiser>();

        // Build the selected network type
        switch (networkType)
        {
            case NetworkType.SimpleIntersection:
                BuildSimpleNetwork();
                break;
            case NetworkType.GridNetwork:
                BuildGridNetwork();
                break;
        }

        // Visualise the network
        visualiser.VisualiseAll();
    }

    /// <summary>
    /// Build a simple 4-way intersection with manual lane connections
    /// </summary>
    void BuildSimpleNetwork()
    {
        Debug.Log("Building simple 4-way intersection...");

        // Create intersections
        Intersection center = roadGraph.CreateIntersection(Vector3.zero);
        Intersection north = roadGraph.CreateIntersection(new Vector3(0, 0, 20));
        Intersection south = roadGraph.CreateIntersection(new Vector3(0, 0, -20));
        Intersection east = roadGraph.CreateIntersection(new Vector3(20, 0, 0));
        Intersection west = roadGraph.CreateIntersection(new Vector3(-20, 0, 0));

        // Create roads (2 lanes in each direction)
        Road roadNorth = roadGraph.CreateRoad(center, north, 2);
        Road roadSouth = roadGraph.CreateRoad(center, south, 2);
        Road roadEast = roadGraph.CreateRoad(center, east, 2);
        Road roadWest = roadGraph.CreateRoad(center, west, 2);

        // Get INCOMING lanes (vehicles approaching the center intersection)
        Lane northIncoming0 = roadNorth.LanesBtoA[0];  // From north to center
        Lane northIncoming1 = roadNorth.LanesBtoA[1];

        Lane southIncoming0 = roadSouth.LanesBtoA[0];  // From south to center
        Lane southIncoming1 = roadSouth.LanesBtoA[1];

        Lane eastIncoming0 = roadEast.LanesBtoA[0];    // From east to center
        Lane eastIncoming1 = roadEast.LanesBtoA[1];

        Lane westIncoming0 = roadWest.LanesBtoA[0];    // From west to center
        Lane westIncoming1 = roadWest.LanesBtoA[1];

        // Get OUTGOING lanes (vehicles leaving the center intersection)
        Lane northOutgoing0 = roadNorth.LanesAtoB[0];  // From center to north
        Lane northOutgoing1 = roadNorth.LanesAtoB[1];

        Lane southOutgoing0 = roadSouth.LanesAtoB[0];  // From center to south
        Lane southOutgoing1 = roadSouth.LanesAtoB[1];

        Lane eastOutgoing0 = roadEast.LanesAtoB[0];    // From center to east
        Lane eastOutgoing1 = roadEast.LanesAtoB[1];

        Lane westOutgoing0 = roadWest.LanesAtoB[0];    // From center to west
        Lane westOutgoing1 = roadWest.LanesAtoB[1];

        // Add lane connections at center intersection

        // === Vehicles coming FROM NORTH ===
        // Right turn: North → East
        roadGraph.AddLaneConnection(northIncoming0, eastOutgoing0);

        // Straight: North → South
        roadGraph.AddLaneConnection(northIncoming0, southOutgoing0);

        // Left turn: North → West
        roadGraph.AddLaneConnection(northIncoming1, westOutgoing1);

        // Straight: North → South (lane 1)
        roadGraph.AddLaneConnection(northIncoming1, southOutgoing1);

        // === Vehicles coming FROM EAST ===
        // Right turn: East → South
        roadGraph.AddLaneConnection(eastIncoming0, southOutgoing0);

        // Straight: East → West
        roadGraph.AddLaneConnection(eastIncoming0, westOutgoing0);

        // Left turn: East → North
        roadGraph.AddLaneConnection(eastIncoming1, northOutgoing1);

        // Straight: East → West (lane 1)
        roadGraph.AddLaneConnection(eastIncoming1, westOutgoing0);

        // === Vehicles coming FROM SOUTH ===
        // Right turn: South → West
        roadGraph.AddLaneConnection(southIncoming0, westOutgoing0);

        // Straight: South → North
        roadGraph.AddLaneConnection(southIncoming0, northOutgoing0);

        // Left turn: South → East
        roadGraph.AddLaneConnection(southIncoming1, eastOutgoing1);

        // Straight: South → North (lane 1)
        roadGraph.AddLaneConnection(southIncoming1, northOutgoing0);

        // === Vehicles coming FROM WEST ===
        // Right turn: West → North
        roadGraph.AddLaneConnection(westIncoming0, northOutgoing0);

        // Straight: West → East
        roadGraph.AddLaneConnection(westIncoming0, eastOutgoing0);

        // Left turn: West → South
        roadGraph.AddLaneConnection(westIncoming1, southOutgoing1);

        // Straight: West → East (lane 1)
        roadGraph.AddLaneConnection(westIncoming1, eastOutgoing0);

        Debug.Log($"Created {roadGraph.Roads.Count} roads and {roadGraph.Intersections.Count} intersections");
        Debug.Log("Lane connections configured for 4-way intersection");
    }

    /// <summary>
    /// Build a grid network with automatic lane connections
    /// </summary>
    void BuildGridNetwork()
    {
        Debug.Log($"Building {gridSize}x{gridSize} grid network...");

        Intersection[,] grid = new Intersection[gridSize, gridSize];

        // Create intersections in a grid pattern
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 position = new Vector3(x * gridSpacing, 0, z * gridSpacing);
                grid[x, z] = roadGraph.CreateIntersection(position);
            }
        }

        // Create horizontal roads (east-west)
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize - 1; x++)
            {
                roadGraph.CreateRoad(grid[x, z], grid[x + 1, z], 2);
            }
        }

        // Create vertical roads (north-south)
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize - 1; z++)
            {
                roadGraph.CreateRoad(grid[x, z], grid[x, z + 1], 2);
            }
        }

        // Automatically connect lanes at all intersections
        foreach (Intersection intersection in roadGraph.Intersections)
        {
            roadGraph.ConnectAllLanes(intersection);
        }

        Debug.Log($"Created grid with {roadGraph.Roads.Count} roads and {roadGraph.Intersections.Count} intersections");
        Debug.Log("All lane connections configured automatically");
    }

    /// <summary>
    /// Rebuild the network (useful for testing)
    /// </summary>
    [ContextMenu("Rebuild Network")]
    public void RebuildNetwork()
    {
        roadGraph.Clear();
        visualiser.ClearVisualisation();
        Start();
    }
}