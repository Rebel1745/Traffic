using System.Collections.Generic;
using UnityEngine;

public class RoadPlacementHandler : MonoBehaviour, IPlacementHandler
{
    [SerializeField] private LineRenderer _previewLine;
    [SerializeField] private float _previewLineWidthMultiplier = 0.75f;
    [SerializeField] private Material _highlightMaterial;

    private float _cellSize;
    private GameObject _highlightedCell;
    private List<Vector3Int> _cellsAlongDragLine = new();
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;

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
    }

    public void OnEnter()
    {
        if (_highlightedCell != null)
            _highlightedCell.SetActive(true);

        EnableRoadPlacementVisuals();
    }
    public void OnExit()
    {
        DisableRoadPlacementVisuals();
    }

    public void OnUpdate()
    {
        UpdateHoverHighlight();
    }

    public void OnLeftClickPressed(Vector3 hitPoint)
    {
        _dragStartPosition = hitPoint;
        _isDragging = true;
        _cellsAlongDragLine.Clear();
    }

    public void OnLeftClickReleased(Vector3 hitPoint)
    {
        if (!_isDragging)
            return;

        Vector3 snappedStart = GridManager.Instance.SnapToGrid(_dragStartPosition);
        Vector3 snappedEnd = GridManager.Instance.SnapToGrid(hitPoint);
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

        GridManager.Instance.UpdateRoadDirections();

        // Update visual representation
        GridManager.Instance.UpdateRoadGrid();

        _isDragging = false;
        _previewLine.enabled = false;
        _cellsAlongDragLine.Clear();
    }

    public void OnRightClickPressed(Vector3 hitPoint)
    {
        Vector3 snappedStart = GridManager.Instance.SnapToGrid(hitPoint);
        Vector3Int gridPos = GridManager.Instance.WorldToGridPosition(snappedStart);
        if (GridManager.Instance.IsValidGridPosition(gridPos))
        {
            GridManager.Instance.SetCellType(gridPos, CellType.Empty);
            GridManager.Instance.UpdateRoadTypes(gridPos);
            GridManager.Instance.UpdateRoadDirections();
            GridManager.Instance.UpdateRoadGrid();
        }
    }

    public void OnMouseMoved(Vector3 hitPoint)
    {
        if (!_isDragging) return;

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

    public void EnableRoadPlacementVisuals()
    {
        if (_highlightedCell != null)
        {
            _highlightedCell.SetActive(true);
        }
    }

    public void DisableRoadPlacementVisuals()
    {
        // if (_highlightedCell != null)
        // {
        //     _highlightedCell.SetActive(false);
        // }
        if (_highlightedCell != null)
        {
            Destroy(_highlightedCell);
        }
        _previewLine.enabled = false;
        _isDragging = false;
        _cellsAlongDragLine.Clear();
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
}
