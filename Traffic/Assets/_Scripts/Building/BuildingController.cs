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

        PedestrianWaypointManager.Instance.AddBuildingPedestrianWaypoints(cell, _insideBuildingWaypoint, _doorWaypoint, _entryExitPropertyWaypoint, _propertyEntryToDoorWaypoints, _entryExitVehicleWaypoint, _parkedToDoorWaypoints);
        RoadWaypointManager.Instance.AddBuildingVehicleWaypoints(cell, _parkedWaypoint, _vehicleEntryToParkedWaypoints, _vehicleEntryExitWaypoint, _vehicleCellCheckWaypoint);

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

    public PedestrianController AddPersonToBuilding()
    {
        PedestrianController pc = PedestrianManager.Instance.AddAndRegisterPerson(PedestrianWaypointManager.Instance.GetWaypointNodeFromPositionInCell(_cell, _doorWaypoint.position));

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
        VehicleController vc = VehicleManager.Instance.AddAndRegisterVehicle(RoadWaypointManager.Instance.GetWaypointNodeFromPositionInCell(_cell, _parkedWaypoint.position));

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
