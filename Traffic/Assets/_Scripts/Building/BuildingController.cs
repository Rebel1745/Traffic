using UnityEngine;

public class BuildingController : MonoBehaviour, ISelectableObject
{
    [Header("Renderers")]
    [SerializeField] private MeshRenderer _buildingRenderer;
    [SerializeField] private MeshRenderer _foundationRenderer;

    [Header("Building Waypoints - Pedestrian")]
    [SerializeField] private Transform _insideBuildingWaypoint; // waypoint for being 'in' building
    [SerializeField] private Transform _doorWaypoint; // entry to the building
    [SerializeField] private Transform _entryExitPropertyWaypoint; // entry/exit to the whole property (will connect to the pavement)
    [SerializeField] private Transform _entryExitVehicleWaypoint; // entry/exit to the vehicle
    [SerializeField] private Transform[] _parkedToDoorWaypoints; // path from car to door
    [SerializeField] private Transform[] _propertyEntryToDoorWaypoints; // path from property entry/exit to door

    [Header("Building Waypoints - Vehicle")]
    [SerializeField] private Transform _parkedWaypoint; // car stopping point
    [SerializeField] private Transform _vehicleEntryExitWaypoint; // entry/exit to the property
    [SerializeField] private Transform[] _vehicleEntryToParkedWaypoints; // path to parked waypoint from entry
    [SerializeField] private Transform _vehicleCellCheckWaypoint; // the position of the cell that is connected to when the car leaves

    [Header("Selected Details")]
    [SerializeField] private string _buildingName;
    public string BuildingName => _buildingName;
    [SerializeField] private Vector3 _cameraFocusOffset; // the offset to apply to the camera that looks at the building when it is selected
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] private Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    public void SetupBuilding(GridCell cell)
    {
        PedestrianWaypointManager.Instance.AddBuildingPedestrianWaypoints(cell, _insideBuildingWaypoint, _doorWaypoint, _entryExitPropertyWaypoint, _propertyEntryToDoorWaypoints, _entryExitVehicleWaypoint, _parkedToDoorWaypoints);
        RoadWaypointManager.Instance.AddBuildingVehicleWaypoints(cell, _parkedWaypoint, _vehicleEntryToParkedWaypoints, _vehicleEntryExitWaypoint, _vehicleCellCheckWaypoint);
    }

    public MeshRenderer GetFoundationRenderer() => _foundationRenderer;

    public void SelectObject()
    {
        UIManager.Instance.LoadBuildingDetails(this);
    }
}
