using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedBuildingDetailsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;

    [Header(("Building Name"))]
    [SerializeField] private TMP_Text _buildingNameText;
    [SerializeField] private Button _editBuildingNameButton;
    [SerializeField] private TMP_InputField _buildingNameInput;
    [SerializeField] private Button _updateBuildingNameButton;
    [SerializeField] private Button _cancelUpdateBuildingNameButton;

    public void LoadBuilding(BuildingController building)
    {
        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(building.transform.position, building.CameraFocusOffset, building.CameraRotation);

        // setup building naming
        _buildingNameText.text = building.BuildingName;
        _buildingNameInput.text = building.BuildingName;
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
}
