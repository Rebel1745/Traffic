using UnityEngine;

public class BuildingHouse : BuildingBase
{
    [Header("Pedestrian Waypoints")]
    [SerializeField] private Transform _insideBuildingWaypointPosition;
    [SerializeField] private Transform _doorWaypointPosition;
    [SerializeField] private Transform _entryExitPropertyWaypointPosition;
    [SerializeField] private Transform[] _entryExitVehicleWaypointPositions;
    [SerializeField] private Transform[] _doorToVehicleWaypointPositions;
    [SerializeField] private Transform[] _propertyEntryToDoorWaypointPositions;

    [Header("Vehicle Waypoints")]
    [SerializeField] private Transform[] _parkingSpotWaypointPositions;
    [SerializeField] private Transform _vehicleEntryExitWaypointPosition;
    [SerializeField] private Transform[] _vehicleEntryToParkedWaypointPositions;
    [SerializeField] private Transform _vehicleCellCheckWaypointPosition;

    [Header("Occupancy Settings")]
    [SerializeField] private int _gridRows = 2; // used in the layout of people when they are added to the house
    [SerializeField] private int _gridCols = 2; // it has nothing to do with the GridCells that make up the world
    [SerializeField] private float _gridSize = 1.0f;
    [SerializeField] private int _maximumOccupancy = 4;
    [SerializeField] private int _maximumVehicleOccupancy = 1;
    private int _currentOccupancy = 0;
    private int _currentVehicleOccupancy = 0;

    // Waypoint references (populated during initialization)
    private WaypointNode _insideBuildingWaypoint;
    private WaypointNode _doorWaypoint;
    public WaypointNode DoorWaypoint => _doorWaypoint;
    private WaypointNode _entryExitPropertyWaypoint;
    private WaypointNode[] _entryExitVehicleWaypoints;
    private WaypointNode[] _parkingSpotWaypoints;
    private WaypointNode _vehicleEntryExitPropertyWaypoint;

    private int MaximumOccupancy => _maximumOccupancy;
    private int MaximumVehicleOccupancy => _maximumVehicleOccupancy;
    private int CurrentOccupancy => _currentOccupancy;
    private int CurrentVehicleOccupancy => _currentVehicleOccupancy;

    private int GetGridRows() => _gridRows;
    private int GetGridCols() => _gridCols;
    private float GetGridSize() => _gridSize;

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        // Validate waypoint counts
        if (_entryExitVehicleWaypointPositions.Length != _parkingSpotWaypointPositions.Length)
        {
            Debug.LogError($"House {Id} has mismatched parking spots and vehicle entry/exit points!");
            return;
        }

        // Initialise waypoints through managers
        InitialisePedestrianWaypoints();
        InitialiseVehicleWaypoints();

        SetupRelationships();

        PopulateBuilding();
    }

    private void InitialisePedestrianWaypoints()
    {
        PedestrianWaypointManager.Instance.AddHousePedestrianWaypoints(
            _cell,
            _insideBuildingWaypointPosition,
            _doorWaypointPosition,
            _entryExitPropertyWaypointPosition,
            _propertyEntryToDoorWaypointPositions,
            _entryExitVehicleWaypointPositions,
            _doorToVehicleWaypointPositions,
            out _insideBuildingWaypoint,
            out _doorWaypoint,
            out _entryExitPropertyWaypoint,
            out _entryExitVehicleWaypoints
        );
    }

    private void InitialiseVehicleWaypoints()
    {
        RoadWaypointManager.Instance.AddHouseVehicleWaypoints(
            _cell,
            _parkingSpotWaypointPositions,
            _vehicleEntryToParkedWaypointPositions,
            _vehicleEntryExitWaypointPosition,
            _vehicleCellCheckWaypointPosition,
            out _parkingSpotWaypoints,
            out _vehicleEntryExitPropertyWaypoint
        );
    }

    private void SetupRelationships()
    {
        // add the relationship between the parking spot and the alight waypoint (i.e. where the person ends up when entering/exiting the vehicle)
        for (int i = 0; i < _entryExitVehicleWaypoints.Length; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.AlightsAt,
                _parkingSpotWaypoints[i].Id,
                _entryExitVehicleWaypoints[i].Id
            );
        }

        // add the relationship between the parking spot and the building it is parking for
        for (int i = 0; i < _parkingSpotWaypoints.Length; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.BuildingParkingSpot,
                Id,
                _parkingSpotWaypoints[i].Id
            );
        }
    }

    public void PopulateBuilding()
    {
        if (_currentOccupancy >= MaximumOccupancy) return;

        AddPersonAndVehicleToBuilding();

        // // Add resident
        // AgentController person = AddPersonToBuilding();

        // // Add vehicle
        // AgentController vehicle = AddVehicleToBuilding();

        // // Link person to vehicle
        // if (person != null && vehicle != null)
        // {
        //     RelationshipManager.Instance.AddRelationship(
        //         RelationshipType.Driver,
        //         person.Id,
        //         vehicle.Id
        //     );
        // }
    }

    public AgentController AddPersonToBuilding()
    {
        if (_currentOccupancy >= MaximumOccupancy) return null;

        Vector3 spawnPosition = GetSpawnPositionForPerson(_doorWaypoint.Position);
        AgentController person = PedestrianManager.Instance.AddAndRegisterPerson(_doorWaypoint, spawnPosition);

        RelationshipManager.Instance.AddRelationship(
            RelationshipType.Resident,
            Id,
            person.Id
        );

        _currentOccupancy++;
        return person;
    }

    public AgentController AddVehicleToBuilding()
    {
        if (_currentVehicleOccupancy >= MaximumVehicleOccupancy) return null;

        WaypointNode parkingSpot = _parkingSpotWaypoints[_currentVehicleOccupancy];
        AgentController vehicle = VehicleManager.Instance.AddAndRegisterVehicle(parkingSpot);

        RelationshipManager.Instance.AddRelationship(
            RelationshipType.HomeBuilding,
            Id,
            vehicle.Id
        );

        RelationshipManager.Instance.AddRelationship(
            RelationshipType.HomeParkingSpot,
            vehicle.Id,
            parkingSpot.Id
        );

        _currentVehicleOccupancy++;
        return vehicle;
    }

    public void AddPersonAndVehicleToBuilding()
    {
        if (_currentOccupancy >= MaximumOccupancy || _currentVehicleOccupancy >= MaximumVehicleOccupancy)
            return;

        AgentController person = AddPersonToBuilding();
        AgentController vehicle = AddVehicleToBuilding();

        // Link person to vehicle
        if (person != null && vehicle != null)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.Driver,
                person.Id,
                vehicle.Id
            );
        }
    }

    private Vector3 GetSpawnPositionForPerson(Vector3 origin)
    {
        int colIndex = CurrentOccupancy % GetGridCols();
        int rowIndex = CurrentOccupancy / GetGridCols();

        float totalWidth = GetGridCols() * GetGridSize();
        float totalDepth = GetGridRows() * GetGridSize();

        float xOffset = (colIndex * GetGridSize()) - (totalWidth / 2f) + (GetGridSize() / 2f);
        float zOffset = (rowIndex * GetGridSize()) - (totalDepth / 2f) + (GetGridSize() / 2f);

        return new Vector3(origin.x + xOffset, origin.y, origin.z - zOffset);
    }
}
