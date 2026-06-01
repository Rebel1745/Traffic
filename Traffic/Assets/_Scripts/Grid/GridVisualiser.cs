using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RoadPlacementHandler))]
[RequireComponent(typeof(TrafficLightPlacementHandler))]
[RequireComponent(typeof(BuildingPlacementHandler))]
public class GridVisualiser : MonoBehaviour
{
    public static GridVisualiser Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private IPlacementHandler _activeHandler;
    private Dictionary<SimulationState, IPlacementHandler> _handlers;
    private SimulationState _currentState;

    private void Start()
    {
        _handlers = new Dictionary<SimulationState, IPlacementHandler>
        {
            { SimulationState.Roads,         GetComponent<RoadPlacementHandler>() },
            { SimulationState.TrafficLights, GetComponent<TrafficLightPlacementHandler>() },
            { SimulationState.Buildings, GetComponent<BuildingPlacementHandler>() }
        };

        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
        InputManager.OnLeftClickReleased += HandleLeftClickReleased;
        InputManager.OnRightClickPressed += HandleRightClickPressed;
        InputManager.OnMouseMoved += HandleMouseMoved;

        SimulationManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void Update() => _activeHandler?.OnUpdate();

    private void HandleStateChanged(GameStateContext newState)
    {
        if (newState.SimulationState == _currentState) return;

        _currentState = newState.SimulationState;
        _activeHandler?.OnExit();
        _handlers.TryGetValue(newState.SimulationState, out _activeHandler);
        _activeHandler?.OnEnter();
    }

    private void HandleLeftClickPressed(Vector2 screenPosition)
    {
        Vector3? hit = GridManager.Instance.GetGroundHitPoint();
        if (hit.HasValue) _activeHandler?.OnLeftClickPressed(hit.Value);
    }

    private void HandleLeftClickReleased(Vector2 screenPosition)
    {
        Vector3? hit = GridManager.Instance.GetGroundHitPoint();
        if (hit.HasValue) _activeHandler?.OnLeftClickReleased(hit.Value);
    }

    private void HandleRightClickPressed(Vector2 screenPosition)
    {
        Vector3? hit = GridManager.Instance.GetGroundHitPoint();
        if (hit.HasValue) _activeHandler?.OnRightClickPressed(hit.Value);
    }

    private void HandleMouseMoved(Vector2 screenPosition)
    {
        Vector3? hit = GridManager.Instance.GetGroundHitPoint();
        if (hit.HasValue) _activeHandler?.OnMouseMoved(hit.Value);
    }
}