using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ButtonGroup : MonoBehaviour
{
    [SerializeField] private List<ToggleButton> _buttons;
    [SerializeField] private bool _hideOnStartup = true;

    [Header("Animation")]
    [SerializeField] private bool _isVertical = true; // True = vertical, False = horizontal
    [SerializeField] private float _animationDuration = 0.2f;
    [SerializeField] private float _staggerDelay = 0.05f;
    [SerializeField] private float _slideDistance = 50f; // Distance to slide from

    public event Action<ToggleButton> OnSelectionChanged;

    private ToggleButton _activeButton;

    private void Start()
    {
        foreach (ToggleButton button in _buttons)
            button.SetGroup(this);

        // If _buttons are visible on startup, just show them without animation
        if (!_hideOnStartup)
        {
            foreach (ToggleButton button in _buttons)
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
        for (int i = 0; i < _buttons.Count; i++)
        {
            ToggleButton button = _buttons[i];
            RectTransform rect = button.GetComponent<RectTransform>();

            // Kill any existing tweens on this button
            rect.DOKill(complete: true);

            Vector2 startOffset = _isVertical
                ? new Vector2(0, -_slideDistance)
                : new Vector2(-_slideDistance, 0);

            button.gameObject.SetActive(true);
            rect.anchoredPosition += startOffset;

            rect.DOAnchorPos(rect.anchoredPosition - startOffset, _animationDuration)
                .SetDelay(i * _staggerDelay)
                .SetEase(Ease.OutCubic);
        }
    }

    public Sequence AnimateButtonsOut()
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < _buttons.Count; i++)
        {
            ToggleButton button = _buttons[i];
            RectTransform rect = button.GetComponent<RectTransform>();

            rect.DOKill(complete: true);

            Vector2 endOffset = _isVertical
                ? new Vector2(0, -_slideDistance)
                : new Vector2(-_slideDistance, 0);

            // Add animation to sequence
            sequence.Insert(i * _staggerDelay,
                rect.DOAnchorPos(rect.anchoredPosition + endOffset, _animationDuration)
                    .SetEase(Ease.InCubic));
        }

        // After all animations complete, hide _buttons
        sequence.OnComplete(() =>
        {
            foreach (ToggleButton button in _buttons)
            {
                button.gameObject.SetActive(false);
                RectTransform rect = button.GetComponent<RectTransform>();
                Vector2 endOffset = _isVertical
                    ? new Vector2(0, -_slideDistance)
                    : new Vector2(-_slideDistance, 0);
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
        foreach (ToggleButton button in _buttons)
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
        if (_buttons.Count == 0) return null;

        if (includeInactive) return _buttons[0];

        foreach (ToggleButton button in _buttons)
        {
            if (button.gameObject.activeInHierarchy) return button;
        }

        return null;
    }

    public List<ToggleButton> GetAllButtons() => _buttons;
    public float GetAnimationDuration() => _animationDuration;
    public float GetStaggerDelay() => _staggerDelay;
}