using System;
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to road grid updates
        RoadMeshRenderer.OnRoadMeshUpdated += RoadMeshUpdated;
    }

    private void RoadMeshUpdated()
    {
        GenerateWaypoints();
    }

    public void GenerateWaypoints()
    {
        GridCell[,] grid = GridManager.Instance.GetGrid();

        cellWaypoints.Clear();
        allWaypoints.Clear();

        laneCentre = RoadMeshRenderer.Instance.GetLaneWidth() / 2f;
        halfCellSize = GridManager.Instance.CellSize / 2f;
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

        cellCentre = GridManager.Instance.GetCellCentre(cell);

        switch (cell.RoadType)
        {
            case RoadType.Straight:
                waypoints = CreateStraightWaypoints(cell);
                break;
            case RoadType.Corner:
                waypoints = CreateCornerWaypoints(cell);
                break;
            case RoadType.TJunction:
                waypoints = CreateTJunctionWaypoints(cell);
                break;
            case RoadType.Crossroads:
                waypoints = CreateCrossroadsWaypoints(cell);
                break;
            case RoadType.DeadEnd:
                waypoints = CreateDeadEndWaypoints(cell);
                break;
        }

        // Store waypoints for this cell
        if (!cellWaypoints.ContainsKey(cell))
        {
            cellWaypoints[cell] = new List<WaypointNode>();
        }
        cellWaypoints[cell].AddRange(waypoints);
        allWaypoints.AddRange(waypoints);
    }

    private List<WaypointNode> CreateStraightWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        bool hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);

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
        Vector3 cellCentre = GridManager.Instance.GetCellCentre(cell);

        // Determine which directions have roads
        bool hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);

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

    private List<WaypointNode> CreateTJunctionWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // Determine which directions have roads
        bool hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        // T-Junction with North, East, and West (missing South)
        if (hasNorth && hasEast && hasWest && !hasSouth)
        {
            // Lane 1: North to East (left turn)
            Vector3 wpNorthEntry1 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint1 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpEastExit1 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode northEntry1 = new WaypointNode(wpNorthEntry1, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
            WaypointNode eastExit1 = new WaypointNode(wpEastExit1, cell, WaypointType.Exit);

            northEntry1.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpNorthEntry1, wpMidpoint1)));
            midpoint1.Connections.Add(new WaypointConnection(eastExit1, Vector3.Distance(wpMidpoint1, wpEastExit1)));

            waypoints.Add(northEntry1);
            waypoints.Add(midpoint1);
            waypoints.Add(eastExit1);

            // Lane 2: North to West (right turn)
            Vector3 wpNorthEntry2 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpWestExit2 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode northEntry2 = new WaypointNode(wpNorthEntry2, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode westExit2 = new WaypointNode(wpWestExit2, cell, WaypointType.Exit);

            northEntry2.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpNorthEntry2, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(westExit2, Vector3.Distance(wpMidpoint2, wpWestExit2)));

            waypoints.Add(northEntry2);
            waypoints.Add(midpoint2);
            waypoints.Add(westExit2);

            // Lane 3: East to North (right turn)
            Vector3 wpEastEntry3 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint3 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpNorthExit3 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode eastEntry3 = new WaypointNode(wpEastEntry3, cell, WaypointType.Entry);
            WaypointNode midpoint3 = new WaypointNode(wpMidpoint3, cell, WaypointType.Midpoint);
            WaypointNode northExit3 = new WaypointNode(wpNorthExit3, cell, WaypointType.Exit);

            eastEntry3.Connections.Add(new WaypointConnection(midpoint3, Vector3.Distance(wpEastEntry3, wpMidpoint3)));
            midpoint3.Connections.Add(new WaypointConnection(northExit3, Vector3.Distance(wpMidpoint3, wpNorthExit3)));

            waypoints.Add(eastEntry3);
            waypoints.Add(midpoint3);
            waypoints.Add(northExit3);

            // Lane 4: East to West (straight through)
            Vector3 wpEastEntry4 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpWestExit4 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode eastEntry4 = new WaypointNode(wpEastEntry4, cell, WaypointType.Entry);
            WaypointNode westExit4 = new WaypointNode(wpWestExit4, cell, WaypointType.Exit);

            eastEntry4.Connections.Add(new WaypointConnection(westExit4, Vector3.Distance(wpEastEntry4, wpWestExit4)));

            waypoints.Add(eastEntry4);
            waypoints.Add(westExit4);

            // Lane 5: West to North (left turn)
            Vector3 wpWestEntry5 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint5 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpNorthExit5 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode westEntry5 = new WaypointNode(wpWestEntry5, cell, WaypointType.Entry);
            WaypointNode midpoint5 = new WaypointNode(wpMidpoint5, cell, WaypointType.Midpoint);
            WaypointNode northExit5 = new WaypointNode(wpNorthExit5, cell, WaypointType.Exit);

            westEntry5.Connections.Add(new WaypointConnection(midpoint5, Vector3.Distance(wpWestEntry5, wpMidpoint5)));
            midpoint5.Connections.Add(new WaypointConnection(northExit5, Vector3.Distance(wpMidpoint5, wpNorthExit5)));

            waypoints.Add(westEntry5);
            waypoints.Add(midpoint5);
            waypoints.Add(northExit5);

            // Lane 6: West to East (straight through)
            Vector3 wpWestEntry6 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpEastExit6 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode westEntry6 = new WaypointNode(wpWestEntry6, cell, WaypointType.Entry);
            WaypointNode eastExit6 = new WaypointNode(wpEastExit6, cell, WaypointType.Exit);

            westEntry6.Connections.Add(new WaypointConnection(eastExit6, Vector3.Distance(wpWestEntry6, wpEastExit6)));

            waypoints.Add(westEntry6);
            waypoints.Add(eastExit6);
        }
        // T-Junction with North, East, and South (missing West)
        else if (hasNorth && hasEast && hasSouth && !hasWest)
        {
            // Lane 1: North to East (left turn)
            Vector3 wpNorthEntry1 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint1 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpEastExit1 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode northEntry1 = new WaypointNode(wpNorthEntry1, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
            WaypointNode eastExit1 = new WaypointNode(wpEastExit1, cell, WaypointType.Exit);

            northEntry1.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpNorthEntry1, wpMidpoint1)));
            midpoint1.Connections.Add(new WaypointConnection(eastExit1, Vector3.Distance(wpMidpoint1, wpEastExit1)));

            waypoints.Add(northEntry1);
            waypoints.Add(midpoint1);
            waypoints.Add(eastExit1);

            // Lane 2: North to South (straight through)
            Vector3 wpNorthEntry2 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpSouthExit2 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode northEntry2 = new WaypointNode(wpNorthEntry2, cell, WaypointType.Entry);
            WaypointNode southExit2 = new WaypointNode(wpSouthExit2, cell, WaypointType.Exit);

            northEntry2.Connections.Add(new WaypointConnection(southExit2, Vector3.Distance(wpNorthEntry2, wpSouthExit2)));

            waypoints.Add(northEntry2);
            waypoints.Add(southExit2);

            // Lane 3: East to South (left turn)
            Vector3 wpEastEntry3 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint3 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpSouthExit3 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode eastEntry3 = new WaypointNode(wpEastEntry3, cell, WaypointType.Entry);
            WaypointNode midpoint3 = new WaypointNode(wpMidpoint3, cell, WaypointType.Midpoint);
            WaypointNode southExit3 = new WaypointNode(wpSouthExit3, cell, WaypointType.Exit);

            eastEntry3.Connections.Add(new WaypointConnection(midpoint3, Vector3.Distance(wpEastEntry3, wpMidpoint3)));
            midpoint3.Connections.Add(new WaypointConnection(southExit3, Vector3.Distance(wpMidpoint3, wpSouthExit3)));

            waypoints.Add(eastEntry3);
            waypoints.Add(midpoint3);
            waypoints.Add(southExit3);

            // Lane 4: East to North (right turn)
            Vector3 wpEastEntry4 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint4 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpNorthExit4 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode eastEntry4 = new WaypointNode(wpEastEntry4, cell, WaypointType.Entry);
            WaypointNode midpoint4 = new WaypointNode(wpMidpoint4, cell, WaypointType.Midpoint);
            WaypointNode northExit4 = new WaypointNode(wpNorthExit4, cell, WaypointType.Exit);

            eastEntry4.Connections.Add(new WaypointConnection(midpoint4, Vector3.Distance(wpEastEntry4, wpMidpoint4)));
            midpoint4.Connections.Add(new WaypointConnection(northExit4, Vector3.Distance(wpMidpoint4, wpNorthExit4)));

            waypoints.Add(eastEntry4);
            waypoints.Add(midpoint4);
            waypoints.Add(northExit4);

            // Lane 5: South to North (straight through)
            Vector3 wpSouthEntry5 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpNorthExit5 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode southEntry5 = new WaypointNode(wpSouthEntry5, cell, WaypointType.Entry);
            WaypointNode northExit5 = new WaypointNode(wpNorthExit5, cell, WaypointType.Exit);

            southEntry5.Connections.Add(new WaypointConnection(northExit5, Vector3.Distance(wpSouthEntry5, wpNorthExit5)));

            waypoints.Add(southEntry5);
            waypoints.Add(northExit5);

            // Lane 6: South to East (right turn)
            Vector3 wpSouthEntry6 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint6 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpEastExit6 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode southEntry6 = new WaypointNode(wpSouthEntry6, cell, WaypointType.Entry);
            WaypointNode midpoint6 = new WaypointNode(wpMidpoint6, cell, WaypointType.Midpoint);
            WaypointNode eastExit6 = new WaypointNode(wpEastExit6, cell, WaypointType.Exit);

            southEntry6.Connections.Add(new WaypointConnection(midpoint6, Vector3.Distance(wpSouthEntry6, wpMidpoint6)));
            midpoint6.Connections.Add(new WaypointConnection(eastExit6, Vector3.Distance(wpMidpoint6, wpEastExit6)));

            waypoints.Add(southEntry6);
            waypoints.Add(midpoint6);
            waypoints.Add(eastExit6);
        }
        // T-Junction with North, South, and West (missing East)
        else if (hasNorth && hasSouth && hasWest && !hasEast)
        {
            // Lane 1: North to South (straight through)
            Vector3 wpNorthEntry1 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpSouthExit1 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode northEntry1 = new WaypointNode(wpNorthEntry1, cell, WaypointType.Entry);
            WaypointNode southExit1 = new WaypointNode(wpSouthExit1, cell, WaypointType.Exit);

            northEntry1.Connections.Add(new WaypointConnection(southExit1, Vector3.Distance(wpNorthEntry1, wpSouthExit1)));

            waypoints.Add(northEntry1);
            waypoints.Add(southExit1);

            // Lane 2: North to West (right turn)
            Vector3 wpNorthEntry2 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpWestExit2 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode northEntry2 = new WaypointNode(wpNorthEntry2, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode westExit2 = new WaypointNode(wpWestExit2, cell, WaypointType.Exit);

            northEntry2.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpNorthEntry2, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(westExit2, Vector3.Distance(wpMidpoint2, wpWestExit2)));

            waypoints.Add(northEntry2);
            waypoints.Add(midpoint2);
            waypoints.Add(westExit2);

            // Lane 3: South to West (left turn)
            Vector3 wpSouthEntry3 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint3 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpWestExit3 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode southEntry3 = new WaypointNode(wpSouthEntry3, cell, WaypointType.Entry);
            WaypointNode midpoint3 = new WaypointNode(wpMidpoint3, cell, WaypointType.Midpoint);
            WaypointNode westExit3 = new WaypointNode(wpWestExit3, cell, WaypointType.Exit);

            southEntry3.Connections.Add(new WaypointConnection(midpoint3, Vector3.Distance(wpSouthEntry3, wpMidpoint3)));
            midpoint3.Connections.Add(new WaypointConnection(westExit3, Vector3.Distance(wpMidpoint3, wpWestExit3)));

            waypoints.Add(southEntry3);
            waypoints.Add(midpoint3);
            waypoints.Add(westExit3);

            // Lane 4: South to North (straight through)
            Vector3 wpSouthEntry4 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpNorthExit4 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode southEntry4 = new WaypointNode(wpSouthEntry4, cell, WaypointType.Entry);
            WaypointNode northExit4 = new WaypointNode(wpNorthExit4, cell, WaypointType.Exit);

            southEntry4.Connections.Add(new WaypointConnection(northExit4, Vector3.Distance(wpSouthEntry4, wpNorthExit4)));

            waypoints.Add(southEntry4);
            waypoints.Add(northExit4);

            // Lane 5: West to North (left turn)
            Vector3 wpWestEntry5 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint5 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpNorthExit5 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode westEntry5 = new WaypointNode(wpWestEntry5, cell, WaypointType.Entry);
            WaypointNode midpoint5 = new WaypointNode(wpMidpoint5, cell, WaypointType.Midpoint);
            WaypointNode northExit5 = new WaypointNode(wpNorthExit5, cell, WaypointType.Exit);

            westEntry5.Connections.Add(new WaypointConnection(midpoint5, Vector3.Distance(wpWestEntry5, wpMidpoint5)));
            midpoint5.Connections.Add(new WaypointConnection(northExit5, Vector3.Distance(wpMidpoint5, wpNorthExit5)));

            waypoints.Add(westEntry5);
            waypoints.Add(midpoint5);
            waypoints.Add(northExit5);

            // Lane 6: West to South (right turn)
            Vector3 wpWestEntry6 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint6 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpSouthExit6 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode westEntry6 = new WaypointNode(wpWestEntry6, cell, WaypointType.Entry);
            WaypointNode midpoint6 = new WaypointNode(wpMidpoint6, cell, WaypointType.Midpoint);
            WaypointNode southExit6 = new WaypointNode(wpSouthExit6, cell, WaypointType.Exit);

            westEntry6.Connections.Add(new WaypointConnection(midpoint6, Vector3.Distance(wpWestEntry6, wpMidpoint6)));
            midpoint6.Connections.Add(new WaypointConnection(southExit6, Vector3.Distance(wpMidpoint6, wpSouthExit6)));

            waypoints.Add(westEntry6);
            waypoints.Add(midpoint6);
            waypoints.Add(southExit6);
        }
        // T-Junction with East, South, and West (missing North)
        else if (hasEast && hasSouth && hasWest && !hasNorth)
        {
            // Lane 1: East to West (straight through)
            Vector3 wpEastEntry1 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpWestExit1 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode eastEntry1 = new WaypointNode(wpEastEntry1, cell, WaypointType.Entry);
            WaypointNode westExit1 = new WaypointNode(wpWestExit1, cell, WaypointType.Exit);

            eastEntry1.Connections.Add(new WaypointConnection(westExit1, Vector3.Distance(wpEastEntry1, wpWestExit1)));

            waypoints.Add(eastEntry1);
            waypoints.Add(westExit1);

            // Lane 2: East to South (right turn)
            Vector3 wpEastEntry2 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpSouthExit2 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode eastEntry2 = new WaypointNode(wpEastEntry2, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
            WaypointNode southExit2 = new WaypointNode(wpSouthExit2, cell, WaypointType.Exit);

            eastEntry2.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpEastEntry2, wpMidpoint2)));
            midpoint2.Connections.Add(new WaypointConnection(southExit2, Vector3.Distance(wpMidpoint2, wpSouthExit2)));

            waypoints.Add(eastEntry2);
            waypoints.Add(midpoint2);
            waypoints.Add(southExit2);

            // Lane 3: South to West (right turn)
            Vector3 wpSouthEntry3 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint3 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpWestExit3 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode southEntry3 = new WaypointNode(wpSouthEntry3, cell, WaypointType.Entry);
            WaypointNode midpoint3 = new WaypointNode(wpMidpoint3, cell, WaypointType.Midpoint);
            WaypointNode westExit3 = new WaypointNode(wpWestExit3, cell, WaypointType.Exit);

            southEntry3.Connections.Add(new WaypointConnection(midpoint3, Vector3.Distance(wpSouthEntry3, wpMidpoint3)));
            midpoint3.Connections.Add(new WaypointConnection(westExit3, Vector3.Distance(wpMidpoint3, wpWestExit3)));

            waypoints.Add(southEntry3);
            waypoints.Add(midpoint3);
            waypoints.Add(westExit3);

            // Lane 4: South to East (left turn)
            Vector3 wpSouthEntry4 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint4 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpEastExit4 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode southEntry4 = new WaypointNode(wpSouthEntry4, cell, WaypointType.Entry);
            WaypointNode midpoint4 = new WaypointNode(wpMidpoint4, cell, WaypointType.Midpoint);
            WaypointNode eastExit4 = new WaypointNode(wpEastExit4, cell, WaypointType.Exit);

            southEntry4.Connections.Add(new WaypointConnection(midpoint4, Vector3.Distance(wpSouthEntry4, wpMidpoint4)));
            midpoint4.Connections.Add(new WaypointConnection(eastExit4, Vector3.Distance(wpMidpoint4, wpEastExit4)));

            waypoints.Add(southEntry4);
            waypoints.Add(midpoint4);
            waypoints.Add(eastExit4);

            // Lane 5: West to East (straight through)
            Vector3 wpWestEntry5 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpEastExit5 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode westEntry5 = new WaypointNode(wpWestEntry5, cell, WaypointType.Entry);
            WaypointNode eastExit5 = new WaypointNode(wpEastExit5, cell, WaypointType.Exit);

            westEntry5.Connections.Add(new WaypointConnection(eastExit5, Vector3.Distance(wpWestEntry5, wpEastExit5)));

            waypoints.Add(westEntry5);
            waypoints.Add(eastExit5);

            // Lane 6: West to South (right turn)
            Vector3 wpWestEntry6 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint6 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpSouthExit6 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode westEntry6 = new WaypointNode(wpWestEntry6, cell, WaypointType.Entry);
            WaypointNode midpoint6 = new WaypointNode(wpMidpoint6, cell, WaypointType.Midpoint);
            WaypointNode southExit6 = new WaypointNode(wpSouthExit6, cell, WaypointType.Exit);

            westEntry6.Connections.Add(new WaypointConnection(midpoint6, Vector3.Distance(wpWestEntry6, wpMidpoint6)));
            midpoint6.Connections.Add(new WaypointConnection(southExit6, Vector3.Distance(wpMidpoint6, wpSouthExit6)));

            waypoints.Add(westEntry6);
            waypoints.Add(midpoint6);
            waypoints.Add(southExit6);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateCrossroadsWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // Determine which directions have roads ( should all be true, but better check anyway )
        bool hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        // Crossroads with all four directions
        if (hasNorth && hasSouth && hasEast && hasWest)
        {
            // Lane 1: North to South (straight through)
            Vector3 wpNorthEntry1 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpSouthExit1 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode northEntry1 = new WaypointNode(wpNorthEntry1, cell, WaypointType.Entry);
            WaypointNode southExit1 = new WaypointNode(wpSouthExit1, cell, WaypointType.Exit);

            northEntry1.Connections.Add(new WaypointConnection(southExit1, Vector3.Distance(wpNorthEntry1, wpSouthExit1)));

            waypoints.Add(northEntry1);
            waypoints.Add(southExit1);

            // Lane 2: South to North (straight through)
            Vector3 wpSouthEntry2 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpNorthExit2 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode southEntry2 = new WaypointNode(wpSouthEntry2, cell, WaypointType.Entry);
            WaypointNode northExit2 = new WaypointNode(wpNorthExit2, cell, WaypointType.Exit);

            southEntry2.Connections.Add(new WaypointConnection(northExit2, Vector3.Distance(wpSouthEntry2, wpNorthExit2)));

            waypoints.Add(southEntry2);
            waypoints.Add(northExit2);

            // Lane 3: East to West (straight through)
            Vector3 wpEastEntry3 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpWestExit3 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode eastEntry3 = new WaypointNode(wpEastEntry3, cell, WaypointType.Entry);
            WaypointNode westExit3 = new WaypointNode(wpWestExit3, cell, WaypointType.Exit);

            eastEntry3.Connections.Add(new WaypointConnection(westExit3, Vector3.Distance(wpEastEntry3, wpWestExit3)));

            waypoints.Add(eastEntry3);
            waypoints.Add(westExit3);

            // Lane 4: West to East (straight through)
            Vector3 wpWestEntry4 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpEastExit4 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode westEntry4 = new WaypointNode(wpWestEntry4, cell, WaypointType.Entry);
            WaypointNode eastExit4 = new WaypointNode(wpEastExit4, cell, WaypointType.Exit);

            westEntry4.Connections.Add(new WaypointConnection(eastExit4, Vector3.Distance(wpWestEntry4, wpEastExit4)));

            waypoints.Add(westEntry4);
            waypoints.Add(eastExit4);

            // Lane 5: North to East (left turn)
            Vector3 wpNorthEntry5 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint5 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpEastExit5 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode northEntry5 = new WaypointNode(wpNorthEntry5, cell, WaypointType.Entry);
            WaypointNode midpoint5 = new WaypointNode(wpMidpoint5, cell, WaypointType.Midpoint);
            WaypointNode eastExit5 = new WaypointNode(wpEastExit5, cell, WaypointType.Exit);

            northEntry5.Connections.Add(new WaypointConnection(midpoint5, Vector3.Distance(wpNorthEntry5, wpMidpoint5)));
            midpoint5.Connections.Add(new WaypointConnection(eastExit5, Vector3.Distance(wpMidpoint5, wpEastExit5)));

            waypoints.Add(northEntry5);
            waypoints.Add(midpoint5);
            waypoints.Add(eastExit5);

            // Lane 6: North to West (right turn)
            Vector3 wpNorthEntry6 = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            Vector3 wpMidpoint6 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpWestExit6 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode northEntry6 = new WaypointNode(wpNorthEntry6, cell, WaypointType.Entry);
            WaypointNode midpoint6 = new WaypointNode(wpMidpoint6, cell, WaypointType.Midpoint);
            WaypointNode westExit6 = new WaypointNode(wpWestExit6, cell, WaypointType.Exit);

            northEntry6.Connections.Add(new WaypointConnection(midpoint6, Vector3.Distance(wpNorthEntry6, wpMidpoint6)));
            midpoint6.Connections.Add(new WaypointConnection(westExit6, Vector3.Distance(wpMidpoint6, wpWestExit6)));

            waypoints.Add(northEntry6);
            waypoints.Add(midpoint6);
            waypoints.Add(westExit6);

            // Lane 7: South to East (right turn)
            Vector3 wpSouthEntry7 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint7 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpEastExit7 = cellCentre + new Vector3(halfCellSize, 0, laneCentre);

            WaypointNode southEntry7 = new WaypointNode(wpSouthEntry7, cell, WaypointType.Entry);
            WaypointNode midpoint7 = new WaypointNode(wpMidpoint7, cell, WaypointType.Midpoint);
            WaypointNode eastExit7 = new WaypointNode(wpEastExit7, cell, WaypointType.Exit);

            southEntry7.Connections.Add(new WaypointConnection(midpoint7, Vector3.Distance(wpSouthEntry7, wpMidpoint7)));
            midpoint7.Connections.Add(new WaypointConnection(eastExit7, Vector3.Distance(wpMidpoint7, wpEastExit7)));

            waypoints.Add(southEntry7);
            waypoints.Add(midpoint7);
            waypoints.Add(eastExit7);

            // Lane 8: South to West (left turn)
            Vector3 wpSouthEntry8 = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            Vector3 wpMidpoint8 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpWestExit8 = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);

            WaypointNode southEntry8 = new WaypointNode(wpSouthEntry8, cell, WaypointType.Entry);
            WaypointNode midpoint8 = new WaypointNode(wpMidpoint8, cell, WaypointType.Midpoint);
            WaypointNode westExit8 = new WaypointNode(wpWestExit8, cell, WaypointType.Exit);

            southEntry8.Connections.Add(new WaypointConnection(midpoint8, Vector3.Distance(wpSouthEntry8, wpMidpoint8)));
            midpoint8.Connections.Add(new WaypointConnection(westExit8, Vector3.Distance(wpMidpoint8, wpWestExit8)));

            waypoints.Add(southEntry8);
            waypoints.Add(midpoint8);
            waypoints.Add(westExit8);

            // Lane 9: East to North (right turn)
            Vector3 wpEastEntry9 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint9 = cellCentre + new Vector3(-laneCentre, 0, -laneCentre);
            Vector3 wpNorthExit9 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode eastEntry9 = new WaypointNode(wpEastEntry9, cell, WaypointType.Entry);
            WaypointNode midpoint9 = new WaypointNode(wpMidpoint9, cell, WaypointType.Midpoint);
            WaypointNode northExit9 = new WaypointNode(wpNorthExit9, cell, WaypointType.Exit);

            eastEntry9.Connections.Add(new WaypointConnection(midpoint9, Vector3.Distance(wpEastEntry9, wpMidpoint9)));
            midpoint9.Connections.Add(new WaypointConnection(northExit9, Vector3.Distance(wpMidpoint9, wpNorthExit9)));

            waypoints.Add(eastEntry9);
            waypoints.Add(midpoint9);
            waypoints.Add(northExit9);

            // Lane 10: East to South (left turn)
            Vector3 wpEastEntry10 = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            Vector3 wpMidpoint10 = cellCentre + new Vector3(laneCentre, 0, -laneCentre);
            Vector3 wpSouthExit10 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode eastEntry10 = new WaypointNode(wpEastEntry10, cell, WaypointType.Entry);
            WaypointNode midpoint10 = new WaypointNode(wpMidpoint10, cell, WaypointType.Midpoint);
            WaypointNode southExit10 = new WaypointNode(wpSouthExit10, cell, WaypointType.Exit);

            eastEntry10.Connections.Add(new WaypointConnection(midpoint10, Vector3.Distance(wpEastEntry10, wpMidpoint10)));
            midpoint10.Connections.Add(new WaypointConnection(southExit10, Vector3.Distance(wpMidpoint10, wpSouthExit10)));

            waypoints.Add(eastEntry10);
            waypoints.Add(midpoint10);
            waypoints.Add(southExit10);

            // Lane 11: West to North (left turn)
            Vector3 wpWestEntry11 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint11 = cellCentre + new Vector3(-laneCentre, 0, laneCentre);
            Vector3 wpNorthExit11 = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);

            WaypointNode westEntry11 = new WaypointNode(wpWestEntry11, cell, WaypointType.Entry);
            WaypointNode midpoint11 = new WaypointNode(wpMidpoint11, cell, WaypointType.Midpoint);
            WaypointNode northExit11 = new WaypointNode(wpNorthExit11, cell, WaypointType.Exit);

            westEntry11.Connections.Add(new WaypointConnection(midpoint11, Vector3.Distance(wpWestEntry11, wpMidpoint11)));
            midpoint11.Connections.Add(new WaypointConnection(northExit11, Vector3.Distance(wpMidpoint11, wpNorthExit11)));

            waypoints.Add(westEntry11);
            waypoints.Add(midpoint11);
            waypoints.Add(northExit11);

            // Lane 12: West to South (right turn)
            Vector3 wpWestEntry12 = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            Vector3 wpMidpoint12 = cellCentre + new Vector3(laneCentre, 0, laneCentre);
            Vector3 wpSouthExit12 = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);

            WaypointNode westEntry12 = new WaypointNode(wpWestEntry12, cell, WaypointType.Entry);
            WaypointNode midpoint12 = new WaypointNode(wpMidpoint12, cell, WaypointType.Midpoint);
            WaypointNode southExit12 = new WaypointNode(wpSouthExit12, cell, WaypointType.Exit);

            westEntry12.Connections.Add(new WaypointConnection(midpoint12, Vector3.Distance(wpWestEntry12, wpMidpoint12)));
            midpoint12.Connections.Add(new WaypointConnection(southExit12, Vector3.Distance(wpMidpoint12, wpSouthExit12)));

            waypoints.Add(westEntry12);
            waypoints.Add(midpoint12);
            waypoints.Add(southExit12);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateDeadEndWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        bool hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        Vector3 wpEntry = Vector3.zero, wpMidpoint1 = Vector3.zero, wpUTurn = Vector3.zero, wpMidpoint2 = Vector3.zero, wpExit = Vector3.zero;
        WaypointNode entry = null, midpoint1 = null, uTurn = null, midpoint2 = null, exit = null;

        if (hasNorth)
        {
            wpEntry = cellCentre + new Vector3(laneCentre, 0, halfCellSize);
            wpMidpoint1 = cellCentre + new Vector3(laneCentre, 0, 0);
            wpUTurn = cellCentre - new Vector3(0, 0, quarterCellSize);
            wpMidpoint2 = cellCentre + new Vector3(-laneCentre, 0, 0);
            wpExit = cellCentre + new Vector3(-laneCentre, 0, halfCellSize);
        }
        else if (hasSouth)
        {
            wpEntry = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize);
            wpMidpoint1 = cellCentre + new Vector3(-laneCentre, 0, 0);
            wpUTurn = cellCentre - new Vector3(0, 0, -quarterCellSize);
            wpMidpoint2 = cellCentre + new Vector3(laneCentre, 0, 0);
            wpExit = cellCentre + new Vector3(laneCentre, 0, -halfCellSize);
        }
        else if (hasEast)
        {
            wpEntry = cellCentre + new Vector3(halfCellSize, 0, -laneCentre);
            wpMidpoint1 = cellCentre + new Vector3(0, 0, -laneCentre);
            wpUTurn = cellCentre - new Vector3(quarterCellSize, 0, 0);
            wpMidpoint2 = cellCentre + new Vector3(0, 0, laneCentre);
            wpExit = cellCentre + new Vector3(halfCellSize, 0, laneCentre);
        }
        else if (hasWest)
        {
            wpEntry = cellCentre + new Vector3(-halfCellSize, 0, laneCentre);
            wpMidpoint1 = cellCentre + new Vector3(0, 0, laneCentre);
            wpUTurn = cellCentre - new Vector3(-quarterCellSize, 0, 0);
            wpMidpoint2 = cellCentre + new Vector3(0, 0, -laneCentre);
            wpExit = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre);
        }

        entry = new WaypointNode(wpEntry, cell, WaypointType.Entry);
        midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
        uTurn = new WaypointNode(wpUTurn, cell, WaypointType.UTurn);
        midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);
        exit = new WaypointNode(wpExit, cell, WaypointType.Exit);

        // Connect entry to exit
        entry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpEntry, wpMidpoint1)));
        midpoint1.Connections.Add(new WaypointConnection(uTurn, Vector3.Distance(wpMidpoint1, wpUTurn)));
        uTurn.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpUTurn, wpMidpoint2)));
        midpoint2.Connections.Add(new WaypointConnection(exit, Vector3.Distance(wpMidpoint2, wpExit)));

        waypoints.Add(entry);
        waypoints.Add(midpoint1);
        waypoints.Add(uTurn);
        waypoints.Add(midpoint2);
        waypoints.Add(exit);

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

    private void ConnectToNeighboringCells(GridCell cell, List<WaypointNode> waypoints)
    {
        // Check all four directions for neighboring roads
        int[] dx = { 0, 0, -1, 1 };
        int[] dz = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = cell.Position.x + dx[i];
            int nz = cell.Position.z + dz[i];
            if (nx >= 0 && nx < GridManager.Instance.GridWidth && nz >= 0 && nz < GridManager.Instance.GridHeight)
            {
                GridCell neighbor = GridManager.Instance.GetCell(nx, nz);
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

    // public void OnDrawGizmos()
    // {
    //     if (allWaypoints.Count == 0) return;

    //     // Draw all waypoints
    //     foreach (var waypoint in allWaypoints)
    //     {
    //         // Color based on waypoint type
    //         Color color = waypoint.Type switch
    //         {
    //             WaypointType.Entry => Color.green,
    //             WaypointType.Exit => Color.red,
    //             WaypointType.Midpoint => Color.blue,
    //             WaypointType.UTurn => Color.magenta,
    //             _ => Color.white
    //         };

    //         // Draw sphere at waypoint position
    //         Gizmos.color = color;
    //         Gizmos.DrawSphere(waypoint.Position, 0.1f);

    //         // Draw connections to neighboring waypoints
    //         foreach (var connection in waypoint.Connections)
    //         {
    //             Gizmos.color = Color.yellow;
    //             Gizmos.DrawLine(waypoint.Position, connection.TargetWaypoint.Position);
    //         }
    //     }
    // }
}