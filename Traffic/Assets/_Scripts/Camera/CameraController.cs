using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 0.1f;
    [SerializeField] private bool invertPan = true;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 100f;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 20f;

    [Header("Boundary Settings")]
    [SerializeField] private bool clampToBoundary = true;
    [SerializeField] private float boundaryPadding = 5f;

    private Camera mainCamera;
    private Vector2 currentMoveDirection;
    private float gridWidth;
    private float gridHeight;
    private float cellSize;

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
        mainCamera = Camera.main;

        // Get grid bounds for clamping
        gridWidth = GridManager.Instance.GridWidth * GridManager.Instance.CellSize;
        gridHeight = GridManager.Instance.GridHeight * GridManager.Instance.CellSize;
        cellSize = GridManager.Instance.CellSize;
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
        if (currentMoveDirection != Vector2.zero)
        {
            ApplyMovement(currentMoveDirection);
        }
    }

    private void HandlePan(Vector2 panDelta)
    {
        // Convert screen space pan delta to world space movement
        float panMultiplier = invertPan ? -1f : 1f;

        Vector3 movement = new Vector3(
            panDelta.x * panSpeed * panMultiplier,
            0f,
            panDelta.y * panSpeed * panMultiplier
        );

        transform.position = ClampToBoundary(transform.position + movement);
    }

    private void HandleZoom(float zoomDelta)
    {
        // Orthographic zoom
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = Mathf.Clamp(
                mainCamera.orthographicSize - zoomDelta * zoomSpeed,
                minZoom,
                maxZoom
            );
        }
        // Perspective zoom - move camera along its forward axis
        else
        {
            Vector3 zoomMovement = mainCamera.transform.forward * zoomDelta * zoomSpeed;
            Vector3 newPosition = transform.position + zoomMovement;
            newPosition.y = Mathf.Clamp(newPosition.y, minZoom, maxZoom);
            transform.position = ClampToBoundary(newPosition);
        }
    }

    private void HandleMove(Vector2 moveDirection)
    {
        // Store the current move direction for use in Update
        currentMoveDirection = moveDirection;
    }

    private void ApplyMovement(Vector2 direction)
    {
        Vector3 movement = new Vector3(
            direction.x * moveSpeed * Time.unscaledDeltaTime,
            0f,
            direction.y * moveSpeed * Time.unscaledDeltaTime
        );

        transform.position = ClampToBoundary(transform.position + movement);
    }

    private Vector3 ClampToBoundary(Vector3 position)
    {
        if (!clampToBoundary)
            return position;

        Vector3 gridOrigin = GridManager.Instance.GridOrigin;

        position.x = Mathf.Clamp(
            position.x,
            gridOrigin.x - boundaryPadding,
            gridOrigin.x + gridWidth + boundaryPadding
        );

        position.z = Mathf.Clamp(
            position.z,
            gridOrigin.z - boundaryPadding,
            gridOrigin.z + gridHeight + boundaryPadding
        );

        return position;
    }
}