using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BuildingCarPark : BuildingBase
{
    [Header("Vehicle Waypoints")]
    [SerializeField] private Transform _cellCheckEntry; // the cell that connects to the entrance of the car park
    [SerializeField] private Transform _entryPosition; // entrance to the car park
    [SerializeField] private Transform[] _entryRoutePositions; // driving route from entrance to the far end of the car park
    [SerializeField] private Transform[] _topParkingSpotPositions; // parking spots along the top of the car park (correspond to the entry route)
    [SerializeField] private Transform[] _exitRoutePositions; // driving route from the far end to the exit
    [SerializeField] private Transform[] _bottomParkingSpotPositions; // parking spots along the bottom of the car park (correspond to the exit route)
    [SerializeField] private Transform _exitPosition; // exit of the car park
    [SerializeField] private Transform _cellCheckExit; // the cell that connects to the exit of the car park

    private WaypointNode _entryWaypoint;
    public WaypointNode PropertyEntryNode => _entryWaypoint;
    private WaypointNode[] _entryRouteWaypoints;
    private WaypointNode[] _topParkingSpotWaypoints;
    private WaypointNode[] _exitRouteWaypoints;
    private WaypointNode[] _bottomParkingSpotWaypoints;
    private WaypointNode _exitWaypoint;
    private List<WaypointNode> _allParkingSpotWaypointList;

    [Header("Pedestrian Waypoints")]
    [SerializeField] private Transform _pedestrianEntryPosition;
    [SerializeField] private Transform _pedestrianExitPosition;
    [SerializeField] private Transform[] _topAlightPositions;
    [SerializeField] private Transform[] _topPedestrianRoutePositions;
    [SerializeField] private Transform[] _bottomAlightPositions;
    [SerializeField] private Transform[] _bottomPedestrianRoutePositions;
    private List<WaypointNode> _allAlightWaypoints;

    private WaypointNode _pedestrianEntryWaypoint;
    private WaypointNode _pedestrianExitWaypoint;
    private WaypointNode[] _topAlightWaypoints;
    private WaypointNode[] _topPedestrianRouteWaypoints;
    private WaypointNode[] _bottomAlightWaypoints;
    private WaypointNode[] _bottomPedestrianRouteWaypoints;

    private int test;

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        // Validate waypoint counts
        if (_entryRoutePositions.Length != _topParkingSpotPositions.Length)
        {
            Debug.LogError($"Car Park {Id} has mismatched parking spots and vehicle entry route points!");
            return;
        }
        if (_exitRoutePositions.Length <= _bottomParkingSpotPositions.Length)
        {
            Debug.LogError($"Car Park {Id} has more parking spots than vehicle exit route points!");
            return;
        }

        InitialiseVehicleWaypoints();
        InitialisePedestrianWaypoints();

        SetupVehicleRelationships();
    }

    private void InitialiseVehicleWaypoints()
    {
        RoadWaypointManager.Instance.AddCarParkVehicleWaypoints(
            _cell,
            _cellCheckEntry,
            _entryPosition,
            _entryRoutePositions,
            _topParkingSpotPositions,
            _exitRoutePositions,
            _bottomParkingSpotPositions,
            _exitPosition,
            _cellCheckExit,
            out _entryWaypoint,
            out _entryRouteWaypoints,
            out _topParkingSpotWaypoints,
            out _exitRouteWaypoints,
            out _bottomParkingSpotWaypoints,
            out _exitWaypoint
        );

        _allParkingSpotWaypointList = new();
        _allParkingSpotWaypointList.AddRange(_topParkingSpotWaypoints);
        _allParkingSpotWaypointList.AddRange(_bottomParkingSpotWaypoints);
    }

    private void InitialisePedestrianWaypoints()
    {
        PedestrianWaypointManager.Instance.AddCarParkPedestrianWaypoints(
            _cell,
            _pedestrianEntryPosition,
            _topAlightPositions,
            _topPedestrianRoutePositions,
            _bottomAlightPositions,
            _bottomPedestrianRoutePositions,
            _pedestrianExitPosition,
            out _pedestrianEntryWaypoint,
            out _topAlightWaypoints,
            out _topPedestrianRouteWaypoints,
            out _bottomAlightWaypoints,
            out _bottomPedestrianRouteWaypoints,
            out _pedestrianExitWaypoint
        );

        _allAlightWaypoints = new();
        _allAlightWaypoints.AddRange(_topAlightWaypoints);
        _allAlightWaypoints.AddRange(_bottomAlightWaypoints);
    }

    private void SetupVehicleRelationships()
    {
        for (int i = 0; i < _allParkingSpotWaypointList.Count - 1; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.AlightsAt,
                _allParkingSpotWaypointList[i].Id,
                _allAlightWaypoints[i].Id
            );
        }
    }

    // TODO: figure out how to deal with empty and taken spaces
    public WaypointNode GetEmptyParkingSpot()
    {
        return _allParkingSpotWaypointList[Random.Range(0, _allParkingSpotWaypointList.Count - 1)];
    }
}
