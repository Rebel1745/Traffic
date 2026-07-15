using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class BuildingPlacementHandler : MonoBehaviour, IPlacementHandler
{
    [Header("References")]
    [SerializeField] private Material _validMaterial;     // Green
    [SerializeField] private Material _invalidMaterial;   // Red

    private float _cellSize;
    private GameObject _previewInstance;
    private GameObject _building;
    private MeshRenderer _foundationRenderer;
    private Material _pavementMaterial;
    private float _pavementHeight;

    // building details
    private GameObject _buildingPrefab;
    private int _buildingXCells;
    private int _buildingZCells;

    // Track if we are currently hovering a valid spot
    private bool _isValidPosition = false;

    public void OnEnter()
    {
        SimulationManager.Instance.OnStateChanged += OnStateChanged;

        _cellSize = GridManager.Instance.CellSize;
        _pavementMaterial = RoadMeshRenderer.Instance.GetPavementMaterial();
        _pavementHeight = RoadMeshRenderer.Instance.GetPavementHeight();

        // BuildingManager.Instance.GetBuildingPrefabDetailsFromSimulationState(out _buildingPrefab, out _buildingXCells, out _buildingZCells);

        // CreatePreviewMesh();
        // _previewInstance.SetActive(true);

    }

    public void OnExit()
    {
        SimulationManager.Instance.OnStateChanged -= OnStateChanged;

        if (_previewInstance != null)
        {
            _previewInstance.SetActive(false);
        }
    }

    private void OnStateChanged(GameStateContext context)
    {
        if (_previewInstance != null) Destroy(_previewInstance);

        BuildingManager.Instance.GetBuildingPrefabDetailsFromSimulationState(out _buildingPrefab, out _buildingXCells, out _buildingZCells);

        StartCoroutine(CreatePreviewMesh());
    }

    public void OnUpdate()
    {
        //UpdatePreviewPosition();
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
        // Check validity
        if (IsValidPlacement(hitPoint))
        {
            SetPreviewColor(_pavementMaterial);
            _isValidPosition = true;
        }
        else
        {
            SetPreviewColor(_invalidMaterial);
            _isValidPosition = false;
        }

        UpdatePreviewPosition(hitPoint);
    }

    private bool IsValidPlacement(Vector3 position)
    {
        Vector3Int anchor = GridManager.Instance.WorldToGridPosition(position);

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

    private System.Collections.IEnumerator CreatePreviewMesh()
    {
        yield return new WaitForEndOfFrame();

        _previewInstance = new GameObject("BuildingPreview");

        // add the building
        _building = Instantiate(_buildingPrefab, Vector3.zero, _buildingPrefab.transform.rotation);
        _building.name = "BuildingPreview";
        _building.transform.parent = _previewInstance.transform;
        _foundationRenderer = _building.GetComponent<BuildingBase>().GetFoundationRenderer();

        _previewInstance.SetActive(true);
    }

    private void UpdatePreviewPosition(Vector3 position)
    {
        if (_previewInstance == null) return;

        // Get the world position of the anchor cell (bottom-left of the footprint)
        Vector3Int anchor = GridManager.Instance.WorldToGridPosition(position);
        Vector3 anchorWorldPos = GridManager.Instance.GridToWorldPosition(anchor.x, anchor.z);

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