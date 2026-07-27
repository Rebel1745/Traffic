using System;
using UnityEngine;

public class BuildingPetrolStation : BuildingBase
{
    [Header("Waypoints")]
    [SerializeField] private Transform _cellCheckEntry;
    [SerializeField] private Transform _propertyEntry;
    private WaypointNode _propertyEntryNode;
    public WaypointNode PropertyEntryNode => _propertyEntryNode;
    [SerializeField] private PumpDetails[] _pumps;
    private WaypointNode[] _pumpNodes;
    [SerializeField] private Transform _propertyExit;
    [SerializeField] private Transform _cellCheckExit;

    private int _nextPumpIndex = 0;

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        InitialiseVehicleWaypoints();
    }

    private void InitialiseVehicleWaypoints()
    {
        RoadWaypointManager.Instance.AddPetrolStationVehicleWaypoints(
            _cell,
            _propertyEntry,
            _pumps,
            _propertyExit,
            _cellCheckEntry,
            _cellCheckExit,
            out _propertyEntryNode,
            out _pumpNodes
        );
    }

    public WaypointNode GetNextAvailablePump()
    {
        int nextPump = _nextPumpIndex;

        _nextPumpIndex = (_nextPumpIndex + 1) % _pumpNodes.Length;

        return _pumpNodes[nextPump];
    }
}
