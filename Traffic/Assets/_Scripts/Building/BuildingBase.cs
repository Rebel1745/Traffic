using UnityEngine;

public abstract class BuildingBase : MonoBehaviour, ISelectableObject
{
    [Header("Renderers")]
    [SerializeField] protected MeshRenderer _buildingRenderer;
    [SerializeField] protected MeshRenderer _foundationRenderer;

    [Header("Building Details")]
    public EntityId Id { get; protected set; }
    [SerializeField] private BuildingType _buildingType;
    public BuildingType BuildingType => _buildingType;
    [SerializeField] private int _buildingXCells = 2;  // X axis
    public int BuildingXCells => _buildingXCells;
    [SerializeField] private int _buildingZCells = 2; // Z axis
    public int BuildingZCells => _buildingZCells;
    protected GridCell _cell;
    public GridCell Cell => _cell;
    [SerializeField] protected string _buildingName;
    public string BuildingName => _buildingName;

    [Header("Selectable Settings")]
    [SerializeField] private bool _isSelectable = false;
    [SerializeField] protected Vector3 _cameraFocusOffset;
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] protected Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    public abstract void InitialiseBuilding(EntityId entityId, GridCell cell);
    public abstract void LoadBuilding(EntityId entityId, GridCell cell);

    public MeshRenderer GetFoundationRenderer() => _foundationRenderer;

    public void SelectObject()
    {
        if (_isSelectable)
            UIManager.Instance.LoadBuildingDetails(this);
    }
}

public enum BuildingType
{
    None,
    House,
    PetrolStation,
    CarPark
}
