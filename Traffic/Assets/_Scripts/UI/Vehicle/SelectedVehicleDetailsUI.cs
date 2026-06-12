using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedVehicleDetailsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;

    [Header(("Vehicle Name"))]
    [SerializeField] private TMP_Text _vehicleNameText;
    [SerializeField] private Button _editVehicleNameButton;
    [SerializeField] private TMP_InputField _vehicleNameInput;
    [SerializeField] private Button _updateVehicleNameButton;
    [SerializeField] private Button _cancelUpdateVehicleNameButton;

    public void LoadVehicle(VehicleController vehicle)
    {
        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(vehicle.transform, vehicle.CameraFocusOffset, vehicle.CameraRotation);

        // setup Vehicle naming
        _vehicleNameText.text = vehicle.VehicleName;
        _vehicleNameInput.text = vehicle.VehicleName;
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
