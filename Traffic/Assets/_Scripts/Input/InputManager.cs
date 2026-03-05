using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static InputManager Instance { get; private set; }

    // Input Actions reference
    private GameInputActions _inputActions;

    // Mouse events
    public static event Action<Vector2> OnLeftClickPressed;
    public static event Action<Vector2> OnLeftClickReleased;
    public static event Action<Vector2> OnRightClickPressed;
    public static event Action<Vector2> OnRightClickReleased;
    public static event Action<Vector2> OnMiddleClickPressed;
    public static event Action<Vector2> OnMiddleClickReleased;
    public static event Action<Vector2> OnMouseMoved;

    // Camera events
    public static event Action<Vector2> OnCameraPan;
    public static event Action<float> OnCameraZoom;
    public static event Action<Vector2> OnCameraMove;

    // Properties for querying input state
    public Vector2 MousePosition { get; private set; }
    public bool IsLeftClickHeld { get; private set; }
    public bool IsRightClickHeld { get; private set; }
    public bool IsMiddleClickHeld { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize input actions
        _inputActions = new GameInputActions();
    }

    private void OnEnable()
    {
        // Enable action maps
        _inputActions.Gameplay.Enable();
        _inputActions.Camera.Enable();

        // Subscribe to Gameplay actions
        _inputActions.Gameplay.LeftClick.performed += OnLeftClickPerformed;
        _inputActions.Gameplay.LeftClick.canceled += OnLeftClickCanceled;
        _inputActions.Gameplay.RightClick.performed += OnRightClickPerformed;
        _inputActions.Gameplay.RightClick.canceled += OnRightClickCanceled;
        _inputActions.Gameplay.MiddleClick.performed += OnMiddleClickPerformed;
        _inputActions.Gameplay.MiddleClick.canceled += OnMiddleClickCanceled;
        _inputActions.Gameplay.MousePosition.performed += OnMousePositionChanged;

        // Subscribe to Camera actions
        _inputActions.Camera.Pan.performed += OnPanPerformed;
        _inputActions.Camera.Zoom.performed += OnZoomPerformed;
        _inputActions.Camera.Move.performed += OnMovePerformed;
        _inputActions.Camera.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        // Unsubscribe from Gameplay actions
        _inputActions.Gameplay.LeftClick.performed -= OnLeftClickPerformed;
        _inputActions.Gameplay.LeftClick.canceled -= OnLeftClickCanceled;
        _inputActions.Gameplay.RightClick.performed -= OnRightClickPerformed;
        _inputActions.Gameplay.RightClick.canceled -= OnRightClickCanceled;
        _inputActions.Gameplay.MiddleClick.performed -= OnMiddleClickPerformed;
        _inputActions.Gameplay.MiddleClick.canceled -= OnMiddleClickCanceled;
        _inputActions.Gameplay.MousePosition.performed -= OnMousePositionChanged;

        // Unsubscribe from Camera actions
        _inputActions.Camera.Pan.performed -= OnPanPerformed;
        _inputActions.Camera.Zoom.performed -= OnZoomPerformed;
        _inputActions.Camera.Move.performed -= OnMovePerformed;
        _inputActions.Camera.Move.canceled -= OnMoveCanceled;

        // Disable action maps
        _inputActions.Gameplay.Disable();
        _inputActions.Camera.Disable();
    }
    private void Update()
    {
        DoNotClickUIAndGameAtSameTime();

    }

    private void DoNotClickUIAndGameAtSameTime()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            _inputActions.Gameplay.Disable();
            _inputActions.Camera.Disable();
        }
        else
        {
            _inputActions.Gameplay.Enable();
            _inputActions.Camera.Enable();
        }
    }

    // Mouse input handlers
    private void OnLeftClickPerformed(InputAction.CallbackContext context)
    {
        IsLeftClickHeld = true;
        OnLeftClickPressed?.Invoke(MousePosition);
    }

    private void OnLeftClickCanceled(InputAction.CallbackContext context)
    {
        IsLeftClickHeld = false;
        OnLeftClickReleased?.Invoke(MousePosition);
    }

    private void OnRightClickPerformed(InputAction.CallbackContext context)
    {
        IsRightClickHeld = true;
        OnRightClickPressed?.Invoke(MousePosition);
    }

    private void OnRightClickCanceled(InputAction.CallbackContext context)
    {
        IsRightClickHeld = false;
        OnRightClickReleased?.Invoke(MousePosition);
    }

    private void OnMiddleClickPerformed(InputAction.CallbackContext context)
    {
        IsMiddleClickHeld = true;
        OnMiddleClickPressed?.Invoke(MousePosition);
    }

    private void OnMiddleClickCanceled(InputAction.CallbackContext context)
    {
        IsMiddleClickHeld = false;
        OnMiddleClickReleased?.Invoke(MousePosition);
    }

    private void OnMousePositionChanged(InputAction.CallbackContext context)
    {
        MousePosition = context.ReadValue<Vector2>();
        OnMouseMoved?.Invoke(MousePosition);
    }

    // Camera input handlers
    private void OnPanPerformed(InputAction.CallbackContext context)
    {
        Vector2 panDelta = context.ReadValue<Vector2>();
        OnCameraPan?.Invoke(panDelta);
    }

    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        float zoomValue = context.ReadValue<float>();
        OnCameraZoom?.Invoke(zoomValue);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveDirection = context.ReadValue<Vector2>();
        OnCameraMove?.Invoke(moveDirection);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        OnCameraMove?.Invoke(Vector2.zero);
    }

    // Utility method to get world position from mouse
    public Vector3 GetMouseWorldPosition(Camera camera, float zDistance = 0f)
    {
        Vector3 mousePos = MousePosition;
        mousePos.z = zDistance;
        return camera.ScreenToWorldPoint(mousePos);
    }
}