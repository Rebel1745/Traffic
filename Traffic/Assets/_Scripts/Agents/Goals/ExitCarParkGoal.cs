using System.Linq;
using UnityEngine;

public class ExitCarParkGoal : Goal
{
    private AgentController _agent;
    private WaypointNode _parkingSpot;
    private BuildingCarPark _carPark;

    public ExitCarParkGoal(string goalName = "Exiting car park")
    {

    }

    public override void Initialise(AgentController agent)
    {
        _agent = agent;

        // we have to update the car park to free up the parking space
        // first, we need the vehicle
        VehicleMovement vm = agent.GetComponent<PedestrianMovement>().CurrentVehicle.GetComponent<VehicleMovement>();

        if (vm == null)
        {
            Debug.LogError("Vehicle movement can not be found. Whaddup?");
            return;
        }

        _parkingSpot = vm.CurrentWaypoint;

        // get the building from the parking spot
        EntityId buildingId = RelationshipManager.Instance.GetBuildingFromParkingSpot(_parkingSpot.Id).First();

        if (!buildingId.IsValid)
        {
            Debug.LogError("Car park id not found from parking spot id");
            return;
        }

        // get the building so we can get the exit waypoint (only when we hit the exit will we free up the parking spot to avoid collisions)
        _carPark = BuildingManager.Instance.GetBuilding(buildingId) as BuildingCarPark;

        if (_carPark == null)
        {
            Debug.LogError("Car park not found");
        }

        _carPark.SetParkingSpotOccupation(_parkingSpot, false);

        agent.AddGoalAfterCurrent(new DriveToWaypointGoal(_carPark.PropertyExitNode, "Driving to the exit"));
        agent.OnMovementFinished();

        // start moving to the exit
        // vm.OnArrivedAtDestination += OnVehicleArrived;

        // List<WaypointNode> path = AStarPathfinder.FindPath(_parkingSpot, _carPark.PropertyExitNode);

        // if (path != null && path.Count > 0)
        // {
        //     vm.SetPath(path);
        // }
        // else
        // {
        //     Debug.LogWarning($"[{vm.gameObject.name}] No path found for goal: {GoalName}");
        //     // Handle failure (retry, pick new goal, etc.)
        // }
    }

    public override void OnArrived(AgentController agent)
    {

    }

    // private void OnVehicleArrived()
    // {
    //     // we have arrived at the exit, we can now set the parking spot as unoccupied
    //     _carPark.SetParkingSpotOccupation(_parkingSpot, false);
    //     _agent.OnMovementFinished();
    // }
}
