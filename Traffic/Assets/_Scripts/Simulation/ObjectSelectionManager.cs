using System;
using UnityEngine;

public class ObjectSelectionManager : MonoBehaviour
{
    public static ObjectSelectionManager Instance;

    [SerializeField] private LayerMask _whatIsSelectable;

    private bool _isSelectablityActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SimulationManager.Instance.OnStateChanged += OnStateChanged;
        InputManager.OnLeftClickPressed += OnLeftClickPressed;
        InputManager.OnRightClickPressed += OnRightClickPressed;
        // TODO: Add a "Deselect on Right Click" or "Deselect on Empty Click"
    }

    private void OnDestroy()
    {
        SimulationManager.Instance.OnStateChanged -= OnStateChanged;
        InputManager.OnLeftClickPressed -= OnLeftClickPressed;
    }

    private void OnStateChanged(GameStateContext context)
    {
        if (context.SimulationState == SimulationState.Running || context.SimulationState == SimulationState.Paused)
            _isSelectablityActive = true;
        else _isSelectablityActive = false;
    }

    private void OnLeftClickPressed(Vector2 screenPos)
    {
        if (!_isSelectablityActive) return;

        // 1. Raycast to the "Interactable" layer
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _whatIsSelectable))
        {
            GameObject hitObj = hit.collider.gameObject;

            // 2. Check if it implements ISelectableObject
            if (hitObj.TryGetComponent(out ISelectableObject selectable))
            {
                selectable.SelectObject();
            }
            else
            {
                // Clicked something that isn't selectable, clear selection
                ClearSelection();
            }
        }
        else
        {
            // Clicked empty space
            ClearSelection();
        }
    }

    private void OnRightClickPressed(Vector2 vector)
    {
        ClearSelection();
    }

    private void ClearSelection()
    {
        Debug.Log("Selection cleared");
    }
}
