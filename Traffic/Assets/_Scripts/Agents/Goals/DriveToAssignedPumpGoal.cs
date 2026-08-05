using System.Collections.Generic;
using UnityEngine;

public class DriveToAssignedPumpGoal : Goal
{
    private AgentController _vehicle;
    private VehicleMovement _vm;
    private AgentController _agent;
    private BuildingPetrolStation _petrolStation;

    public DriveToAssignedPumpGoal(BuildingPetrolStation petrolStation, string goalName)
    {
        GoalName = goalName;
        _petrolStation = petrolStation;
    }

    public override void Initialise(AgentController agent)
    {
        _agent = agent;
        _vehicle = agent.GetComponent<PedestrianMovement>().CurrentVehicle;

        _vm = _vehicle.GetComponent<VehicleMovement>();

        _petrolStation.GetNextAvailablePump(out WaypointNode availablePump, out WaypointNode alightWaypoint, out WaypointNode fillUpWaypoint);

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

        // we are now diving towards the pump, lets create the goals that will let us fill up the car and pay for it (in reverse order)
        agent.AddGoalAfterCurrent(new EnterVehicleGoal(_vehicle));
        agent.AddGoalAfterCurrent(new WalkToWaypointGoal(alightWaypoint, "Walking back to the car"));
        agent.AddGoalAfterCurrent(new WaitGoal(2f));
        agent.AddGoalAfterCurrent(new WalkToWaypointGoal(_petrolStation.InsideBuildingWaypoint, "Going inside the petrol station"));
        agent.AddGoalAfterCurrent(new WaitGoal(2f));
        agent.AddGoalAfterCurrent(new WalkToWaypointGoal(fillUpWaypoint, "Walking to the fill up waypoint node"));
        agent.AddGoalAfterCurrent(new ExitVehicleGoal());
    }

    public override void OnArrived(AgentController agent)
    {
        _vm.OnArrivedAtDestination -= OnVehicleArrived;
    }

    private void OnVehicleArrived()
    {
        _vehicle.transform.eulerAngles = new Vector3(0, Utils.SnapToClosestCardinalDirection(_vehicle.transform.eulerAngles.y), 0);

        if (_agent != _vehicle) _agent.OnMovementFinished();
    }
}
