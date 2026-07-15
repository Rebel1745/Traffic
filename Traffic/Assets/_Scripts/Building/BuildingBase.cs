using UnityEngine;

public abstract class BuildingBase : MonoBehaviour, ISelectableObject
{
    [Header("Renderers")]
    [SerializeField] protected MeshRenderer _buildingRenderer;
    [SerializeField] protected MeshRenderer _foundationRenderer;

    [Header("Building Details")]
    public EntityId Id { get; protected set; }
    [SerializeField] private int _buildingXCells = 2;  // X axis
    public int BuildingXCells => _buildingXCells;
    [SerializeField] private int _buildingZCells = 2; // Z axis
    public int BuildingZCells => _buildingZCells;
    protected GridCell _cell;
    [SerializeField] protected string _buildingName;
    public string BuildingName => _buildingName;

    [Header("Camera Focus Settings")]
    [SerializeField] protected Vector3 _cameraFocusOffset;
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] protected Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    protected abstract int MaximumOccupancy { get; }
    protected abstract int CurrentOccupancy { get; }

    protected abstract int MaximumVehicleOccupancy { get; }
    protected abstract int CurrentVehicleOccupancy { get; }

    public abstract void InitialiseBuilding(EntityId entityId, GridCell cell);
    public abstract void PopulateBuilding();
    public abstract AgentController AddPersonToBuilding();
    public abstract AgentController AddVehicleToBuilding();

    public MeshRenderer GetFoundationRenderer() => _foundationRenderer;

    public void SelectObject()
    {
        UIManager.Instance.LoadBuildingDetails(this);
    }

    protected Vector3 GetSpawnPositionForPerson(Vector3 origin)
    {
        // Keep your existing grid-based spawning logic here
        // This can be overridden if needed for specific building types
        int colIndex = CurrentOccupancy % GetGridCols();
        int rowIndex = CurrentOccupancy / GetGridCols();

        float totalWidth = GetGridCols() * GetGridSize();
        float totalDepth = GetGridRows() * GetGridSize();

        float xOffset = (colIndex * GetGridSize()) - (totalWidth / 2f) + (GetGridSize() / 2f);
        float zOffset = (rowIndex * GetGridSize()) - (totalDepth / 2f) + (GetGridSize() / 2f);

        return new Vector3(origin.x + xOffset, origin.y, origin.z - zOffset);
    }

    protected abstract int GetGridRows();
    protected abstract int GetGridCols();
    protected abstract float GetGridSize();
}
