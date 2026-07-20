using UnityEngine;

public class GFAD_WalkToAlightGoal : Goal
{
    private WaypointNode _target;

    public GFAD_WalkToAlightGoal(WaypointNode target, string name)
    {
        _target = target;
    }

    public override void Initialise(AgentController agent)
    {

    }

    public override void OnArrived(AgentController agent)
    {
        Debug.Log($"{agent.name} got to the alight point. Lets get in the car.");

        // get the vehicle
        EntityId vehicleId = PedestrianManager.Instance.GetPersonsVehicle(agent.Id);
        AgentController ac = VehicleManager.Instance.GetVehicle(vehicleId);
        VehicleMovement vm = ac.GetComponent<VehicleMovement>();

        string goalName = "Get into car " + vm.gameObject.name;

        agent.AddGoalAfterCurrent(new GFAD_EnterVehicleGoal(vm.CurrentWaypoint, ac, goalName));
    }
}
