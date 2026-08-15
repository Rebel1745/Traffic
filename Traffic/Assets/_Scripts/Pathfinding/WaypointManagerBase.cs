using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class WaypointManagerBase : MonoBehaviour
{
    // waypoint storage
    protected Dictionary<EntityId, WaypointNode> _allWaypoints;
    protected List<WaypointNode>[,] _cellWaypoints;

    // cell calculations
    protected int _gridWidth;
    protected int _gridHeight;
    protected Vector3 _cellCentre;
    protected float _laneCentre;
    protected float _halfCellSize;
    protected float _quarterCellSize;
    protected float _halfPavementSize;

    // Waypoint values
    protected bool _hasNorth, _hasSouth, _hasWest, _hasEast;

    protected virtual void Start()
    {
        _gridWidth = GridManager.Instance.GridWidth;
        _gridHeight = GridManager.Instance.GridHeight;

        _cellWaypoints = new List<WaypointNode>[_gridWidth, _gridHeight];
        _allWaypoints = new();

        _laneCentre = RoadMeshRenderer.Instance.GetLaneWidth() / 2f;
        _halfCellSize = GridManager.Instance.CellSize / 2f;
        _quarterCellSize = _halfCellSize / 2f;
        _halfPavementSize = RoadMeshRenderer.Instance.GetPavementWidth() / 2f;
    }

    protected WaypointNode CreateWaypoint(GridCell cell, Vector3 position, WaypointType type, WaypointNetworkType networkType = WaypointNetworkType.Vehicle, WaypointNode laneNode = null, RoadDirection lightPos = RoadDirection.None)
    {
        return new(position, cell, type, networkType);
    }

    protected void AddWaypoint(GridCell cell, WaypointNode waypoint)
    {
        _allWaypoints[waypoint.Id] = waypoint;

        if (_cellWaypoints[cell.Position.x, cell.Position.z] == null)
            _cellWaypoints[cell.Position.x, cell.Position.z] = new List<WaypointNode>();

        _cellWaypoints[cell.Position.x, cell.Position.z].Add(waypoint);
    }

    protected WaypointNode CreateAndAddWaypoint(GridCell cell, Vector3 position, WaypointType type, WaypointNetworkType networkType = WaypointNetworkType.Vehicle, WaypointNode laneNode = null, RoadDirection lightPos = RoadDirection.None)
    {
        // check to see if we have this waypoint already
        WaypointNode existing = GetWaypointNodeFromPositionInCell(cell, position, 0.1f, type);
        if (existing != null)
        {
            Debug.Log($"New waypoint {type.ToString()} at {position} is already present with type {existing.Type} at {position}");
        }

        // create the waypoint
        WaypointNode newNode = CreateWaypoint(cell, position, type, networkType);

        // add the waypoint
        AddWaypoint(cell, newNode);

        return newNode;
    }

    protected void AddWaypointConnection(WaypointNode source, WaypointNode target, float cost = -1, bool twoWay = false)
    {
        if (cost == -1) cost = Vector3.Distance(source.Position, target.Position);

        if (!source.HasConnection(target))
            source.Connections[target] = cost;

        if (twoWay && !target.HasConnection(source))
            target.Connections[source] = cost;
    }

    private void RemoveCellWaypoints(GridCell cell)
    {
        // we are deleting this cell, remove any connections between this and its neighbours
        RemoveConnectionsToNeighbours(cell);

        List<WaypointNode> cellWaypoints = GetCellWaypoints(cell);
        if (cellWaypoints == null || cellWaypoints.Count == 0) return;

        Debug.Log($"Removing {cellWaypoints.Count} waypoints from {_allWaypoints.Count}");

        // remove waypoints from cell in _allWaypoints
        foreach (WaypointNode node in cellWaypoints)
        {
            _allWaypoints.Remove(node.Id);
        }

        Debug.Log($"{_allWaypoints.Count} remain");

        // remove cell from _cellWaypoints
        _cellWaypoints[cell.Position.x, cell.Position.z].Clear();
    }

    // this function runs for each updated cell as an opposite function to connect cells
    private void RemoveConnectionsToNeighbours(GridCell cell)
    {
        List<WaypointNode> cellWaypoints = GetCellWaypoints(cell);
        if (cellWaypoints == null || cellWaypoints.Count == 0) return;

        List<GridCell> neighbours = GridManager.Instance.GetCellRoadNeighbours(cell);
        if (neighbours.Count == 0) return;

        List<WaypointNode> neighbourWaypoints;

        Debug.Log($"Cell at {cell.Position.x}, {cell.Position.z} has {neighbours.Count} neighbours");

        // remove each 
        foreach (GridCell gc in neighbours)
        {
            neighbourWaypoints = GetCellWaypoints(gc);
            if (neighbourWaypoints == null || neighbourWaypoints.Count == 0) continue;

            foreach (WaypointNode neighbourNode in neighbourWaypoints)
            {
                foreach (WaypointNode cellNode in cellWaypoints)
                {
                    if (neighbourNode.HasConnection(cellNode))
                    {
                        Debug.Log($"Removing connection to cell {cellNode.ParentCell.Position.x}, {cellNode.ParentCell.Position.z}");
                        neighbourNode.RemoveConnection(cellNode);
                    }
                }
            }
        }
    }

    protected virtual void CalculateEntryExitAndMidpointsForCell(GridCell cell)
    {
        _cellCentre = GridManager.Instance.GetCellCentre(cell);

        _hasNorth = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.North);
        _hasSouth = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.South);
        _hasEast = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.East);
        _hasWest = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.West);
    }

    protected virtual void GenerateWaypoints()
    {
        GridCell[,] grid = GridManager.Instance.GetGrid();
        GridCell currentCell;

        // First pass: remove waypoints from newly empty cell
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                currentCell = grid[x, y];
                if (currentCell.CellType == CellType.Empty && currentCell.IsUpdated)
                {
                    RemoveCellWaypoints(currentCell);
                }
            }
        }

        // Second pass: Create waypoints for each updated cell
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                currentCell = grid[x, y];
                if (currentCell.CellType != CellType.Road) continue;

                if (currentCell.IsUpdated)
                {
                    RemoveCellWaypoints(currentCell);
                    CreateAndConnectWaypoints(currentCell);
                }
            }
        }

        ConnectAllCells();
    }

    private void CreateAndConnectWaypoints(GridCell cell)
    {
        CalculateEntryExitAndMidpointsForCell(cell);

        switch (cell.RoadType)
        {
            case RoadType.Empty:
            case RoadType.Single:
                RemoveCellWaypoints(cell);
                break;
            case RoadType.Straight:
                CreateStraightWaypoints(cell);
                break;
            case RoadType.Corner:
                CreateCornerWaypoints(cell);
                break;
            case RoadType.TJunction:
                CreateTJunctionWaypoints(cell);
                break;
            case RoadType.Crossroads:
                CreateCrossroadsWaypoints(cell);
                break;
            case RoadType.DeadEnd:
                CreateDeadEndWaypoints(cell);
                break;
        }

        ConfigureTrafficLights(cell);
    }

    private void ConnectAllCells()
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                GridCell currentCell = GridManager.Instance.GetCell(x, y);

                if (currentCell.CellType != CellType.Road) continue;

                List<WaypointNode> cellWaypoints = _cellWaypoints[x, y];

                if (cellWaypoints == null || cellWaypoints.Count == 0)
                    continue;

                ConnectToNeighboringCells(currentCell, cellWaypoints);
            }
        }
    }

    private void ConnectToNeighboringCells(GridCell cell, List<WaypointNode> waypoints)
    {
        // Check all four directions for neighboring roads
        int[] dx = { 0, 0, -1, 1 };
        int[] dz = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = cell.Position.x + dx[i];
            int nz = cell.Position.z + dz[i];
            if (nx >= 0 && nx < GridManager.Instance.GridWidth && nz >= 0 && nz < GridManager.Instance.GridHeight)
            {
                GridCell neighbor = GridManager.Instance.GetCell(nx, nz);
                if (neighbor != null && neighbor.CellType == CellType.Road)
                {
                    // Connect waypoints to neighbor cell
                    ConnectWaypointsToNeighbour(waypoints, neighbor);
                }
            }
        }
    }

    protected abstract void ConnectWaypointsToNeighbour(List<WaypointNode> waypoints, GridCell neighbour);

    protected abstract void CreateStraightWaypoints(GridCell cell);
    protected abstract void CreateCornerWaypoints(GridCell cell);
    protected abstract void CreateTJunctionWaypoints(GridCell cell);
    protected abstract void CreateCrossroadsWaypoints(GridCell cell);
    protected abstract void CreateDeadEndWaypoints(GridCell cell);

    protected virtual void ConfigureTrafficLights(GridCell cell)
    { }

    #region Cell Functions
    public List<WaypointNode> GetAllWaypoints()
    {
        return _allWaypoints.Values.ToList();
    }

    public List<WaypointNode> GetCellWaypoints(GridCell cell)
    {
        return _cellWaypoints[cell.Position.x, cell.Position.z];
    }

    public WaypointNode GetWaypointFromId(EntityId id)
    {
        if (_allWaypoints.ContainsKey(id))
            return _allWaypoints[id];

        return null;
    }

    protected List<WaypointNode> FindClosestNodesInCellFromPosition(Vector3 cellCheckPosition, Vector3 position, int count, WaypointType type)
    {
        GridCell neighbour = GridManager.Instance.GetCellAtWorldPosition(cellCheckPosition);
        List<WaypointNode> allNodes = GetCellWaypoints(neighbour);
        List<WaypointNode> selectedNodes = new List<WaypointNode>();

        if (neighbour != null)
        {
            foreach (WaypointNode node in allNodes)
            {
                if (node.Type != type) continue;
                selectedNodes.Add(node);
            }

            // Sort nodes by distance
            selectedNodes.Sort((a, b) =>
            {
                float distA = Utils.GetDistanceWithSetHeight(position, a.Position, 0f);
                float distB = Utils.GetDistanceWithSetHeight(position, b.Position, 0f);
                return distA.CompareTo(distB);
            });

            // Return up to 'count' nodes
            int nodesToReturn = Mathf.Min(count, selectedNodes.Count);
            return selectedNodes.GetRange(0, nodesToReturn);
        }

        return new List<WaypointNode>();
    }

    protected WaypointNode GetWaypointNodeFromPositionInCell(GridCell cell, Vector3 position, float distance, WaypointType type)
    {
        List<WaypointNode> allNodes = GetCellWaypoints(cell);
        if (allNodes == null || allNodes.Count == 0) return null;

        List<WaypointNode> selectedNodes = new();

        foreach (WaypointNode node in allNodes)
        {
            if (node.Type != type) continue;
            selectedNodes.Add(node);
        }

        if (selectedNodes.Count > 0)
        {
            selectedNodes.Sort((a, b) =>
            {
                float distA = Utils.GetDistanceWithSetHeight(position, a.Position, 0f);
                float distB = Utils.GetDistanceWithSetHeight(position, b.Position, 0f);
                return distA.CompareTo(distB);
            });

            if (Vector3.Distance(selectedNodes.First().Position, position) < distance)
                return selectedNodes.First();
        }

        return null;
    }

    #endregion
}
