using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [Header("Colours")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.6f, 0.8f, 1f);

    [Header("Sub-Group")]
    [SerializeField] private ButtonGroup subGroup; // This button's sub-buttons

    private Button _button;
    private Image _image;
    private ButtonGroup _group;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        _group?.OnButtonClicked(this);
    }

    public void SetActive(bool isActive)
    {
        _image.color = isActive ? activeColor : normalColor;
        _button.interactable = !isActive;
    }

    public void ShowSubGroup(bool show)
    {
        if (subGroup == null) return;

        subGroup.ShowHideButtons(show);

        // If activating, auto-select the first visible sub-button
        if (show)
        {
            ToggleButton firstSubButton = FindFirstSubButton();
            firstSubButton?.OnClicked();
        }
    }

    public void SetGroup(ButtonGroup group)
    {
        _group = group;
    }

    private ToggleButton FindFirstSubButton()
    {
        if (subGroup == null) return null;

        // Find all ToggleButton components in the sub-group container
        return subGroup.GetFirstButton(false);
    }
}