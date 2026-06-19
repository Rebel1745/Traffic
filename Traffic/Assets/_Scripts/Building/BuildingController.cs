using UnityEngine;

public class BuildingController : MonoBehaviour, ISelectableObject
{
    [Header("Renderers")]
    [SerializeField] private MeshRenderer _buildingRenderer;
    [SerializeField] private MeshRenderer _foundationRenderer;

    [Header("Building Waypoint Positions - Pedestrian")]
    [SerializeField] private Transform _insideBuildingWaypointPosition; // waypoint for being 'in' building
    [SerializeField] private Transform _doorWaypointPosition; // entry to the building
    [SerializeField] private Transform _entryExitPropertyWaypointPosition; // entry/exit to the whole property (will connect to the pavement)
    [SerializeField] private Transform _entryExitVehicleWaypointPosition; // entry/exit to the vehicle
    [SerializeField] private Transform[] _parkedToDoorWaypointPositions; // path from car to door
    [SerializeField] private Transform[] _propertyEntryToDoorWaypointPositions; // path from property entry/exit to door
    private WaypointNode _insideBuildingWaypoint;
    public WaypointNode InsideBuildingWaypoint => _insideBuildingWaypoint;
    private WaypointNode _doorWaypoint;
    public WaypointNode DoorWaypoint => _doorWaypoint;
    private WaypointNode _entryExitPropertyWaypoint;
    public WaypointNode EntryExitPropertyWaypoint => _entryExitPropertyWaypoint;
    private WaypointNode _entryExitVehicleWaypoint;
    public WaypointNode EntryExitVehicleWaypoint => _entryExitVehicleWaypoint;

    [Header("Building Waypoint Positions - Vehicle")]
    [SerializeField] private Transform _parkedWaypointPosition; // car stopping point
    [SerializeField] private Transform _vehicleEntryExitWaypointPosition; // entry/exit to the property
    [SerializeField] private Transform[] _vehicleEntryToParkedWaypointPositions; // path to parked waypoint from entry
    [SerializeField] private Transform _vehicleCellCheckWaypointPosition; // the position of the cell that is connected to when the car leaves
    private WaypointNode _parkedWaypoint;
    public WaypointNode ParkedWaypoint => _parkedWaypoint;
    private WaypointNode _vehicleEntryExitPropertyWaypoint;
    public WaypointNode VehicleEntryExitPropertyWaypoint => _vehicleEntryExitPropertyWaypoint;

    [Header("Selected Details")]
    public EntityId Id { get; private set; }
    private GridCell _cell;
    [SerializeField] private string _buildingName;
    public string BuildingName => _buildingName;
    [SerializeField] private Vector3 _cameraFocusOffset; // the offset to apply to the camera that looks at the building when it is selected
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] private Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    public void SetupBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        PedestrianWaypointManager.Instance.AddBuildingPedestrianWaypoints(cell, this, _insideBuildingWaypointPosition, _doorWaypointPosition, _entryExitPropertyWaypointPosition, _propertyEntryToDoorWaypointPositions, _entryExitVehicleWaypointPosition, _parkedToDoorWaypointPositions);
        RoadWaypointManager.Instance.AddBuildingVehicleWaypoints(cell, this, _parkedWaypointPosition, _vehicleEntryToParkedWaypointPositions, _vehicleEntryExitWaypointPosition, _vehicleCellCheckWaypointPosition);

        // add a person
        PedestrianController pc = AddPersonToBuilding();

        // add a vehicle
        VehicleController vc = AddVehicleToBuilding();

        // link the person to the vehicle
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.Driver,
            pc.Id, // Source: person
            vc.Id // Target: vehicle
        );
    }

    public void SetBuildingPedestrianWaypoints(WaypointNode insideBuilding, WaypointNode door, WaypointNode entryExit, WaypointNode vehicleEntryExit)
    {
        _insideBuildingWaypoint = insideBuilding;
        _doorWaypoint = door;
        _entryExitPropertyWaypoint = entryExit;
        _entryExitVehicleWaypoint = vehicleEntryExit;
    }

    public void SetBuildingVehicleWaypoints(WaypointNode parked, WaypointNode vehicleEntryExit)
    {
        _parkedWaypoint = parked;
        _vehicleEntryExitPropertyWaypoint = vehicleEntryExit;
    }

    public PedestrianController AddPersonToBuilding()
    {
        PedestrianController pc = PedestrianManager.Instance.AddAndRegisterPerson(_doorWaypoint);

        // link the person to the building
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.Resident,
            Id, // Source: Building
            pc.Id // Target: Person
        );

        return pc;
    }

    public VehicleController AddVehicleToBuilding()
    {
        VehicleController vc = VehicleManager.Instance.AddAndRegisterVehicle(_parkedWaypoint);

        // link the vehicle to the building
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.ParksAt,
            Id, // Source: Building
            vc.Id // Target: vehicle
        );

        return vc;
    }

    public MeshRenderer GetFoundationRenderer() => _foundationRenderer;

    public void SelectObject()
    {
        UIManager.Instance.LoadBuildingDetails(this);
    }
}
