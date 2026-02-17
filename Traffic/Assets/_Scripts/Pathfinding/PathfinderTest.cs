using System.Collections.Generic;
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

        // Select two random Entry waypoints
        int startIndex = Random.Range(0, allWaypoints.Count - 1);
        int endIndex = Random.Range(0, allWaypoints.Count - 1);

        // Ensure they're different
        while (endIndex == startIndex)
        {
            endIndex = Random.Range(0, allWaypoints.Count);
        }

        startWaypoint = allWaypoints[startIndex];
        endWaypoint = allWaypoints[endIndex];

        // Draw debug spheres at start and end
        Debug.Log($"Start waypoint: {startWaypoint.ParentCell.Position}, End waypoint: {endWaypoint.ParentCell.Position}");
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
        // For now, we'll iterate through all waypoints and filter for Entry types
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
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(startWaypoint.Position, 0.4f);
        }

        // Draw end waypoint
        if (endWaypoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(endWaypoint.Position, 0.4f);
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