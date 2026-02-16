using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathfindingTest : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private WaypointNode startWaypoint;
    private WaypointNode endWaypoint;
    private List<WaypointNode> currentPath;

    private void Start()
    {
        // Create a new GameObject with LineRenderer
        GameObject lineObject = new GameObject("PathLine");
        lineObject.transform.parent = transform;
        lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Configure LineRenderer
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 0;

        currentPath = new List<WaypointNode>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2)) // Middle mouse button
        {
            SelectRandomWaypoints();
            FindAndDrawPath();
        }
    }

    private void SelectRandomWaypoints()
    {
        // Get all waypoints from WaypointManager
        var allWaypoints = GetAllEntryWaypoints();

        if (allWaypoints.Count < 2)
        {
            Debug.LogWarning("Not enough Entry waypoints available for pathfinding test!");
            return;
        }

        // Group waypoints by their parent cell (lane)
        var waypointsByCell = new Dictionary<GridCell, List<WaypointNode>>();
        foreach (var waypoint in allWaypoints)
        {
            if (!waypointsByCell.ContainsKey(waypoint.ParentCell))
            {
                waypointsByCell[waypoint.ParentCell] = new List<WaypointNode>();
            }
            waypointsByCell[waypoint.ParentCell].Add(waypoint);
        }

        // Filter cells that have at least 2 entry waypoints
        var validCells = waypointsByCell.Where(kvp => kvp.Value.Count >= 2).ToList();

        if (validCells.Count == 0)
        {
            Debug.LogWarning("No cells have at least 2 entry waypoints for pathfinding!");
            return;
        }

        // Select a random cell with at least 2 entry waypoints
        int cellIndex = Random.Range(0, validCells.Count);
        var selectedCell = validCells[cellIndex].Key;
        var cellWaypoints = validCells[cellIndex].Value;

        // Select two random entry waypoints from the same cell
        int startIndex = Random.Range(0, cellWaypoints.Count);
        int endIndex = Random.Range(0, cellWaypoints.Count);

        // Ensure they're different
        while (endIndex == startIndex)
        {
            endIndex = Random.Range(0, cellWaypoints.Count);
        }

        startWaypoint = cellWaypoints[startIndex];
        endWaypoint = cellWaypoints[endIndex];

        // Draw debug spheres at start and end
        Debug.Log($"Start waypoint: {startWaypoint.Position}, End waypoint: {endWaypoint.Position}");
        // Gizmos.color = Color.green;
        // Gizmos.DrawSphere(startWaypoint.Position, 0.2f);
        // Gizmos.color = Color.red;
        // Gizmos.DrawSphere(endWaypoint.Position, 0.2f);
    }

    private void FindAndDrawPath()
    {
        if (startWaypoint == null || endWaypoint == null)
        {
            Debug.LogWarning("Start or end waypoint is null!");
            return;
        }

        // Find path using A*
        currentPath = AStarPathfinder.FindPath(startWaypoint, endWaypoint);

        if (currentPath.Count == 0)
        {
            Debug.LogWarning("No path found between waypoints!");
            lineRenderer.positionCount = 0;
            return;
        }

        Debug.Log($"Path found with {currentPath.Count} waypoints!");

        // Draw path with LineRenderer
        lineRenderer.positionCount = currentPath.Count;
        for (int i = 0; i < currentPath.Count; i++)
        {
            lineRenderer.SetPosition(i, currentPath[i].Position);
        }
    }

    private List<WaypointNode> GetAllEntryWaypoints()
    {
        List<WaypointNode> entryWaypoints = new List<WaypointNode>();

        // Access WaypointManager's waypoints through reflection or a public getter
        var waypointManager = WaypointManager.Instance;

        // You may need to add a public method to WaypointManager to access all waypoints
        // For example: public List<WaypointNode> GetAllWaypoints() { return allWaypoints; }

        // Assuming you add this method to WaypointManager:
        var allWaypoints = waypointManager.GetAllWaypoints();

        foreach (var waypoint in allWaypoints)
        {
            if (waypoint.Type == WaypointType.Entry)
            {
                entryWaypoints.Add(waypoint);
            }
        }

        return entryWaypoints;
    }

    private void OnDrawGizmos()
    {
        // Draw start waypoint
        if (startWaypoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startWaypoint.Position, 0.2f);
        }

        // Draw end waypoint
        if (endWaypoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endWaypoint.Position, 0.2f);
        }

        // Draw current path
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i].Position, currentPath[i + 1].Position);
            }
        }
    }
}