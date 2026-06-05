using UnityEngine;

public class BuildingController : MonoBehaviour
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
    [SerializeField] private Transform[] _vehicleEntryToParkedWaypoints; // path to parked waypoint

    public void SetupBuilding(GridCell cell)
    {
        PedestrianWaypointManager.Instance.AddBuildingPedestrianWaypoints(cell, _insideBuildingWaypoint, _doorWaypoint, _entryExitPropertyWaypoint, _propertyEntryToDoorWaypoints, _entryExitVehicleWaypoint, _parkedToDoorWaypoints);
    }

    public MeshRenderer GetFoundationRenderer() => _foundationRenderer;

}
