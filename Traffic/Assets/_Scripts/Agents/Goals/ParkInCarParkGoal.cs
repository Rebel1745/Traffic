using System.Collections.Generic;
using UnityEngine;

public class ParkInCarParkGoal : Goal
{
    private AgentController _vehicle;
    private VehicleMovement _vm;
    private AgentController _agent;
    private BuildingCarPark _carPark;

    public ParkInCarParkGoal(BuildingCarPark carPark, string goalName = "Parking car")
    {
        GoalName = goalName;
        _carPark = carPark;
    }

    public override void Initialise(AgentController agent)
    {
        _agent = agent;
        _vehicle = agent.GetComponent<PedestrianMovement>().CurrentVehicle;

        _vm = _vehicle.GetComponent<VehicleMovement>();

        // get a parking spot, random for now
        WaypointNode parkingSpot = _carPark.GetRandomEmptyParkingSpot();

        // set that spot as occupied so no other vehicles can take it
        _carPark.SetParkingSpotOccupation(parkingSpot, true);

        _vm.OnArrivedAtDestination += OnVehicleArrived;

        List<WaypointNode> path = AStarPathfinder.FindPath(_vehicle.Mover.CurrentWaypoint, parkingSpot);

        if (path != null && path.Count > 0)
        {
            _vehicle.Mover.SetPath(path);
        }
        else
        {
            Debug.LogWarning($"[{_vehicle.gameObject.name}] No path found for goal: {GoalName}");
            // Handle failure (retry, pick new goal, etc.)
        }

    }

    public override void OnArrived(AgentController agent)
    {
        _vm.OnArrivedAtDestination -= OnVehicleArrived;
    }

    private void OnVehicleArrived()
    {
        //OnArrived(_vehicle);
        if (_agent != _vehicle) _agent.OnMovementFinished();
    }
}
