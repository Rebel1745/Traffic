using System;
using UnityEngine;

public class BuildingPetrolStation : BuildingBase
{
    [Header("Vehicle Waypoints")]
    [SerializeField] private Transform _cellCheckEntry;
    [SerializeField] private Transform _propertyEntry;
    private WaypointNode _propertyEntryNode;
    public WaypointNode PropertyEntryNode => _propertyEntryNode;
    [SerializeField] private VehiclePumpDetails[] _pumps;
    private WaypointNode[] _pumpWaypoints;
    [SerializeField] private Transform _propertyExit;
    [SerializeField] private Transform _cellCheckExit;

    [Header("Pedestrian Waypoints")]
    [SerializeField] private Transform _insideBuildingPosition;
    [SerializeField] private Transform _frontDoorPosition;
    [SerializeField] private Transform _pointBeforeFrontDoorPosition; // the point the path to store of each pump converges
    [SerializeField] private PedestrianPumpDetails[] _pedestrianPumps;
    private WaypointNode _insideBuildingWaypoint;
    public WaypointNode InsideBuildingWaypoint => _insideBuildingWaypoint;
    private WaypointNode[] _alightWaypoints;
    private WaypointNode[] _fillUpWaypoints;

    private int _nextPumpIndex = 0;

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        InitialiseVehicleWaypoints();
        InitialisePedestrianWaypoints();

        SetupRelationships();
    }

    private void InitialiseVehicleWaypoints()
    {
        VehicleWaypointManager.Instance.AddPetrolStationVehicleWaypoints(
            _cell,
            _propertyEntry,
            _pumps,
            _propertyExit,
            _cellCheckEntry,
            _cellCheckExit,
            out _propertyEntryNode,
            out _pumpWaypoints
        );
    }

    private void InitialisePedestrianWaypoints()
    {
        PedestrianWaypointManager.Instance.AddPetrolStationPedestrianWaypoints(
            _cell,
            _insideBuildingPosition,
            _frontDoorPosition,
            _pointBeforeFrontDoorPosition,
            _pedestrianPumps,
            out _insideBuildingWaypoint,
            out _alightWaypoints,
            out _fillUpWaypoints
        );
    }

    private void SetupRelationships()
    {
        // add the relationship between the pump and the alight waypoint (i.e. where the person ends up when entering/exiting the vehicle)
        for (int i = 0; i < _pumpWaypoints.Length; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.AlightsAt,
                _pumpWaypoints[i].Id,
                _alightWaypoints[i].Id
            );
        }
    }

    public void GetNextAvailablePump(out WaypointNode pumpWaypoint, out WaypointNode alightWaypoint, out WaypointNode fillUpWaypoint)
    {
        int nextPump = _nextPumpIndex;

        _nextPumpIndex = (_nextPumpIndex + 1) % _pumpWaypoints.Length;

        pumpWaypoint = _pumpWaypoints[nextPump];
        alightWaypoint = _alightWaypoints[nextPump];
        fillUpWaypoint = _fillUpWaypoints[nextPump];
    }
}
