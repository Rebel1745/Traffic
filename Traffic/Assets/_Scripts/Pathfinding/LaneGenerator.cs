using UnityEngine;

public class LaneGenerator
{
    private Vector3 cellCentre;
    private float laneCentre;
    private float halfCellSize;
    private float quarterCellSize;

    public void GenerateAllLanes(GridCell[,] grid)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                GridCell cell = grid[x, z];

                if (cell.CellType != CellType.Road)
                {
                    cell.LaneData = null;
                    continue;
                }

                cell.LaneData = GenerateLanesForCell(cell);
            }
        }
    }

    private LaneData GenerateLanesForCell(GridCell cell)
    {
        LaneData laneData = new LaneData();
        cellCentre = RoadGrid.Instance.GetCellCentre(cell);
        laneCentre = RoadGrid.Instance.GetLaneWidth() / 2f;
        halfCellSize = RoadGrid.Instance.GetCellSize() / 2f;
        quarterCellSize = halfCellSize / 2f;

        switch (cell.RoadType)
        {
            case RoadType.Empty:
            case RoadType.Single:
                // No lanes for empty or isolated roads
                break;

            case RoadType.DeadEnd:
                GenerateDeadEndLanes(cell, laneData);
                break;

            case RoadType.Straight:
                GenerateStraightLanes(cell, laneData);
                break;

            case RoadType.Corner:
                GenerateCornerLanes(cell, laneData);
                break;

            case RoadType.TJunction:
                GenerateTJunctionLanes(cell, laneData);
                break;

            case RoadType.Crossroads:
                GenerateCrossroadsLanes(cell, laneData);
                break;
        }

        return laneData;
    }

    private void GenerateDeadEndLanes(GridCell cell, LaneData laneData)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        if (hasNorth)
        {
            // Lane coming in (South)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, 0),
                Direction = RoadDirection.South
            });

            // turn from south to centre of dead end
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, 0),
                EndWaypoint = cellCentre + new Vector3(0, 0, -quarterCellSize),
                Direction = RoadDirection.South
            });

            // turn from centre to north lane
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(0, 0, -quarterCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, 0),
                Direction = RoadDirection.North
            });

            // Lane going out (North)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, 0),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
        }
        else if (hasEast)
        {
            // Lane coming in (West)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(0, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // turn from west to centre of dead end
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(0, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-quarterCellSize, 0, 0),
                Direction = RoadDirection.West
            });

            // turn from centre to east lane
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-quarterCellSize, 0, 0),
                EndWaypoint = cellCentre + new Vector3(0, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Lane going out (East)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(0, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });
        }
        else if (hasSouth)
        {
            // Lane coming in (North)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, 0),
                Direction = RoadDirection.North
            });

            // turn from north to centre of dead end
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, 0),
                EndWaypoint = cellCentre + new Vector3(0, 0, quarterCellSize),
                Direction = RoadDirection.North
            });

            // turn from centre to south lane
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(0, 0, quarterCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, 0),
                Direction = RoadDirection.South
            });

            // Lane going out (South)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, 0),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (hasWest)
        {
            // Lane coming in (East)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(0, 0, -laneCentre),
                Direction = RoadDirection.East
            });

            // turn from east to centre of dead end
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(0, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(quarterCellSize, 0, 0),
                Direction = RoadDirection.East
            });

            // turn from centre to west lane
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(quarterCellSize, 0, 0),
                EndWaypoint = cellCentre + new Vector3(0, 0, laneCentre),
                Direction = RoadDirection.West
            });

            // Lane going out (West)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(0, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });
        }
    }

    private void GenerateStraightLanes(GridCell cell, LaneData laneData)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        if (hasNorth && hasSouth) // Vertical road
        {
            // Lane going North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Lane going South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (hasEast && hasWest) // Horizontal road
        {
            // Lane going East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Lane going West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });
        }
    }

    private void GenerateCornerLanes(GridCell cell, LaneData laneData)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        Vector3 cellCentre = RoadGrid.Instance.GetCellCentre(cell);

        if (hasNorth && hasEast)
        {
            // Lane: West to midpoint (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // midpoint to north
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Lane: South to midpoint (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.South
            });

            // Lane: midpoint to east (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });
        }
        else if (hasEast && hasSouth)
        {
            // Lane: North to midpoint (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.North
            });

            // midpoint to east
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Lane: West to midpoint (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Lane: midpoint to south (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (hasSouth && hasWest)
        {
            // Lane: East to midpoint (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // midpoint to south
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.South
            });

            // Lane: North to midpoint (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.North
            });

            // Lane: midpoint to West (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });
        }
        else if (hasWest && hasNorth)
        {
            // Lane: East to midpoint (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Lane: South to midpoint (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.South
            });

            // Lane: midpoint to West (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });
        }
    }

    private void GenerateTJunctionLanes(GridCell cell, LaneData laneData)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        Vector3 cellCentre = RoadGrid.Instance.GetCellCentre(cell);

        if (!hasNorth) // T-Junction pointing South (roads on E, S, W)
        {
            // Through lane: West to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Through lane: East to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: South to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.North
            });

            // Turn lane: Midpoint to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: South to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.North
            });

            // Turn lane: midpoint to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: East to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: midpoint to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Turn lane: West to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: midpoint to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (!hasEast) // T-Junction pointing West (roads on N, S, W)
        {
            // Through lane: North to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Through lane: South to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: West to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: West to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: Midpoint to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Turn lane: North to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: midpoint to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: South to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.North
            });

            // Turn lane: Midpoint to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });
        }
        else if (!hasSouth) // T-Junction pointing North (roads on N, E, W)
        {
            // Through lane: West to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Through lane: East to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: North to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.South
            });

            // Turn lane: Midpoint to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: North to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: midpoint to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: East to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: Midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: West to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
        }
        else if (!hasWest) // T-Junction pointing East (roads on N, S, E)
        {
            // Through lane: North to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Through lane: South to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: East to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: midpoint to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Turn lane: East to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: Midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: North to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.South
            });

            // Turn lane: Midpoint to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: South to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.North
            });

            // Turn lane: Midpoint to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });
        }
    }

    private void GenerateCrossroadsLanes(GridCell cell, LaneData laneData)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        Vector3 cellCentre = RoadGrid.Instance.GetCellCentre(cell);

        // All four directions have roads (crossroads)
        if (hasNorth && hasEast && hasSouth && hasWest)
        {
            // Through roads
            // Through lane: North to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Through lane: South to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Through lane: West to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Through lane: East to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // turns from north
            // north to east
            // Turn lane: North to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.South
            });

            // Turn lane: Midpoint to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });
            // north to west
            // Turn lane: North to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: midpoint to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turns from east
            // east to north
            // Turn lane: East to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: Midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
            // East to south
            // Turn lane: East to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: midpoint to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // turns from south
            // south to east
            // Turn lane: South to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.North
            });

            // Turn lane: Midpoint to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });
            // south to west
            // Turn lane: South to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                Direction = RoadDirection.North
            });

            // Turn lane: Midpoint to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // turns from west
            // west to north
            // Turn lane: West to midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: midpoint to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
            // west to south
            // Turn lane: West to Midpoint
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: Midpoint to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
    }
}