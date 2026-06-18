using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedPedestrianDetailsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;
    private PedestrianController _pedestrianController;

    [Header(("Pedestrian Name"))]
    [SerializeField] private TMP_Text _pedestrianNameText;
    [SerializeField] private Button _editPedestrianNameButton;
    [SerializeField] private TMP_InputField _pedestrianNameInput;
    [SerializeField] private Button _updatePedestrianNameButton;
    [SerializeField] private Button _cancelUpdatePedestrianNameButton;

    [Header("Action Buttons")]
    [SerializeField] private Button _goToRandomWaypointButton;
    [SerializeField] private Button _goHomeButton;

    public void LoadPedestrian(PedestrianController pedestrian)
    {
        _pedestrianController = pedestrian;

        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(pedestrian.transform, pedestrian.CameraFocusOffset, pedestrian.CameraRotation);

        // setup Pedestrian naming
        _pedestrianNameText.text = pedestrian.PedestrianName;
        _pedestrianNameInput.text = pedestrian.PedestrianName;

        _goToRandomWaypointButton.onClick.AddListener(OnGoToRandomWaypointClicked);
        _goHomeButton.onClick.AddListener(OnGoHomeClicked);
    }

    private void OnGoToRandomWaypointClicked()
    {
        PedestrianManager.Instance.GoToRandomWaypoint(_pedestrianController);
    }

    private void OnGoHomeClicked()
    {
        PedestrianManager.Instance.GoHome(_pedestrianController);
    }

    public void OnEditPedestrianNameClicked()
    {
        _pedestrianNameInput.text = _pedestrianNameText.text;
        ShowHidePedestrianNameButtons(true);
    }

    public void OnUpdatePedestrianNameClicked()
    {
        _pedestrianNameText.text = _pedestrianNameInput.text;
        ShowHidePedestrianNameButtons(false);
    }

    public void OnCancelUpdatePedestrianNameClicked()
    {
        _pedestrianNameInput.text = _pedestrianNameText.text;
        ShowHidePedestrianNameButtons(false);
    }

    private void ShowHidePedestrianNameButtons(bool show)
    {
        _pedestrianNameInput.gameObject.SetActive(show);
        _updatePedestrianNameButton.gameObject.SetActive(show);
        _cancelUpdatePedestrianNameButton.gameObject.SetActive(show);
    }
}
