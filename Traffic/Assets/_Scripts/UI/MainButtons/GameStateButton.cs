using UnityEngine;
using UnityEngine.UI;

public class GameStateButton : MonoBehaviour
{
    [Header("Main State")]
    [SerializeField] private SimulationState _simulationState;
    [SerializeField] private bool _setSimulationState = false;

    [Header("Sub States")]
    [SerializeField] private RoadSubState _roadSubState;
    [SerializeField] private VehicleSubState _vehicleSubState;
    [SerializeField] private TrafficLightSubState _trafficLightSubState;
    [SerializeField] private PedestrianSubState _pedestrianSubState;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (_setSimulationState)
        {
            SimulationManager.Instance.SetSimulationState(_simulationState);
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
                SimulationManager.Instance.SetRoadSubState(_roadSubState);
                break;
            case SimulationState.Vehicles:
                SimulationManager.Instance.SetVehicleSubState(_vehicleSubState);
                break;
            case SimulationState.TrafficLights:
                SimulationManager.Instance.SetTrafficLightSubState(_trafficLightSubState);
                break;
            case SimulationState.Pedestrians:
                SimulationManager.Instance.SetPedestrianSubState(_pedestrianSubState);
                break;
        }
    }
}