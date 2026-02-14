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
                //CreateCrossroadsWaypoints(cell);
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

            wpSouthEntry.AddConnection(wpNorthExit);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpNorthExit);

            // Lane going South (entry from North, exit to South)
            TrafficWaypoint wpNorthEntry = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize), cell.Position.x, cell.Position.y, 1);
            TrafficWaypoint wpSouthExit = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, -halfCellSize), cell.Position.x, cell.Position.y, 1);

            wpNorthEntry.AddConnection(wpSouthExit);
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

            wpWestEntry.AddConnection(wpEastExit);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEastExit);

            // Lane going West
            TrafficWaypoint wpEastEntry = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize, 0, -laneCentre), cell.Position.x, cell.Position.y, 1);
            TrafficWaypoint wpWestExit = new TrafficWaypoint(
                cellCentre + new Vector3(-halfCellSize, 0, -laneCentre), cell.Position.x, cell.Position.y, 1);

            wpEastEntry.AddConnection(wpWestExit);
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

        // Determine which direction the dead end opens to
        RoadDirection openDirection = RoadDirection.North;
        if (hasSouth) openDirection = RoadDirection.South;
        else if (hasEast) openDirection = RoadDirection.East;
        else if (hasWest) openDirection = RoadDirection.West;

        TrafficWaypoint wpEntry, wpMidIncoming, wpUTurn, wpMidOutgoing, wpExit;

        if (hasNorth || hasSouth)
        {
            // Vertical dead end (opens North or South)
            int multiplier = hasNorth ? 1 : -1;

            // Lane 0: Entry from open side, U-turn, exit back
            // In UK, left-hand traffic means the left lane (negative x) is the primary lane
            wpEntry = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, halfCellSize * multiplier), cell.Position.x, cell.Position.y, 0);

            wpMidIncoming = new TrafficWaypoint(
                cellCentre + new Vector3(-laneCentre, 0, 0), cell.Position.x, cell.Position.y, 0);

            wpUTurn = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, -quarterCellSize * multiplier), cell.Position.x, cell.Position.y, 0);

            wpMidOutgoing = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, 0), cell.Position.x, cell.Position.y, 1);

            wpExit = new TrafficWaypoint(
                cellCentre + new Vector3(laneCentre, 0, halfCellSize * multiplier), cell.Position.x, cell.Position.y, 1);
        }
        else
        {
            // Horizontal dead end (opens East or West)
            int multiplier = hasEast ? 1 : -1;

            // Lane 0: Entry from open side, U-turn, exit back
            // In UK, left-hand traffic means the left lane (negative x) is the primary lane
            wpEntry = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize * multiplier, 0, -laneCentre), cell.Position.x, cell.Position.y, 0);

            wpMidIncoming = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, -laneCentre), cell.Position.x, cell.Position.y, 0);

            wpUTurn = new TrafficWaypoint(
                cellCentre + new Vector3(-quarterCellSize * multiplier, 0, 0), cell.Position.x, cell.Position.y, 0);

            wpMidOutgoing = new TrafficWaypoint(
                cellCentre + new Vector3(0, 0, laneCentre), cell.Position.x, cell.Position.y, 1);

            wpExit = new TrafficWaypoint(
                cellCentre + new Vector3(halfCellSize * multiplier, 0, laneCentre), cell.Position.x, cell.Position.y, 1);
        }

        // Connect waypoints in sequence
        wpEntry.AddConnection(wpMidIncoming);
        wpMidIncoming.AddConnection(wpUTurn);
        wpUTurn.AddConnection(wpMidOutgoing);
        wpMidOutgoing.AddConnection(wpExit);

        // Mark entry and exit waypoints
        cell.WaypointData.ExitWaypoints[openDirection].Add(wpEntry);
        cell.WaypointData.ExitWaypoints[GetOppositeDirection(openDirection)].Add(wpExit);

        // Mark internal waypoints
        cell.WaypointData.InternalWaypoints.AddRange(new[] { wpMidIncoming, wpUTurn, wpMidOutgoing });

        cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry, wpMidIncoming, wpUTurn, wpMidOutgoing, wpExit });
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
        }

        // Connect waypoints
        wpEntry1.AddConnection(wpTurn1);
        wpTurn1.AddConnection(wpExit1);

        wpEntry2.AddConnection(wpTurn2);
        wpTurn2.AddConnection(wpExit2);

        // Mark entry and exit waypoints
        cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry1);
        cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry2);
        cell.WaypointData.ExitWaypoints[GetOppositeDirection(RoadDirection.North)].Add(wpExit1);
        cell.WaypointData.ExitWaypoints[GetOppositeDirection(RoadDirection.East)].Add(wpExit2);

        // Mark internal waypoints
        cell.WaypointData.InternalWaypoints.AddRange(new[] { wpTurn1, wpTurn2 });

        cell.WaypointData.AllWaypoints.AddRange(new[] { wpEntry1, wpTurn1, wpExit1, wpEntry2, wpTurn2, wpExit2 });
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

            // Mark entry and exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry1);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry2);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry3);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpEntry4);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpEntry5);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry6);

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

            // Mark entry and exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpEntry1);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpEntry2);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry3);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpEntry4);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry5);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpEntry6);

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

            // Mark entry and exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry1);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpEntry2);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpEntry3);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpEntry4);
            cell.WaypointData.ExitWaypoints[RoadDirection.South].Add(wpEntry5);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry6);

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

            // Mark entry and exit waypoints
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry1);
            cell.WaypointData.ExitWaypoints[RoadDirection.North].Add(wpEntry2);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry3);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpEntry4);
            cell.WaypointData.ExitWaypoints[RoadDirection.East].Add(wpEntry5);
            cell.WaypointData.ExitWaypoints[RoadDirection.West].Add(wpEntry6);

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