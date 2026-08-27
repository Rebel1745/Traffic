using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianBrain : MonoBehaviour, IAgentBrain
{
    public void DecideNextAction(AgentController agent)
    {
        Debug.Log("Setting new goal queue");
        LinkedList<Goal> goalList = new();

        // start the new goal queue with a random wait time
        goalList.AddLast(new WaitGoal(Random.Range(2f, 7f)));

        // use a random number to determine what building to go to
        float rand = Random.Range(0f, 1f);
        Goal backupGoal = new WalkToWaypointGoal(PedestrianManager.Instance.GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway));
        WaypointNode targetNode = null;
        BuildingBase target = null;
        bool goHome = false;

        // figure out where we want to go
        if (rand < 0.1)
        {
            // go home
            target = BuildingManager.Instance.GetBuilding(RelationshipManager.Instance.GetHomeBuildingsForPerson(agent.Id).First());
            targetNode = target.InsideBuildingWaypoint;
            goHome = true;
        }
        else if (rand < 0.65f)
        {
            // go to a random store
            List<EntityId> stores = BuildingManager.Instance.GetBuildingsByFunction(BuildingFunction.Store);
            if (stores.Count > 0)
            {
                target = BuildingManager.Instance.GetBuilding(stores[Random.Range(0, stores.Count - 1)]);
                targetNode = target.InsideBuildingWaypoint;
            }
        }
        else
        {
            // go to random food or drink establishment
            int foodOrDrink = Random.Range(0, 2);
            if (foodOrDrink == 0)
            {
                List<EntityId> foodStores = BuildingManager.Instance.GetBuildingsByFunction(BuildingFunction.Food);
                if (foodStores.Count > 0)
                {
                    target = BuildingManager.Instance.GetBuilding(foodStores[Random.Range(0, foodStores.Count - 1)]);
                    targetNode = target.InsideBuildingWaypoint;
                }
            }
            else
            {
                List<EntityId> drinkStores = BuildingManager.Instance.GetBuildingsByFunction(BuildingFunction.Drink);
                if (drinkStores.Count > 0)
                {
                    target = BuildingManager.Instance.GetBuilding(drinkStores[Random.Range(0, drinkStores.Count - 1)]);
                    targetNode = target.InsideBuildingWaypoint;
                }
            }
        }

        // do we have a car?
        EntityId vehicleId = PedestrianManager.Instance.GetPersonsVehicle(agent.Id);
        bool hasVehicle = vehicleId.IsValid;

        if (!hasVehicle)
        {
            if (targetNode != null)
                goalList.AddLast(new WalkToWaypointGoal(targetNode));
            else goalList.AddLast(backupGoal);

            Debug.Log("No vehicle, walking");
        }
        else
        {
            AgentController vehicle = VehicleManager.Instance.GetVehicle(vehicleId);

            if (goHome)
            {
                // we have a car and have to go home
                if (agent.GetComponent<PedestrianMovement>().CurrentVehicle == null)
                    goalList.AddLast(new WalkToAndEnterVehicleGoal(vehicle: vehicle));

                goalList.AddLast(new DriveHomeGoal());
                goalList.AddLast(new ExitVehicleGoal());
                goalList.AddLast(new WalkToWaypointGoal(targetNode));
            }
            else
            {
                // do we need to use the car
                // get closest car park
                List<EntityId> carParks = BuildingManager.Instance.GetBuildingsByType(BuildingSubState.CarPark);
                EntityId closestCarParkId = EntityId.None;
                // where are we parked?
                EntityId currentBuildingId = RelationshipManager.Instance.GetBuildingFromParkingSpot(vehicle.Mover.CurrentWaypoint.Id).First();

                if (carParks.Count == 0)
                {
                    if (targetNode != null)
                        goalList.AddLast(new WalkToWaypointGoal(targetNode));
                    else goalList.AddLast(backupGoal);

                    Debug.Log("No car parks, walking");
                }
                else
                {
                    if (carParks.Count == 1)
                    {
                        closestCarParkId = carParks[0];
                    }
                    else
                        closestCarParkId = BuildingManager.Instance.GetClosestBuildingToPosition(carParks, targetNode.Position);

                    if (closestCarParkId.Equals(currentBuildingId))
                    {
                        if (targetNode != null)
                            goalList.AddLast(new WalkToWaypointGoal(targetNode));
                        else goalList.AddLast(backupGoal);

                        Debug.Log("We are already in the car park, walk");
                    }
                    else
                    {
                        BuildingCarPark carPark = BuildingManager.Instance.GetBuilding(closestCarParkId) as BuildingCarPark;

                        // get distance to the car
                        float distToCar = Utils.GetDistanceWithSetHeight(agent.Mover.CurrentWaypoint.Position, vehicle.Mover.CurrentWaypoint.Position, 0f);
                        // get distance from the car to the car park
                        float distToCarPark = Utils.GetDistanceWithSetHeight(vehicle.Mover.CurrentWaypoint.Position, carPark.PropertyEntryNode.Position, 0f);
                        // get distance from the car park to the target
                        float distFromCarParkToTarget = Utils.GetDistanceWithSetHeight(carPark.PropertyEntryNode.Position, targetNode.Position, 0f);
                        // get distance from the pedestrians current waypoint to the target
                        float distToTarget = Utils.GetDistanceWithSetHeight(agent.Mover.CurrentWaypoint.Position, targetNode.Position, 0f);
                        // comapre the distances (maybe weighted in some way). longer to drive? walk, otherwise drive
                        float distTotal = distToCar + distToCarPark + distFromCarParkToTarget;

                        Debug.Log($"Person: {agent.Mover.CurrentWaypoint.Position}. Car {vehicle.Mover.CurrentWaypoint.Position}. CP: {carPark.PropertyEntryNode.Position}. Target ({target.BuildingType}) {targetNode.Position}.");
                        Debug.Log($"To car: {distToCar}. To CP: {distToCarPark}. CP to target {distFromCarParkToTarget}. Total: {distTotal}. Person to target {distToTarget}.");

                        // check to see if the distance to drive is further than the distance to walk (multiplied by some factor to represent the extra time taken to walk)
                        if (distTotal > (distToTarget * 20f))
                        {
                            if (targetNode != null)
                                goalList.AddLast(new WalkToWaypointGoal(targetNode));
                            else goalList.AddLast(backupGoal);

                            Debug.Log("Quicker walking. Do it!");
                        }
                        else
                        {
                            if (targetNode != null)
                            {
                                // if we aren't already in a car, get in the car
                                if (agent.GetComponent<PedestrianMovement>().CurrentVehicle == null)
                                    goalList.AddLast(new WalkToAndEnterVehicleGoal(vehicle: vehicle));

                                // drive to the car park
                                goalList.AddLast(new DriveToWaypointGoal(carPark.PropertyEntryNode, "Driving to car park entrance"));
                                goalList.AddLast(new ParkInCarParkGoal(carPark));
                                goalList.AddLast(new WaitGoal(2f));
                                goalList.AddLast(new ExitVehicleGoal());
                                goalList.AddLast(new WalkToWaypointGoal(targetNode));

                                Debug.Log("We're driving to the car park and then walking to the target");
                            }
                            else goalList.AddLast(backupGoal);
                        }
                    }
                }
            }
        }

        agent.SetGoalList(goalList);
    }
}
