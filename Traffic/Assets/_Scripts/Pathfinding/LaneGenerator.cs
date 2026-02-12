using UnityEngine;

public class LaneGenerator
{
    private Vector3 cellCentre;
    private float laneCentre;
    private float halfCellSize;

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

        Vector3 cellCentre = RoadGrid.Instance.GetCellCentre(cell);

        if (hasNorth)
        {
            // Lane going out (North)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Lane coming in (South) - allows for U-turn
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (hasEast)
        {
            // Lane going out (East)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.East
            });

            // Lane coming in (West) - allows for U-turn
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });
        }
        else if (hasSouth)
        {
            // Lane going out (South)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Lane coming in (North) - allows for U-turn
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
        }
        else if (hasWest)
        {
            // Lane going out (West)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.West
            });

            // Lane coming in (East) - allows for U-turn
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });
        }
    }

    private void GenerateStraightLanes(GridCell cell, LaneData laneData)
    {
        bool hasNorth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = RoadGrid.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        Vector3 cellCentre = RoadGrid.Instance.GetCellCentre(cell);

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

        if (hasNorth && hasEast) // North-East corner
        {
            // Lane: South to East (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.East
            });

            // Lane: West to North (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
        }
        else if (hasEast && hasSouth) // East-South corner
        {
            // Lane: North to East (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Lane: West to South (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (hasSouth && hasWest) // South-West corner
        {
            // Lane: North to West (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });

            // Lane: East to South (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (hasWest && hasNorth) // West-North corner
        {
            // Lane: East to North (outer turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Lane: South to West (inner turn)
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
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

            // Turn lane: South to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: South to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: East to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Turn lane: West to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });
        }
        else if (!hasEast) // T-Junction pointing West (roads on N, S, W)
        {
            // Through lane: North to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Through lane: South to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: West to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: West to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Turn lane: North to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: South to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
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

            // Turn lane: North to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: North to West
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });

            // Turn lane: East to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: West to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });
        }
        else if (!hasWest) // T-Junction pointing East (roads on N, S, E)
        {
            // Through lane: North to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Through lane: South to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: East to South
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // Turn lane: East to North
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            // Turn lane: North to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                Direction = RoadDirection.East
            });

            // Turn lane: South to East
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
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
            // North-South lanes
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-laneCentre, 0, -halfCellSize),
                EndWaypoint = cellCentre + new Vector3(-laneCentre, 0, halfCellSize),
                Direction = RoadDirection.North
            });

            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(laneCentre, 0, halfCellSize),
                EndWaypoint = cellCentre + new Vector3(laneCentre, 0, -halfCellSize),
                Direction = RoadDirection.South
            });

            // East-West lanes
            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(-halfCellSize, 0, -laneCentre),
                EndWaypoint = cellCentre + new Vector3(halfCellSize, 0, -laneCentre),
                Direction = RoadDirection.East
            });

            laneData.Lanes.Add(new LaneSegment
            {
                StartWaypoint = cellCentre + new Vector3(halfCellSize, 0, laneCentre),
                EndWaypoint = cellCentre + new Vector3(-halfCellSize, 0, laneCentre),
                Direction = RoadDirection.West
            });
        }
    }
}