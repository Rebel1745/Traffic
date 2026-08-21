using UnityEngine;

public class BuildingStoreRoadside : BuildingBase
{
    [SerializeField] private Transform _insideBuildingPosition;
    [SerializeField] private Transform _buildingEntrancePosition;
    [SerializeField] private Transform _propertyEntrancePosition;
    [SerializeField] private Transform _checkCellPosition;

    private WaypointNode _insideBuildingWaypoint;
    public WaypointNode InsideBuildingWaypoint => _insideBuildingWaypoint;
    private WaypointNode _buildingEntranceWaypoint;
    public WaypointNode BuildingEntranceWaypoint => _buildingEntranceWaypoint;
    private WaypointNode _propertyEntranceWaypoint;
    public WaypointNode PropertyEntranceWaypoint => _propertyEntranceWaypoint;

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        InitialisePedestrianWaypoints();
    }

    public override void LoadBuilding(EntityId entityId, GridCell cell)
    {
        // seems to be the same as the initialise function, may as well just call it
        InitialiseBuilding(entityId, cell);
    }

    private void InitialisePedestrianWaypoints()
    {
        PedestrianWaypointManager.Instance.AddStoreRoadsidePedestrianWaypoints(
            _cell,
            _insideBuildingPosition,
            _buildingEntrancePosition,
            _propertyEntrancePosition,
            _checkCellPosition,
            out _insideBuildingWaypoint,
            out _buildingEntranceWaypoint,
            out _propertyEntranceWaypoint
        );
    }
}
