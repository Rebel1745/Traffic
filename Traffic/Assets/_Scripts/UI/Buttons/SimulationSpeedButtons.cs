using UnityEngine;
using UnityEngine.UI;

public class SimulationSpeedButtons : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _doubleSpeedButton;
    [SerializeField] private Button _tripleSpeedButton;

    void Start()
    {
        _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        _playButton.onClick.AddListener(OnPlayButtonClicked);
        _doubleSpeedButton.onClick.AddListener(OnDoubleSpeedButtonClicked);
        _tripleSpeedButton.onClick.AddListener(OnTripleButtonClicked);

        OnPlayButtonClicked();
    }

    private void OnPauseButtonClicked()
    {
        SimulationManager.Instance.SetSimulationSpeed(0f);
        SetButtonsActive(true);
        SetButtonActive(_pauseButton, false);
    }

    private void OnPlayButtonClicked()
    {
        SimulationManager.Instance.SetSimulationSpeed(1f);
        SetButtonsActive(true);
        SetButtonActive(_playButton, false);
    }

    private void OnDoubleSpeedButtonClicked()
    {
        SimulationManager.Instance.SetSimulationSpeed(2f);
        SetButtonsActive(true);
        SetButtonActive(_doubleSpeedButton, false);
    }

    private void OnTripleButtonClicked()
    {
        SimulationManager.Instance.SetSimulationSpeed(3f);
        SetButtonsActive(true);
        SetButtonActive(_tripleSpeedButton, false);
    }

    private void SetButtonsActive(bool active)
    {
        SetButtonActive(_pauseButton, active);
        SetButtonActive(_playButton, active);
        SetButtonActive(_doubleSpeedButton, active);
        SetButtonActive(_tripleSpeedButton, active);
    }

    private void SetButtonActive(Button button, bool active)
    {
        button.enabled = active;
    }
}
