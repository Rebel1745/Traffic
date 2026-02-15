using System.Collections.Generic;
using UnityEngine;

public class TrafficWaypointManager
{
    public GridCell[,] roadGrid;
    private Vector3 cellCentre;
    private float laneCentre;
    private float halfCellSize;
    private float quarterCellSize;

    private List<TrafficWaypoint> allWaypoints = new List<TrafficWaypoint>();

    public void GenerateWaypoints(GridCell[,] grid)
    {
        roadGrid = grid;

        // First pass: Create waypoints for each cell
        for (int x = 0; x < roadGrid.GetLength(0); x++)
        {
            for (int y = 0; y < roadGrid.GetLength(1); y++)
            {
                if (roadGrid[x, y].CellType != CellType.Empty)
                {
                    CreateWaypointsForCell(roadGrid[x, y]);
                }
            }
        }

        // Second pass: Connect waypoints between adjacent cells
        for (int x = 0; x < roadGrid.GetLength(0); x++)
        {
            for (int y = 0; y < roadGrid.GetLength(1); y++)
            {
                if (roadGrid[x, y].CellType != CellType.Empty)
                {
                    ConnectAdjacentCells(roadGrid[x, y]);
                }
            }
        }
    }

    void CreateWaypointsForCell(GridCell cell)
    {
        // Skip empty cells
        if (cell.CellType == CellType.Empty) return;

        cellCentre = RoadGrid.Instance.GetCellCentre(cell);
        laneCentre = RoadGrid.Instance.GetLaneWidth() / 2f;
        halfCellSize = RoadGrid.Instance.GetCellSize() / 2f;
        quarterCellSize = halfCellSize / 2f;

        // Initialize waypoint data for road cells
        cell.WaypointData = new CellWaypointData();

        switch (cell.RoadType)
        {
            case RoadType.Straight:
                GenerateStraightLaneWaypointss(cell);
                break;
            case RoadType.Corner:
                GenerateCornerLaneWaypoints(cell);
                break;
            case RoadType.TJunction:
                GenerateTJunctionLaneWaypoints(cell);
                break;
            case RoadType.Crossroads:
                GenerateCrossroadsLaneWaypoints(cell);
                break;
            case RoadType.DeadEnd:
                GenerateDeadEndLaneWaypoints(cell);
                break;
        }

        allWaypoints.AddRange(cell.WaypointData.AllWaypoints);
    }

    private void GenerateStraightLaneWaypointss(GridCell cell)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        if (hasNorth && hasSouth) // Vertical road
        {
            TrafficWaypoint wpSouthEntry = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize), cell.Position.x, cell.Position.y, 0);
            TrafficWaypoint wpNorthExit = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize), cell.Position.x, cell.Position.y, 0);

            // Lane going South (entry from North, exit to South)
            TrafficWaypoint wpNorthEntry = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize), cell.Position.x, cell.Position.y, 1);
            TrafficWaypoint wpSouthExit = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize), cell.Position.x, cell.Position.y, 1);

            wpSouthEntry.AddConnection(wpNorthExit);
            wpNorthEntry.AddConnection(wpSouthExit);

            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpNorthExit);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpSouthExit);

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpSouthEntry, wpNorthExit, wpNorthEntry, wpSouthExit });
        }
        else if (hasEast && hasWest) // Horizontal road
        {
            // Lane going East
            TrafficWaypoint wpWestEntry = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre), cell.Position.x, cell.Position.y, 0);
            TrafficWaypoint wpEastExit = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre), cell.Position.x, cell.Position.y, 0);

            // Lane going West
            TrafficWaypoint wpEastEntry = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre), cell.Position.x, cell.Position.y, 1);
            TrafficWaypoint wpWestExit = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre), cell.Position.x, cell.Position.y, 1);

            wpWestEntry.AddConnection(wpEastExit);
            wpEastEntry.AddConnection(wpWestExit);

            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEastExit);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpWestExit);

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpWestEntry, wpEastExit, wpEastEntry, wpWestExit });
        }
    }

    private void GenerateDeadEndLaneWaypoints(GridCell cell)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        TrafficWaypoint wpEntry = null, wpMidIncoming = null, wpUTurn = null, wpMidOutgoing = null, wpExit = null;

        if (hasNorth)
        {
            // Lane 0: Entry from open side, U-turn, exit back
            wpEntry = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize), cell.Position.x, cell.Position.y, 0);

            wpMidIncoming = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, 0), cell.Position.x, cell.Position.y, 0);

            wpUTurn = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, -quarterCellSize), cell.Position.x, cell.Position.y, 0);

            wpMidOutgoing = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, 0), cell.Position.x, cell.Position.y, 1);

            wpExit = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize), cell.Position.x, cell.Position.y, 1);

            // Connect waypoints in sequence
            wpEntry.AddConnection(wpMidIncoming);
            wpMidIncoming.AddConnection(wpUTurn);
            wpUTurn.AddConnection(wpMidOutgoing);
            wpMidOutgoing.AddConnection(wpExit);

            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpMidIncoming, wpUTurn, wpMidOutgoing });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry, wpMidIncoming, wpUTurn, wpMidOutgoing, wpExit });
        }
        else if (hasSouth)
        {
            // Lane 0: Entry from open side, U-turn, exit back
            wpEntry = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize), cell.Position.x, cell.Position.y, 0);

            wpMidIncoming = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, 0), cell.Position.x, cell.Position.y, 0);

            wpUTurn = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, quarterCellSize), cell.Position.x, cell.Position.y, 0);

            wpMidOutgoing = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, 0), cell.Position.x, cell.Position.y, 1);

            wpExit = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize), cell.Position.x, cell.Position.y, 1);

            // Connect waypoints in sequence
            wpEntry.AddConnection(wpMidIncoming);
            wpMidIncoming.AddConnection(wpUTurn);
            wpUTurn.AddConnection(wpMidOutgoing);
            wpMidOutgoing.AddConnection(wpExit);

            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpMidIncoming, wpUTurn, wpMidOutgoing });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry, wpMidIncoming, wpUTurn, wpMidOutgoing, wpExit });
        }
        else if (hasEast)
        {
            // Lane 0: Entry from open side, U-turn, exit back
            wpEntry = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre), cell.Position.x, cell.Position.y, 0);

            wpMidIncoming = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, -laneCentre), cell.Position.x, cell.Position.y, 0);

            wpUTurn = new TrafficWaypoint(
                cellCentre + new Vector3(-quarterCellSize, 0, 0), cell.Position.x, cell.Position.y, 0);

            wpMidOutgoing = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, laneCentre), cell.Position.x, cell.Position.y, 1);

            wpExit = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre), cell.Position.x, cell.Position.y, 1);

            // Connect waypoints in sequence
            wpEntry.AddConnection(wpMidIncoming);
            wpMidIncoming.AddConnection(wpUTurn);
            wpUTurn.AddConnection(wpMidOutgoing);
            wpMidOutgoing.AddConnection(wpExit);

            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpMidIncoming, wpUTurn, wpMidOutgoing });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry, wpMidIncoming, wpUTurn, wpMidOutgoing, wpExit });
        }
        else if (hasWest)
        {
            // Lane 0: Entry from open side, U-turn, exit back
            wpEntry = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre), cell.Position.x, cell.Position.y, 0);

            wpMidIncoming = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, -laneCentre), cell.Position.x, cell.Position.y, 0);

            wpUTurn = new TrafficWaypoint(
                cellCentre + new Vector3(quarterCellSize, 0, 0), cell.Position.x, cell.Position.y, 0);

            wpMidOutgoing = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, laneCentre), cell.Position.x, cell.Position.y, 1);

            wpExit = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre), cell.Position.x, cell.Position.y, 1);

            // Connect waypoints in sequence
            wpEntry.AddConnection(wpMidIncoming);
            wpMidIncoming.AddConnection(wpUTurn);
            wpUTurn.AddConnection(wpMidOutgoing);
            wpMidOutgoing.AddConnection(wpExit);

            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpMidIncoming, wpUTurn, wpMidOutgoing });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry, wpMidIncoming, wpUTurn, wpMidOutgoing, wpExit });
        }
    }

    private void GenerateCornerLaneWaypoints(GridCell cell)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        TrafficWaypoint wpEntry1 = null, wpTurn1 = null, wpExit1 = null;
        TrafficWaypoint wpEntry2 = null, wpTurn2 = null, wpExit2 = null;

        if (hasNorth && hasEast)
        {
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);

            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);

            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: From dir2 to dir1
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);

            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit2);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2 });
        }
        else if (hasNorth && hasWest)
        {
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: From dir2 to dir1
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit2);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2 });
        }
        else if (hasSouth && hasEast)
        {
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 0);

            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: From dir2 to dir1
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit2);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2 });
        }
        else if (hasSouth && hasWest)
        {
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 0);

            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: From dir2 to dir1
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit2);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2 });
        }
    }

    private void GenerateTJunctionLaneWaypoints(GridCell cell)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        TrafficWaypoint wpEntry1 = null, wpTurn1 = null, wpExit1 = null;
        TrafficWaypoint wpEntry2 = null, wpTurn2 = null, wpExit2 = null;
        TrafficWaypoint wpEntry3 = null, wpTurn3 = null, wpExit3 = null;
        TrafficWaypoint wpEntry4 = null, wpTurn4 = null, wpExit4 = null;
        TrafficWaypoint wpEntry5 = null, wpExit5 = null;
        TrafficWaypoint wpEntry6 = null, wpExit6 = null;

        if (!hasWest)
        {
            // Lane 1: East to North
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);
            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);
            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: East to South
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // Lane 3: North to East
            wpEntry3 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);
            wpTurn3 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit3 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Lane 4: South to East
            wpEntry4 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);
            wpTurn4 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit4 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // direction 5: South to North (straight)
            wpEntry5 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 2);
            wpExit5 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 2);

            // direction 6: North to South (straight)
            wpEntry6 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 2);
            wpExit6 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 2);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            wpEntry3.AddConnection(wpTurn3);
            wpTurn3.AddConnection(wpExit3);

            wpEntry4.AddConnection(wpTurn4);
            wpTurn4.AddConnection(wpExit4);

            wpEntry5.AddConnection(wpExit5);

            wpEntry6.AddConnection(wpExit6);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit2);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit3);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit4);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit5);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit6);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2, wpTurn3, wpTurn4 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2, wpEntry3, wpTurn3, wpExit3, wpEntry4, wpTurn4, wpExit4, wpEntry5, wpExit5, wpEntry6, wpExit6 });
        }
        else if (!hasNorth)
        {
            // Lane 1: South to East
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 0);
            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);
            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: South to West
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);
            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Lane 3: East to south
            wpEntry3 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn3 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit3 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // Lane 4: West to South
            wpEntry4 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn4 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit4 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // direction 5: East to West (straight)
            wpEntry5 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 2);
            wpExit5 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 2);

            // direction 6: West to east (straight)
            wpEntry6 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 2);
            wpExit6 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 2);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            wpEntry3.AddConnection(wpTurn3);
            wpTurn3.AddConnection(wpExit3);

            wpEntry4.AddConnection(wpTurn4);
            wpTurn4.AddConnection(wpExit4);

            wpEntry5.AddConnection(wpExit5);

            wpEntry6.AddConnection(wpExit6);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit2);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit3);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit4);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit5);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit6);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2, wpTurn3, wpTurn4 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2, wpEntry3, wpTurn3, wpExit3, wpEntry4, wpTurn4, wpExit4, wpEntry5, wpExit5, wpEntry6, wpExit6 });
        }
        else if (!hasEast)
        {
            // Lane 1: North to West
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 0);
            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);
            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: South to West
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);
            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Lane 3: West to North
            wpEntry3 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn3 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit3 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // Lane 4: West to South
            wpEntry4 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn4 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit4 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // direction 5: South to North (straight)
            wpEntry5 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 2);
            wpExit5 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 2);

            // direction 6: North to South (straight)
            wpEntry6 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 2);
            wpExit6 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                cell.Position.x, cell.Position.y, 2);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            wpEntry3.AddConnection(wpTurn3);
            wpTurn3.AddConnection(wpExit3);

            wpEntry4.AddConnection(wpTurn4);
            wpTurn4.AddConnection(wpExit4);

            wpEntry5.AddConnection(wpExit5);

            wpEntry6.AddConnection(wpExit6);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit2);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit3);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit4);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit5);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit6);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2, wpTurn3, wpTurn4 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2, wpEntry3, wpTurn3, wpExit3, wpEntry4, wpTurn4, wpExit4, wpEntry5, wpExit5, wpEntry6, wpExit6 });
        }
        else if (!hasSouth)
        {
            // Lane 1: North to East
            wpEntry1 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 0);
            wpTurn1 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);
            wpExit1 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 0);

            // Lane 2: North to West
            wpEntry2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);
            wpTurn2 = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit2 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);

            // Lane 3: East to North
            wpEntry3 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn3 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit3 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // Lane 4: West to North
            wpEntry4 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpTurn4 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                cell.Position.x, cell.Position.y, 1);
            wpExit4 = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                cell.Position.x, cell.Position.y, 1);

            // direction 5: East to West (straight)
            wpEntry5 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 2);
            wpExit5 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                cell.Position.x, cell.Position.y, 2);

            // direction 6: West to east (straight)
            wpEntry6 = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 2);
            wpExit6 = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                cell.Position.x, cell.Position.y, 2);

            // Connect waypoints
            wpEntry1.AddConnection(wpTurn1);
            wpTurn1.AddConnection(wpExit1);

            wpEntry2.AddConnection(wpTurn2);
            wpTurn2.AddConnection(wpExit2);

            wpEntry3.AddConnection(wpTurn3);
            wpTurn3.AddConnection(wpExit3);

            wpEntry4.AddConnection(wpTurn4);
            wpTurn4.AddConnection(wpExit4);

            wpEntry5.AddConnection(wpExit5);

            wpEntry6.AddConnection(wpExit6);

            // Mark exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit1);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit2);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit3);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit4);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit5);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit6);

            // Mark internal waypoints
            cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2, wpTurn3, wpTurn4 });

            cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2, wpEntry3, wpTurn3, wpExit3, wpEntry4, wpTurn4, wpExit4, wpEntry5, wpExit5, wpEntry6, wpExit6 });
        }
    }

    private void GenerateCrossroadsLaneWaypoints(GridCell cell)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        // Only process if all four directions exist
        if (!hasNorth || !hasSouth || !hasEast || !hasWest)
            return;

        // Define all 12 routes
        TrafficWaypoint wpEntry1 = null, wpEntry2 = null, wpEntry3 = null, wpEntry4 = null, wpEntry5 = null, wpEntry6 = null, wpEntry7 = null, wpEntry8 = null, wpEntry9 = null, wpEntry10 = null, wpEntry11 = null, wpEntry12;
        TrafficWaypoint wpExit1 = null, wpExit2 = null, wpExit3 = null, wpExit4 = null, wpExit5 = null, wpExit6 = null, wpExit7 = null, wpExit8 = null, wpExit9 = null, wpExit10 = null, wpExit11 = null, wpExit12;

        // Turn routes (1-8)
        TrafficWaypoint wpTurn1 = null, wpTurn2 = null, wpTurn3 = null, wpTurn4 = null, wpTurn5 = null, wpTurn6 = null, wpTurn7 = null, wpTurn8 = null;

        // Route 1: East to North (turn)
        wpEntry1 = new TrafficWaypoint(
            cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 0);
        wpTurn1 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 0);
        wpExit1 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
            cell.Position.x, cell.Position.y, 0);

        // Route 2: East to South (turn)
        wpEntry2 = new TrafficWaypoint(
            cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 0);
        wpTurn2 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 0);
        wpExit2 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
            cell.Position.x, cell.Position.y, 0);

        // Route 3: North to East (turn)
        wpEntry3 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, halfCellSize),
            cell.Position.x, cell.Position.y, 1);
        wpTurn3 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, laneCentre),
            cell.Position.x, cell.Position.y, 1);
        wpExit3 = new TrafficWaypoint(
            cellCentre + new Vector3(halfCellSize, 0, laneCentre),
            cell.Position.x, cell.Position.y, 1);

        // Route 4: North to West (turn)
        wpEntry4 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, halfCellSize),
            cell.Position.x, cell.Position.y, 1);
        wpTurn4 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 1);
        wpExit4 = new TrafficWaypoint(
            cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 1);

        // Route 5: West to North (turn)
        wpEntry5 = new TrafficWaypoint(
            cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
            cell.Position.x, cell.Position.y, 2);
        wpTurn5 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, laneCentre),
            cell.Position.x, cell.Position.y, 2);
        wpExit5 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
            cell.Position.x, cell.Position.y, 2);

        // Route 6: West to South (turn)
        wpEntry6 = new TrafficWaypoint(
            cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
            cell.Position.x, cell.Position.y, 2);
        wpTurn6 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, laneCentre),
            cell.Position.x, cell.Position.y, 2);
        wpExit6 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
            cell.Position.x, cell.Position.y, 2);

        // Route 7: South to West (turn)
        wpEntry7 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
            cell.Position.x, cell.Position.y, 3);
        wpTurn7 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 3);
        wpExit7 = new TrafficWaypoint(
            cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 3);

        // Route 8: South to East (turn)
        wpEntry8 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
            cell.Position.x, cell.Position.y, 3);
        wpTurn8 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, laneCentre),
            cell.Position.x, cell.Position.y, 3);
        wpExit8 = new TrafficWaypoint(
            cellCentre + new Vector3(halfCellSize, 0, laneCentre),
            cell.Position.x, cell.Position.y, 3);

        // Route 9: East to West (straight)
        wpEntry9 = new TrafficWaypoint(
            cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 4);
        wpExit9 = new TrafficWaypoint(
            cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
            cell.Position.x, cell.Position.y, 4);

        // Route 10: West to East (straight)
        wpEntry10 = new TrafficWaypoint(
            cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
            cell.Position.x, cell.Position.y, 4);
        wpExit10 = new TrafficWaypoint(
            cellCentre + new Vector3(halfCellSize, 0, laneCentre),
            cell.Position.x, cell.Position.y, 4);

        // Route 11: North to South (straight)
        wpEntry11 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, halfCellSize),
            cell.Position.x, cell.Position.y, 5);
        wpExit11 = new TrafficWaypoint(
            cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
            cell.Position.x, cell.Position.y, 5);

        // Route 12: South to North (straight)
        wpEntry12 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
            cell.Position.x, cell.Position.y, 5);
        wpExit12 = new TrafficWaypoint(
            cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
            cell.Position.x, cell.Position.y, 5);

        // Connect waypoints
        wpEntry1.AddConnection(wpTurn1);
        wpTurn1.AddConnection(wpExit1);

        wpEntry2.AddConnection(wpTurn2);
        wpTurn2.AddConnection(wpExit2);

        wpEntry3.AddConnection(wpTurn3);
        wpTurn3.AddConnection(wpExit3);

        wpEntry4.AddConnection(wpTurn4);
        wpTurn4.AddConnection(wpExit4);

        wpEntry5.AddConnection(wpTurn5);
        wpTurn5.AddConnection(wpExit5);

        wpEntry6.AddConnection(wpTurn6);
        wpTurn6.AddConnection(wpExit6);

        wpEntry7.AddConnection(wpTurn7);
        wpTurn7.AddConnection(wpExit7);

        wpEntry8.AddConnection(wpTurn8);
        wpTurn8.AddConnection(wpExit8);

        wpEntry9.AddConnection(wpExit9);
        wpEntry10.AddConnection(wpExit10);
        wpEntry11.AddConnection(wpExit11);
        wpEntry12.AddConnection(wpExit12);

        // Mark exit waypoints only (consistent with straight roads)
        cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit1);
        cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit2);
        cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit3);
        cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit4);
        cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit5);
        cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit6);
        cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit7);
        cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit8);
        cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpExit9);
        cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpExit10);
        cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpExit11);
        cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpExit12);

        // Mark internal waypoints
        cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2, wpTurn3, wpTurn4, wpTurn5, wpTurn6, wpTurn7, wpTurn8 });

        // Add all waypoints
        cell.WaypointData.AllWaypoints.AddRange(new[] {
            wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2, wpEntry3, wpTurn3, wpExit3, wpEntry4, wpTurn4, wpExit4,
            wpEntry5, wpTurn5, wpExit5, wpEntry6, wpTurn6, wpExit6, wpEntry7, wpTurn7, wpExit7, wpEntry8, wpTurn8, wpExit8,
            wpEntry9, wpExit9, wpEntry10, wpExit10, wpEntry11, wpExit11, wpEntry12, wpExit12
        });
    }

    // Helper method to get the opposite direction
    private RoadDirection GetOppositeDirection(RoadDirection direction)
    {
        switch (direction)
        {
            case RoadDirection.North: return RoadDirection.South;
            case RoadDirection.South: return RoadDirection.North;
            case RoadDirection.East: return RoadDirection.West;
            case RoadDirection.West: return RoadDirection.East;
            default: return RoadDirection.North;
        }
    }

    void ConnectAdjacentCells(GridCell cell)
    {
        // Check North neighbor
        if (cell.Position.y + 1 < roadGrid.GetLength(1) &&
            roadGrid[cell.Position.x, cell.Position.y + 1].CellType != CellType.Empty)
        {
            ConnectCells(cell, RoadDirection.North,
                        roadGrid[cell.Position.x, cell.Position.y + 1], RoadDirection.South);
        }

        // Check East neighbor
        if (cell.Position.x + 1 < roadGrid.GetLength(0) &&
            roadGrid[cell.Position.x + 1, cell.Position.y].CellType != CellType.Empty)
        {
            ConnectCells(cell, RoadDirection.East,
                        roadGrid[cell.Position.x + 1, cell.Position.y], RoadDirection.West);
        }

        // South and West are handled by other cells
    }

    void ConnectCells(GridCell fromCell, RoadDirection fromDir,
                     GridCell toCell, RoadDirection toDir)
    {
        var exitWaypoints = fromCell.WaypointData.ExitWaypoints[fromDir];
        var entryWaypoints = toCell.WaypointData.ExitWaypoints[toDir];

        // Connect matching lanes (you may need more sophisticated matching)
        for (int i = 0; i < Mathf.Min(exitWaypoints.Count, entryWaypoints.Count); i++)
        {
            // Find the closest entry waypoint to this exit
            TrafficWaypoint exitWp = exitWaypoints[i];
            TrafficWaypoint closestEntry = null;
            float minDist = float.MaxValue;

            foreach (var entryWp in entryWaypoints)
            {
                float dist = Vector3.Distance(exitWp.Position, entryWp.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEntry = entryWp;
                }
            }

            if (closestEntry != null)
            {
                exitWp.AddConnection(closestEntry);
            }
        }
    }
}