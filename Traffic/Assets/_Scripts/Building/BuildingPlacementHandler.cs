using UnityEngine;
using System.Collections.Generic;

public class BuildingPlacementHandler : MonoBehaviour, IPlacementHandler
{
    [Header("References")][SerializeField] private Material _validMaterial;     // Green
    [SerializeField] private Material _invalidMaterial;   // Red
    [SerializeField] private GameObject _buildingPrefab; // Your prefab reference

    [Header("Building Configuration")][SerializeField] private int _buildingXCells = 2;  // X axis
    [SerializeField] private int _buildingZCells = 2; // Z axis

    private float _cellSize;
    private GameObject _previewInstance;
    // private GameObject _foundation;
    private MeshRenderer _foundationRenderer;
    private Material _pavementMaterial;
    private float _pavementHeight;

    // Track if we are currently hovering a valid spot
    private Vector3Int _gridPos;
    private bool _isValidPosition = false;

    public void OnEnter()
    {
        _cellSize = GridManager.Instance.CellSize;
        _pavementMaterial = RoadMeshRenderer.Instance.GetPavementMaterial();
        _pavementHeight = RoadMeshRenderer.Instance.GetPavementHeight();
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
    }

    public void OnLeftClickReleased(Vector3 hitPoint)
    {
        if (_isValidPosition)
        {
            PlaceBuilding(hitPoint);
        }
    }

    public void OnRightClickPressed(Vector3 hitPoint)
    {
        // Cancel placement or delete? For now, just cancel.
        // Or if you want to delete buildings, check if mouse is over an existing building.
    }

    public void OnMouseMoved(Vector3 hitPoint)
    {
        _gridPos = GridManager.Instance.WorldToGridPosition(hitPoint);

        // Check validity
        if (IsValidPlacement(_gridPos))
        {
            SetPreviewColor(_pavementMaterial);
            _isValidPosition = true;
        }
        else
        {
            SetPreviewColor(_invalidMaterial);
            _isValidPosition = false;
        }

        UpdatePreviewPosition();
    }

    private bool IsValidPlacement(Vector3Int anchor)
    {
        // Check bounds for all 4 cells
        for (int x = 0; x < _buildingXCells; x++)
        {
            for (int z = 0; z < _buildingZCells; z++)
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

    private void PlaceBuilding(Vector3 position)
    {
        // 1. Mark cells in GridManager
        for (int x = 0; x < _buildingXCells; x++)
        {
            for (int z = 0; z < _buildingZCells; z++)
            {
                Vector3Int pos = GridManager.Instance.WorldToGridPosition(new Vector3(position.x + x, 0, position.z + z));
                if (GridManager.Instance.IsValidGridPosition(pos))
                {
                    GridManager.Instance.SetCellType(pos, CellType.Building);
                }
            }
        }

        BuildingManager.Instance.PlaceAndRegisterBuilding(_buildingPrefab, GridManager.Instance.WorldToGridPosition(position), _buildingXCells, _buildingZCells);
    }

    private void CreatePreviewMesh()
    {
        if (_previewInstance != null) return;

        _previewInstance = new GameObject("BuildingPreview");

        // add the 'foundations' of the building (basically a pavement)
        // _foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // _foundation.name = "FoundationPreview";
        // _foundation.transform.localScale = new Vector3(_buildingXCells * _cellSize, _pavementHeight, _buildingZCells * _cellSize);
        // _foundation.transform.parent = _previewInstance.transform;

        // add th building on top of the foundation
        GameObject building = Instantiate(_buildingPrefab, Vector3.zero, _buildingPrefab.transform.rotation);
        building.name = "BuildingPreview";
        building.transform.parent = _previewInstance.transform;
        _foundationRenderer = building.GetComponent<BuildingController>().GetFoundationRenderer();
    }

    private void UpdatePreviewPosition()
    {
        if (_previewInstance == null) return;

        // Get the world position of the anchor cell (bottom-left of the footprint)
        Vector3 anchorWorldPos = GridManager.Instance.GridToWorldPosition(_gridPos.x, _gridPos.z);

        float xOffset = ((_buildingXCells * _cellSize) / 2f) - (_cellSize / 2f);
        float zOffset = ((_buildingZCells * _cellSize) / 2f) - (_cellSize / 2f);

        Vector3 finalPosition = new Vector3(
            anchorWorldPos.x + xOffset,
            anchorWorldPos.y, // Keep Y from grid world pos (usually terrain height or 0)
            anchorWorldPos.z + zOffset
        );

        _previewInstance.transform.position = finalPosition;
    }

    private void SetPreviewColor(Material mat)
    {
        if (_foundationRenderer == null) return;

        _foundationRenderer.material = mat;
    }
}