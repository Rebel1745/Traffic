using System.Collections.Generic;
using UnityEngine;

public class PedestrianWaypointManager : MonoBehaviour, IWaypointNetwork, ISaveable
{
    public static PedestrianWaypointManager Instance { get; private set; }

    public string SaveKey => "PedestrianWaypoints";

    // Completely separate collections — no cross-contamination
    private Dictionary<GridCell, List<WaypointNode>> _cellWaypoints = new();
    private List<WaypointNode> _allWaypoints = new();

    // cell calculations
    private Vector3 _cellCentre;
    private float _laneCentre;
    private float _halfCellSize;
    private float _quarterCellSize;
    private float _halfPavementSize;

    // waypoint values
    private Vector3 _northWestFromNorth, _northWestFromWest;
    private Vector3 _northEastFromNorth, _northEastFromEast;
    private Vector3 _southWestFromSouth, _southWestFromWest;
    private Vector3 _southEastFromSouth, _southEastFromEast;

    private Vector3 _midpointNW, _midpointNE, _midpointSW, _midpointSE;

    private bool _subscribedToSaveManager = false;
    private bool _subscribedToRoadMeshRenderer = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!_subscribedToSaveManager)
            TryToSubscribeToSaveManager();

        if (!_subscribedToRoadMeshRenderer)
            TryToSubscribeToRoadMeshRenderer();
    }

    private void OnEnable()
    {
        TryToSubscribeToSaveManager();
        TryToSubscribeToRoadMeshRenderer();
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
            _subscribedToSaveManager = false;
        }

        if (RoadMeshRenderer.Instance != null)
        {
            RoadMeshRenderer.Instance.OnRoadMeshUpdated -= RoadMeshUpdated;
            _subscribedToRoadMeshRenderer = false;
        }
    }

    private void TryToSubscribeToSaveManager()
    {
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.RegisterSaveable(this);
        _subscribedToSaveManager = true;
    }

    private void TryToSubscribeToRoadMeshRenderer()
    {
        if (RoadMeshRenderer.Instance == null) return;

        RoadMeshRenderer.Instance.OnRoadMeshUpdated += RoadMeshUpdated;
        _subscribedToRoadMeshRenderer = true;
    }

    private void RoadMeshUpdated()
    {
        GenerateWaypoints();
    }

    private void CalculateEntryExitAndMidpointsForCell(GridCell cell)
    {
        _cellCentre = GridManager.Instance.GetCellCentre(cell);

        _northWestFromNorth = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0.5f, _halfCellSize);
        _northWestFromWest = _cellCentre + new Vector3(-_halfCellSize, 0.5f, _halfCellSize - _halfPavementSize);

        _northEastFromNorth = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0.5f, _halfCellSize);
        _northEastFromEast = _cellCentre + new Vector3(_halfCellSize, 0.5f, _halfCellSize - _halfPavementSize);

        _southWestFromSouth = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0.5f, -_halfCellSize);
        _southWestFromWest = _cellCentre + new Vector3(-_halfCellSize, 0.5f, -_halfCellSize + _halfPavementSize);

        _southEastFromSouth = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0.5f, -_halfCellSize);
        _southEastFromEast = _cellCentre + new Vector3(_halfCellSize, 0.5f, -_halfCellSize + _halfPavementSize);

        _midpointNW = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0.5f, _halfCellSize - _halfPavementSize);
        _midpointNE = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0.5f, _halfCellSize - _halfPavementSize);
        _midpointSW = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0.5f, -_halfCellSize + _halfPavementSize);
        _midpointSE = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0.5f, -_halfCellSize + _halfPavementSize);
    }

    public void GenerateWaypoints()
    {
        GridCell[,] grid = GridManager.Instance.GetGrid();

        _cellWaypoints.Clear();
        _allWaypoints.Clear();

        _laneCentre = RoadMeshRenderer.Instance.GetLaneWidth() / 2f;
        _halfCellSize = GridManager.Instance.CellSize / 2f;
        _quarterCellSize = _halfCellSize / 2f;
        _halfPavementSize = RoadMeshRenderer.Instance.GetPavementWidth() / 2f;

        // First pass: Create waypoints for each cell
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y].CellType != CellType.Empty)
                {
                    CreateAndConnectWaypoints(grid[x, y]);
                }
            }
        }

        //ConnectAllCells();
    }

    private void CreateAndConnectWaypoints(GridCell cell)
    {
        if (cell.CellType == CellType.Empty) return;

        List<WaypointNode> waypoints = new List<WaypointNode>();

        CalculateEntryExitAndMidpointsForCell(cell);

        switch (cell.RoadType)
        {
            case RoadType.Straight:
                waypoints = CreateStraightWaypoints(cell);
                break;
            case RoadType.Corner:
                //waypoints = CreateCornerWaypoints(cell);
                break;
            case RoadType.TJunction:
                //waypoints = CreateTJunctionWaypoints(cell);
                break;
            case RoadType.Crossroads:
                //waypoints = CreateCrossroadsWaypoints(cell);
                break;
            case RoadType.DeadEnd:
                //waypoints = CreateDeadEndWaypoints(cell);
                break;
        }

        // Store waypoints for this cell
        if (!_cellWaypoints.ContainsKey(cell))
        {
            _cellWaypoints[cell] = new List<WaypointNode>();
        }
        _cellWaypoints[cell].AddRange(waypoints);
        _allWaypoints.AddRange(waypoints);
    }

    private List<WaypointNode> CreateStraightWaypoints(GridCell cell)
    {
        Debug.Log("PedestrianWaypointManager::CreateStraightWaypoints");
        List<WaypointNode> waypoints = new List<WaypointNode>();

        bool hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        bool hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        bool hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        bool hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);

        if (hasNorth && hasSouth) // Vertical road
        {
            // Left pavement
            WaypointNode north = new WaypointNode(_northWestFromNorth, cell, WaypointType.PedestrianWalkway);
            WaypointNode south = new WaypointNode(_southWestFromSouth, cell, WaypointType.PedestrianWalkway);

            // Connect north to south
            ConnectPavementNodes(north, south);

            waypoints.Add(north);
            waypoints.Add(south);

            // Right pavement
            north = new WaypointNode(_northEastFromNorth, cell, WaypointType.PedestrianWalkway);
            south = new WaypointNode(_southEastFromSouth, cell, WaypointType.PedestrianWalkway);

            // Connect north to south
            ConnectPavementNodes(north, south);

            waypoints.Add(north);
            waypoints.Add(south);

        }
        else if (hasEast && hasWest) // Horizontal road
        {
            // Top pavement
            WaypointNode west = new WaypointNode(_northWestFromWest, cell, WaypointType.PedestrianWalkway);
            WaypointNode east = new WaypointNode(_northEastFromEast, cell, WaypointType.PedestrianWalkway);

            // Connect west to east
            ConnectPavementNodes(west, east);

            waypoints.Add(west);
            waypoints.Add(east);

            // Bottom pavement
            west = new WaypointNode(_southWestFromWest, cell, WaypointType.PedestrianWalkway);
            east = new WaypointNode(_southEastFromEast, cell, WaypointType.PedestrianWalkway);

            // Connect west to east
            ConnectPavementNodes(west, east);

            waypoints.Add(west);
            waypoints.Add(east);
        }

        return waypoints;
    }

    // manual distance allows custom value (probably used for road crossing, high number for normal crossing, low number for traffic light crossing)
    private void ConnectPavementNodes(WaypointNode a, WaypointNode b, float manualDistance = -1)
    {
        float dist = manualDistance == -1 ? Vector3.Distance(a.Position, b.Position) : manualDistance;
        a.Connections.Add(new WaypointConnection(b, dist));
        b.Connections.Add(new WaypointConnection(a, dist)); // bidirectional
    }

    public List<WaypointNode> GetAllWaypoints()
    {
        return _allWaypoints;
    }

    public List<WaypointNode> GetCellWaypoints(GridCell cell)
    {
        List<WaypointNode> cellWaypoints = new();

        foreach (WaypointNode node in _allWaypoints)
        {
            if (node.ParentCell == cell)
                cellWaypoints.Add(node);
        }

        return cellWaypoints;
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        throw new System.NotImplementedException();
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        throw new System.NotImplementedException();
    }

    private void OnDrawGizmos()
    {
        if (_allWaypoints.Count <= 0) return;

        foreach (WaypointNode node in _allWaypoints)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(node.Position, 0.2f);
        }
    }
}