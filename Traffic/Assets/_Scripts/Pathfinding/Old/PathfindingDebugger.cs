using System.Collections.Generic;
using UnityEngine;

public class PathfindingDebugger : MonoBehaviour
{
    [SerializeField] private bool debugMode = true;

    private void Update()
    {
        if (Input.GetMouseButtonUp(2))
        {
            DebugPathfinding();
        }
    }

    private void DebugPathfinding()
    {
        if (!debugMode)
            return;

        // Get the grid
        GridCell[,] grid = RoadGrid.Instance.GetGrid();
        if (grid == null || grid.Length == 0)
        {
            Debug.LogError("Grid is empty or not initialized");
            return;
        }

        // Find valid start and goal cells
        GridCell startCell = GetRandomRoadCell(grid);
        GridCell goalCell = GetRandomRoadCell(grid);
        int attempts = 0;
        while (goalCell == startCell && attempts < 10)
        {
            goalCell = GetRandomRoadCell(grid);
            attempts++;
        }

        if (startCell == null || goalCell == null)
        {
            Debug.LogError("Could not find valid start or goal cells");
            return;
        }

        // Check if both cells have waypoints
        if (startCell.WaypointData.AllWaypoints.Count == 0)
        {
            Debug.LogError($"Start cell ({startCell.Position.x}, {startCell.Position.z}) has no waypoints");
            return;
        }

        if (goalCell.WaypointData.AllWaypoints.Count == 0)
        {
            Debug.LogError($"Goal cell ({goalCell.Position.x}, {goalCell.Position.z}) has no waypoints");
            return;
        }

        // Check if cells are connected
        bool areConnected = CheckCellConnection(startCell, goalCell);
        if (!areConnected)
        {
            Debug.LogWarning($"Cells ({startCell.Position.x}, {startCell.Position.z}) and ({goalCell.Position.x}, {goalCell.Position.z}) are not connected");
        }

        // Find path
        List<TrafficWaypoint> path = TrafficPathfinder.FindPathBetweenCells(startCell, goalCell);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("No path found between cells");
            VisualizePathfindingFailure(startCell, goalCell);
            return;
        }

        // Visualize successful path
        Debug.Log($"Path found from cell ({startCell.Position.x}, {startCell.Position.z}) to cell ({goalCell.Position.x}, {goalCell.Position.z}) with {path.Count} waypoints");
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

    private bool CheckCellConnection(GridCell startCell, GridCell goalCell)
    {
        // Check if cells are adjacent in x and z
        if (Mathf.Abs(startCell.Position.x - goalCell.Position.x) <= 1 &&
            Mathf.Abs(startCell.Position.z - goalCell.Position.z) <= 1)
        {
            // Check if they are directly connected via road
            if (startCell.Position.x == goalCell.Position.x && startCell.Position.z == goalCell.Position.z)
                return true;

            // Check if they are adjacent in the grid and have road connections
            if (startCell.Position.x == goalCell.Position.x + 1)
                return RoadGrid.Instance.HasRoadNeighbor(startCell, RoadDirection.East) &&
                       RoadGrid.Instance.HasRoadNeighbor(goalCell, RoadDirection.West);
            if (startCell.Position.x == goalCell.Position.x - 1)
                return RoadGrid.Instance.HasRoadNeighbor(startCell, RoadDirection.West) &&
                       RoadGrid.Instance.HasRoadNeighbor(goalCell, RoadDirection.East);
            if (startCell.Position.z == goalCell.Position.z + 1)
                return RoadGrid.Instance.HasRoadNeighbor(startCell, RoadDirection.North) &&
                       RoadGrid.Instance.HasRoadNeighbor(goalCell, RoadDirection.South);
            if (startCell.Position.z == goalCell.Position.z - 1)
                return RoadGrid.Instance.HasRoadNeighbor(startCell, RoadDirection.South) &&
                       RoadGrid.Instance.HasRoadNeighbor(goalCell, RoadDirection.North);
        }
        return false;
    }

    private void VisualizePathfindingFailure(GridCell startCell, GridCell goalCell)
    {
        // Visualize the cells
        Debug.Log($"Start cell: {startCell.Position.x}, {startCell.Position.z}");
        Debug.Log($"Goal cell: {goalCell.Position.x}, {goalCell.Position.z}");

        // Check if cells have waypoints
        Debug.Log($"Start cell waypoints count: {startCell.WaypointData.AllWaypoints.Count}");
        Debug.Log($"Goal cell waypoints count: {goalCell.WaypointData.AllWaypoints.Count}");

        // Check connections between cells
        Debug.Log("Checking connections between cells...");
        foreach (RoadDirection dir in System.Enum.GetValues(typeof(RoadDirection)))
        {
            if (RoadGrid.Instance.HasRoadNeighbor(startCell, dir))
            {
                GridCell neighbor = RoadGrid.Instance.GetNeighborInDirection(startCell, dir);
                Debug.Log($"Start cell has neighbor in direction {dir}, cell at ({neighbor.Position.x}, {neighbor.Position.z})");
            }
        }

        // Check if the goal cell has any exit waypoints
        foreach (RoadDirection dir in System.Enum.GetValues(typeof(RoadDirection)))
        {
            if (goalCell.WaypointData.ExitWaypoints.ContainsKey(dir))
            {
                Debug.Log($"Goal cell has {goalCell.WaypointData.ExitWaypoints[dir].Count} waypoints in direction {dir}");
            }
        }
    }
}