using UnityEngine;

public class RoadMeshGenerator
{
    private RoadConfig _config;
    private Vector3 _cellCentre;
    private float _cellSize;
    private float _halfCell;
    private float _pavementWidth;

    public RoadMeshGenerator(RoadConfig config)
    {
        _config = config;
    }

    public MeshData GenerateRoadNetwork(GridCell[,] grid, Vector3 gridOrigin, float cellSize)
    {
        MeshData meshData = new MeshData();

        _halfCell = cellSize / 2f;
        _pavementWidth = _config.pavementWidth;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                GridCell cell = grid[x, z];
                if (cell.CellType == CellType.Road)
                {
                    _cellCentre = gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);

                    Vector3 roadMin = _cellCentre + new Vector3(-_halfCell, 0, -_halfCell);
                    Vector3 roadMax = _cellCentre + new Vector3(_halfCell, 0, _halfCell);

                    meshData.AddCuboid(roadMin, roadMax, _config.roadThickness, cell.RoadType);
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
                    _cellCentre = gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);

                    switch (cell.RoadType)
                    {
                        case RoadType.Single:
                            GenerateSingle(meshData);
                            break;
                        case RoadType.Straight:
                            GenerateStraight(meshData, grid, x, z);
                            break;
                        case RoadType.DeadEnd:
                            GenerateDeadEnd(meshData, grid, x, z);
                            break;
                        case RoadType.Corner:
                            GenerateCorner(meshData, grid, x, z);
                            break;
                        case RoadType.TJunction:
                            GenerateTJunction(meshData, grid, x, z);
                            break;
                        case RoadType.Crossroads:
                            GenerateCrossroad(meshData);
                            break;
                    }
                }
            }
        }

        return meshData;
    }

    private void GenerateSingle(MeshData meshData)
    {

        // Left pavement
        Vector3 leftMin = _cellCentre + new Vector3(-_halfCell, 0, -_halfCell);
        Vector3 leftMax = _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell);
        meshData.AddCuboid(leftMin, leftMax, _config.pavementThickness, RoadType.Empty);

        // Right pavement
        Vector3 rightMin = _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell);
        Vector3 rightMax = _cellCentre + new Vector3(_halfCell, 0, _halfCell);
        meshData.AddCuboid(rightMin, rightMax, _config.pavementThickness, RoadType.Empty);

        // Top pavement
        Vector3 topMin = _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell - _pavementWidth);
        Vector3 topMax = _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell);
        meshData.AddCuboid(topMin, topMax, _config.pavementThickness, RoadType.Empty);

        // Bottom pavement
        Vector3 bottomMin = _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell);
        Vector3 bottomMax = _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell + _pavementWidth);
        meshData.AddCuboid(bottomMin, bottomMax, _config.pavementThickness, RoadType.Empty);
    }

    private void GenerateStraight(MeshData meshData, GridCell[,] grid, int x, int z)
    {
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

        // Generate pavement on the two sides without neighbors
        switch (neighborDirection)
        {
            case 0: // Left neighbor - pavement on top, and bottom
            case 1: // Right neighbor - pavement on top, and bottom
                    // Bottom pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                    _config.pavementThickness, RoadType.Empty);
                // Top pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                break;

            case 2: // Up neighbor - pavement on left, right
            case 3: // Down neighbor - pavement on left, right
                    // Left pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                    _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                // Right pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                break;
        }
    }

    private void GenerateCorner(MeshData meshData, GridCell[,] grid, int x, int z)
    {
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


        // Determine the pavement placement based on the neighbor directions
        if ((neighborDirection1 == 0 && neighborDirection2 == 3) || // Left and Up
            (neighborDirection1 == 3 && neighborDirection2 == 0)) // Up and Left
        {
            // pavement on bottom and right
            // bottom pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);

            // right pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell + _pavementWidth),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // top left corner for linking to pavements of neighbours
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);
        }
        else if ((neighborDirection1 == 0 && neighborDirection2 == 2) || // Left and Down
                 (neighborDirection1 == 2 && neighborDirection2 == 0)) // Down and Left
        {
            // pavement on right and top
            // right pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // top pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // bottom left corner for linking to pavements of neighbours
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);
        }
        else if ((neighborDirection1 == 1 && neighborDirection2 == 3) || // Right and Up
                 (neighborDirection1 == 3 && neighborDirection2 == 1)) // Up and Right
        {
            // pavement on left and bottom
            // left pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // bottom pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);

            // top right corner for linking to pavements of neighbours
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);
        }
        else if ((neighborDirection1 == 1 && neighborDirection2 == 2) || // Right and Down
                 (neighborDirection1 == 2 && neighborDirection2 == 1)) // Down and Right
        {
            // pavement on top and left
            // top pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // left pavement
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell - _pavementWidth),
                _config.pavementThickness, RoadType.Empty);

            // bottom right corner for linking to pavements of neighbours
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);
        }
    }

    private void GenerateDeadEnd(MeshData meshData, GridCell[,] grid, int x, int z)
    {
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


        // Generate pavement on the three sides without a neighbor
        switch (neighborDirection)
        {
            case 0: // Left neighbor - pavement on right, top, and bottom
                    // Right pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                // Top pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                    _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                // Bottom pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell + _pavementWidth),
                    _config.pavementThickness, RoadType.Empty);
                break;

            case 1: // Right neighbor - pavement on left, top, and bottom
                    // Left pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                    _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                // Top pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell - _pavementWidth),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                //Bottom pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                    _config.pavementThickness, RoadType.Empty);
                break;

            case 2: // Up neighbor - pavement on left, right, and bottom
                    // Bottom pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                // Left pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                    _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell - _pavementWidth),
                    _config.pavementThickness, RoadType.Empty);
                // Right pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell - _pavementWidth),
                    _config.pavementThickness, RoadType.Empty);
                break;

            case 3: // Down neighbor - pavement on left, right, and top
                    // Top pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                    _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                    _config.pavementThickness, RoadType.Empty);
                // Left pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(-_halfCell, 0, -_halfCell + _pavementWidth),
                    _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                // Right pavement
                meshData.AddCuboid(
                    _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell + _pavementWidth),
                    _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                    _config.pavementThickness, RoadType.Empty);
                break;
        }
    }

    private void GenerateTJunction(MeshData meshData, GridCell[,] grid, int x, int z)
    {
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

        // no road left (T like |-)
        if (!neighbours[0])
        {
            // pavement on left
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // nubbin top right
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // nubbing bottom right
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);
        }
        // no road right (T like -|)
        else if (!neighbours[1])
        {
            // pavement on right
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // nubbin top left
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // nubbing bottom left
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);
        }
        // no road below (upside down T)
        else if (!neighbours[2])
        {
            // pavement on bottom
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);

            // nubbin top left
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // nubbing top right
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);
        }
        // no road above (classic T shape)
        else if (!neighbours[3])
        {
            // pavement on top
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
                _cellCentre + new Vector3(_halfCell, 0, _halfCell),
                _config.pavementThickness, RoadType.Empty);

            // nubbin bottom left
            meshData.AddCuboid(
                _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
                _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);

            // nubbing bottom right
            meshData.AddCuboid(
                _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
                _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
                _config.pavementThickness, RoadType.Empty);
        }
        else
        {
            Debug.LogError("All neighbours of a T junction are roads... this should not be!");
        }
    }

    private void GenerateCrossroad(MeshData meshData)
    {
        // top left pavement
        meshData.AddCuboid(
            _cellCentre + new Vector3(-_halfCell, 0, _halfCell - _pavementWidth),
            _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, _halfCell),
            _config.pavementThickness, RoadType.Empty);

        // top right pavement
        meshData.AddCuboid(
            _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, _halfCell - _pavementWidth),
            _cellCentre + new Vector3(_halfCell, 0, _halfCell),
            _config.pavementThickness, RoadType.Empty);

        // bottom left pavement
        meshData.AddCuboid(
            _cellCentre + new Vector3(-_halfCell, 0, -_halfCell),
            _cellCentre + new Vector3(-_halfCell + _pavementWidth, 0, -_halfCell + _pavementWidth),
            _config.pavementThickness, RoadType.Empty);

        // bottom right pavement
        meshData.AddCuboid(
            _cellCentre + new Vector3(_halfCell - _pavementWidth, 0, -_halfCell),
            _cellCentre + new Vector3(_halfCell, 0, -_halfCell + _pavementWidth),
            _config.pavementThickness, RoadType.Empty);
    }

    private bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < 50 && z >= 0 && z < 50; // Adjust based on your grid size
    }
}