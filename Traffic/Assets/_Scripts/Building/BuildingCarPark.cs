using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BuildingCarPark : BuildingBase
{
    [Header("Vehicle Waypoints")]
    [SerializeField] private Transform _cellCheckEntry; // the cell that connects to the entrance of the car park
    [SerializeField] private Transform _entryWaypointPosition; // entrance to the car park
    [SerializeField] private Transform[] _entryRouteWaypointPositions; // driving route from entrance to the far end of the car park
    [SerializeField] private Transform[] _topParkingSpotWaypointPositions; // parking spots along the top of the car park (correspond to the entry route)
    [SerializeField] private Transform[] _exitRouteWaypointPositions; // driving route from the far end to the exit
    [SerializeField] private Transform[] _bottomParkingSpotWaypointPositions; // parking spots along the bottom of the car park (correspond to the exit route)
    [SerializeField] private Transform _exitWaypointPosition; // exit of the car park
    [SerializeField] private Transform _cellCheckExit; // the cell that connects to the exit of the car park

    private WaypointNode _entryWaypoint;
    private WaypointNode[] _entryRouteWaypoints;
    private WaypointNode[] _topParkingSpotWaypoints;
    private WaypointNode[] _exitRouteWaypoints;
    private WaypointNode[] _bottomParkingSpotWaypoints;
    private WaypointNode _exitWaypoint;
    private List<WaypointNode> _allParkingSpotWaypointList;

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        // Validate waypoint counts
        if (_entryRouteWaypointPositions.Length != _topParkingSpotWaypointPositions.Length)
        {
            Debug.LogError($"Car Park {Id} has mismatched parking spots and vehicle entry route points!");
            return;
        }
        if (_exitRouteWaypointPositions.Length <= _bottomParkingSpotWaypointPositions.Length)
        {
            Debug.LogError($"Car Park {Id} has more parking spots than vehicle exit route points!");
            return;
        }

        InitialiseVehicleWaypoints();
    }

    private void InitialiseVehicleWaypoints()
    {
        RoadWaypointManager.Instance.AddCarParkVehicleWaypoints(
            _cell,
            _cellCheckEntry,
            _entryWaypointPosition,
            _entryRouteWaypointPositions,
            _topParkingSpotWaypointPositions,
            _exitRouteWaypointPositions,
            _bottomParkingSpotWaypointPositions,
            _exitWaypointPosition,
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

    // TODO: figure out how to deal with empty and taken spaces
    public WaypointNode GetEmptyParkingSpot()
    {
        return _allParkingSpotWaypointList[Random.Range(0, _allParkingSpotWaypointList.Count - 1)];
    }
}
