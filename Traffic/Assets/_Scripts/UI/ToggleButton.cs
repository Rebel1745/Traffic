using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [Header("Colours")][SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.6f, 0.8f, 1f);

    [Header("Sub-Group")][SerializeField] private ButtonGroup subGroup;
    [SerializeField] private bool autoSelectFirstSubGroupButton = true;
    [SerializeField] private bool collapsible = false; // Toggle to enable collapsible behavior
    private SimulationState _previousState; // Store the state before opening menu

    private Button _button;
    private Image _image;
    private ButtonGroup _group;
    private bool _isExpanded = false; // Track if sub-group is expanded

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // If collapsible and already expanded, collapse instead of triggering group logic
        if (collapsible && _isExpanded)
        {
            Collapse();
            return;
        }

        // Save current state before opening menu (only for collapsible buttons)
        if (collapsible && !_isExpanded)
            _previousState = SimulationManager.Instance.CurrentState.SimulationState;

        _group?.OnButtonClicked(this);
    }

    public void SetActive(bool isActive)
    {
        if (_image == null || _button == null) return;

        // Only proceed if the button is active in the hierarchy
        if (!gameObject.activeInHierarchy) return;

        _image.color = isActive ? activeColor : normalColor;

        // Only disable button if it's not collapsible
        if (!collapsible)
            _button.interactable = !isActive;
    }

    public void ShowSubGroup(bool show)
    {
        if (subGroup == null) return;
        _isExpanded = show;

        if (show)
        {
            if (autoSelectFirstSubGroupButton)
            {
                // Find the first button BEFORE showing (pass includeInactive=true)
                ToggleButton firstSubButton = subGroup.GetFirstButton(includeInactive: true);

                // Now show all buttons
                subGroup.ShowHideButtons(true);

                // Now simulate the click
                firstSubButton?.SimulateClick();
            }
            else
                subGroup.ShowHideButtons(true);
        }
        else
        {
            subGroup.ShowHideButtons(false);
        }
    }

    public void SetGroup(ButtonGroup group)
    {
        _group = group;
    }

    private ToggleButton FindFirstSubButton()
    {
        if (subGroup == null) return null;
        return subGroup.GetFirstButton(false);
    }

    public void SimulateClick()
    {
        _button.onClick.Invoke();
    }

    private void Collapse()
    {
        // Recursively collapse all nested sub-buttons
        CollapseNestedMenus(0);

        _image.color = normalColor;
        _isExpanded = false;

        // Restore the previous state
        SimulationManager.Instance.SetSimulationState(_previousState);
    }

    private void CollapseNestedMenus(int depth = 0)
    {
        if (subGroup == null || depth > 10) return;

        List<ToggleButton> allButtons = subGroup.GetAllButtons();
        if (allButtons == null || allButtons.Count == 0) return;

        // First, recursively collapse all nested menus
        foreach (ToggleButton button in allButtons)
        {
            if (button == null) continue;
            button.CollapseNestedMenus(depth + 1);
        }

        // Then reset the active state of buttons in this level
        foreach (ToggleButton button in allButtons)
        {
            if (button == null) continue;
            if (!button.gameObject.activeInHierarchy) continue; // Skip inactive buttons
            Debug.Log(button.name + " deactivate");
            button.SetActive(false);
        }

        // Finally, hide the sub-group container
        subGroup.ShowHideButtons(false);
    }
}