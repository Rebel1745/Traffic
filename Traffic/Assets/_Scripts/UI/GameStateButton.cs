using UnityEngine;
using UnityEngine.UI;

public class GameStateButton : MonoBehaviour
{
    [Header("Main State")][SerializeField] private SimulationState simulationState;
    [SerializeField] private bool setSimulationState = false;

    [Header("Sub States")][SerializeField] private RoadSubState roadSubState;
    [SerializeField] private VehicleSubState vehicleSubState;
    [SerializeField] private TrafficLightSubState trafficLightSubState;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (setSimulationState)
        {
            SimulationManager.Instance.SetSimulationState(simulationState);
            return;
        }

        // Defer sub-state change to next frame so main state sets first
        StartCoroutine(SetSubStateNextFrame());
    }

    private System.Collections.IEnumerator SetSubStateNextFrame()
    {
        yield return null; // Wait one frame

        switch (SimulationManager.Instance.CurrentState.SimulationState)
        {
            case SimulationState.Roads:
                SimulationManager.Instance.SetRoadSubState(roadSubState);
                break;
            case SimulationState.Vehicles:
                SimulationManager.Instance.SetVehicleSubState(vehicleSubState);
                break;
            case SimulationState.TrafficLights:
                SimulationManager.Instance.SetTrafficLightSubState(trafficLightSubState);
                break;
        }
    }
}