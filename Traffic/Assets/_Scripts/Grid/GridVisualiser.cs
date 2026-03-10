using UnityEngine;
using System.Collections.Generic;

public class GridVisualiser : MonoBehaviour
{
    public static GridVisualiser Instance { get; private set; }

    [Header("Cell Highlighting")]
    [SerializeField] private Material _highlightMaterial;
    [SerializeField] private LineRenderer _previewLine;
    [SerializeField] private float _previewLineWidthMultiplier = 0.75f;
    private float _cellSize;

    private GameObject _highlightedCell;
    private List<Vector3Int> _cellsAlongDragLine = new List<Vector3Int>();
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;

    [Header("Traffic Light Preview")]
    private GameObject _trafficLightPrefab;

    private List<GameObject> _previewLights = new List<GameObject>();
    private GridCell _lastPreviewCell = null;

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
        _cellSize = GridManager.Instance.CellSize;

        if (_previewLine == null)
        {
            GameObject previewObj = new GameObject("RoadPreview");
            previewObj.transform.parent = transform;
            _previewLine = previewObj.AddComponent<LineRenderer>();
            _previewLine.startWidth = _cellSize * _previewLineWidthMultiplier;
            _previewLine.endWidth = _cellSize * _previewLineWidthMultiplier;
            _previewLine.material = new Material(Shader.Find("Sprites/Default"));
            _previewLine.startColor = Color.gray;
            _previewLine.endColor = Color.gray;

            // Fix billboarding - align to Transform Z so it lies flat on the ground
            _previewLine.alignment = LineAlignment.TransformZ;

            // Rotate the parent object so the Z axis points downward into the ground plane
            previewObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _previewLine.enabled = false;
        }

        // Subscribe to input events
        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
        InputManager.OnLeftClickReleased += HandleLeftClickReleased;
        InputManager.OnRightClickPressed += HandleRightClickPressed;
        InputManager.OnMouseMoved += HandleMouseMoved;

        // Subscribe to simulation state changes
        SimulationManager.Instance.OnStateChanged += HandleStateChanged;

        _trafficLightPrefab = TrafficLightManager.Instance.TrafficLightPrefab;
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        InputManager.OnLeftClickPressed -= HandleLeftClickPressed;
        InputManager.OnLeftClickReleased -= HandleLeftClickReleased;
        InputManager.OnRightClickPressed -= HandleRightClickPressed;
        InputManager.OnMouseMoved -= HandleMouseMoved;

        // Unsubscribe from simulation state changes
        SimulationManager.Instance.OnStateChanged -= HandleStateChanged;

        if (_highlightedCell != null)
        {
            Destroy(_highlightedCell);
        }
    }

    private void Update()
    {
        // Only update hover highlight when in road placement mode
        if (SimulationManager.Instance.CurrentState.SimulationState == SimulationState.Roads)
        {
            UpdateHoverHighlight();
        }
        else if (SimulationManager.Instance.CurrentState.SimulationState == SimulationState.TrafficLights)
        {
            UpdateTrafficLightPreview();
        }
    }

    private void HandleStateChanged(GameStateContext newState)
    {
        if (newState.SimulationState == SimulationState.Roads)
        {
            EnableRoadPlacementVisuals();
            DisableTrafficLightVisuals();
        }
        else if (newState.SimulationState == SimulationState.TrafficLights)
        {
            DisableRoadPlacementVisuals();
            // Visuals are handled dynamically on mouse move
        }
        else
        {
            DisableRoadPlacementVisuals();
            DisableTrafficLightVisuals();
        }
    }

    public void EnableRoadPlacementVisuals()
    {
        if (_highlightedCell != null)
        {
            _highlightedCell.SetActive(true);
        }
    }

    public void DisableRoadPlacementVisuals()
    {
        if (_highlightedCell != null)
        {
            _highlightedCell.SetActive(false);
        }
        _previewLine.enabled = false;
        _isDragging = false;
        _cellsAlongDragLine.Clear();
    }

    private void HandleLeftClickPressed(Vector2 screenPosition)
    {
        // Only handle input when in road placement mode
        if (SimulationManager.Instance.CurrentState.SimulationState == SimulationState.Roads)
        {

            Vector3? hitPoint = GridManager.Instance.GetGroundHitPoint();
            if (hitPoint.HasValue)
            {
                _dragStartPosition = hitPoint.Value;
                _isDragging = true;
                _cellsAlongDragLine.Clear();
            }
        }
        else if (SimulationManager.Instance.CurrentState.SimulationState == SimulationState.TrafficLights)
        {
            HandleTrafficLightPlacement();
        }
    }

    private void HandleLeftClickReleased(Vector2 screenPosition)
    {
        // Only handle input when in road placement mode
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Roads)
            return;

        if (!_isDragging)
            return;

        Vector3? hitPoint = GridManager.Instance.GetGroundHitPoint();
        if (hitPoint.HasValue)
        {
            Vector3 snappedStart = GridManager.Instance.SnapToGrid(_dragStartPosition);
            Vector3 snappedEnd = GridManager.Instance.SnapToGrid(hitPoint.Value);
            Vector3 alignedEnd = GridManager.Instance.AlignToCardinalDirection(snappedStart, snappedEnd);

            float distance = Vector3.Distance(snappedStart, alignedEnd);

            if (distance <= 0.5f) // drag threshold
            {
                // Single click - place a single road cell at the clicked position
                Vector3Int gridPos = GridManager.Instance.WorldToGridPosition(snappedStart);
                if (GridManager.Instance.IsValidGridPosition(gridPos))
                {
                    GridManager.Instance.SetCellType(gridPos, CellType.Road);
                    GridManager.Instance.UpdateRoadTypes(gridPos);
                }
            }
            else
            {
                // Drag - place road along the dragged path
                _cellsAlongDragLine = GridManager.Instance.GetCellsAlongLine(snappedStart, alignedEnd);

                // Place roads for all cells along the line
                foreach (Vector3Int gridPos in _cellsAlongDragLine)
                {
                    if (GridManager.Instance.IsValidGridPosition(gridPos))
                    {
                        GridManager.Instance.SetCellType(gridPos, CellType.Road);
                        GridManager.Instance.UpdateRoadTypes(gridPos);
                    }
                }
            }

            // Update visual representation
            GridManager.Instance.UpdateRoadGrid();

            _isDragging = false;
            _previewLine.enabled = false;
            _cellsAlongDragLine.Clear();
        }
    }

    private void HandleRightClickPressed(Vector2 screenPosition)
    {
        // Only handle input when in road placement mode
        if (SimulationManager.Instance.CurrentState.SimulationState == SimulationState.Roads)
        {

            Vector3? hitPoint = GridManager.Instance.GetGroundHitPoint();
            if (hitPoint.HasValue)
            {
                Vector3 snappedStart = GridManager.Instance.SnapToGrid(hitPoint.Value);
                Vector3Int gridPos = GridManager.Instance.WorldToGridPosition(snappedStart);
                if (GridManager.Instance.IsValidGridPosition(gridPos))
                {
                    GridManager.Instance.SetCellType(gridPos, CellType.Empty);
                    GridManager.Instance.UpdateRoadTypes(gridPos);
                    GridManager.Instance.UpdateRoadGrid();
                }
            }
        }
        else if (SimulationManager.Instance.CurrentState.SimulationState == SimulationState.TrafficLights)
        {
            HandleTrafficLightRemoval();
        }
    }

    private void HandleMouseMoved(Vector2 screenPosition)
    {
        // Only update mouse position when in road placement mode
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Roads)
            return;

        if (_isDragging)
        {
            Vector3? currentPoint = GridManager.Instance.GetGroundHitPoint();
            if (currentPoint.HasValue)
            {
                Vector3 snappedEnd = GridManager.Instance.SnapToGrid(currentPoint.Value);
                Vector3 alignedEnd = GridManager.Instance.AlignToCardinalDirection(_dragStartPosition, snappedEnd);

                // Calculate cells along the drag line
                _cellsAlongDragLine = GridManager.Instance.GetCellsAlongLine(_dragStartPosition, alignedEnd);

                // Calculate the actual preview line endpoints based on cells
                if (_cellsAlongDragLine.Count > 0)
                {
                    Vector3Int firstCell = _cellsAlongDragLine[0];
                    Vector3Int lastCell = _cellsAlongDragLine[_cellsAlongDragLine.Count - 1];

                    Vector3 firstCellCenter = GridManager.Instance.GridToWorldPosition(firstCell.x, firstCell.z);
                    Vector3 lastCellCenter = GridManager.Instance.GridToWorldPosition(lastCell.x, lastCell.z);

                    // Determine the direction of the line
                    Vector3 direction = (lastCellCenter - firstCellCenter).normalized;

                    // If single cell, show no line or a point
                    if (_cellsAlongDragLine.Count == 1)
                    {
                        direction = Vector3.right; // Default direction for single cell
                    }

                    // Extend to the edges of the cells
                    float halfCell = _cellSize / 2f;
                    Vector3 previewStart = firstCellCenter - direction * halfCell;
                    previewStart.y = 0.5f;
                    Vector3 previewEnd = lastCellCenter + direction * halfCell;
                    previewEnd.y = 0.5f;

                    _previewLine.SetPosition(0, previewStart);
                    _previewLine.SetPosition(1, previewEnd);
                    _previewLine.enabled = true;
                }
                else
                {
                    // If no cells, just show a point at the start
                    _previewLine.SetPosition(0, _dragStartPosition);
                    _previewLine.SetPosition(1, _dragStartPosition);
                }
            }
        }
    }

    private void UpdateHoverHighlight()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();

        if (GridManager.Instance.IsValidGridPosition(gridPos))
        {
            Vector3 cellWorldPos = GridManager.Instance.GridToWorldPosition(gridPos.x, gridPos.z);

            if (_highlightedCell == null)
            {
                _highlightedCell = CreateHighlightCell();
            }

            _highlightedCell.transform.position = cellWorldPos;
            _highlightedCell.SetActive(true);
        }
        else if (_highlightedCell != null)
        {
            _highlightedCell.SetActive(false);
        }
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
            new Vector3(-_cellSize / 2, 0.01f, -_cellSize / 2),
            new Vector3(_cellSize / 2, 0.01f, -_cellSize / 2),
            new Vector3(_cellSize / 2, 0.01f, _cellSize / 2),
            new Vector3(-_cellSize / 2, 0.01f, _cellSize / 2)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = _highlightMaterial;

        return highlight;
    }

    private void UpdateTrafficLightPreview()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();

        if (!GridManager.Instance.IsValidGridPosition(gridPos))
        {
            ClearPreviewLights();
            _lastPreviewCell = null;
            return;
        }

        GridCell cell = GridManager.Instance.GetCell(gridPos);

        // Only show preview on road cells
        if (cell == null || cell.CellType != CellType.Road)
        {
            ClearPreviewLights();
            _lastPreviewCell = null;
            return;
        }

        // Only regenerate preview if we've moved to a different cell
        if (cell == _lastPreviewCell)
            return;

        _lastPreviewCell = cell;
        ClearPreviewLights();

        // Get valid TrafficLightLocation waypoints for the current substate
        List<WaypointNode> validWaypoints = GetValidWaypointsForSubState(cell);

        // Spawn a preview light at each valid waypoint
        foreach (WaypointNode waypoint in validWaypoints)
        {
            // Skip waypoints that already have a confirmed light
            if (waypoint.AssignedLight != null)
                continue;

            GameObject preview = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);

            // Make preview semi-transparent to distinguish from confirmed lights
            SetPreviewTransparency(preview, 0.5f);

            _previewLights.Add(preview);
        }
    }

    private List<WaypointNode> GetValidWaypointsForSubState(GridCell cell)
    {
        TrafficLightSubState subState = SimulationManager.Instance.CurrentState.TrafficLightSubState;

        // Filter waypoints by type and substate
        return WaypointManager.Instance.GetCellWaypoints(cell).FindAll(w =>
        {
            if (w.Type != WaypointType.TrafficLightLocation)
                return false;

            return subState switch
            {
                TrafficLightSubState.AddJunctionLights =>
                    cell.RoadType == RoadType.TJunction || cell.RoadType == RoadType.Crossroads,
                TrafficLightSubState.AddPedestrianCrossings =>
                    cell.RoadType == RoadType.Straight,
                _ => false
            };
        });
    }

    private void SetPreviewTransparency(GameObject previewObj, float alpha)
    {
        foreach (Renderer renderer in previewObj.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in renderer.materials)
            {
                // Enable transparency on the material
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                Color colour = mat.color;
                colour.a = alpha;
                mat.color = colour;
            }
        }
    }

    private void ClearPreviewLights()
    {
        foreach (GameObject preview in _previewLights)
        {
            if (preview != null)
                Destroy(preview);
        }
        _previewLights.Clear();
    }

    private void DisableTrafficLightVisuals()
    {
        ClearPreviewLights();
        _lastPreviewCell = null;
    }

    private void HandleTrafficLightPlacement()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();
        if (!GridManager.Instance.IsValidGridPosition(gridPos))
            return;

        GridCell cell = GridManager.Instance.GetCell(gridPos);
        if (cell == null || cell.CellType != CellType.Road)
            return;

        List<WaypointNode> validWaypoints = GetValidWaypointsForSubState(cell);
        if (validWaypoints.Count == 0)
            return;

        // Confirm all previewed lights for this cell
        foreach (WaypointNode waypoint in validWaypoints)
        {
            if (waypoint.AssignedLight != null)
                continue;

            TrafficLightManager.Instance.PlaceLightAtWaypoint(waypoint);
        }

        // Refresh preview for the same cell (to hide confirmed previews)
        _lastPreviewCell = null;
    }

    private void HandleTrafficLightRemoval()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();
        if (!GridManager.Instance.IsValidGridPosition(gridPos))
            return;

        GridCell cell = GridManager.Instance.GetCell(gridPos);
        if (cell == null || cell.CellType != CellType.Road)
            return;

        // Remove all confirmed lights on this cell
        foreach (WaypointNode waypoint in WaypointManager.Instance.GetCellWaypoints(cell))
        {
            if (waypoint.Type == WaypointType.TrafficLightLocation && waypoint.AssignedLight != null)
            {
                TrafficLightManager.Instance.RemoveLightAtWaypoint(waypoint);
            }
        }

        // Refresh preview
        _lastPreviewCell = null;
    }
}