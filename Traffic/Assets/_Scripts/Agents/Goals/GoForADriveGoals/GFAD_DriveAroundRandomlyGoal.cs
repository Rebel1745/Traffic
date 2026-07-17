using UnityEngine;

public class GFAD_DriveAroundRandomlyGoal : Goal
{
    private AgentController _vehicle;
    private VehicleMovement _vehicleMovement;
    private System.Action _onCarArrived;
    private WaypointNode _target;

    public GFAD_DriveAroundRandomlyGoal(WaypointNode target, AgentController vehicle, string name) : base(target, name, requiresMovement: true)
    {
        _target = target;
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

            // Trigger the NEXT goal in the Pedestrian's queue
            // We manually call the Agent's method to start the next step
            // (e.g., DriveBackHomeGoal)
            _vehicle.AddGoalAfterCurrent(new GFAD_DriveHomeGoal(_target, _vehicle, agent, "Driving home"));
        };

        // Subscribe to the Car's event
        _vehicleMovement.OnArrivedAtDestination += _onCarArrived;

        WaypointNode randomNode = VehicleManager.Instance.FindValidTarget(_vehicleMovement.CurrentWaypoint, type: WaypointType.PetrolStationPump);
        string name = "Drive to random node at " + randomNode.Position;

        if (randomNode != null)
            _vehicle.AddGoal(new DriveToRandomGoal(randomNode, name));
        else Debug.LogWarning("No random location found");
    }
}
