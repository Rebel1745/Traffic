using System;
using UnityEngine;

public class InputTesting : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to events
        InputManager.OnLeftClickPressed += HandleLeftClick;
        InputManager.OnLeftClickReleased += HandleLeftRelease;
        InputManager.OnRightClickPressed += HandleRightClick;
        InputManager.OnRightClickReleased += HandleRightRelease;

        InputManager.OnCameraMove += HandleCameraMove;
        InputManager.OnCameraPan += HandleCameraPan;
        InputManager.OnCameraZoom += HandleCameraZoom;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        InputManager.OnLeftClickPressed -= HandleLeftClick;
        InputManager.OnLeftClickReleased -= HandleLeftRelease;
        InputManager.OnRightClickPressed -= HandleRightClick;
        InputManager.OnRightClickReleased -= HandleRightRelease;

        InputManager.OnCameraMove -= HandleCameraMove;
        InputManager.OnCameraPan -= HandleCameraPan;
        InputManager.OnCameraZoom -= HandleCameraZoom;
    }

    private void HandleLeftClick(Vector2 screenPosition)
    {
        Debug.Log($"Left click at screen position: {screenPosition}");

        // Get world position if needed
        Vector3 worldPos = InputManager.Instance.GetMouseWorldPosition(Camera.main);
        Debug.Log($"World position: {worldPos}");
    }

    private void HandleLeftRelease(Vector2 screenPosition)
    {
        Debug.Log($"Left click released at: {screenPosition}");
    }

    private void HandleRightClick(Vector2 screenPosition)
    {
        Debug.Log($"Right click at screen position: {screenPosition}");

        // Get world position if needed
        Vector3 worldPos = InputManager.Instance.GetMouseWorldPosition(Camera.main);
        Debug.Log($"World position: {worldPos}");
    }

    private void HandleRightRelease(Vector2 screenPosition)
    {
        Debug.Log($"Right click released at: {screenPosition}");
    }

    private void HandleCameraMove(Vector2 vector)
    {
        Debug.Log($"Camera moved by {vector}");
    }

    private void HandleCameraPan(Vector2 vector)
    {
        Debug.Log($"Camera panned by {vector}");
    }

    private void HandleCameraZoom(float amount)
    {
        Debug.Log($"Camera zoomed by {amount}");
    }
}
