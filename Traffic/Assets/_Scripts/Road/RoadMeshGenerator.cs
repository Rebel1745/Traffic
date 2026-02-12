using UnityEngine;

public class RoadMeshGenerator
{
    private RoadConfig config;
    private float halfLaneWidth;
    private float halfPavementWidth;

    public RoadMeshGenerator(RoadConfig config)
    {
        this.config = config;
        halfLaneWidth = config.laneWidth / 2f;
        halfPavementWidth = config.pavementWidth / 2f;
    }

    public MeshData GenerateRoadNetwork(GridCell[,] grid, Vector3 gridOrigin, float cellSize)
    {
        MeshData meshData = new MeshData();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                GridCell cell = grid[x, z];
                if (cell.CellType == CellType.Road)
                {
                    Vector3 cellCenter = gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);

                    switch (cell.RoadType)
                    {
                        case RoadType.Single:
                            GenerateSingle(meshData, cellCenter, cellSize, true);
                            break;
                        case RoadType.Straight:
                            GenerateStraight(meshData, cellCenter, cellSize, grid, x, z, true);
                            break;
                        case RoadType.DeadEnd:
                            GenerateDeadEnd(meshData, cellCenter, cellSize, grid, x, z, true);
                            break;
                        case RoadType.Corner:
                            GenerateCorner(meshData, cellCenter, cellSize, grid, x, z, true);
                            break;
                        case RoadType.TJunction:
                            GenerateTJunction(meshData, cellCenter, cellSize, grid, x, z, true);
                            break;
                        case RoadType.Crossroads:
                            GenerateCrossroad(meshData, cellCenter, cellSize, true);
                            break;
                    }
                }
            }
        }

        return meshData;
    }

    public MeshData GeneratePavementNetwork(GridCell[,] grid, Vector3 gridOrigin, float cellSize)
    {
        MeshData meshData = new MeshData();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                GridCell cell = grid[x, z];
                if (cell.CellType == CellType.Road)
                {
                    Vector3 cellCenter = gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);

                    switch (cell.RoadType)
                    {
                        case RoadType.Single:
                            GenerateSingle(meshData, cellCenter, cellSize, false);
                            break;
                        case RoadType.Straight:
                            GenerateStraight(meshData, cellCenter, cellSize, grid, x, z, false);
                            break;
                        case RoadType.DeadEnd:
                            GenerateDeadEnd(meshData, cellCenter, cellSize, grid, x, z, false);
                            break;
                        case RoadType.Corner:
                            GenerateCorner(meshData, cellCenter, cellSize, grid, x, z, false);
                            break;
                        case RoadType.TJunction:
                            GenerateTJunction(meshData, cellCenter, cellSize, grid, x, z, false);
                            break;
                        case RoadType.Crossroads:
                            GenerateCrossroad(meshData, cellCenter, cellSize, false);
                            break;
                    }
                }
            }
        }

        return meshData;
    }

    private void GenerateSingle(MeshData meshData, Vector3 cellCenter, float cellSize, bool isRoad)
    {
        float halfCell = cellSize / 2f;
        float pavementWidth = config.pavementWidth;

        if (isRoad)
        {
            float roadWidth = cellSize - pavementWidth * 2;
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-roadWidth / 2f, 0, -roadWidth / 2f);
            Vector3 roadMax = cellCenter + new Vector3(roadWidth / 2f, 0, roadWidth / 2f);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Left pavement
            Vector3 leftMin = cellCenter + new Vector3(-halfCell, 0, -halfCell);
            Vector3 leftMax = cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell);
            meshData.AddCuboid(leftMin, leftMax, config.pavementThickness);

            // Right pavement
            Vector3 rightMin = cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell);
            Vector3 rightMax = cellCenter + new Vector3(halfCell, 0, halfCell);
            meshData.AddCuboid(rightMin, rightMax, config.pavementThickness);

            // Top pavement
            Vector3 topMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell - pavementWidth);
            Vector3 topMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell);
            meshData.AddCuboid(topMin, topMax, config.pavementThickness);

            // Bottom pavement
            Vector3 bottomMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell);
            Vector3 bottomMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell + pavementWidth);
            meshData.AddCuboid(bottomMin, bottomMax, config.pavementThickness);
        }
    }

    private void GenerateStraight(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;
        float pavementWidth = config.pavementWidth;

        // Check neighbors to determine direction
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };
        int neighborDirection = -1;

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
            {
                neighborDirection = i;
                break;
            }
        }

        if (neighborDirection == -1) return; // No neighbor found

        if (isRoad)
        {
            Vector3 roadMin = Vector3.zero;
            Vector3 roadMax = Vector3.zero;

            switch (neighborDirection)
            {
                case 0: // Left neighbor - road extends to the left
                case 1: // Right neighbor - road extends to the right (same as left)
                    roadMin = cellCenter + new Vector3(-halfCell, 0, -halfCell + pavementWidth);
                    roadMax = cellCenter + new Vector3(halfCell, 0, halfCell - pavementWidth);
                    break;
                case 2: // Up neighbor - road extends upward (negative Z)
                case 3: // Down neighbor - road extends downward (positive Z) (same as up)
                    roadMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell);
                    roadMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell);
                    break;
            }

            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Generate pavement on the two sides without neighbors
            switch (neighborDirection)
            {
                case 0: // Left neighbor - pavement on top, and bottom
                case 1: // Right neighbor - pavement on top, and bottom
                    // Bottom pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell),
                        cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                        config.pavementThickness);
                    // Top pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                        cellCenter + new Vector3(halfCell, 0, halfCell),
                        config.pavementThickness);
                    break;

                case 2: // Up neighbor - pavement on left, right
                case 3: // Down neighbor - pavement on left, right
                        // Left pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell),
                        cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                        config.pavementThickness);
                    // Right pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                        cellCenter + new Vector3(halfCell, 0, halfCell),
                        config.pavementThickness);
                    break;
            }
        }
    }

    private void GenerateCorner(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;
        float pavementWidth = config.pavementWidth;

        // Check neighbors to determine the corner shape
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };
        int neighborDirection1 = -1;
        int neighborDirection2 = -1;

        // Find the two neighbors that form the corner
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
            {
                if (neighborDirection1 == -1)
                {
                    neighborDirection1 = i;
                }
                else
                {
                    neighborDirection2 = i;
                    break;
                }
            }
        }

        if (neighborDirection1 == -1 || neighborDirection2 == -1) return; // Not a corner

        if (isRoad)
        {
            // Generate a curved road piece for the corner
            Vector3 roadMin = Vector3.zero;
            Vector3 roadMax = Vector3.zero;

            // Determine the corner shape based on the neighbor directions
            if ((neighborDirection1 == 0 && neighborDirection2 == 3) || // Left and Up
                (neighborDirection1 == 3 && neighborDirection2 == 0)) // Up and Left
            {
                // Corner from left to up
                roadMin = cellCenter + new Vector3(-halfCell, 0, -halfCell + pavementWidth);
                roadMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell);
            }
            else if ((neighborDirection1 == 0 && neighborDirection2 == 2) || // Left and Down
                     (neighborDirection1 == 2 && neighborDirection2 == 0)) // Down and Left
            {
                // Corner from left to down
                roadMin = cellCenter + new Vector3(-halfCell, 0, -halfCell);
                roadMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth);
            }
            else if ((neighborDirection1 == 1 && neighborDirection2 == 3) || // Right and Up
                     (neighborDirection1 == 3 && neighborDirection2 == 1)) // Up and Right
            {
                // Corner from right to up
                roadMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth);
                roadMax = cellCenter + new Vector3(halfCell, 0, halfCell);
            }
            else if ((neighborDirection1 == 1 && neighborDirection2 == 2) || // Right and Down
                     (neighborDirection1 == 2 && neighborDirection2 == 1)) // Down and Right
            {
                // Corner from right to down
                roadMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell);
                roadMax = cellCenter + new Vector3(halfCell, 0, halfCell - pavementWidth);
            }

            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Determine the pavement placement based on the neighbor directions
            if ((neighborDirection1 == 0 && neighborDirection2 == 3) || // Left and Up
                (neighborDirection1 == 3 && neighborDirection2 == 0)) // Up and Left
            {
                // pavement on bottom and right
                // bottom pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                    config.pavementThickness);

                // right pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell + pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);

                // top left corner for linking to pavements of neighbours
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                    config.pavementThickness);
            }
            else if ((neighborDirection1 == 0 && neighborDirection2 == 2) || // Left and Down
                     (neighborDirection1 == 2 && neighborDirection2 == 0)) // Down and Left
            {
                // pavement on right and top
                // right pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);

                // top pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell),
                    config.pavementThickness);

                // bottom left corner for linking to pavements of neighbours
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth),
                    config.pavementThickness);
            }
            else if ((neighborDirection1 == 1 && neighborDirection2 == 3) || // Right and Up
                     (neighborDirection1 == 3 && neighborDirection2 == 1)) // Up and Right
            {
                // pavement on left and bottom
                // left pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                    config.pavementThickness);

                // bottom pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                    config.pavementThickness);

                // top right corner for linking to pavements of neighbours
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);
            }
            else if ((neighborDirection1 == 1 && neighborDirection2 == 2) || // Right and Down
                     (neighborDirection1 == 2 && neighborDirection2 == 1)) // Down and Right
            {
                // pavement on top and left
                // top pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);

                // left pavement
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell - pavementWidth),
                    config.pavementThickness);

                // bottom right corner for linking to pavements of neighbours
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                    config.pavementThickness);
            }
        }
    }

    private void GenerateDeadEnd(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;
        float pavementWidth = config.pavementWidth;

        // Check neighbors to determine which direction the dead end faces
        int[] dx = { -1, 1, 0, 0 };  // Left, Right, Up, Down
        int[] dz = { 0, 0, -1, 1 };
        int neighborDirection = -1;

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
            {
                neighborDirection = i;
                break;
            }
        }

        if (neighborDirection == -1)
        {
            Debug.LogError("No neighbour found, shouldn't happen for dead end ");
            return; // No neighbour found, shouldn't happen for dead end  
        }

        if (isRoad)
        {
            Vector3 roadMin = Vector3.zero;
            Vector3 roadMax = Vector3.zero;

            switch (neighborDirection)
            {
                case 0: // Left neighbor - road extends to the left
                    roadMin = cellCenter + new Vector3(-halfCell, 0, -halfCell + pavementWidth);
                    roadMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth);
                    break;
                case 1: // Right neighbor - road extends to the right
                    roadMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth);
                    roadMax = cellCenter + new Vector3(halfCell, 0, halfCell - pavementWidth);
                    break;
                case 2: // Up neighbor - road extends upward (negative Z)
                    roadMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell);
                    roadMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth);
                    break;
                case 3: // Down neighbor - road extends downward (positive Z)
                    roadMin = cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth);
                    roadMax = cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell);
                    break;
            }

            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Generate pavement on the three sides without a neighbor
            switch (neighborDirection)
            {
                case 0: // Left neighbor - pavement on right, top, and bottom
                        // Right pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                        cellCenter + new Vector3(halfCell, 0, halfCell),
                        config.pavementThickness);
                    // Top pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                        cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell),
                        config.pavementThickness);
                    // Bottom pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell),
                        cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell + pavementWidth),
                        config.pavementThickness);
                    break;

                case 1: // Right neighbor - pavement on left, top, and bottom
                        // Left pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell),
                        cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                        config.pavementThickness);
                    // Top pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell - pavementWidth),
                        cellCenter + new Vector3(halfCell, 0, halfCell),
                        config.pavementThickness);
                    //Bottom pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell),
                        cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                        config.pavementThickness);
                    break;

                case 2: // Up neighbor - pavement on left, right, and bottom
                    // Bottom pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                        cellCenter + new Vector3(halfCell, 0, halfCell),
                        config.pavementThickness);
                    // Left pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell),
                        cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell - pavementWidth),
                        config.pavementThickness);
                    // Right pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                        cellCenter + new Vector3(halfCell, 0, halfCell - pavementWidth),
                        config.pavementThickness);
                    break;

                case 3: // Down neighbor - pavement on left, right, and top
                    // Top pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell),
                        cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                        config.pavementThickness);
                    // Left pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(-halfCell, 0, -halfCell + pavementWidth),
                        cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                        config.pavementThickness);
                    // Right pavement
                    meshData.AddCuboid(
                        cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell + pavementWidth),
                        cellCenter + new Vector3(halfCell, 0, halfCell),
                        config.pavementThickness);
                    break;
            }
        }
    }

    private void GenerateTJunction(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;
        float pavementWidth = config.pavementWidth;

        // Check neighbors to determine the corner shape
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };
        bool[] neighbours = new bool[dx.Length];

        // Find the imposter with no neighbours
        for (int i = 0; i < 4; i++)
        {
            neighbours[i] = false;

            int nx = x + dx[i];
            int nz = z + dz[i];

            if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
            {
                neighbours[i] = true;
            }
        }

        if (isRoad)
        {
            // no road left (T like |-)
            if (!neighbours[0])
            {
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.roadThickness);
            }
            // no road right (T like -|)
            else if (!neighbours[1])
            {
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell),
                    config.roadThickness);
            }
            // no road below (upside down T)
            else if (!neighbours[2])
            {
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell + pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.roadThickness);
            }
            // no road above (classic T shape)
            else if (!neighbours[3])
            {
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, halfCell - pavementWidth),
                    config.roadThickness);
            }
            else
            {
                Debug.LogError("All neighbours of a T junction are roads... this should not be!");
            }
        }
        else
        {
            // no road left (T like |-)
            if (!neighbours[0])
            {
                // pavement on left
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                    config.pavementThickness);

                // nubbin top right
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);

                // nubbing bottom right
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                    config.pavementThickness);
            }
            // no road right (T like -|)
            else if (!neighbours[1])
            {
                // pavement on right
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);

                // nubbin top left
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                    config.pavementThickness);

                // nubbing bottom left
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth),
                    config.pavementThickness);
            }
            // no road below (upside down T)
            else if (!neighbours[2])
            {
                // pavement on bottom
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                    config.pavementThickness);

                // nubbin top left
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                    config.pavementThickness);

                // nubbing top right
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);
            }
            // no road above (classic T shape)
            else if (!neighbours[3])
            {
                // pavement on top
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                    cellCenter + new Vector3(halfCell, 0, halfCell),
                    config.pavementThickness);

                // nubbin bottom left
                meshData.AddCuboid(
                    cellCenter + new Vector3(-halfCell, 0, -halfCell),
                    cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth),
                    config.pavementThickness);

                // nubbing bottom right
                meshData.AddCuboid(
                    cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                    cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                    config.pavementThickness);
            }
            else
            {
                Debug.LogError("All neighbours of a T junction are roads... this should not be!");
            }
        }
    }

    private void GenerateCrossroad(MeshData meshData, Vector3 cellCenter, float cellSize, bool isRoad)
    {
        float halfCell = cellSize / 2f;
        float pavementWidth = config.pavementWidth;

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfCell, 0, -halfCell);
            Vector3 roadMax = cellCenter + new Vector3(halfCell, 0, halfCell);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // top left pavement
            meshData.AddCuboid(
                cellCenter + new Vector3(-halfCell, 0, halfCell - pavementWidth),
                cellCenter + new Vector3(-halfCell + pavementWidth, 0, halfCell),
                config.pavementThickness);

            // top right pavement
            meshData.AddCuboid(
                cellCenter + new Vector3(halfCell - pavementWidth, 0, halfCell - pavementWidth),
                cellCenter + new Vector3(halfCell, 0, halfCell),
                config.pavementThickness);

            // bottom left pavement
            meshData.AddCuboid(
                cellCenter + new Vector3(-halfCell, 0, -halfCell),
                cellCenter + new Vector3(-halfCell + pavementWidth, 0, -halfCell + pavementWidth),
                config.pavementThickness);

            // bottom right pavement
            meshData.AddCuboid(
                cellCenter + new Vector3(halfCell - pavementWidth, 0, -halfCell),
                cellCenter + new Vector3(halfCell, 0, -halfCell + pavementWidth),
                config.pavementThickness);
        }
    }

    private bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < 50 && z >= 0 && z < 50; // Adjust based on your grid size
    }
}