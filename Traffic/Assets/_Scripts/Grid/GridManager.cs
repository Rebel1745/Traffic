using UnityEngine;
using System.Collections.Generic;
using System;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private LayerMask planeLayer;

    private GridCell[,] grid;
    private Vector3 gridOrigin;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public Vector3 GridOrigin => gridOrigin;

    public static event Action OnRoadGridUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        grid = new GridCell[gridWidth, gridHeight];
        gridOrigin = transform.position;

        // Initialize grid cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                grid[x, z] = new GridCell
                {
                    Position = new Vector3Int(x, 0, z),
                    CellType = CellType.Empty
                };
            }
        }
    }

    // Grid data access
    public GridCell GetCell(int x, int z)
    {
        return IsValidGridPosition(new Vector3Int(x, 0, z)) ? grid[x, z] : null;
    }

    public GridCell GetCellAtWorldPosition(Vector3 worldPos)
    {
        Vector3Int gridPos = WorldToGridPosition(worldPos);
        return GetCell(gridPos.x, gridPos.z);
    }

    public GridCell[,] GetGrid()
    {
        return grid;
    }

    // Grid queries
    public bool IsValidGridPosition(Vector3Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.z >= 0 && gridPos.z < gridHeight;
    }

    public Vector3Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / cellSize);
        int z = Mathf.RoundToInt(relativePos.z / cellSize);
        return new Vector3Int(x, 0, z);
    }

    public Vector3 GridToWorldPosition(int x, int z)
    {
        return gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);
    }

    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / cellSize);
        int z = Mathf.RoundToInt(relativePos.z / cellSize);
        return gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);
    }

    public Vector3? GetGroundHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, planeLayer))
        {
            return hit.point;
        }

        return null;
    }

    public Vector3Int GetGridPositionFromMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, gridOrigin.y);

        if (gridPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.origin + ray.direction * distance;
            return WorldToGridPosition(hitPoint);
        }

        return new Vector3Int(-1, -1, -1);
    }

    public Vector3 AlignToCardinalDirection(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float absX = Mathf.Abs(direction.x);
        float absZ = Mathf.Abs(direction.z);

        if (absX > absZ)
        {
            // Align to X axis
            return new Vector3(end.x, start.y, start.z);
        }
        else
        {
            // Align to Z axis
            return new Vector3(start.x, start.y, end.z);
        }
    }

    public List<Vector3Int> GetCellsAlongLine(Vector3 start, Vector3 end)
    {
        List<Vector3Int> cells = new List<Vector3Int>();

        Vector3Int startGrid = WorldToGridPosition(start);
        Vector3Int endGrid = WorldToGridPosition(end);

        // Use Bresenham-like algorithm for grid traversal
        int x0 = startGrid.x;
        int z0 = startGrid.z;
        int x1 = endGrid.x;
        int z1 = endGrid.z;

        int dx = Mathf.Abs(x1 - x0);
        int dz = Mathf.Abs(z1 - z0);
        int sx = x0 < x1 ? 1 : -1;
        int sz = z0 < z1 ? 1 : -1;

        if (dx > dz)
        {
            // Traverse along X axis
            for (int x = x0; x != x1 + sx; x += sx)
            {
                Vector3Int gridPos = new Vector3Int(x, 0, z0);
                if (IsValidGridPosition(gridPos))
                {
                    cells.Add(gridPos);
                }
            }
        }
        else
        {
            // Traverse along Z axis
            for (int z = z0; z != z1 + sz; z += sz)
            {
                Vector3Int gridPos = new Vector3Int(x0, 0, z);
                if (IsValidGridPosition(gridPos))
                {
                    cells.Add(gridPos);
                }
            }
        }

        return cells;
    }
    public Vector3 GetCellCentre(GridCell cell)
    {
        return gridOrigin + new Vector3(cell.Position.x * cellSize, 0, cell.Position.z * cellSize);
    }

    // Road type management
    public void UpdateRoadTypes(Vector3Int placedCell)
    {
        if (grid[placedCell.x, placedCell.z].CellType == CellType.Empty)
        {
            grid[placedCell.x, placedCell.z].RoadType = RoadType.Empty;
        }
        else
        {
            // Update the placed cell
            grid[placedCell.x, placedCell.z].RoadType = GetRoadType(placedCell.x, placedCell.z);
        }

        // Create a list of cells to update
        List<Vector3Int> cellsToUpdate = new List<Vector3Int>();
        cellsToUpdate.Add(placedCell);

        // Add adjacent cells to the list
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = placedCell.x + dx[i];
            int nz = placedCell.z + dz[i];

            if (IsValidGridPosition(new Vector3Int(nx, 0, nz)) && grid[nx, nz].CellType == CellType.Road)
            {
                cellsToUpdate.Add(new Vector3Int(nx, 0, nz));
            }
        }

        // Update the road type of all cells in the list
        foreach (Vector3Int cell in cellsToUpdate)
        {
            grid[cell.x, cell.z].RoadType = GetRoadType(cell.x, cell.z);
        }
    }

    private RoadType GetRoadType(int x, int z)
    {
        // First check if this cell is even a road
        if (grid[x, z].CellType != CellType.Road)
            return RoadType.Empty;

        int roadCount = 0;
        List<Vector3Int> adjacentRoads = new List<Vector3Int>();

        // Check the four cardinal directions
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (IsValidGridPosition(new Vector3Int(nx, 0, nz)) && grid[nx, nz].CellType == CellType.Road)
            {
                roadCount++;
                adjacentRoads.Add(new Vector3Int(nx, 0, nz));
            }
        }

        // Classify road type based on count and positions
        if (roadCount == 0) return RoadType.Single;
        if (roadCount == 1) return RoadType.DeadEnd;
        if (roadCount == 2)
        {
            if (IsStraightLine(adjacentRoads))
                return RoadType.Straight;
            else
                return RoadType.Corner;
        }
        if (roadCount == 3) return RoadType.TJunction;
        if (roadCount == 4) return RoadType.Crossroads;

        return RoadType.Single;
    }

    private bool IsStraightLine(List<Vector3Int> adjacentRoads)
    {
        // Check if all adjacent roads are in a straight line
        if (adjacentRoads.Count != 2) return false;

        Vector3Int first = adjacentRoads[0];
        Vector3Int second = adjacentRoads[1];

        // Check if they are in the same row or column
        return first.x == second.x || first.z == second.z;
    }

    // Cell modification
    public void SetCellType(Vector3Int position, CellType type)
    {
        if (IsValidGridPosition(position))
        {
            grid[position.x, position.z].CellType = type;
        }
    }

    public void SetCellTypeAtWorldPosition(Vector3 worldPos, CellType type)
    {
        Vector3Int gridPos = WorldToGridPosition(worldPos);
        SetCellType(gridPos, type);
    }

    // Utility methods for other systems
    public bool HasRoadNeighbor(GridCell cell, RoadDirection direction)
    {
        int newX = cell.Position.x;
        int newZ = cell.Position.z;

        switch (direction)
        {
            case RoadDirection.North: newZ++; break;
            case RoadDirection.East: newX++; break;
            case RoadDirection.South: newZ--; break;
            case RoadDirection.West: newX--; break;
        }

        if (!IsValidGridPosition(new Vector3Int(newX, 0, newZ)))
            return false;

        return grid[newX, newZ].CellType == CellType.Road;
    }

    public GridCell GetNeighborInDirection(GridCell cell, RoadDirection direction)
    {
        int newX = cell.Position.x;
        int newZ = cell.Position.z;

        switch (direction)
        {
            case RoadDirection.North: newZ--; break;
            case RoadDirection.East: newX++; break;
            case RoadDirection.South: newZ++; break;
            case RoadDirection.West: newX--; break;
        }

        if (!IsValidGridPosition(new Vector3Int(newX, 0, newZ)))
            return null;

        return grid[newX, newZ];
    }

    public void UpdateRoadGrid()
    {
        // This method should trigger the road mesh regeneration
        // Since we're separating concerns, we'll create an event for this
        OnRoadGridUpdated?.Invoke();
    }
}