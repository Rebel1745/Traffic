using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedVehicleDetailsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;
    private AgentController _agent;
    private VehicleData _vehicle;

    [Header(("Vehicle Name"))]
    [SerializeField] private TMP_Text _vehicleNameText;
    [SerializeField] private Button _editVehicleNameButton;
    [SerializeField] private TMP_InputField _vehicleNameInput;
    [SerializeField] private Button _updateVehicleNameButton;
    [SerializeField] private Button _cancelUpdateVehicleNameButton;

    [Header("Action Buttons")]
    [SerializeField] private Button _goToRandomWaypointButton;
    [SerializeField] private Button _goHomeButton;

    public void LoadVehicle(AgentController agent, VehicleData vehicle)
    {
        _agent = agent;
        _vehicle = vehicle;

        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(vehicle.transform, agent.CameraFocusOffset, agent.CameraRotation);

        // setup Vehicle naming
        _vehicleNameText.text = vehicle.VehicleName;
        _vehicleNameInput.text = vehicle.VehicleName;

        _goToRandomWaypointButton.onClick.RemoveAllListeners();
        _goToRandomWaypointButton.onClick.AddListener(OnGoToRandomWaypointClicked);
        _goHomeButton.onClick.RemoveAllListeners();
        _goHomeButton.onClick.AddListener(OnGoHomeClicked);
    }

    private void OnGoToRandomWaypointClicked()
    {
        VehicleManager.Instance.GoToRandomWaypoint(_agent);
    }

    private void OnGoHomeClicked()
    {
        VehicleManager.Instance.GoHome(_agent);
    }

    public void OnEditVehicleNameClicked()
    {
        _vehicleNameInput.text = _vehicleNameText.text;
        ShowHideVehicleNameButtons(true);
    }

    public void OnUpdateVehicleNameClicked()
    {
        _vehicleNameText.text = _vehicleNameInput.text;
        ShowHideVehicleNameButtons(false);
    }

    public void OnCancelUpdateVehicleNameClicked()
    {
        _vehicleNameInput.text = _vehicleNameText.text;
        ShowHideVehicleNameButtons(false);
    }

    private void ShowHideVehicleNameButtons(bool show)
    {
        _vehicleNameInput.gameObject.SetActive(show);
        _updateVehicleNameButton.gameObject.SetActive(show);
        _cancelUpdateVehicleNameButton.gameObject.SetActive(show);
    }
}
