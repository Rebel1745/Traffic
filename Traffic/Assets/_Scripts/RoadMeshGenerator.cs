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

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfLaneWidth, 0, -halfLaneWidth);
            Vector3 roadMax = cellCenter + new Vector3(halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Left pavement
            Vector3 leftMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
            Vector3 leftMax = cellCenter + new Vector3(-halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(leftMin, leftMax, config.pavementThickness);

            // Right pavement
            Vector3 rightMin = cellCenter + new Vector3(halfLaneWidth, 0, -halfLaneWidth);
            Vector3 rightMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);
            meshData.AddCuboid(rightMin, rightMax, config.pavementThickness);

            // Top pavement
            Vector3 topMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
            Vector3 topMax = cellCenter + new Vector3(halfCell, 0, -halfLaneWidth);
            meshData.AddCuboid(topMin, topMax, config.pavementThickness);

            // Bottom pavement
            Vector3 bottomMin = cellCenter + new Vector3(-halfCell, 0, halfLaneWidth);
            Vector3 bottomMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);
            meshData.AddCuboid(bottomMin, bottomMax, config.pavementThickness);
        }
    }

    private void GenerateStraight(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfLaneWidth, 0, -halfLaneWidth);
            Vector3 roadMax = cellCenter + new Vector3(halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Check neighbors to determine pavement layout
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int nz = z + dz[i];

                if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
                {
                    Vector3 pavementMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
                    Vector3 pavementMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);

                    switch (i)
                    {
                        case 0: // Left
                            pavementMin += new Vector3(-halfCell, 0, 0);
                            pavementMax += new Vector3(-halfLaneWidth, 0, 0);
                            break;
                        case 1: // Right
                            pavementMin += new Vector3(halfLaneWidth, 0, 0);
                            pavementMax += new Vector3(halfCell, 0, 0);
                            break;
                        case 2: // Up
                            pavementMin += new Vector3(0, 0, -halfCell);
                            pavementMax += new Vector3(0, 0, -halfLaneWidth);
                            break;
                        case 3: // Down
                            pavementMin += new Vector3(0, 0, halfLaneWidth);
                            pavementMax += new Vector3(0, 0, halfCell);
                            break;
                    }

                    meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);
                }
            }
        }
    }

    private void GenerateDeadEnd(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfLaneWidth, 0, -halfLaneWidth);
            Vector3 roadMax = cellCenter + new Vector3(halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Check neighbors to determine pavement layout
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int nz = z + dz[i];

                if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
                {
                    Vector3 pavementMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
                    Vector3 pavementMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);

                    switch (i)
                    {
                        case 0: // Left
                            pavementMin += new Vector3(-halfCell, 0, 0);
                            pavementMax += new Vector3(-halfLaneWidth, 0, 0);
                            break;
                        case 1: // Right
                            pavementMin += new Vector3(halfLaneWidth, 0, 0);
                            pavementMax += new Vector3(halfCell, 0, 0);
                            break;
                        case 2: // Up
                            pavementMin += new Vector3(0, 0, -halfCell);
                            pavementMax += new Vector3(0, 0, -halfLaneWidth);
                            break;
                        case 3: // Down
                            pavementMin += new Vector3(0, 0, halfLaneWidth);
                            pavementMax += new Vector3(0, 0, halfCell);
                            break;
                    }

                    meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);
                }
            }
        }
    }

    private void GenerateCorner(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfLaneWidth, 0, -halfLaneWidth);
            Vector3 roadMax = cellCenter + new Vector3(halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Check neighbors to determine pavement layout
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int nz = z + dz[i];

                if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
                {
                    Vector3 pavementMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
                    Vector3 pavementMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);

                    switch (i)
                    {
                        case 0: // Left
                            pavementMin += new Vector3(-halfCell, 0, 0);
                            pavementMax += new Vector3(-halfLaneWidth, 0, 0);
                            break;
                        case 1: // Right
                            pavementMin += new Vector3(halfLaneWidth, 0, 0);
                            pavementMax += new Vector3(halfCell, 0, 0);
                            break;
                        case 2: // Up
                            pavementMin += new Vector3(0, 0, -halfCell);
                            pavementMax += new Vector3(0, 0, -halfLaneWidth);
                            break;
                        case 3: // Down
                            pavementMin += new Vector3(0, 0, halfLaneWidth);
                            pavementMax += new Vector3(0, 0, halfCell);
                            break;
                    }

                    meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);
                }
            }
        }
    }

    private void GenerateTJunction(MeshData meshData, Vector3 cellCenter, float cellSize, GridCell[,] grid, int x, int z, bool isRoad)
    {
        float halfCell = cellSize / 2f;

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfLaneWidth, 0, -halfLaneWidth);
            Vector3 roadMax = cellCenter + new Vector3(halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Check neighbors to determine pavement layout
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int nz = z + dz[i];

                if (IsValidGridPosition(nx, nz) && grid[nx, nz].CellType == CellType.Road)
                {
                    Vector3 pavementMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
                    Vector3 pavementMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);

                    switch (i)
                    {
                        case 0: // Left
                            pavementMin += new Vector3(-halfCell, 0, 0);
                            pavementMax += new Vector3(-halfLaneWidth, 0, 0);
                            break;
                        case 1: // Right
                            pavementMin += new Vector3(halfLaneWidth, 0, 0);
                            pavementMax += new Vector3(halfCell, 0, 0);
                            break;
                        case 2: // Up
                            pavementMin += new Vector3(0, 0, -halfCell);
                            pavementMax += new Vector3(0, 0, -halfLaneWidth);
                            break;
                        case 3: // Down
                            pavementMin += new Vector3(0, 0, halfLaneWidth);
                            pavementMax += new Vector3(0, 0, halfCell);
                            break;
                    }

                    meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);
                }
            }
        }
    }

    private void GenerateCrossroad(MeshData meshData, Vector3 cellCenter, float cellSize, bool isRoad)
    {
        float halfCell = cellSize / 2f;

        if (isRoad)
        {
            // Road is centered in the cell
            Vector3 roadMin = cellCenter + new Vector3(-halfLaneWidth, 0, -halfLaneWidth);
            Vector3 roadMax = cellCenter + new Vector3(halfLaneWidth, 0, halfLaneWidth);
            meshData.AddCuboid(roadMin, roadMax, config.roadThickness);
        }
        else
        {
            // Pavement in all directions
            Vector3 pavementMin = cellCenter + new Vector3(-halfCell, 0, -halfLaneWidth);
            Vector3 pavementMax = cellCenter + new Vector3(halfCell, 0, halfLaneWidth);

            // Left pavement
            meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);

            // Right pavement
            meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);

            // Up pavement
            meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);

            // Down pavement
            meshData.AddCuboid(pavementMin, pavementMax, config.pavementThickness);
        }
    }

    private bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < 50 && z >= 0 && z < 50; // Adjust based on your grid size
    }
}