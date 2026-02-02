using UnityEngine;

public class RoadPlacementInput : MonoBehaviour
{
    [SerializeField] private RoadNetwork roadNetwork;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float gridSize = 5f; // Snap to grid

    private Vector3? dragStartPosition;
    private LineRenderer previewLine;

    void Start()
    {
        // Create preview line
        GameObject previewObj = new GameObject("RoadPreview");
        previewLine = previewObj.AddComponent<LineRenderer>();
        previewLine.startWidth = 0.5f;
        previewLine.endWidth = 0.5f;
        previewLine.material = new Material(Shader.Find("Sprites/Default"));
        previewLine.startColor = Color.yellow;
        previewLine.endColor = Color.yellow;
        previewLine.enabled = false;
    }

    void Update()
    {
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

                // Only create road if there's meaningful distance
                if (Vector3.Distance(dragStartPosition.Value, alignedEnd) > 0.1f)
                {
                    roadNetwork.AddRoadSegment(dragStartPosition.Value, alignedEnd);
                }
            }

            dragStartPosition = null;
            previewLine.enabled = false;
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

        // Determine if more horizontal or vertical
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
        {
            // More horizontal - make it purely east-west
            return new Vector3(end.x, start.y, start.z);
        }
        else
        {
            // More vertical - make it purely north-south
            return new Vector3(start.x, start.y, end.z);
        }
    }
}