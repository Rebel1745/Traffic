using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ButtonGroup : MonoBehaviour
{
    [SerializeField] private List<ToggleButton> buttons;
    [SerializeField] private bool hideOnStartup = true;

    [Header("Animation")]
    [SerializeField] private bool isVertical = true; // True = vertical, False = horizontal
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private float staggerDelay = 0.05f;
    [SerializeField] private float slideDistance = 50f; // Distance to slide from

    public event Action<ToggleButton> OnSelectionChanged;

    private ToggleButton _activeButton;

    private void Start()
    {
        foreach (ToggleButton button in buttons)
            button.SetGroup(this);

        // If buttons are visible on startup, just show them without animation
        if (!hideOnStartup)
        {
            foreach (ToggleButton button in buttons)
                button.gameObject.SetActive(true);
        }
    }

    public void ShowHideButtons(bool show)
    {
        if (show)
            AnimateButtonsIn();
        else
            AnimateButtonsOut();
    }

    private void AnimateButtonsIn()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            ToggleButton button = buttons[i];
            RectTransform rect = button.GetComponent<RectTransform>();

            // Kill any existing tweens on this button
            rect.DOKill(complete: true);

            Vector2 startOffset = isVertical
                ? new Vector2(0, -slideDistance)
                : new Vector2(-slideDistance, 0);

            button.gameObject.SetActive(true);
            rect.anchoredPosition += startOffset;

            rect.DOAnchorPos(rect.anchoredPosition - startOffset, animationDuration)
                .SetDelay(i * staggerDelay)
                .SetEase(Ease.OutCubic);
        }
    }

    public Sequence AnimateButtonsOut()
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < buttons.Count; i++)
        {
            ToggleButton button = buttons[i];
            RectTransform rect = button.GetComponent<RectTransform>();

            rect.DOKill(complete: true);

            Vector2 endOffset = isVertical
                ? new Vector2(0, -slideDistance)
                : new Vector2(-slideDistance, 0);

            // Add animation to sequence
            sequence.Insert(i * staggerDelay,
                rect.DOAnchorPos(rect.anchoredPosition + endOffset, animationDuration)
                    .SetEase(Ease.InCubic));
        }

        // After all animations complete, hide buttons
        sequence.OnComplete(() =>
        {
            foreach (ToggleButton button in buttons)
            {
                button.gameObject.SetActive(false);
                RectTransform rect = button.GetComponent<RectTransform>();
                Vector2 endOffset = isVertical
                    ? new Vector2(0, -slideDistance)
                    : new Vector2(-slideDistance, 0);
                rect.anchoredPosition -= endOffset;
            }
        });

        return sequence;
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

    public ToggleButton GetFirstButton(bool includeInactive = false)
    {
        if (buttons.Count == 0) return null;

        if (includeInactive) return buttons[0];

        foreach (ToggleButton button in buttons)
        {
            if (button.gameObject.activeInHierarchy) return button;
        }

        return null;
    }

    public List<ToggleButton> GetAllButtons() => buttons;
    public float GetAnimationDuration() => animationDuration;
    public float GetStaggerDelay() => staggerDelay;
}