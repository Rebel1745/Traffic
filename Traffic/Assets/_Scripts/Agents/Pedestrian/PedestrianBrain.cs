using UnityEngine;

public class PedestrianBrain : MonoBehaviour, IAgentBrain
{
    public void DecideNextAction(AgentController agent)
    {
        Debug.Log("Setting new goal of driving");
        PedestrianManager.Instance.DriveToPetrolStationAndHome(agent);
    }
}
