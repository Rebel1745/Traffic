using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedBuildingDetailsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;
    private BuildingBase _buildingBase;

    [Header(("Building Name"))]
    [SerializeField] private TMP_Text _buildingNameText;
    [SerializeField] private Button _editBuildingNameButton;
    [SerializeField] private TMP_InputField _buildingNameInput;
    [SerializeField] private Button _updateBuildingNameButton;
    [SerializeField] private Button _cancelUpdateBuildingNameButton;

    [Header("Action Buttons")]
    [SerializeField] private Button _addPersonToBuildingButton;
    [SerializeField] private Button _addVehicleToBuildingButton;
    [SerializeField] private Button _addPersonAndVehicleToBuildingButton;

    public void LoadBuilding(BuildingBase building)
    {
        _buildingBase = building;

        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(building.transform.position, building.CameraFocusOffset, building.CameraRotation);

        // setup building naming
        _buildingNameText.text = building.BuildingName;
        _buildingNameInput.text = building.BuildingName;

        _addPersonToBuildingButton.onClick.RemoveAllListeners();
        _addPersonToBuildingButton.onClick.AddListener(OnAddPersonToBuilding);

        _addVehicleToBuildingButton.onClick.RemoveAllListeners();
        _addVehicleToBuildingButton.onClick.AddListener(OnAddVehicleToBuilding);

        _addPersonAndVehicleToBuildingButton.onClick.RemoveAllListeners();
        _addPersonAndVehicleToBuildingButton.onClick.AddListener(OnAddPersonAndVehicleToBuilding);
    }

    public void OnEditBuildingNameClicked()
    {
        _buildingNameInput.text = _buildingNameText.text;
        ShowHideBuildingNameButtons(true);
    }

    public void OnUpdateBuildingNameClicked()
    {
        _buildingNameText.text = _buildingNameInput.text;
        ShowHideBuildingNameButtons(false);
    }

    public void OnCancelUpdateBuildingNameClicked()
    {
        _buildingNameInput.text = _buildingNameText.text;
        ShowHideBuildingNameButtons(false);
    }

    private void ShowHideBuildingNameButtons(bool show)
    {
        _buildingNameInput.gameObject.SetActive(show);
        _updateBuildingNameButton.gameObject.SetActive(show);
        _cancelUpdateBuildingNameButton.gameObject.SetActive(show);
    }

    private void OnAddPersonToBuilding()
    {
        if (_buildingBase is BuildingHouse house)
            house.AddPersonToBuilding();
    }

    private void OnAddVehicleToBuilding()
    {
        if (_buildingBase is BuildingHouse house)
            house.AddVehicleToBuilding();
    }

    private void OnAddPersonAndVehicleToBuilding()
    {
        if (_buildingBase is BuildingHouse house)
            house.AddPersonAndVehicleToBuilding();
    }
}
