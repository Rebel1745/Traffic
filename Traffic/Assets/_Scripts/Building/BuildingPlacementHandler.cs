using UnityEngine;
using System.Collections.Generic;

public class BuildingPlacementHandler : MonoBehaviour, IPlacementHandler
{
    [Header("References")][SerializeField] private Material _validMaterial;     // Green
    [SerializeField] private Material _invalidMaterial;   // Red
    [SerializeField] private GameObject _buildingPrefab; // Your prefab reference

    [Header("Building Configuration")][SerializeField] private int _buildingWidth = 2;  // X axis
    [SerializeField] private int _buildingHeight = 2; // Z axis

    private float _cellSize;
    private GameObject _previewInstance;
    private MeshRenderer _previewRenderer;
    private Vector3Int _currentTopLeft; // Or bottom-left depending on your logic

    // Track if we are currently hovering a valid spot
    private bool _isValidPosition = false;

    public void OnEnter()
    {
        _cellSize = GridManager.Instance.CellSize;
        CreatePreviewMesh();
        _previewInstance.SetActive(true);
    }

    public void OnExit()
    {
        if (_previewInstance != null)
        {
            _previewInstance.SetActive(false);
        }
    }

    public void OnUpdate()
    {
        UpdatePreviewPosition();
    }

    public void OnLeftClickPressed(Vector3 hitPoint)
    {
        // For multi-tile, we usually just "commit" on release or immediate click.
        // Let's implement immediate placement if valid, or wait for release if you prefer drag.
        if (_isValidPosition)
        {
            PlaceBuilding(_currentTopLeft);
        }
    }

    public void OnLeftClickReleased(Vector3 hitPoint)
    {
        if (_isValidPosition)
        {
            PlaceBuilding(_currentTopLeft);
        }
    }

    public void OnRightClickPressed(Vector3 hitPoint)
    {
        // Cancel placement or delete? For now, just cancel.
        // Or if you want to delete buildings, check if mouse is over an existing building.
    }

    public void OnMouseMoved(Vector3 hitPoint)
    {
        Vector3Int gridPos = GridManager.Instance.WorldToGridPosition(hitPoint);

        // Calculate the top-left (or bottom-left) anchor point for the 2x2 block
        // Assuming gridPos is the bottom-left corner of the building
        _currentTopLeft = gridPos;

        // Check validity
        if (IsValidPlacement(_currentTopLeft))
        {
            SetPreviewColor(_validMaterial);
            _isValidPosition = true;
        }
        else
        {
            SetPreviewColor(_invalidMaterial);
            _isValidPosition = false;
        }

        // Update position
        UpdatePreviewPosition();
    }

    private bool IsValidPlacement(Vector3Int anchor)
    {
        // Check bounds for all 4 cells
        for (int x = 0; x < _buildingWidth; x++)
        {
            for (int z = 0; z < _buildingHeight; z++)
            {
                Vector3Int checkPos = new Vector3Int(anchor.x + x, 0, anchor.z + z);

                if (!GridManager.Instance.IsValidGridPosition(checkPos))
                    return false;

                // Check if empty
                GridCell cell = GridManager.Instance.GetCell(checkPos);
                if (cell == null || cell.CellType != CellType.Empty)
                    return false;
            }
        }
        return true;
    }

    private void PlaceBuilding(Vector3Int anchor)
    {
        // 1. Mark cells in GridManager
        for (int x = 0; x < _buildingWidth; x++)
        {
            for (int z = 0; z < _buildingHeight; z++)
            {
                Vector3Int pos = new Vector3Int(anchor.x + x, 0, anchor.z + z);
                if (GridManager.Instance.IsValidGridPosition(pos))
                {
                    GridManager.Instance.SetCellType(pos, CellType.Building);
                    // You might need to set a specific building ID here if you have one
                }
            }
        }

        // 2. Trigger grid update (if needed)
        // GridManager.Instance.UpdateGrid(); 
    }

    private void CreatePreviewMesh()
    {
        if (_previewInstance != null) return;

        _previewInstance = Instantiate(_buildingPrefab.gameObject, Vector3.zero, Quaternion.identity);
        _previewInstance.name = "BuildingPreview";
        _previewInstance.SetActive(false);

        // Ensure it's childed to GridVisualiser or a similar empty parent
        _previewInstance.transform.SetParent(transform.parent);

        _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        if (_previewRenderer == null)
        {
            Debug.LogError("BuildingPreview prefab has no MeshRenderer!");
        }
    }

    private void UpdatePreviewPosition()
    {
        if (_previewInstance == null) return;

        // Convert grid anchor (bottom-left) to world center of the 2x2 block
        // Example: If anchor is (0,0,0) and size is 2, the center is (0.5, 0, 0.5)
        float xOffset = (_buildingWidth * _cellSize) / 2f;
        float zOffset = (_buildingHeight * _cellSize) / 2f;

        Vector3 worldCenter = GridManager.Instance.GridToWorldPosition(_currentTopLeft.x, _currentTopLeft.z);
        worldCenter += new Vector3(xOffset, 0, zOffset);

        // Snap to grid center if needed, or keep exact
        _previewInstance.transform.position = worldCenter;
    }

    private void SetPreviewColor(Material mat)
    {
        if (_previewRenderer == null) return;

        // Simple way: set the material of the renderer
        // Note: Modifying sharedMaterial affects all instances. 
        // Ideally, you'd instantiate a new material per preview, but for a single preview, 
        // checking the material reference is fine.

        // If your prefab has multiple materials, you might need to iterate
        // For now, assuming one main material
        _previewRenderer.material = mat;
    }
}