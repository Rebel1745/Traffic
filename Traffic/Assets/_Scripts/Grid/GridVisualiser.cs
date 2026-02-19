using UnityEngine;
using System.Collections.Generic;

public class GridVisualiser : MonoBehaviour
{
    public static GridVisualiser Instance { get; private set; }

    [SerializeField] private Material highlightMaterial;
    [SerializeField] private LineRenderer previewLine;
    [SerializeField] private float previewLineWidthMultiplier = 0.75f;
    private float cellSize;

    private GameObject highlightedCell;
    private List<Vector3Int> cellsAlongDragLine = new List<Vector3Int>();
    private Vector3 dragStartPosition;
    private bool isDragging = false;

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
        cellSize = GridManager.Instance.CellSize;

        // Initialize line renderer
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
    }

    private void OnEnable()
    {
        // Subscribe to input events
        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
        InputManager.OnLeftClickReleased += HandleLeftClickReleased;
        InputManager.OnRightClickPressed += HandleRightClickPressed;
        InputManager.OnMouseMoved += HandleMouseMoved;

        // Subscribe to simulation state changes
        SimulationManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        InputManager.OnLeftClickPressed -= HandleLeftClickPressed;
        InputManager.OnLeftClickReleased -= HandleLeftClickReleased;
        InputManager.OnRightClickPressed -= HandleRightClickPressed;
        InputManager.OnMouseMoved -= HandleMouseMoved;

        // Unsubscribe from simulation state changes
        SimulationManager.OnStateChanged -= HandleStateChanged;

        if (highlightedCell != null)
        {
            Destroy(highlightedCell);
        }
    }

    private void Update()
    {
        // Only update hover highlight when in road placement mode
        if (SimulationManager.Instance.IsInState(SimulationState.PlacingRoads))
        {
            UpdateHoverHighlight();
        }
    }

    private void HandleStateChanged(SimulationState newState)
    {
        if (newState == SimulationState.PlacingRoads)
        {
            EnableRoadPlacementVisuals();
        }
        else
        {
            DisableRoadPlacementVisuals();
        }
    }

    public void EnableRoadPlacementVisuals()
    {
        if (highlightedCell != null)
        {
            highlightedCell.SetActive(true);
        }
    }

    public void DisableRoadPlacementVisuals()
    {
        if (highlightedCell != null)
        {
            highlightedCell.SetActive(false);
        }
        previewLine.enabled = false;
        isDragging = false;
        cellsAlongDragLine.Clear();
    }

    private void HandleLeftClickPressed(Vector2 screenPosition)
    {
        // Only handle input when in road placement mode
        if (!SimulationManager.Instance.IsInState(SimulationState.PlacingRoads))
            return;

        Vector3? hitPoint = GridManager.Instance.GetGroundHitPoint();
        if (hitPoint.HasValue)
        {
            dragStartPosition = hitPoint.Value;
            isDragging = true;
            cellsAlongDragLine.Clear();
        }
    }

    private void HandleLeftClickReleased(Vector2 screenPosition)
    {
        // Only handle input when in road placement mode
        if (!SimulationManager.Instance.IsInState(SimulationState.PlacingRoads))
            return;

        if (!isDragging)
            return;

        Vector3? hitPoint = GridManager.Instance.GetGroundHitPoint();
        if (hitPoint.HasValue)
        {
            Vector3 snappedStart = GridManager.Instance.SnapToGrid(dragStartPosition);
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
                cellsAlongDragLine = GridManager.Instance.GetCellsAlongLine(snappedStart, alignedEnd);

                // Place roads for all cells along the line
                foreach (Vector3Int gridPos in cellsAlongDragLine)
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

            isDragging = false;
            previewLine.enabled = false;
            cellsAlongDragLine.Clear();
        }
    }

    private void HandleRightClickPressed(Vector2 screenPosition)
    {
        // Only handle input when in road placement mode
        if (!SimulationManager.Instance.IsInState(SimulationState.PlacingRoads))
            return;

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

    private void HandleMouseMoved(Vector2 screenPosition)
    {
        // Only update mouse position when in road placement mode
        if (!SimulationManager.Instance.IsInState(SimulationState.PlacingRoads))
            return;

        if (isDragging)
        {
            Vector3? currentPoint = GridManager.Instance.GetGroundHitPoint();
            if (currentPoint.HasValue)
            {
                Vector3 snappedEnd = GridManager.Instance.SnapToGrid(currentPoint.Value);
                Vector3 alignedEnd = GridManager.Instance.AlignToCardinalDirection(dragStartPosition, snappedEnd);

                // Calculate cells along the drag line
                cellsAlongDragLine = GridManager.Instance.GetCellsAlongLine(dragStartPosition, alignedEnd);

                // Calculate the actual preview line endpoints based on cells
                if (cellsAlongDragLine.Count > 0)
                {
                    Vector3Int firstCell = cellsAlongDragLine[0];
                    Vector3Int lastCell = cellsAlongDragLine[cellsAlongDragLine.Count - 1];

                    Vector3 firstCellCenter = GridManager.Instance.GridToWorldPosition(firstCell.x, firstCell.z);
                    Vector3 lastCellCenter = GridManager.Instance.GridToWorldPosition(lastCell.x, lastCell.z);

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
                    previewLine.SetPosition(0, dragStartPosition);
                    previewLine.SetPosition(1, dragStartPosition);
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

    private Vector3? GetGroundHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.MousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            return hit.point;
        }
        return null;
    }
}