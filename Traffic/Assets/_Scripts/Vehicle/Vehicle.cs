using UnityEngine;
using System.Collections.Generic;

public class Vehicle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float waypointReachDistance = 0.2f;

    private VehicleController controller;
    private LanePathfinder pathfinder;
    private RoadGrid roadGrid;

    private LaneSegment currentLane;
    private List<LaneSegment> currentPath;
    private int currentPathIndex = 0;
    private Vector3 currentTargetWaypoint;

    private GridCell targetCell;
    private LaneSegment targetLane;

    public void Initialize(VehicleController controller, LaneSegment startLane, LanePathfinder pathfinder, RoadGrid roadGrid)
    {
        this.controller = controller;
        this.currentLane = startLane;
        this.pathfinder = pathfinder;
        this.roadGrid = roadGrid;

        currentTargetWaypoint = startLane.EndWaypoint;

        // Set initial rotation to face the lane direction
        Vector3 direction = (startLane.EndWaypoint - startLane.StartWaypoint).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        SetNewRandomTarget();
    }

    private void Update()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            SetNewRandomTarget();
            return;
        }

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        // Move towards current waypoint
        Vector3 direction = (currentTargetWaypoint - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Check if reached waypoint
        float distance = Vector3.Distance(transform.position, currentTargetWaypoint);
        if (distance < waypointReachDistance)
        {
            AdvanceToNextWaypoint();
        }
    }

    private void AdvanceToNextWaypoint()
    {
        // Move to the end waypoint of the current lane segment
        if (currentPathIndex < currentPath.Count)
        {
            currentLane = currentPath[currentPathIndex];
            currentTargetWaypoint = currentLane.EndWaypoint;
            currentPathIndex++;
        }
        else
        {
            // Reached the target, set a new one
            SetNewRandomTarget();
        }
    }

    private void SetNewRandomTarget()
    {
        targetCell = RoadGrid.Instance.GetRandomRoadCell();
        if (targetCell == null)
        {
            Debug.LogWarning("No valid target cell found!");
            return;
        }

        targetLane = RoadGrid.Instance.GetRandomLaneFromCell(targetCell);
        if (targetLane == null)
        {
            Debug.LogWarning("No valid target lane found!");
            return;
        }

        // Find path from current lane to target lane
        currentPath = pathfinder.FindPath(currentLane, targetLane);

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning($"No path found from {currentLane} to {targetLane}");
            // Try again with a different target
            SetNewRandomTarget();
            return;
        }

        currentPathIndex = 0;
        currentTargetWaypoint = currentPath[currentPath.Count - 1].EndWaypoint;

        Debug.Log($"New path set with {currentPath.Count} segments to target at {targetCell.Position}");
    }

    public void DrawDebugPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        // Draw the current position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Draw line from vehicle to first waypoint
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, currentTargetWaypoint);

        // Draw the path
        for (int i = currentPathIndex; i < currentPath.Count; i++)
        {
            LaneSegment segment = currentPath[i];

            // Draw the segment
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(segment.StartWaypoint, segment.EndWaypoint);

            // Draw connection to next segment
            if (i < currentPath.Count - 1)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(segment.EndWaypoint, currentPath[i + 1].StartWaypoint);
            }
        }

        // Draw the target
        if (targetLane != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetLane.EndWaypoint, 0.4f);
        }
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.RemoveVehicle(this);
        }
    }
}