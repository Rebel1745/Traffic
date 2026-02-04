using UnityEngine;

public class RoadPlacementInput : MonoBehaviour
{
    [SerializeField] private RoadNetwork roadNetwork;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float gridSize = 5f; // Snap to grid
    [SerializeField] private float singleClickRoadLength = 5f; // Length of road for single click
    [SerializeField] private float dragThreshold = 0.5f; // Minimum distance to consider it a drag

    private Vector3? dragStartPosition;
    private LineRenderer previewLine;
    private GameObject gridHighlight;

    void Start()
    {
        CreatePreviewLine();
        CreateGridHighlight();
    }

    void Update()
    {
        UpdateGridHighlight();

        if (Input.GetMouseButtonDown(0))
        {
            Vector3? hitPoint = GetGroundHitPoint();
            if (hitPoint.HasValue)
            {
                dragStartPosition = SnapToGrid(hitPoint.Value);
                previewLine.enabled = true;
            }
        }

        if (Input.GetMouseButton(0) && dragStartPosition.HasValue)
        {
            Vector3? currentPoint = GetGroundHitPoint();
            if (currentPoint.HasValue)
            {
                Vector3 snappedEnd = SnapToGrid(currentPoint.Value);
                Vector3 alignedEnd = AlignToCardinalDirection(dragStartPosition.Value, snappedEnd);

                previewLine.SetPosition(0, dragStartPosition.Value);
                previewLine.SetPosition(1, alignedEnd);
            }
        }

        if (Input.GetMouseButtonUp(0) && dragStartPosition.HasValue)
        {
            Vector3? hitPoint = GetGroundHitPoint();
            if (hitPoint.HasValue)
            {
                Vector3 snappedEnd = SnapToGrid(hitPoint.Value);
                Vector3 alignedEnd = AlignToCardinalDirection(dragStartPosition.Value, snappedEnd);

                float distance = Vector3.Distance(dragStartPosition.Value, alignedEnd);

                // Check if it's a single click (no meaningful drag)
                if (distance <= dragThreshold)
                {
                    // Single click - place a short road segment in the direction the camera is facing
                    // Or default to east direction
                    Vector3 endPoint = dragStartPosition.Value + new Vector3(singleClickRoadLength, 0, 0);
                    roadNetwork.AddRoadSegment(dragStartPosition.Value, endPoint);
                }
                else
                {
                    // Drag - place road along the dragged path
                    roadNetwork.AddRoadSegment(dragStartPosition.Value, alignedEnd);
                }
            }

            dragStartPosition = null;
            previewLine.enabled = false;
        }
    }

    private void CreatePreviewLine()
    {
        GameObject previewObj = new GameObject("RoadPreview");
        previewLine = previewObj.AddComponent<LineRenderer>();
        previewLine.startWidth = gridSize;
        previewLine.endWidth = gridSize;
        previewLine.material = new Material(Shader.Find("Sprites/Default"));
        previewLine.startColor = Color.yellow;
        previewLine.endColor = Color.yellow;
        previewLine.enabled = false;
    }

    private void CreateGridHighlight()
    {
        gridHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gridHighlight.name = "GridHighlight";
        Destroy(gridHighlight.GetComponent<Collider>());
        gridHighlight.transform.rotation = Quaternion.Euler(90, 0, 0);
        gridHighlight.transform.localScale = new Vector3(gridSize, gridSize, 1);

        Material highlightMat = new Material(Shader.Find("Sprites/Default"));
        highlightMat.color = new Color(1f, 1f, 1f, 0.3f);
        gridHighlight.GetComponent<Renderer>().material = highlightMat;
        gridHighlight.SetActive(false);
    }

    private void UpdateGridHighlight()
    {
        Vector3? hitPoint = GetGroundHitPoint();

        if (hitPoint.HasValue)
        {
            Vector3 snappedPosition = SnapToGrid(hitPoint.Value);
            gridHighlight.transform.position = snappedPosition + Vector3.up * 0.1f;
            gridHighlight.SetActive(true);
        }
        else
        {
            gridHighlight.SetActive(false);
        }
    }

    private Vector3? GetGroundHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            return hit.point;
        }
        return null;
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            position.y,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    private Vector3 AlignToCardinalDirection(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
        {
            return new Vector3(end.x, start.y, start.z);
        }
        else
        {
            return new Vector3(start.x, start.y, end.z);
        }
    }
}