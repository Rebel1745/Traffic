using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    private Dictionary<GridCell, List<WaypointNode>> cellWaypoints = new Dictionary<GridCell, List<WaypointNode>>();
    private List<WaypointNode> allWaypoints = new List<WaypointNode>();


    private Vector3 cellCentre;
    private float laneCentre;
    private float halfCellSize;
    private float quarterCellSize;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void GenerateWaypoints(GridCell[,] grid)
    {
        laneCentre = RoadGrid.Instance.GetLaneWidth() / 2f;
        halfCellSize = RoadGrid.Instance.GetCellSize() / 2f;
        quarterCellSize = halfCellSize / 2f;

        // First pass: Create waypoints for each cell
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y].CellType != CellType.Empty)
                {
                    CreateAndConnectWaypoints(grid[x, y]);
                }
            }
        }

        ConnectAllCells();
    }

    private void CreateAndConnectWaypoints(GridCell cell)
    {
        if (cell.CellType == CellType.Empty) return;

        List<WaypointNode> waypoints = new List<WaypointNode>();

        cellCentre = RoadGrid.Instance.GetCellCentre(cell);

        switch (cell.RoadType)
        {
            case RoadType.Straight:
                waypoints = CreateStraightWaypoints(cell);
                break;
            case RoadType.Corner:
                waypoints = CreateCornerWaypoints(cell);
                break;
            case RoadType.TJunction:
                //waypoints = CreateTJunctionWaypoints(cell);
                break;
            case RoadType.Crossroads:
                //waypoints = CreateCrossroadsWaypoints(cell);
                break;
            case RoadType.DeadEnd:
                //waypoints = CreateDeadEndWaypoints(cell);
                break;
        }

        // Store waypoints for this cell
        if (!cellWaypoints.ContainsKey(cell))
        {
            cellWaypoints[cell] = new List<WaypointNode>();
        }
        cellWaypoints[cell].AddRange(waypoints);
        allWaypoints.AddRange(waypoints);

        // Connect waypoints within this cell
        //ConnectWaypointsWithinCell(cell, waypoints);
    }

    private List<WaypointNode> CreateStraightWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        if (hasNorth && hasSouth) // Vertical road
        {
            // Lane going North (traffic flows from South to North)
            Vector3 wpSouthEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpNorthExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);
            WaypointNode southEntry = new WaypointNode(wpSouthEntry, cell, WaypointType.Entry);
            WaypointNode northExit = new WaypointNode(wpNorthExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            southEntry.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(wpSouthEntry, wpNorthExit)));

            waypoints.Add(southEntry);
            waypoints.Add(northExit);

            // Lane going South (traffic flows from North to South)
            Vector3 wpNorthEntry = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpSouthExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);
            WaypointNode northEntry = new WaypointNode(wpNorthEntry, cell, WaypointType.Entry);
            WaypointNode southExit = new WaypointNode(wpSouthExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            northEntry.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(wpNorthEntry, wpSouthExit)));

            waypoints.Add(northEntry);
            waypoints.Add(southExit);

        }
        else if (hasEast && hasWest) // Horizontal road
        {
            // Lane going East (traffic flows from West to East)
            Vector3 wpWestEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpEastExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);
            WaypointNode westEntry = new WaypointNode(wpWestEntry, cell, WaypointType.Entry);
            WaypointNode eastExit = new WaypointNode(wpEastExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            westEntry.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(wpWestEntry, wpEastExit)));

            waypoints.Add(westEntry);
            waypoints.Add(eastExit);

            // Lane going West (traffic flows from East to West)
            Vector3 wpEastEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpWestExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);
            WaypointNode eastEntry = new WaypointNode(wpEastEntry, cell, WaypointType.Entry);
            WaypointNode westExit = new WaypointNode(wpWestExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            eastEntry.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(wpEastEntry, wpWestExit)));

            waypoints.Add(eastEntry);
            waypoints.Add(westExit);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateCornerWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();
        Vector3 cellCentre = RoadGrid.Instance.GetCellCentre(cell);
        float laneCentre = RoadGrid.Instance.GetLaneWidth() / 2f;
        float halfCellSize = RoadGrid.Instance.GetCellSize() / 2f;

        // Determine which directions have roads
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        // Corner cases
        if (hasNorth && hasEast) // Corner from North to East
        {
            // Lane going North to East
            Vector3 wpNorthEntry = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint1 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpEastExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode northEntry = new WaypointNode(wpNorthEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
            WaypointNode eastExit = new WaypointNode(wpEastExit, cell, WaypointType.Exit);

            // connections
            northEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpNorthEntry, wpMidpoint1)));
            midpoint1.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(wpMidpoint1, wpEastExit)));

            // add waypoints
            waypoints.Add(northEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(eastExit);

            // Lane going East to North (reverse direction)
            Vector3 wpEastEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpNorthExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode eastEntry = new WaypointNode(wpEastEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode northExit = new WaypointNode(wpNorthExit, cell, WaypointType.Exit);

            // connections
            eastEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpEastEntry, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(wpMidpoint2, wpNorthExit)));

            // add waypoints
            waypoints.Add(eastEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(northExit);
        }
        else if (hasNorth && hasWest) // Corner from North to West
        {
            // Lane going North to West
            Vector3 wpNorthEntry = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint1 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpWestExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode northEntry = new WaypointNode(wpNorthEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
            WaypointNode westExit = new WaypointNode(wpWestExit, cell, WaypointType.Exit);

            // connections
            northEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpNorthEntry, wpMidpoint1)));
            midpoint1.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(wpMidpoint1, wpWestExit)));

            // add waypoints
            waypoints.Add(northEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(westExit);

            // Lane going West to North (reverse direction)
            Vector3 wpWestEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpNorthExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode westEntry = new WaypointNode(wpWestEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode northExit = new WaypointNode(wpNorthExit, cell, WaypointType.Exit);

            // connections
            westEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpWestEntry, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(wpMidpoint2, wpNorthExit)));

            // add waypoints
            waypoints.Add(westEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(westExit);
        }
        else if (hasSouth && hasEast) // Corner from South to East
        {
            // Lane going South to East
            Vector3 wpSouthEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint1 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpEastExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode southEntry = new WaypointNode(wpSouthEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
            WaypointNode eastExit = new WaypointNode(wpEastExit, cell, WaypointType.Exit);

            // connections
            southEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpSouthEntry, wpMidpoint1)));
            midpoint1.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(wpMidpoint1, wpEastExit)));

            // add waypoints
            waypoints.Add(southEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(eastExit);

            // Lane going East to South (reverse direction)
            Vector3 wpEastEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpSouthExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode eastEntry = new WaypointNode(wpEastEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode southExit = new WaypointNode(wpSouthExit, cell, WaypointType.Exit);

            // connections
            eastEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpEastEntry, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(wpMidpoint1, wpSouthExit)));

            // add waypoints
            waypoints.Add(eastEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(southExit);
        }
        else if (hasSouth && hasWest) // Corner from South to West
        {
            // Lane going South to West
            Vector3 wpSouthEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint1 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpWestExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode southEntry = new WaypointNode(wpSouthEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
            WaypointNode westExit = new WaypointNode(wpWestExit, cell, WaypointType.Exit);

            // connections
            southEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpSouthEntry, wpMidpoint1)));
            midpoint1.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(wpMidpoint1, wpWestExit)));

            // add waypoints
            waypoints.Add(southEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(westExit);

            // Lane going West to South (reverse direction)
            Vector3 wpWestEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpSouthExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode westEntry = new WaypointNode(wpWestEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode southExit = new WaypointNode(wpSouthExit, cell, WaypointType.Exit);

            // connections
            westEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpWestEntry, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(wpMidpoint1, wpSouthExit)));

            // add waypoints
            waypoints.Add(westEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(southExit);
        }

        return waypoints;
    }

    public void ConnectAllCells()
    {
        // After all cells have been created, connect neighboring cells
        foreach (var kvp in cellWaypoints)
        {
            GridCell cell = kvp.Key;
            List<WaypointNode> waypoints = kvp.Value;
            ConnectToNeighboringCells(cell, waypoints);
        }
    }

    // private void ConnectWaypointsWithinCell(GridCell cell, List<WaypointNode> waypoints)
    // {
    //     // Connect waypoints in sequence
    //     for (int i = 0; i < waypoints.Count - 1; i++)
    //     {
    //         waypoints[i].Connections.Add(new WaypointConnection(waypoints[i + 1], Vector3.Distance(waypoints[i].Position, waypoints[i + 1].Position)));
    //     }
    // }

    private void ConnectToNeighboringCells(GridCell cell, List<WaypointNode> waypoints)
    {
        // Check all four directions for neighboring roads
        int[] dx = { 0, 0, -1, 1 };
        int[] dz = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = cell.Position.x + dx[i];
            int nz = cell.Position.z + dz[i];
            if (nx >= 0 && nx < RoadGrid.Instance.GetGridWidth() && nz >= 0 && nz < RoadGrid.Instance.GetGridHeight())
            {
                GridCell neighbor = RoadGrid.Instance.GetGridCell(nx, nz);
                if (neighbor != null && neighbor.CellType == CellType.Road)
                {
                    // Connect waypoints to neighbor cell
                    ConnectWaypointsToNeighbor(cell, waypoints, neighbor, i);
                }
            }
        }
    }

    private void ConnectWaypointsToNeighbor(GridCell cell, List<WaypointNode> waypoints, GridCell neighbor, int direction)
    {
        if (!cellWaypoints.ContainsKey(neighbor))
            return;

        List<WaypointNode> cellExitWaypoints = new List<WaypointNode>();
        List<WaypointNode> neighborEntryWaypoints = new List<WaypointNode>();

        // Get exit waypoints from current cell and entry waypoints from neighbor
        cellExitWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
        neighborEntryWaypoints = cellWaypoints[neighbor].Where(w => w.Type == WaypointType.Entry).ToList();

        // Connect exit waypoints to entry waypoints only if they are at the same position (or very close)
        foreach (var exitWaypoint in cellExitWaypoints)
        {
            foreach (var entryWaypoint in neighborEntryWaypoints)
            {
                // Check if the waypoints are at the same position (or very close)
                float distance = Vector3.Distance(exitWaypoint.Position, entryWaypoint.Position);

                // If the distance is very small (essentially zero), connect them
                if (distance < 0.01f) // Tolerance for floating point precision
                {
                    exitWaypoint.Connections.Add(new WaypointConnection(entryWaypoint, distance));
                }
            }
        }
    }

    public List<WaypointNode> GetAllWaypoints()
    {
        return allWaypoints;
    }

    public void OnDrawGizmos()
    {
        // Draw all waypoints
        // foreach (var waypoint in allWaypoints)
        // {
        //     // Color based on waypoint type
        //     Color color = waypoint.Type switch
        //     {
        //         WaypointType.Entry => Color.green,
        //         WaypointType.Exit => Color.red,
        //         WaypointType.Midpoint => Color.blue,
        //         WaypointType.UTurn => Color.magenta,
        //         _ => Color.white
        //     };

        //     // Draw sphere at waypoint position
        //     Gizmos.color = color;
        //     Gizmos.DrawSphere(waypoint.Position, 0.1f);

        //     // Draw connections to neighboring waypoints
        //     foreach (var connection in waypoint.Connections)
        //     {
        //         Gizmos.color = Color.yellow;
        //         Gizmos.DrawLine(waypoint.Position, connection.TargetWaypoint.Position);
        //     }
        // }
    }
}