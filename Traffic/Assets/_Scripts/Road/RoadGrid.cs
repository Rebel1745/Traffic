using UnityEngine;
using System.Collections.Generic;
using System;

public class RoadGrid : MonoBehaviour
{
    public static RoadGrid Instance { get; private set; }

    [SerializeField] private RoadConfig config;
    [SerializeField] private MeshFilter roadMeshFilter;
    [SerializeField] private MeshFilter pavementMeshFilter;
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private LineRenderer previewLine;
    [SerializeField] private float dragThreshold = 0.5f;
    [SerializeField] private LayerMask planeLayer;
    [SerializeField] private float previewLineWidthMultiplier = 0.75f;

    private GridCell[,] grid;
    private GameObject highlightedCell;
    private Vector3 gridOrigin;
    private Vector3? dragStartPosition;
    private List<Vector3Int> cellsAlongDragLine = new List<Vector3Int>();

    private TrafficWaypointManager trafficWaypointManager;

    public void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        Initialise();
    }

    private void Initialise()
    {
        if (roadMeshFilter == null)
        {
            GameObject roadObj = new GameObject("RoadMesh");
            roadObj.transform.parent = transform;
            roadMeshFilter = roadObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = roadObj.AddComponent<MeshRenderer>();
            renderer.material = config.roadMaterial;
        }

        if (pavementMeshFilter == null)
        {
            GameObject pavementObj = new GameObject("PavementMesh");
            pavementObj.transform.parent = transform;
            pavementMeshFilter = pavementObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = pavementObj.AddComponent<MeshRenderer>();
            renderer.material = config.pavementMaterial;
        }

        if (previewLine == null)
        {
            GameObject previewObj = new GameObject("RoadPreview");
            previewObj.transform.parent = transform;
            previewLine = previewObj.AddComponent<LineRenderer>();
            previewLine.startWidth = cellSize * previewLineWidthMultiplier;
            previewLine.endWidth = cellSize * previewLineWidthMultiplier;
            previewLine.material = new Material(Shader.Find("Sprites/Default"));
            previewLine.startColor = Color.dimGray;
            previewLine.endColor = Color.dimGray;
            previewLine.enabled = false;
        }

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

        trafficWaypointManager = new TrafficWaypointManager();
    }

    private void Update()
    {
        UpdateHoverHighlight();

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseDown();
        }

        if (Input.GetMouseButton(0) && dragStartPosition.HasValue)
        {
            HandleMouseDrag();
        }

        if (Input.GetMouseButtonUp(0) && dragStartPosition.HasValue)
        {
            HandleMouseUp();
        }

        if (Input.GetMouseButtonUp(1))
        {
            HandleRightClick();
        }
    }

    private void HandleMouseDown()
    {
        Vector3? hitPoint = GetGroundHitPoint();
        if (hitPoint.HasValue)
        {
            dragStartPosition = hitPoint.Value;
            cellsAlongDragLine.Clear();
        }
    }

    private void HandleMouseDrag()
    {
        Vector3? currentPoint = GetGroundHitPoint();
        if (currentPoint.HasValue)
        {
            Vector3 snappedEnd = SnapToGrid(currentPoint.Value);
            Vector3 alignedEnd = AlignToCardinalDirection(dragStartPosition.Value, snappedEnd);

            // Calculate cells along the drag line
            cellsAlongDragLine = GetCellsAlongLine(dragStartPosition.Value, alignedEnd);

            // Calculate the actual preview line endpoints based on cells
            if (cellsAlongDragLine.Count > 0)
            {
                Vector3Int firstCell = cellsAlongDragLine[0];
                Vector3Int lastCell = cellsAlongDragLine[cellsAlongDragLine.Count - 1];

                Vector3 firstCellCenter = GridToWorldPosition(firstCell.x, firstCell.z);
                Vector3 lastCellCenter = GridToWorldPosition(lastCell.x, lastCell.z);

                // Determine the direction of the line
                Vector3 direction = (lastCellCenter - firstCellCenter).normalized;

                // If single cell, show no line or a point
                if (cellsAlongDragLine.Count == 1)
                {
                    direction = Vector3.right; // Default direction for single cell
                }

                // Extend to the edges of the cells
                float halfCell = cellSize / 2f;
                Vector3 previewStart = firstCellCenter - direction * halfCell;
                previewStart.y = 0.5f;
                Vector3 previewEnd = lastCellCenter + direction * halfCell;
                previewEnd.y = 0.5f;

                previewLine.SetPosition(0, previewStart);
                previewLine.SetPosition(1, previewEnd);
                previewLine.enabled = true;
            }
            else
            {
                // If no cells, just show a point at the start
                previewLine.SetPosition(0, dragStartPosition.Value);
                previewLine.SetPosition(1, dragStartPosition.Value);
            }
        }
    }

    private void HandleMouseUp()
    {
        Vector3? hitPoint = GetGroundHitPoint();
        if (hitPoint.HasValue)
        {
            Vector3 snappedStart = SnapToGrid(dragStartPosition.Value);
            Vector3 snappedEnd = SnapToGrid(hitPoint.Value);
            Vector3 alignedEnd = AlignToCardinalDirection(snappedStart, snappedEnd);

            float distance = Vector3.Distance(snappedStart, alignedEnd);

            if (distance <= dragThreshold)
            {
                // Single click - place a single road cell at the clicked position
                Vector3Int gridPos = WorldToGridPosition(snappedStart);
                if (IsValidGridPosition(gridPos))
                {
                    grid[gridPos.x, gridPos.z].CellType = CellType.Road;
                    UpdateRoadTypes(gridPos);
                }
            }
            else
            {
                // Drag - place road along the dragged path
                cellsAlongDragLine = GetCellsAlongLine(snappedStart, alignedEnd);

                // Place roads for all cells along the line
                foreach (Vector3Int gridPos in cellsAlongDragLine)
                {
                    if (IsValidGridPosition(gridPos))
                    {
                        grid[gridPos.x, gridPos.z].CellType = CellType.Road;
                        UpdateRoadTypes(gridPos);
                    }
                }
            }

            //DebugGrid();
            UpdateRoadGrid();

            dragStartPosition = null;
            previewLine.enabled = false;
            cellsAlongDragLine.Clear();
        }
    }

    private void HandleRightClick()
    {
        Vector3? hitPoint = GetGroundHitPoint();
        if (hitPoint.HasValue)
        {
            Vector3 snappedStart = SnapToGrid(hitPoint.Value);
            Vector3Int gridPos = WorldToGridPosition(snappedStart);
            if (IsValidGridPosition(gridPos))
            {
                grid[gridPos.x, gridPos.z].CellType = CellType.Empty;
                UpdateRoadTypes(gridPos);
                UpdateRoadGrid();
            }
        }
    }

    private Vector3? GetGroundHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, planeLayer))
        {
            return hit.point;
        }

        return null;
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / cellSize);
        int z = Mathf.RoundToInt(relativePos.z / cellSize);
        return gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);
    }

    private Vector3 AlignToCardinalDirection(Vector3 start, Vector3 end)
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

    private List<Vector3Int> GetCellsAlongLine(Vector3 start, Vector3 end)
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

    private Vector3Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / cellSize);
        int z = Mathf.RoundToInt(relativePos.z / cellSize);
        return new Vector3Int(x, 0, z);
    }

    private bool IsValidGridPosition(Vector3Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.z >= 0 && gridPos.z < gridHeight;
    }

    private void UpdateHoverHighlight()
    {
        Vector3Int gridPos = GetGridPositionFromMouse();

        if (IsValidGridPosition(gridPos))
        {
            Vector3 cellWorldPos = GridToWorldPosition(gridPos.x, gridPos.z);

            if (highlightedCell == null)
            {
                highlightedCell = CreateHighlightCell();
            }

            highlightedCell.transform.position = cellWorldPos;
            highlightedCell.SetActive(true);
        }
        else if (highlightedCell != null)
        {
            highlightedCell.SetActive(false);
        }
    }

    private Vector3Int GetGridPositionFromMouse()
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

    private Vector3 GridToWorldPosition(int x, int z)
    {
        return gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);
    }

    private GameObject CreateHighlightCell()
    {
        GameObject highlight = new GameObject("HighlightCell");
        highlight.transform.parent = transform;

        MeshFilter meshFilter = highlight.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = highlight.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-cellSize / 2, 0.01f, -cellSize / 2),
            new Vector3(cellSize / 2, 0.01f, -cellSize / 2),
            new Vector3(cellSize / 2, 0.01f, cellSize / 2),
            new Vector3(-cellSize / 2, 0.01f, cellSize / 2)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = highlightMaterial;

        return highlight;
    }

    private void UpdateRoadTypes(Vector3Int placedCell)
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

    private void UpdateRoadGrid()
    {
        RegenerateMesh();
        WaypointManager.Instance.GenerateWaypoints(grid);
    }

    private void RegenerateMesh()
    {
        RoadMeshGenerator generator = new RoadMeshGenerator(config);

        MeshData roadMeshData = generator.GenerateRoadNetwork(grid, gridOrigin, cellSize);
        MeshData pavementMeshData = generator.GeneratePavementNetwork(grid, gridOrigin, cellSize);

        // Apply road mesh
        Mesh roadMesh = new Mesh();
        roadMesh.vertices = roadMeshData.vertices.ToArray();
        roadMesh.triangles = roadMeshData.triangles.ToArray();
        roadMesh.uv = roadMeshData.uvs.ToArray();
        roadMesh.RecalculateNormals();
        roadMeshFilter.mesh = roadMesh;

        // Apply pavement mesh
        Mesh pavementMesh = new Mesh();
        pavementMesh.vertices = pavementMeshData.vertices.ToArray();
        pavementMesh.triangles = pavementMeshData.triangles.ToArray();
        pavementMesh.uv = pavementMeshData.uvs.ToArray();
        pavementMesh.RecalculateNormals();
        pavementMeshFilter.mesh = pavementMesh;
    }

    public GridCell[,] GetGrid()
    {
        return grid;
    }

    public GridCell GetGridCell(int x, int z)
    {
        return IsValidGridPosition(new Vector3Int(x, 0, z)) ? grid[x, z] : null;
    }

    public Vector3 GetGridOrigin()
    {
        return gridOrigin;
    }

    public float GetGridHeight()
    {
        return gridHeight;
    }

    public float GetGridWidth()
    {
        return gridWidth;
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    public float GetLaneWidth()
    {
        return config.laneWidth;
    }

    public Vector3 GetCellCentre(GridCell cell)
    {
        return gridOrigin + new Vector3(cell.Position.x * cellSize, 0, cell.Position.z * cellSize);
    }

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

    private void OnDrawGizmos()
    {
        if (grid == null || grid.Length == 0)
            return;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                GridCell cell = grid[x, y];

                if (cell.CellType == CellType.Empty || cell.WaypointData == null)
                    continue;

                Gizmos.color = Color.red;
                // Draw all waypoints as spheres
                foreach (var waypoint in cell.WaypointData.AllWaypoints)
                {
                    Gizmos.DrawSphere(waypoint.Position, 0.1f);
                }

                // Draw exit waypoints in yellow
                Gizmos.color = Color.yellow;
                foreach (var direction in cell.WaypointData.ExitWaypoints.Keys)
                {
                    foreach (var waypoint in cell.WaypointData.ExitWaypoints[direction])
                    {
                        Gizmos.DrawSphere(waypoint.Position, 0.1f);
                    }
                }

                // Draw internal waypoints in magenta
                Gizmos.color = Color.magenta;
                foreach (var waypoint in cell.WaypointData.InternalWaypoints)
                {
                    Gizmos.DrawSphere(waypoint.Position, 0.1f);
                }

                // Draw connections between waypoints
                Gizmos.color = Color.white;
                foreach (var waypoint in cell.WaypointData.AllWaypoints)
                {
                    foreach (var connection in waypoint.Connections)
                    {
                        Gizmos.DrawLine(waypoint.Position, connection.Position);
                    }
                }
            }
        }
    }
}