using System;
using UnityEngine;

public class BuildingPetrolStation : BuildingBase
{
    // base class references
    protected override int MaximumOccupancy => 0;
    protected override int CurrentOccupancy => 0;

    private int _maximumVehicleOccupancy = 0;
    protected override int MaximumVehicleOccupancy => _maximumVehicleOccupancy;
    private int _currentVehicleOccupancy = 0;
    protected override int CurrentVehicleOccupancy => _currentVehicleOccupancy;

    [Header("Waypoints")]
    [SerializeField] private Transform _cellCheckEntry;
    [SerializeField] private Transform _propertyEntry;
    [SerializeField] private PumpDetails[] _pumps;
    [SerializeField] private Transform _propertyExit;
    [SerializeField] private Transform _cellCheckExit;

    public override AgentController AddPersonToBuilding()
    {
        throw new System.NotImplementedException();
    }

    public override AgentController AddVehicleToBuilding()
    {
        throw new System.NotImplementedException();
    }

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        _maximumVehicleOccupancy = _pumps.Length;

        InitialiseVehicleWaypoints();
    }

    private void InitialiseVehicleWaypoints()
    {
        RoadWaypointManager.Instance.AddPetrolStationVehicleWaypoints(_cell, _propertyEntry, _pumps, _propertyExit, _cellCheckEntry, _cellCheckExit);
    }

    public override void PopulateBuilding()
    {
        throw new System.NotImplementedException();
    }

    protected override int GetGridCols()
    {
        return 0;
    }

    protected override int GetGridRows()
    {
        return 0;
    }

    protected override float GetGridSize()
    {
        return 0;
    }
}
