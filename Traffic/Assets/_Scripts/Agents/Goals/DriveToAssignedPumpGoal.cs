using System.Collections.Generic;
using UnityEngine;

public class DriveToAssignedPumpGoal : Goal
{
    private AgentController _vehicle;
    private VehicleMovement _vm;
    private AgentController _agent;
    private BuildingPetrolStation _petrolStation;

    public DriveToAssignedPumpGoal(AgentController vehicle, BuildingPetrolStation petrolStation, string goalName)
    {
        GoalName = goalName;
        _vehicle = vehicle;
        _petrolStation = petrolStation;

        _vm = _vehicle.GetComponent<VehicleMovement>();
    }

    public override void Initialise(AgentController agent)
    {
        _agent = agent;

        WaypointNode availablePump = _petrolStation.GetNextAvailablePump();

        _vm.OnArrivedAtDestination += OnVehicleArrived;

        List<WaypointNode> path = AStarPathfinder.FindPath(_vehicle.Mover.CurrentWaypoint, availablePump);

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
