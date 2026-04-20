using UnityEngine;

public interface IPlacementHandler
{
    void OnEnter();
    void OnExit();
    void OnUpdate();
    void OnLeftClickPressed(Vector3 hitPoint);
    void OnLeftClickReleased(Vector3 hitPoint);
    void OnRightClickPressed(Vector3 hitPoint);
    void OnMouseMoved(Vector3 hitPoint);
}