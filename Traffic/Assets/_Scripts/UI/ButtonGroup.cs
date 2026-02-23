using System;
using System.Collections.Generic;
using UnityEngine;

public class ButtonGroup : MonoBehaviour
{
    [SerializeField] private List<ToggleButton> buttons;
    [SerializeField] private bool hideOnStartup = true;

    public event Action<ToggleButton> OnSelectionChanged;

    private ToggleButton _activeButton;

    private void Awake()
    {
        foreach (ToggleButton button in buttons)
        {
            button.SetGroup(this);
        }

        ShowHideButtons(!hideOnStartup);
    }

    public void ShowHideButtons(bool show)
    {
        foreach (ToggleButton button in buttons)
        {
            button.gameObject.SetActive(show);
        }
    }

    public void OnButtonClicked(ToggleButton clicked)
    {
        // Deactivate previous
        if (_activeButton != null)
            _activeButton.SetActive(false);

        // Activate new
        _activeButton = clicked;
        _activeButton.SetActive(true);

        // Show the clicked button's sub-group
        _activeButton.ShowSubGroup(true);

        // Hide any other button's sub-group
        foreach (ToggleButton button in buttons)
        {
            if (button != _activeButton)
                button.ShowSubGroup(false);
        }

        OnSelectionChanged?.Invoke(clicked);
    }

    public void Deselect()
    {
        if (_activeButton != null)
        {
            _activeButton.SetActive(false);
            _activeButton.ShowSubGroup(false);
            _activeButton = null;
        }
    }

    public ToggleButton GetFirstButton(bool includeInactive)
    {
        if (buttons.Count == 0) return null;

        if (includeInactive) return buttons[0];

        foreach (ToggleButton button in buttons)
        {
            if (button.gameObject.activeInHierarchy) return button;
        }

        return null;
    }
}