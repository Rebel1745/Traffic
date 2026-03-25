using UnityEngine;
using System.Collections.Generic;
using System;

public class GridManager : MonoBehaviour, ISaveable
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int _gridWidth = 50;
    [SerializeField] private int _gridHeight = 50;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private LayerMask _planeLayer;

    private GridCell[,] _grid;
    private Vector3 _gridOrigin;

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public float CellSize => _cellSize;
    public Vector3 GridOrigin => _gridOrigin;

    public string SaveKey => "Grid";

    public static event Action OnRoadGridUpdated;

    private bool _subscribedToSaveManager = false;

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

    private void Update()
    {
        if (!_subscribedToSaveManager)
            TryToSubscribeToSaveManager();
    }

    private void OnEnable()
    {
        TryToSubscribeToSaveManager();
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
            _subscribedToSaveManager = false;
        }
    }

    private void TryToSubscribeToSaveManager()
    {
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.RegisterSaveable(this);
        _subscribedToSaveManager = true;
    }

    private void InitializeGrid()
    {
        _grid = new GridCell[_gridWidth, _gridHeight];
        _gridOrigin = transform.position;

        // Initialize grid cells
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridHeight; z++)
            {
                _grid[x, z] = new GridCell
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
        return IsValidGridPosition(new Vector3Int(x, 0, z)) ? _grid[x, z] : null;
    }

    public GridCell GetCell(Vector3Int pos)
    {
        return IsValidGridPosition(new Vector3Int(pos.x, 0, pos.z)) ? _grid[pos.x, pos.z] : null;
    }

    public GridCell GetCellAtWorldPosition(Vector3 worldPos)
    {
        Vector3Int gridPos = WorldToGridPosition(worldPos);
        return GetCell(gridPos.x, gridPos.z);
    }

    public GridCell[,] GetGrid()
    {
        return _grid;
    }

    // Grid queries
    public bool IsValidGridPosition(Vector3Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _gridWidth &&
               gridPos.z >= 0 && gridPos.z < _gridHeight;
    }

    public Vector3Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - _gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / _cellSize);
        int z = Mathf.RoundToInt(relativePos.z / _cellSize);
        return new Vector3Int(x, 0, z);
    }

    public Vector3 GridToWorldPosition(int x, int z)
    {
        return _gridOrigin + new Vector3(x * _cellSize, 0, z * _cellSize);
    }

    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - _gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / _cellSize);
        int z = Mathf.RoundToInt(relativePos.z / _cellSize);
        return _gridOrigin + new Vector3(x * _cellSize, 0, z * _cellSize);
    }

    public Vector3? GetGroundHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _planeLayer))
        {
            return hit.point;
        }

        return null;
    }

    public Vector3Int GetGridPositionFromMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, _gridOrigin.y);

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
        return _gridOrigin + new Vector3(cell.Position.x * _cellSize, 0, cell.Position.z * _cellSize);
    }

    // Road type management
    public void UpdateRoadTypes(Vector3Int placedCell)
    {
        if (_grid[placedCell.x, placedCell.z].CellType == CellType.Empty)
        {
            _grid[placedCell.x, placedCell.z].RoadType = RoadType.Empty;
        }
        else
        {
            // Update the placed cell
            _grid[placedCell.x, placedCell.z].RoadType = GetRoadType(placedCell.x, placedCell.z);
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

            if (IsValidGridPosition(new Vector3Int(nx, 0, nz)) && _grid[nx, nz].CellType == CellType.Road)
            {
                cellsToUpdate.Add(new Vector3Int(nx, 0, nz));
            }
        }

        // Update the road type of all cells in the list
        foreach (Vector3Int cell in cellsToUpdate)
        {
            _grid[cell.x, cell.z].RoadType = GetRoadType(cell.x, cell.z);
        }
    }

    private RoadType GetRoadType(int x, int z)
    {
        // First check if this cell is even a road
        if (_grid[x, z].CellType != CellType.Road)
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

            if (IsValidGridPosition(new Vector3Int(nx, 0, nz)) && _grid[nx, nz].CellType == CellType.Road)
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

    public void UpdateRoadDirections()
    {
        foreach (GridCell cell in _grid)
        {
            if (cell.CellType != CellType.Road)
            {
                cell.RoadDirection = RoadDirection.None;
            }
            else
            {
                cell.RoadDirection = GetRoadDirection(cell);
            }
        }
    }

    private RoadDirection GetRoadDirection(GridCell cell)
    {
        RoadDirection dir = RoadDirection.None;

        // check for cell neighbours
        bool hasNorth = HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = HasRoadNeighbor(cell, RoadDirection.West);

        switch (cell.RoadType)
        {
            case RoadType.Empty:
            case RoadType.Single:
            case RoadType.Crossroads:
                return RoadDirection.None;
            case RoadType.DeadEnd:
                if (hasNorth) return RoadDirection.North;
                if (hasSouth) return RoadDirection.South;
                if (hasEast) return RoadDirection.East;
                return RoadDirection.West;
            case RoadType.Straight:
                if (hasNorth || hasSouth) return RoadDirection.NorthSouth;
                return RoadDirection.WestEast;
            case RoadType.TJunction:
                if (!hasNorth) return RoadDirection.South;
                if (!hasSouth) return RoadDirection.North;
                if (!hasWest) return RoadDirection.East;
                return RoadDirection.West;
            case RoadType.Corner:
                if (hasNorth && hasWest) return RoadDirection.NorthWest;
                if (hasNorth && hasEast) return RoadDirection.NorthEast;
                if (hasSouth && hasWest) return RoadDirection.SouthWest;
                return RoadDirection.SouthEast;
        }

        return dir;
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
            _grid[position.x, position.z].CellType = type;
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

        return _grid[newX, newZ].CellType == CellType.Road;
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

        return _grid[newX, newZ];
    }

    public void UpdateRoadGrid()
    {
        // This method should trigger the road mesh regeneration
        // Since we're separating concerns, we'll create an event for this
        OnRoadGridUpdated?.Invoke();
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        var gridData = new GridSaveData { width = _gridWidth, height = _gridHeight };

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridHeight; z++)
            {
                var cell = _grid[x, z];

                // Only save non-empty cells
                if (cell.CellType == CellType.Empty)
                    continue;

                var cellData = new GridCellSaveData
                {
                    x = cell.Position.x,
                    z = cell.Position.z,
                    cellType = cell.CellType,
                    roadType = cell.RoadType,
                    roadDirection = cell.RoadDirection
                };
                gridData.cells.Add(cellData);
            }
        }

        saveData.grid = gridData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.grid == null)
        {
            Debug.LogWarning("[GridManager] No grid data in save file.");
            return;
        }

        var gridData = saveData.grid;
        _gridWidth = gridData.width;
        _gridHeight = gridData.height;

        // Initialize grid with all Empty cells
        _grid = new GridCell[_gridWidth, _gridHeight];
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridHeight; z++)
            {
                _grid[x, z] = new GridCell
                {
                    Position = new Vector3Int(x, 0, z),
                    CellType = CellType.Empty,
                    RoadType = RoadType.Empty,
                    RoadDirection = RoadDirection.None
                };
            }
        }

        // Only load the non-empty cells from save data
        foreach (var cellData in gridData.cells)
        {
            var position = new Vector3Int(cellData.x, 0, cellData.z);
            var cell = new GridCell
            {
                Position = position,
                CellType = cellData.cellType,
                RoadType = cellData.roadType,
                RoadDirection = cellData.roadDirection
            };

            int gridX = cellData.x;
            int gridZ = cellData.z;

            if (IsValidGridPosition(new Vector3Int(gridX, 0, gridZ)))
                _grid[gridX, gridZ] = cell;
            else
                Debug.LogWarning($"[GridManager] Cell at ({gridX}, {gridZ}) is out of bounds.");
        }

        Debug.Log($"[GridManager] Loaded {_gridWidth}x{_gridHeight} grid with {gridData.cells.Count} non-empty cells.");

        RoadMeshRenderer.Instance.UpdateRoadMesh(false);
    }
}