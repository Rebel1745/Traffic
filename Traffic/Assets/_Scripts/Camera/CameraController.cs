using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Pan Settings")]
    [SerializeField] private float _panSpeed = 0.1f;
    [SerializeField] private bool _invertPan = true;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 100f;

    [Header("Move Settings")]
    [SerializeField] private float _moveSpeed = 20f;

    [Header("Boundary Settings")]
    [SerializeField] private bool _clampToBoundary = true;
    [SerializeField] private float _boundaryPadding = 5f;

    private Camera _mainCamera;
    private Vector2 _currentMoveDirection;
    private float _gridWidth;
    private float _gridHeight;
    private float _cellSize;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _mainCamera = Camera.main;

        // Get grid bounds for clamping
        _cellSize = GridManager.Instance.CellSize;
        _gridWidth = GridManager.Instance.GridWidth * _cellSize;
        _gridHeight = GridManager.Instance.GridHeight * _cellSize;
    }

    private void OnEnable()
    {
        InputManager.OnCameraPan += HandlePan;
        InputManager.OnCameraZoom += HandleZoom;
        InputManager.OnCameraMove += HandleMove;
    }

    private void OnDisable()
    {
        InputManager.OnCameraPan -= HandlePan;
        InputManager.OnCameraZoom -= HandleZoom;
        InputManager.OnCameraMove -= HandleMove;
    }

    private void Update()
    {
        // Handle continuous WASD movement
        if (_currentMoveDirection != Vector2.zero)
        {
            ApplyMovement(_currentMoveDirection);
        }
    }

    private void HandlePan(Vector2 panDelta)
    {
        // Convert screen space pan delta to world space movement
        float panMultiplier = _invertPan ? -1f : 1f;

        Vector3 movement = new Vector3(
            panDelta.x * _panSpeed * panMultiplier,
            0f,
            panDelta.y * _panSpeed * panMultiplier
        );

        transform.position = _ClampToBoundary(transform.position + movement);
    }

    private void HandleZoom(float zoomDelta)
    {
        // Orthographic zoom
        if (_mainCamera.orthographic)
        {
            _mainCamera.orthographicSize = Mathf.Clamp(
                _mainCamera.orthographicSize - zoomDelta * _zoomSpeed,
                _minZoom,
                _maxZoom
            );
        }
        // Perspective zoom - move camera along its forward axis
        else
        {
            Vector3 zoomMovement = _mainCamera.transform.forward * zoomDelta * _zoomSpeed;
            Vector3 newPosition = transform.position + zoomMovement;
            newPosition.y = Mathf.Clamp(newPosition.y, _minZoom, _maxZoom);
            transform.position = _ClampToBoundary(newPosition);
        }
    }

    private void HandleMove(Vector2 moveDirection)
    {
        // Store the current move direction for use in Update
        _currentMoveDirection = moveDirection;
    }

    private void ApplyMovement(Vector2 direction)
    {
        Vector3 movement = new Vector3(
            direction.x * _moveSpeed * Time.unscaledDeltaTime,
            0f,
            direction.y * _moveSpeed * Time.unscaledDeltaTime
        );

        transform.position = _ClampToBoundary(transform.position + movement);
    }

    private Vector3 _ClampToBoundary(Vector3 position)
    {
        if (!_clampToBoundary)
            return position;

        Vector3 gridOrigin = GridManager.Instance.GridOrigin;

        position.x = Mathf.Clamp(
            position.x,
            gridOrigin.x - _boundaryPadding,
            gridOrigin.x + _gridWidth + _boundaryPadding
        );

        position.z = Mathf.Clamp(
            position.z,
            gridOrigin.z - _boundaryPadding,
            gridOrigin.z + _gridHeight + _boundaryPadding
        );

        return position;
    }
}