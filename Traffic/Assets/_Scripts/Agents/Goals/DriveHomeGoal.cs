using UnityEngine;

public class DriveHomeGoal : Goal
{
    public DriveHomeGoal(string goalName = "Drive home")
    {
        GoalName = goalName;
    }

    public override void Initialise(AgentController agent)
    {
        // get home building
        EntityId buildingId = PedestrianManager.Instance.GetHomeBuilding(agent.Id);
        if (!buildingId.IsValid) Debug.LogError("Home building not found");

        BuildingBase bb = BuildingManager.Instance.GetBuilding(buildingId);

        WaypointNode frontDoorWaypoint = null;

        if (bb is BuildingHouse house)
            frontDoorWaypoint = house.DoorWaypoint;
        else Debug.LogError("Building is not a house");

        AgentController vehicle = agent.GetComponent<PedestrianMovement>().CurrentVehicle;

        // what is the cars home parking spot?
        EntityId homeSpotId = VehicleManager.Instance.GetVehiclesHomeSpotId(vehicle.Id);

        if (!homeSpotId.IsValid) Debug.LogError("HomeSpot not found");

        WaypointNode homeNode = RoadWaypointManager.Instance.GetWaypointFromId(homeSpotId);

        agent.AddGoalAfterCurrent(new DriveToWaypointGoal(homeNode, "Drive home"));
        agent.OnMovementFinished();
    }

    public override void OnArrived(AgentController agent)
    {

    }
}
