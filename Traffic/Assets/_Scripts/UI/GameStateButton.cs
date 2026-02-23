using UnityEngine;

public class GameStateButton : MonoBehaviour
{
    [SerializeField] private SimulationState state;

    private void Awake()
    {
        //GetComponent<ToggleButton>().Group.OnSelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(ToggleButton selected)
    {
        if (selected == GetComponent<ToggleButton>())
            SimulationManager.Instance.SetState(state);
    }
}