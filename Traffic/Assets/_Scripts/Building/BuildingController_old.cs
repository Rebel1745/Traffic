using System.Linq;
using UnityEngine;

public class BuildingController_old : MonoBehaviour, ISelectableObject
{
    [Header("Renderers")]
    [SerializeField] private MeshRenderer _buildingRenderer;
    [SerializeField] private MeshRenderer _foundationRenderer;

    [Header("Building Waypoint Positions - Pedestrian")]
    [SerializeField] private Transform _insideBuildingWaypointPosition; // waypoint for being 'in' building
    [SerializeField] private Transform _doorWaypointPosition; // entry to the building
    [SerializeField] private Transform _entryExitPropertyWaypointPosition; // entry/exit to the whole property (will connect to the pavement)
    [SerializeField] private Transform[] _entryExitVehicleWaypointPositions; // entry/exit to the vehicle
    [SerializeField] private Transform[] _parkedToDoorWaypointPositions; // path from car to door
    [SerializeField] private Transform[] _propertyEntryToDoorWaypointPositions; // path from property entry/exit to door
    private WaypointNode _insideBuildingWaypoint;
    public WaypointNode InsideBuildingWaypoint => _insideBuildingWaypoint;
    private WaypointNode _doorWaypoint;
    public WaypointNode DoorWaypoint => _doorWaypoint;
    private WaypointNode _entryExitPropertyWaypoint;
    public WaypointNode EntryExitPropertyWaypoint => _entryExitPropertyWaypoint;
    private WaypointNode[] _entryExitVehicleWaypoints;
    public WaypointNode[] EntryExitVehicleWaypoint => _entryExitVehicleWaypoints;

    [Header("Building Waypoint Positions - Vehicle")]
    [SerializeField] private Transform[] _parkingSpotWaypointPositions; // car parking spots
    [SerializeField] private Transform _vehicleEntryExitWaypointPosition; // entry/exit to the property
    [SerializeField] private Transform[] _vehicleEntryToParkedWaypointPositions; // path to parked waypoint from entry
    [SerializeField] private Transform _vehicleCellCheckWaypointPosition; // the position of the cell that is connected to when the car leaves
    private WaypointNode[] _parkedWaypoints;
    public WaypointNode[] ParkedWaypoints => _parkedWaypoints;
    private WaypointNode _vehicleEntryExitPropertyWaypoint;
    public WaypointNode VehicleEntryExitPropertyWaypoint => _vehicleEntryExitPropertyWaypoint;

    [Header("Building Details")]
    public EntityId Id { get; private set; }
    private GridCell _cell;
    [SerializeField] private string _buildingName;
    public string BuildingName => _buildingName;

    [Header("Occupancy")]
    [SerializeField] private int _gridRows;
    [SerializeField] private int _gridCols;
    [SerializeField] private float _gridSize;
    [SerializeField] private int _maximumOccupancy = 4;
    private int _currentOccupancy = 0;
    private int _maximumVehicleOccupancy;
    private int _currentVehicleOccupancy = 0;

    [Header("Camera Focus Settings")]
    [SerializeField] private Vector3 _cameraFocusOffset; // the offset to apply to the camera that looks at the building when it is selected
    public Vector3 CameraFocusOffset => _cameraFocusOffset;
    [SerializeField] private Vector3 _cameraRotation;
    public Vector3 CameraRotation => _cameraRotation;

    public void SetupBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        // check that we have the same number of parking spots as entry exit vehicle points
        if (_entryExitVehicleWaypointPositions.Length != _parkingSpotWaypointPositions.Length)
        {
            Debug.LogError("We have different number of parking spots and vehicle entry exit points!!");
            return;
        }

        _maximumVehicleOccupancy = _parkingSpotWaypointPositions.Count();

        // PedestrianWaypointManager.Instance.AddBuildingPedestrianWaypoints(cell, this, _insideBuildingWaypointPosition, _doorWaypointPosition, _entryExitPropertyWaypointPosition, _propertyEntryToDoorWaypointPositions, _entryExitVehicleWaypointPositions, _parkedToDoorWaypointPositions);
        // RoadWaypointManager.Instance.AddBuildingVehicleWaypoints(cell, this, _parkingSpotWaypointPositions, _vehicleEntryToParkedWaypointPositions, _vehicleEntryExitWaypointPosition, _vehicleCellCheckWaypointPosition);

        // now that we have set up the buildings waypoints, we can update the parking spot waypoints with their corresponding vehicle entry / exit waypoints
        for (int i = 0; i < _entryExitVehicleWaypoints.Length; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.AlightsAt,
                _parkedWaypoints[i].Id,
                _entryExitVehicleWaypoints[i].Id
            );
            // _entryExitVehicleWaypoints[i].PairedParkingSpotWaypoint = _parkedWaypoints[i];
            // _entryExitVehicleWaypoints[i].PairedParkingSpotWaypointId = _parkedWaypoints[i].Id;

            // _parkedWaypoints[i].PairedParkingSpotWaypoint = _entryExitVehicleWaypoints[i];
            // _parkedWaypoints[i].PairedParkingSpotWaypointId = _entryExitVehicleWaypoints[i].Id;
        }

        // add a person
        AgentController pc = AddPersonToBuilding();

        // add a vehicle
        AgentController vc = AddVehicleToBuilding();

        // link the person to the vehicle
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.Driver,
            pc.Id, // Source: person
            vc.Id // Target: vehicle
        );
    }

    public void SetBuildingPedestrianWaypoints(WaypointNode insideBuilding, WaypointNode door, WaypointNode entryExit, WaypointNode[] vehicleEntryExit)
    {
        _insideBuildingWaypoint = insideBuilding;
        _doorWaypoint = door;
        _entryExitPropertyWaypoint = entryExit;
        _entryExitVehicleWaypoints = vehicleEntryExit;
    }

    public void SetBuildingVehicleWaypoints(WaypointNode[] parked, WaypointNode vehicleEntryExit)
    {
        _parkedWaypoints = parked;
        _vehicleEntryExitPropertyWaypoint = vehicleEntryExit;
    }

    public AgentController AddPersonToBuilding()
    {
        if (_currentOccupancy >= _maximumOccupancy) return null;

        Vector3 spawnPosition = GetSpawnPositionForPerson(_doorWaypoint.Position);

        AgentController pc = PedestrianManager.Instance.AddAndRegisterPerson(_doorWaypoint, spawnPosition);

        // link the person to the building
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.Resident,
            Id, // Source: Building
            pc.Id // Target: Person
        );

        _currentOccupancy++;

        return pc;
    }

    private Vector3 GetSpawnPositionForPerson(Vector3 origin)
    {
        if (_currentOccupancy >= _maximumOccupancy) return Vector3.zero;

        // 1. Calculate current grid coordinates based on occupancy index
        // Occupancy 0 -> (0,0), Occupancy 1 -> (1,0), etc.
        int colIndex = _currentOccupancy % _gridCols;
        int rowIndex = _currentOccupancy / _gridCols;

        // 2. Convert grid index to world offset
        // We subtract half the total width/height to center the grid on the origin
        float totalWidth = _gridCols * _gridSize;
        float totalDepth = _gridRows * _gridSize;

        float xOffset = (colIndex * _gridSize) - (totalWidth / 2f) + (_gridSize / 2f);
        float zOffset = (rowIndex * _gridSize) - (totalDepth / 2f) + (_gridSize / 2f);

        // 3. Apply to origin
        // Note: In Unity, +X is right, +Z is forward. 
        return new Vector3(origin.x + xOffset, origin.y, origin.z - zOffset);
    }

    public AgentController AddVehicleToBuilding()
    {
        if (_currentVehicleOccupancy >= _maximumVehicleOccupancy) return null;

        WaypointNode parkingSpot = _parkedWaypoints[_parkedWaypoints.Length - 1];

        AgentController vc = VehicleManager.Instance.AddAndRegisterVehicle(parkingSpot);

        // link the vehicle to the building
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.HomeBuilding,
            Id, // Source: Building
            vc.Id // Target: vehicle
        );

        // link the parking spot to the vehicle
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.HomeParkingSpot,
            vc.Id,
            parkingSpot.Id
        );

        // link the parking spot to the vehicles current spot
        RelationshipManager.Instance.AddRelationship(
            RelationshipType.CurrentParkingSpot,
            vc.Id,
            parkingSpot.Id
        );

        _currentVehicleOccupancy++;

        return vc;
    }

    public MeshRenderer GetFoundationRenderer() => _foundationRenderer;

    public void SelectObject()
    {
        // UIManager.Instance.LoadBuildingDetails(this);
    }
}
