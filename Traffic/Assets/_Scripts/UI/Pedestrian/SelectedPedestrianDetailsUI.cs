using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedPedestrianDetailsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiPanel;
    private AgentController _agent;
    private PedestrianData _pedestrian;

    [Header(("Pedestrian Name"))]
    [SerializeField] private TMP_Text _pedestrianNameText;
    [SerializeField] private Button _editPedestrianNameButton;
    [SerializeField] private TMP_InputField _pedestrianNameInput;
    [SerializeField] private Button _updatePedestrianNameButton;
    [SerializeField] private Button _cancelUpdatePedestrianNameButton;

    [Header("Action Buttons")]
    [SerializeField] private Button _goToRandomWaypointButton;
    [SerializeField] private Button _goHomeButton;

    public void LoadPedestrian(AgentController agent, PedestrianData pedestrian)
    {
        _agent = agent;
        _pedestrian = pedestrian;

        uiPanel.SetActive(true);
        CameraFollow.Instance.SetFollowTarget(pedestrian.transform, agent.CameraFocusOffset, agent.CameraRotation);

        // setup Pedestrian naming
        _pedestrianNameText.text = pedestrian.FullName;
        _pedestrianNameInput.text = pedestrian.FullName;

        _goToRandomWaypointButton.onClick.RemoveAllListeners();
        _goToRandomWaypointButton.onClick.AddListener(OnGoToRandomWaypointClicked);
        _goHomeButton.onClick.RemoveAllListeners();
        _goHomeButton.onClick.AddListener(OnGoHomeClicked);
    }

    private void OnGoToRandomWaypointClicked()
    {
        PedestrianManager.Instance.GoToRandomWaypoint(_agent);
    }

    private void OnGoHomeClicked()
    {
        PedestrianManager.Instance.GoHome(_agent);
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
