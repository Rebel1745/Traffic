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
        ConnectWaypointsWithinCell(cell, waypoints);
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
            // Lane going North
            Vector3 wpSouthEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpNorthExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            // Lane going South
            Vector3 wpNorthEntry = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpSouthExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            // Create waypoints
            waypoints.Add(new WaypointNode(wpSouthEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpNorthExit, cell, WaypointType.Exit));
            waypoints.Add(new WaypointNode(wpNorthEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpSouthExit, cell, WaypointType.Exit));
        }
        else if (hasEast && hasWest) // Horizontal road
        {
            // Lane going East
            Vector3 wpWestEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpEastExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            // Lane going West
            Vector3 wpEastEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpWestExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            // Create waypoints
            waypoints.Add(new WaypointNode(wpWestEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpEastExit, cell, WaypointType.Exit));
            waypoints.Add(new WaypointNode(wpEastEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpWestExit, cell, WaypointType.Exit));
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
            Vector3 wpNorthEntry = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpEastExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);
            waypoints.Add(new WaypointNode(wpNorthEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpEastExit, cell, WaypointType.Exit));

            // Lane going East to North (reverse direction)
            Vector3 wpEastEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpNorthExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);
            waypoints.Add(new WaypointNode(wpEastEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpNorthExit, cell, WaypointType.Exit));
        }
        else if (hasNorth && hasWest) // Corner from North to West
        {
            // Lane going North to West
            Vector3 wpNorthEntry = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpWestExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);
            waypoints.Add(new WaypointNode(wpNorthEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpWestExit, cell, WaypointType.Exit));

            // Lane going West to North (reverse direction)
            Vector3 wpWestEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpNorthExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);
            waypoints.Add(new WaypointNode(wpWestEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpNorthExit, cell, WaypointType.Exit));
        }
        else if (hasSouth && hasEast) // Corner from South to East
        {
            // Lane going South to East
            Vector3 wpSouthEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpEastExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);
            waypoints.Add(new WaypointNode(wpSouthEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpEastExit, cell, WaypointType.Exit));

            // Lane going East to South (reverse direction)
            Vector3 wpEastEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpSouthExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);
            waypoints.Add(new WaypointNode(wpEastEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpSouthExit, cell, WaypointType.Exit));
        }
        else if (hasSouth && hasWest) // Corner from South to West
        {
            // Lane going South to West
            Vector3 wpSouthEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpWestExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);
            waypoints.Add(new WaypointNode(wpSouthEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpWestExit, cell, WaypointType.Exit));

            // Lane going West to South (reverse direction)
            Vector3 wpWestEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpSouthExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);
            waypoints.Add(new WaypointNode(wpWestEntry, cell, WaypointType.Entry));
            waypoints.Add(new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint));
            waypoints.Add(new WaypointNode(wpSouthExit, cell, WaypointType.Exit));
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

    private void ConnectWaypointsWithinCell(GridCell cell, List<WaypointNode> waypoints)
    {
        // Sort waypoints by position to ensure correct order
        waypoints.Sort((a, b) => Vector3.Distance(a.Position, Vector3.zero).CompareTo(Vector3.Distance(b.Position, Vector3.zero)));

        // Connect waypoints in sequence
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            waypoints[i].Connections.Add(new WaypointConnection(waypoints[i + 1], Vector3.Distance(waypoints[i].Position, waypoints[i + 1].Position)));
        }
    }

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
        // Find entry and exit points based on direction
        List<WaypointNode> entryWaypoints = new List<WaypointNode>();
        List<WaypointNode> exitWaypoints = new List<WaypointNode>();

        // Determine which waypoints are entry and exit points based on direction
        if (direction == 0) // North
        {
            entryWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
            exitWaypoints = cellWaypoints[neighbor].Where(w => w.Type == WaypointType.Entry).ToList();
        }
        else if (direction == 1) // South
        {
            entryWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
            exitWaypoints = cellWaypoints[neighbor].Where(w => w.Type == WaypointType.Entry).ToList();
        }
        else if (direction == 2) // West
        {
            entryWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
            exitWaypoints = cellWaypoints[neighbor].Where(w => w.Type == WaypointType.Entry).ToList();
        }
        else if (direction == 3) // East
        {
            entryWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
            exitWaypoints = cellWaypoints[neighbor].Where(w => w.Type == WaypointType.Entry).ToList();
        }

        // Connect entry waypoints to exit waypoints
        foreach (var entryWaypoint in entryWaypoints)
        {
            foreach (var exitWaypoint in exitWaypoints)
            {
                // Add connection from entry to exit
                entryWaypoint.Connections.Add(new WaypointConnection(exitWaypoint, Vector3.Distance(entryWaypoint.Position, exitWaypoint.Position)));
            }
        }
    }

    public void OnDrawGizmos()
    {
        // Draw all waypoints
        foreach (var waypoint in allWaypoints)
        {
            // Color based on waypoint type
            Color color = waypoint.Type switch
            {
                WaypointType.Entry => Color.green,
                WaypointType.Exit => Color.red,
                WaypointType.Midpoint => Color.blue,
                WaypointType.UTurn => Color.magenta,
                _ => Color.white
            };

            // Draw sphere at waypoint position
            Gizmos.color = color;
            Gizmos.DrawSphere(waypoint.Position, 0.1f);

            // Draw connections to neighboring waypoints
            foreach (var connection in waypoint.Connections)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoint.Position, connection.TargetWaypoint.Position);
            }
        }
    }
}