using System.Collections.Generic;
using UnityEngine;

public class PathfindingTester : MonoBehaviour
{
    [SerializeField] private float sphereRadius = 0.5f;
    [SerializeField] private Color startSphereColor = Color.green;
    [SerializeField] private Color endSphereColor = Color.red;
    [SerializeField] private Color pathLineColor = Color.yellow;
    [SerializeField] private float lineWidth = 0.1f;

    private List<TrafficWaypoint> currentPath;
    private GameObject pathVisualization;

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            //TestPathfinding();
        }
    }

    private void TestPathfinding()
    {
        // Clear previous visualization
        ClearPathVisualization();

        // Get the grid
        GridCell[,] grid = RoadGrid.Instance.GetGrid();
        if (grid == null || grid.Length == 0)
        {
            Debug.LogError("Grid is empty or not initialized");
            return;
        }

        // Select random start cell
        GridCell startCell = GetRandomRoadCell(grid);
        if (startCell == null)
        {
            Debug.LogError("No valid start cell found");
            return;
        }

        // Select random goal cell (different from start)
        GridCell goalCell = GetRandomRoadCell(grid);
        int attempts = 0;
        while (goalCell == startCell && attempts < 10)
        {
            goalCell = GetRandomRoadCell(grid);
            attempts++;
        }

        if (goalCell == null || goalCell == startCell)
        {
            Debug.LogError("Could not find valid goal cell");
            return;
        }

        // Find path
        currentPath = TrafficPathfinder.FindPathBetweenCells(startCell, goalCell);

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning("No path found between cells");
            return;
        }

        // Visualize path
        VisualizeePath(currentPath);

        Debug.Log($"Path found from cell ({startCell.Position.x}, {startCell.Position.y}) to cell ({goalCell.Position.x}, {goalCell.Position.y}) with {currentPath.Count} waypoints");
    }

    private GridCell GetRandomRoadCell(GridCell[,] grid)
    {
        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);

        // Try to find a random road cell
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int randomX = Random.Range(0, gridWidth);
            int randomY = Random.Range(0, gridHeight);

            GridCell cell = grid[randomX, randomY];
            if (cell != null && cell.CellType != CellType.Empty && cell.WaypointData.AllWaypoints.Count > 0)
            {
                return cell;
            }
        }

        // Fallback: find first valid road cell
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = grid[x, y];
                if (cell != null && cell.CellType != CellType.Empty && cell.WaypointData.AllWaypoints.Count > 0)
                {
                    return cell;
                }
            }
        }

        return null;
    }

    private void VisualizeePath(List<TrafficWaypoint> path)
    {
        // Create parent object for visualization
        pathVisualization = new GameObject("PathVisualization");

        if (path.Count == 0)
            return;

        // Draw start sphere
        DrawSphere(path[0].Position, sphereRadius, startSphereColor, "StartPoint");

        // Draw end sphere
        DrawSphere(path[path.Count - 1].Position, sphereRadius, endSphereColor, "EndPoint");

        // Draw path line
        DrawPathLine(path);
    }

    private void DrawSphere(Vector3 position, float radius, Color color, string name)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * radius * 2;

        // Remove collider
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        // Set material color
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        sphere.GetComponent<Renderer>().material = mat;
    }

    private void DrawPathLine(List<TrafficWaypoint> path)
    {
        // Create line renderer
        LineRenderer lineRenderer = pathVisualization.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.startColor = pathLineColor;
        lineRenderer.endColor = pathLineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = path.Count;

        // Set line positions
        Vector3[] positions = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            positions[i] = path[i].Position;
        }
        lineRenderer.SetPositions(positions);
    }

    private void ClearPathVisualization()
    {
        if (pathVisualization != null)
        {
            GameObject.Destroy(pathVisualization);
            pathVisualization = null;
        }
    }
}