using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianBrain : MonoBehaviour, IAgentBrain
{
    public void DecideNextAction(AgentController agent)
    {
        LinkedList<Goal> goalList = new();
        goalList.AddLast(new WaitGoal(Random.Range(2f, 7f)));

        float rand = Random.Range(0f, 1f);
        Goal backupGoalWalking = new WalkToWaypointGoal(PedestrianManager.Instance.GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway));
        Goal backupGoalDriving = new DriveToWaypointGoal(VehicleManager.Instance.GetRandomVehicleWaypoint(WaypointType.Entry));
        BuildingBase target = null;
        WaypointNode targetNode = null;
        bool goHome = false;

        // Target Selection
        if (rand < 0.1f)
        {
            goHome = true;
            List<EntityId> homeId = RelationshipManager.Instance.GetHomeBuildingsForPerson(agent.Id);
            if (homeId.Count == 0 || !homeId.First().IsValid)
            {
                goalList.AddLast(backupGoalWalking);
                agent.SetGoalList(goalList);
                return;
            }
            target = BuildingManager.Instance.GetBuilding(homeId.First());
            targetNode = target?.InsideBuildingWaypoint;
        }
        else if (rand < 0.65f)
        {
            List<EntityId> stores = BuildingManager.Instance.GetBuildingsByFunction(BuildingFunction.Store);
            if (stores.Count > 0) target = BuildingManager.Instance.GetBuilding(stores[Random.Range(0, stores.Count)]);
        }
        else
        {
            List<EntityId> food = BuildingManager.Instance.GetBuildingsByFunction(BuildingFunction.Food);
            List<EntityId> drink = BuildingManager.Instance.GetBuildingsByFunction(BuildingFunction.Drink);
            List<EntityId> options = new List<EntityId>(food).Concat(drink).ToList();
            if (options.Count > 0) target = BuildingManager.Instance.GetBuilding(options[Random.Range(0, options.Count)]);
        }
        if (target != null) targetNode = target.InsideBuildingWaypoint;

        // Vehicle Handling
        EntityId vehicleId = PedestrianManager.Instance.GetPersonsVehicle(agent.Id);
        bool hasVehicle = vehicleId.IsValid;

        if (!hasVehicle)
        {
            goalList.AddLast(new WalkToWaypointGoal(targetNode));
            return;
        }

        AgentController vehicle = VehicleManager.Instance.GetVehicle(vehicleId);

        if (targetNode == null)
        {
            goalList.AddLast(backupGoalWalking);
            agent.SetGoalList(goalList);
            return;
        }

        if (goHome)
        {
            if (agent.GetComponent<PedestrianMovement>().CurrentVehicle == null)
                goalList.AddLast(new WalkToAndEnterVehicleGoal(vehicle: vehicle));
            goalList.AddLast(new DriveHomeGoal());
            goalList.AddLast(new ExitVehicleGoal());
            goalList.AddLast(new WalkToWaypointGoal(targetNode));
            agent.SetGoalList(goalList);
            return;
        }

        // Car Park Logic
        var carParks = BuildingManager.Instance.GetBuildingsByType(BuildingSubState.CarPark);
        if (carParks.Count == 0)
        {
            goalList.AddLast(new WalkToWaypointGoal(targetNode));
            agent.SetGoalList(goalList);
            return;
        }

        EntityId closestCarParkId = carParks.Count == 1 ? carParks[0] : BuildingManager.Instance.GetClosestBuildingToPosition(carParks, targetNode.Position);
        BuildingCarPark carPark = BuildingManager.Instance.GetBuilding(closestCarParkId) as BuildingCarPark;
        List<EntityId> currentBuildingId = RelationshipManager.Instance.GetBuildingFromParkingSpot(vehicle.Mover.CurrentWaypoint.Id);

        if (currentBuildingId.Count == 0 || !currentBuildingId.First().IsValid || closestCarParkId.Equals(currentBuildingId))
        {
            goalList.AddLast(new WalkToWaypointGoal(targetNode));
            agent.SetGoalList(goalList);
            return;
        }

        // Distance Comparison
        float distToCar = CalculateDistance(agent.Mover.CurrentWaypoint, vehicle.Mover.CurrentWaypoint);
        float distToCarPark = CalculateDistance(vehicle.Mover.CurrentWaypoint, carPark.PropertyEntryNode);
        float distFromCarParkToTarget = CalculateDistance(carPark.PropertyEntryNode, targetNode);
        float distTotal = distToCar + distToCarPark + distFromCarParkToTarget;
        float distToTarget = CalculateDistance(agent.Mover.CurrentWaypoint, targetNode);

        if (distTotal > distToTarget * 20f)
        {
            goalList.AddLast(new WalkToWaypointGoal(targetNode));
        }
        else
        {
            if (agent.GetComponent<PedestrianMovement>().CurrentVehicle == null)
                goalList.AddLast(new WalkToAndEnterVehicleGoal(vehicle: vehicle));
            goalList.AddLast(new DriveToWaypointGoal(carPark.PropertyEntryNode, "Driving to car park entrance"));
            goalList.AddLast(new ParkInCarParkGoal(carPark));
            goalList.AddLast(new WaitGoal(2f));
            goalList.AddLast(new ExitVehicleGoal());
            goalList.AddLast(new WalkToWaypointGoal(targetNode));
        }

        agent.SetGoalList(goalList);
    }

    float CalculateDistance(WaypointNode from, WaypointNode to)
    {
        return Utils.GetDistanceWithSetHeight(from.Position, to.Position, 0f);
    }
}
