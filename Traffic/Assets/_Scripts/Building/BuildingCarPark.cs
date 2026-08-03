using System.Collections.Generic;
using System.Linq;
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
    public WaypointNode PropertyExitNode => _exitWaypoint;
    private List<WaypointNode> _allParkingSpotWaypointList;
    private Dictionary<WaypointNode, bool> _parkingSpotOccupation;

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

        SetupRelationships();
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

        SetupParkingSpots();
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

    private void SetupRelationships()
    {
        // add the relationship between the parking spot and the alight waypoint (i.e. where the person ends up when entering/exiting the vehicle)
        for (int i = 0; i < _allParkingSpotWaypointList.Count - 1; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.AlightsAt,
                _allParkingSpotWaypointList[i].Id,
                _allAlightWaypoints[i].Id
            );
        }

        // add the relationship between the parking spot and the building it is parking for
        for (int i = 0; i < _allParkingSpotWaypointList.Count - 1; i++)
        {
            RelationshipManager.Instance.AddRelationship(
                RelationshipType.BuildingParkingSpot,
                Id,
                _allParkingSpotWaypointList[i].Id
            );
        }
    }

    private void SetupParkingSpots()
    {
        _allParkingSpotWaypointList = new();
        _allParkingSpotWaypointList.AddRange(_topParkingSpotWaypoints);
        _allParkingSpotWaypointList.AddRange(_bottomParkingSpotWaypoints);

        _parkingSpotOccupation = new Dictionary<WaypointNode, bool>();

        // set all parking spots as unoccupied
        foreach (WaypointNode node in _topParkingSpotWaypoints)
        {
            _parkingSpotOccupation.Add(node, false);
        }
        foreach (WaypointNode node in _bottomParkingSpotWaypoints)
        {
            _parkingSpotOccupation.Add(node, false);
        }
    }

    public WaypointNode GetRandomEmptyParkingSpot()
    {
        List<WaypointNode> unoccupiedSpots = _parkingSpotOccupation
        .Where(kvp => !kvp.Value)
        .Select(kvp => kvp.Key)
        .ToList();

        return unoccupiedSpots[Random.Range(0, unoccupiedSpots.Count - 1)];
    }

    public WaypointNode GetFirstEmptyParkingSpot()
    {
        List<WaypointNode> unoccupiedSpots = _parkingSpotOccupation
        .Where(kvp => !kvp.Value)
        .Select(kvp => kvp.Key)
        .ToList();

        return unoccupiedSpots.First();
    }

    public void SetParkingSpotOccupation(WaypointNode spot, bool isOccupied)
    {
        _parkingSpotOccupation[spot] = isOccupied;
    }
}
