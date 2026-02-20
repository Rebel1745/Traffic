using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Called by UI buttons in the Inspector
    public void OnPlaceRoadsButtonClicked()
    {
        SimulationManager.Instance.SetState(SimulationState.PlacingRoads);
    }

    public void OnPlaceTrafficLightsButtonClicked()
    {
        SimulationManager.Instance.SetState(SimulationState.PlacingTrafficLights);
    }

    public void OnPlaceBuildingsButtonClicked()
    {
        SimulationManager.Instance.SetState(SimulationState.PlacingBuildings);
    }

    public void OnSpawnVehicleButtonClicked()
    {
        // Only spawn if in running state
        if (SimulationManager.Instance.IsInState(SimulationState.Running))
        {
            VehicleSpawner.Instance.SpawnVehicle();
        }
    }

    public void OnPauseButtonClicked()
    {
        SimulationManager.Instance.TogglePause();
    }

    public void OnReturnToRunningButtonClicked()
    {
        SimulationManager.Instance.ReturnToRunning();
    }

    public void OnSimulationSpeedChanged(float speed)
    {
        SimulationManager.Instance.SetSimulationSpeed(speed);
    }
}