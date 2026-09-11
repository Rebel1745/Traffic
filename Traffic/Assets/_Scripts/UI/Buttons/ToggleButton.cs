using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [Header("Colours")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _activeColor = new Color(0.6f, 0.8f, 1f);

    [Header("Sub-Group")]
    [SerializeField] private ButtonGroup _subGroup;
    [SerializeField] private bool _autoSelectFirstSubGroupButton = true;
    [SerializeField] private bool _collapsible = false; // Toggle to enable collapsible behavior
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
        if (_collapsible && _isExpanded)
        {
            Collapse();
            return;
        }

        // Save current state before opening menu (only for collapsible buttons)
        if (_collapsible && !_isExpanded)
            _previousState = SimulationManager.Instance.CurrentState.SimulationState;

        _group?.OnButtonClicked(this);
    }

    public void SetActive(bool isActive)
    {
        if (_image == null || _button == null) return;

        // Only proceed if the button is active in the hierarchy
        if (!gameObject.activeInHierarchy) return;

        _image.color = isActive ? _activeColor : _normalColor;

        // Only disable button if it's not collapsible
        if (_collapsible)
            StartCoroutine(UnDisableButton(isActive));
        else
            _button.interactable = !isActive;
    }

    private IEnumerator UnDisableButton(bool disable)
    {
        _button.interactable = !disable;

        yield return new WaitForSeconds(0.5f);

        _button.interactable = disable;
    }

    public void ShowSubGroup(bool show)
    {
        if (_subGroup == null) return;
        _isExpanded = show;

        if (show)
        {
            if (_autoSelectFirstSubGroupButton)
            {
                // Find the first button BEFORE showing (pass includeInactive=true)
                ToggleButton firstSubButton = _subGroup.GetFirstButton(includeInactive: true);

                // Now show all buttons
                _subGroup.ShowHideButtons(true);

                // Now simulate the click
                firstSubButton?.SimulateClick();
            }
            else
                _subGroup.ShowHideButtons(true);
        }
        else
        {
            _subGroup.ShowHideButtons(false);
        }
    }

    public void SetGroup(ButtonGroup group)
    {
        _group = group;
    }

    private ToggleButton FindFirstSubButton()
    {
        if (_subGroup == null) return null;
        return _subGroup.GetFirstButton(false);
    }

    public void SimulateClick()
    {
        _button.onClick.Invoke();
    }

    private void Collapse()
    {
        // disable then re-enable
        StartCoroutine(UnDisableButton(true));
        // Recursively collapse all nested sub-buttons
        CollapseNestedMenus(0);

        _image.color = _normalColor;
        _isExpanded = false;

        // Restore the previous state
        SimulationManager.Instance.SetSimulationState(_previousState);
    }

    private void CollapseNestedMenus(int depth = 0)
    {
        if (_subGroup == null || depth > 10) return;

        List<ToggleButton> allButtons = _subGroup.GetAllButtons();
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
            if (!button.gameObject.activeInHierarchy) continue;
            button.SetActive(false);
        }

        // Finally, animate out the sub-group container
        if (_subGroup != null)
        {
            _subGroup.AnimateButtonsOut();
        }
    }
}