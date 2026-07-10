using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GFAD_DriveHomeGoal : Goal
{
    private AgentController _vehicle;
    private AgentController _person;
    private VehicleMovement _vehicleMovement;
    private System.Action _onCarArrived;

    public GFAD_DriveHomeGoal(WaypointNode target, AgentController vehicle, AgentController person, string name) : base(target, name, requiresMovement: true)
    {
        _person = person;
        _vehicle = vehicle;
        _vehicleMovement = vehicle.GetComponent<VehicleMovement>();
    }

    public override void OnArrived(AgentController agent)
    {
        _onCarArrived = () =>
        {
            Debug.Log("Car arrived! Resuming Pedestrian Goals.");

            // Unsubscribe to the Car's event
            _vehicleMovement.OnArrivedAtDestination -= _onCarArrived;

            EntityId currentSpotId = VehicleManager.Instance.GetVehiclesCurrentSpotId(agent.Id);
            EntityId alightId = PedestrianManager.Instance.GetAlightWaypointId(currentSpotId);
            WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightId);

            PedestrianManager.Instance.ReParentPedestrian(_person);
            _person.ShowHideAgent(true);

            _person.InterruptAndAddGoal(new GFAD_ExitVehicleGoal(alightWaypoint, _person, "Exit Car"));
        };

        // Subscribe to the Car's event
        _vehicleMovement.OnArrivedAtDestination += _onCarArrived;

        EntityId waypointId = RelationshipManager.Instance.GetHomeParkingSpot(_vehicle.Id).FirstOrDefault();

        if (!waypointId.IsValid) Debug.LogError("Home building not found");

        WaypointNode parkingSpot = RoadWaypointManager.Instance.GetWaypointFromId(waypointId);

        List<WaypointNode> newPath = AStarPathfinder.FindPath(_vehicleMovement.CurrentWaypoint, parkingSpot);

        if (newPath == null || newPath.Count == 0) Debug.LogError("Path to home node not found");

        string name = "Driven home to " + parkingSpot.Position;

        _vehicle.InterruptAndAddGoal(new ParkAtHomeGoal(parkingSpot, name));
    }
}
