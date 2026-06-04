using UnityEngine;

public class BuildingController : MonoBehaviour
{
    [Header("Building Waypoints")]
    [SerializeField] private Transform _insideBuildingWaypoint; // waypoint for being 'in' building
    [SerializeField] private Transform _doorWaypoint; // entry to the building
    [SerializeField] private Transform _parkedWaypoint; // car stopping point
    [SerializeField] private Transform[] _entryToParkedWaypoints; // path to parked waypoint
    [SerializeField] private Transform[] _parkedToDoorWaypoints; // path from car to door

    public void SetupBuilding(GridCell cell)
    {
        PedestrianWaypointManager.Instance.AddBuildingPedestrianWaypoints(cell, _insideBuildingWaypoint, _doorWaypoint, _parkedToDoorWaypoints);
    }

}
