using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    private bool _hasNorth, _hasSouth, _hasWest, _hasEast;

    private bool _subscribedToSaveManager = false;
    private bool _subscribedToRoadWaypointUpdated = false;

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

        if (!_subscribedToRoadWaypointUpdated)
            TryToSubscribeToRoadWaypointUpdated();
    }

    private void OnEnable()
    {
        TryToSubscribeToSaveManager();
        TryToSubscribeToRoadWaypointUpdated();
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
            _subscribedToSaveManager = false;
        }

        if (RoadWaypointManager.Instance != null)
        {
            RoadWaypointManager.Instance.OnRoadWaypointsUpdated -= RoadWaypointsUpdated;
            _subscribedToRoadWaypointUpdated = false;
        }
    }

    private void TryToSubscribeToSaveManager()
    {
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.RegisterSaveable(this);
        _subscribedToSaveManager = true;
    }

    private void TryToSubscribeToRoadWaypointUpdated()
    {
        if (RoadWaypointManager.Instance == null) return;

        RoadWaypointManager.Instance.OnRoadWaypointsUpdated += RoadWaypointsUpdated;
        _subscribedToRoadWaypointUpdated = true;
    }

    private void RoadWaypointsUpdated()
    {
        GenerateWaypoints();
    }

    private void CalculateEntryExitAndMidpointsForCell(GridCell cell)
    {
        _cellCentre = GridManager.Instance.GetCellCentre(cell);

        _northWestFromNorth = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, _halfCellSize);
        _northWestFromWest = _cellCentre + new Vector3(-_halfCellSize, 0, _halfCellSize - _halfPavementSize);

        _northEastFromNorth = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, _halfCellSize);
        _northEastFromEast = _cellCentre + new Vector3(_halfCellSize, 0, _halfCellSize - _halfPavementSize);

        _southWestFromSouth = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, -_halfCellSize);
        _southWestFromWest = _cellCentre + new Vector3(-_halfCellSize, 0, -_halfCellSize + _halfPavementSize);

        _southEastFromSouth = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, -_halfCellSize);
        _southEastFromEast = _cellCentre + new Vector3(_halfCellSize, 0, -_halfCellSize + _halfPavementSize);

        _midpointNW = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
        _midpointNE = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
        _midpointSW = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);
        _midpointSE = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);

        _hasNorth = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.North);
        _hasSouth = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.South);
        _hasEast = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.East);
        _hasWest = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.West);
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

        ConnectAllCells();
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
                waypoints = CreateCornerWaypoints(cell);
                break;
            case RoadType.TJunction:
                waypoints = CreateTJunctionWaypoints(cell);
                break;
            case RoadType.Crossroads:
                waypoints = CreateCrossroadsWaypoints(cell);
                break;
            case RoadType.DeadEnd:
                waypoints = CreateDeadEndWaypoints(cell);
                break;
        }

        // Store waypoints for this cell
        if (!_cellWaypoints.ContainsKey(cell))
        {
            _cellWaypoints[cell] = new List<WaypointNode>();
        }
        _cellWaypoints[cell].AddRange(waypoints);
        _allWaypoints.AddRange(waypoints);

        ConfigureTrafficLights(cell, waypoints);
    }

    private List<WaypointNode> CreateStraightWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        if (_hasNorth && _hasSouth) // Vertical road
        {
            // Left pavement
            Vector3 crossing = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, 0);
            WaypointNode north = new WaypointNode(_northWestFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint1 = new WaypointNode(crossing, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode south = new WaypointNode(_southWestFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect north to south
            ConnectPavementNodes(north, crossingPoint1);
            ConnectPavementNodes(crossingPoint1, south);

            waypoints.Add(north);
            waypoints.Add(crossingPoint1);
            waypoints.Add(south);

            // Right pavement
            crossing = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, 0);
            north = new WaypointNode(_northEastFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint2 = new WaypointNode(crossing, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            south = new WaypointNode(_southEastFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect north to south
            ConnectPavementNodes(north, crossingPoint2);
            ConnectPavementNodes(crossingPoint2, south);

            waypoints.Add(north);
            waypoints.Add(crossingPoint2);
            waypoints.Add(south);

            // connect crossing points
            ConnectPavementNodes(crossingPoint1, crossingPoint2);

        }
        else if (_hasEast && _hasWest) // Horizontal road
        {
            // Top pavement
            Vector3 crossing = _cellCentre + new Vector3(0, 0, _halfCellSize - _halfPavementSize);
            WaypointNode west = new WaypointNode(_northWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint1 = new WaypointNode(crossing, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode east = new WaypointNode(_northEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect west to east
            ConnectPavementNodes(west, crossingPoint1);
            ConnectPavementNodes(crossingPoint1, east);

            waypoints.Add(west);
            waypoints.Add(crossingPoint1);
            waypoints.Add(east);

            // Bottom pavement
            crossing = _cellCentre + new Vector3(0, 0, -_halfCellSize + _halfPavementSize);
            west = new WaypointNode(_southWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint2 = new WaypointNode(crossing, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            east = new WaypointNode(_southEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect west to east
            ConnectPavementNodes(west, crossingPoint2);
            ConnectPavementNodes(crossingPoint2, east);

            waypoints.Add(west);
            waypoints.Add(crossingPoint2);
            waypoints.Add(east);

            // connect crossing points
            ConnectPavementNodes(crossingPoint1, crossingPoint2);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateCornerWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();
        Vector3 _cellCentre = GridManager.Instance.GetCellCentre(cell);

        // Corner cases
        if (_hasNorth && _hasEast) // Corner from North to East
        {
            // start with the long corner pavement
            WaypointNode northWestFromNorth = new WaypointNode(_northWestFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSW = new WaypointNode(_midpointSW, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromEast = new WaypointNode(_southEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(northWestFromNorth, midpointSW);
            ConnectPavementNodes(midpointSW, southEastFromEast);

            // add waypoints
            waypoints.Add(northWestFromNorth);
            waypoints.Add(midpointSW);
            waypoints.Add(southEastFromEast);

            // short corner pavement
            WaypointNode northEastFromNorth = new WaypointNode(_northEastFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNE = new WaypointNode(_midpointNE, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromEast = new WaypointNode(_northEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(northEastFromNorth, midpointNE);
            ConnectPavementNodes(midpointNE, northEastFromEast);

            // add waypoints
            waypoints.Add(northEastFromNorth);
            waypoints.Add(midpointNE);
            waypoints.Add(northEastFromEast);
        }
        else if (_hasNorth && _hasWest) // Corner from North to West
        {
            // start with the long corner pavement
            WaypointNode northEastFromNorth = new WaypointNode(_northEastFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSE = new WaypointNode(_midpointSE, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromWest = new WaypointNode(_southWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(northEastFromNorth, midpointSE);
            ConnectPavementNodes(midpointSE, southWestFromWest);

            // add waypoints
            waypoints.Add(northEastFromNorth);
            waypoints.Add(midpointSE);
            waypoints.Add(southWestFromWest);

            // short corner pavement
            WaypointNode northWestFromNorth = new WaypointNode(_northWestFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNW = new WaypointNode(_midpointNW, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northWestFromWest = new WaypointNode(_northWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(northWestFromNorth, midpointNW);
            ConnectPavementNodes(midpointNW, northWestFromWest);

            // add waypoints
            waypoints.Add(northWestFromNorth);
            waypoints.Add(midpointNW);
            waypoints.Add(northWestFromWest);
        }
        else if (_hasSouth && _hasEast) // Corner from South to East
        {
            // start with the long corner pavement
            WaypointNode southWestFromSouth = new WaypointNode(_southWestFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNW = new WaypointNode(_midpointNW, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromEast = new WaypointNode(_northEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(southWestFromSouth, midpointNW);
            ConnectPavementNodes(midpointNW, northEastFromEast);

            // add waypoints
            waypoints.Add(southWestFromSouth);
            waypoints.Add(midpointNW);
            waypoints.Add(northEastFromEast);

            // short corner pavement
            WaypointNode southEastFromSouth = new WaypointNode(_southEastFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSE = new WaypointNode(_midpointSE, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromEast = new WaypointNode(_southEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(southEastFromSouth, midpointSE);
            ConnectPavementNodes(midpointSE, southEastFromEast);

            // add waypoints
            waypoints.Add(southEastFromSouth);
            waypoints.Add(midpointSE);
            waypoints.Add(southEastFromEast);
        }
        else if (_hasSouth && _hasWest) // Corner from South to West
        {
            // start with the long corner pavement
            WaypointNode southEastFromSouth = new WaypointNode(_southEastFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNE = new WaypointNode(_midpointNE, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northWestFromWest = new WaypointNode(_northWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(southEastFromSouth, midpointNE);
            ConnectPavementNodes(midpointNE, northWestFromWest);

            // add waypoints
            waypoints.Add(southEastFromSouth);
            waypoints.Add(midpointNE);
            waypoints.Add(northWestFromWest);

            // short corner pavement
            WaypointNode southWestFromSouth = new WaypointNode(_southWestFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSW = new WaypointNode(_midpointSW, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromWest = new WaypointNode(_southWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            ConnectPavementNodes(southWestFromSouth, midpointSW);
            ConnectPavementNodes(midpointSW, southWestFromWest);

            // add waypoints
            waypoints.Add(southWestFromSouth);
            waypoints.Add(midpointSW);
            waypoints.Add(southWestFromWest);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateTJunctionWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // first create all of the waypoints
        // we will only need three quarters, but it really doesn't matter, we won,t add the unwanted nodes to the list
        WaypointNode northWestFromNorth = new WaypointNode(_northWestFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode northWestFromWest = new WaypointNode(_northWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode northEastFromNorth = new WaypointNode(_northEastFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode northEastFromEast = new WaypointNode(_northEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southWestFromSouth = new WaypointNode(_southWestFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southWestFromWest = new WaypointNode(_southWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southEastFromSouth = new WaypointNode(_southEastFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southEastFromEast = new WaypointNode(_southEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpointNW = new WaypointNode(_midpointNW, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpointNE = new WaypointNode(_midpointNE, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpointSW = new WaypointNode(_midpointSW, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpointSE = new WaypointNode(_midpointSE, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

        // T-Junction with North, East, and West (missing South)
        if (_hasNorth && _hasEast && _hasWest && !_hasSouth)
        {
            // north west connections
            ConnectPavementNodes(northWestFromNorth, midpointNW);
            ConnectPavementNodes(midpointNW, northWestFromWest);
            // north east connections
            ConnectPavementNodes(northEastFromNorth, midpointNE);
            ConnectPavementNodes(midpointNE, northEastFromEast);
            // south west to south east
            ConnectPavementNodes(southWestFromWest, southEastFromEast);

            waypoints.Add(midpointNW);
            waypoints.Add(midpointNE);
        }
        // T-Junction with North, East, and South (missing West)
        else if (_hasNorth && _hasEast && _hasSouth && !_hasWest)
        {
            // north east connections
            ConnectPavementNodes(northEastFromNorth, midpointNE);
            ConnectPavementNodes(midpointNE, northEastFromEast);
            // south east
            ConnectPavementNodes(southEastFromSouth, midpointSE);
            ConnectPavementNodes(midpointSE, southEastFromEast);
            // north west to south west
            ConnectPavementNodes(northWestFromNorth, southWestFromSouth);

            waypoints.Add(midpointNE);
            waypoints.Add(midpointSE);
        }
        // T-Junction with North, South, and West (missing East)
        else if (_hasNorth && _hasSouth && _hasWest && !_hasEast)
        {
            // north west connections
            ConnectPavementNodes(northWestFromNorth, midpointNW);
            ConnectPavementNodes(midpointNW, northWestFromWest);
            // south west
            ConnectPavementNodes(southWestFromSouth, midpointSW);
            ConnectPavementNodes(midpointSW, southWestFromWest);
            // north east to south east
            ConnectPavementNodes(northEastFromNorth, southEastFromSouth);

            waypoints.Add(midpointNW);
            waypoints.Add(midpointSW);
        }
        // T-Junction with East, South, and West (missing North)
        else if (_hasEast && _hasSouth && _hasWest && !_hasNorth)
        {
            // south west
            ConnectPavementNodes(southWestFromSouth, midpointSW);
            ConnectPavementNodes(midpointSW, southWestFromWest);
            // south east
            ConnectPavementNodes(southEastFromSouth, midpointSE);
            ConnectPavementNodes(midpointSE, southEastFromEast);
            // north west to north east
            ConnectPavementNodes(northWestFromWest, northEastFromEast);

            waypoints.Add(midpointSW);
            waypoints.Add(midpointSE);
        }

        if (_hasNorth)
        {
            waypoints.Add(northWestFromNorth);
            waypoints.Add(northEastFromNorth);
        }
        if (_hasSouth)
        {
            waypoints.Add(southWestFromSouth);
            waypoints.Add(southEastFromSouth);
        }
        if (_hasWest)
        {
            waypoints.Add(northWestFromWest);
            waypoints.Add(southWestFromWest);
        }
        if (_hasEast)
        {
            waypoints.Add(northEastFromEast);
            waypoints.Add(southEastFromEast);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateCrossroadsWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // Crossroads with all four directions
        if (_hasNorth && _hasSouth && _hasEast && _hasWest)
        {
            // first create all of the waypoints
            WaypointNode northWestFromNorth = new WaypointNode(_northWestFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northWestFromWest = new WaypointNode(_northWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromNorth = new WaypointNode(_northEastFromNorth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromEast = new WaypointNode(_northEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromSouth = new WaypointNode(_southWestFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromWest = new WaypointNode(_southWestFromWest, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromSouth = new WaypointNode(_southEastFromSouth, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromEast = new WaypointNode(_southEastFromEast, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNW = new WaypointNode(_midpointNW, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNE = new WaypointNode(_midpointNE, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSW = new WaypointNode(_midpointSW, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSE = new WaypointNode(_midpointSE, cell, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);

            // north west connections
            ConnectPavementNodes(northWestFromNorth, midpointNW);
            ConnectPavementNodes(midpointNW, northWestFromWest);
            // north east connections
            ConnectPavementNodes(northEastFromNorth, midpointNE);
            ConnectPavementNodes(midpointNE, northEastFromEast);
            // south west
            ConnectPavementNodes(southWestFromSouth, midpointSW);
            ConnectPavementNodes(midpointSW, southWestFromWest);
            // south east
            ConnectPavementNodes(southEastFromSouth, midpointSE);
            ConnectPavementNodes(midpointSE, southEastFromEast);

            // pedestrian crossing connections
            ConnectPavementNodes(midpointNW, midpointNE);
            ConnectPavementNodes(midpointNW, midpointSW);
            ConnectPavementNodes(midpointSE, midpointSW);
            ConnectPavementNodes(midpointSE, midpointNE);

            // add the waypoints
            waypoints.Add(northWestFromNorth);
            waypoints.Add(northWestFromWest);
            waypoints.Add(northEastFromNorth);
            waypoints.Add(northEastFromEast);
            waypoints.Add(southWestFromSouth);
            waypoints.Add(southWestFromWest);
            waypoints.Add(southEastFromSouth);
            waypoints.Add(southEastFromEast);
            waypoints.Add(midpointNW);
            waypoints.Add(midpointNE);
            waypoints.Add(midpointSW);
            waypoints.Add(midpointSE);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateDeadEndWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        Vector3 entryPos, midpoint1Pos, midpoint2Pos, exitPos;

        if (_hasNorth)
        {
            entryPos = _northWestFromNorth;
            midpoint1Pos = _midpointSW;
            midpoint2Pos = _midpointSE;
            exitPos = _northEastFromNorth;
        }
        else if (_hasSouth)
        {
            entryPos = _southWestFromSouth;
            midpoint1Pos = _midpointNW;
            midpoint2Pos = _midpointNE;
            exitPos = _southEastFromSouth;
        }
        else if (_hasEast)
        {
            entryPos = _southEastFromEast;
            midpoint1Pos = _midpointSW;
            midpoint2Pos = _midpointNW;
            exitPos = _northEastFromEast;
        }
        else
        {
            entryPos = _southWestFromWest;
            midpoint1Pos = _midpointSE;
            midpoint2Pos = _midpointNE;
            exitPos = _northWestFromWest;
        }

        WaypointNode entry = new WaypointNode(entryPos, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpoint1 = new WaypointNode(midpoint1Pos, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpoint2 = new WaypointNode(midpoint2Pos, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode exit = new WaypointNode(exitPos, cell, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

        // connections
        ConnectPavementNodes(entry, midpoint1);
        ConnectPavementNodes(midpoint1, midpoint2);
        ConnectPavementNodes(midpoint2, exit);

        waypoints.Add(entry);
        waypoints.Add(midpoint1);
        waypoints.Add(midpoint2);
        waypoints.Add(exit);

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

    public void ConnectAllCells()
    {
        // After all cells have been created, connect Neighbouring cells
        foreach (var kvp in _cellWaypoints)
        {
            GridCell cell = kvp.Key;
            List<WaypointNode> waypoints = kvp.Value;
            ConnectToNeighbouringCells(cell, waypoints);
        }
    }

    private void ConnectToNeighbouringCells(GridCell cell, List<WaypointNode> waypoints)
    {
        // Check all four directions for Neighbouring roads
        int[] dx = { 0, 0, -1, 1 };
        int[] dz = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = cell.Position.x + dx[i];
            int nz = cell.Position.z + dz[i];
            if (nx >= 0 && nx < GridManager.Instance.GridWidth && nz >= 0 && nz < GridManager.Instance.GridHeight)
            {
                GridCell Neighbour = GridManager.Instance.GetCell(nx, nz);
                if (Neighbour != null && Neighbour.CellType == CellType.Road)
                {
                    // Connect waypoints to Neighbour cell
                    ConnectWaypointsToNeighbour(waypoints, Neighbour);
                }
            }
        }
    }

    private void ConnectWaypointsToNeighbour(List<WaypointNode> cellWaypoints, GridCell Neighbour)
    {
        // Get the Neighbour's waypoints
        if (!_cellWaypoints.ContainsKey(Neighbour)) { Debug.Log("No Neighbours"); return; }
        List<WaypointNode> NeighbourWaypoints = _cellWaypoints[Neighbour];

        foreach (WaypointNode w1 in cellWaypoints)
        {
            foreach (WaypointNode w2 in NeighbourWaypoints)
            {
                // Check if the waypoints are at the same position (or very close)
                float distance = Vector3.Distance(w1.Position, w2.Position);

                // If the distance is very small (essentially zero), connect them
                if (distance < 0.05f) // Tolerance for floating point precision
                {
                    w1.Connections.Add(new WaypointConnection(w2, distance));
                }
            }
        }
    }

    private void ConfigureTrafficLights(GridCell cell, List<WaypointNode> cellWaypoints)
    {
        List<WaypointNode> pedestrianCrossingWaypoints = cellWaypoints.Where(w => w.Type == WaypointType.PedestrianRoadCrossing).ToList();
        List<WaypointNode> cellRoadWaypoints = RoadWaypointManager.Instance.GetCellWaypoints(cell).Where(w => w.Type == WaypointType.TrafficLightLocation).ToList();

        foreach (WaypointNode waypoint in pedestrianCrossingWaypoints)
        {
            foreach (WaypointNode node in cellRoadWaypoints)
            {
                if (Vector3.Distance(new Vector3(node.Position.x, 0f, node.Position.z), new Vector3(waypoint.Position.x, 0f, waypoint.Position.z)) < 0.05f)
                {
                    waypoint.LaneNodeForTrafficLight = node.LaneNodeForTrafficLight;
                    break;
                }
            }
        }
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        var waypointData = new WaypointSaveData();

        foreach (var node in _allWaypoints)
        {
            var nodeData = new WaypointNodeSaveData
            {
                Id = node.Id,
                X = node.Position.x,
                Z = node.Position.z,
                Type = node.Type,
                NetworkType = node.NetworkType,
                ParentCellX = node.ParentCell.Position.x,
                ParentCellZ = node.ParentCell.Position.z,
                PairedCrossingWaypointId = node.PairedCrossingWaypoint?.Id,
                LaneNodeForTrafficLightId = node.LaneNodeForTrafficLight?.Id,
                LightPosition = node.LightPosition
            };

            foreach (var connection in node.Connections)
            {
                nodeData.Connections.Add(new WaypointConnectionSaveData
                {
                    TargetNodeId = connection.TargetWaypoint.Id,
                    Cost = connection.Cost
                });
            }

            waypointData.Nodes.Add(nodeData);
        }

        saveData.PedestrianWaypoints = waypointData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.PedestrianWaypoints == null)
        {
            Debug.LogWarning("[PedestrianWaypointManager] No waypoint data in save file.");
            return;
        }

        _allWaypoints.Clear();
        var nodeLookup = new Dictionary<string, WaypointNode>();

        // First pass — create all nodes
        foreach (var nodeData in saveData.PedestrianWaypoints.Nodes)
        {
            // Retrieve the parent cell from the grid
            var parentCell = GridManager.Instance.GetCell(nodeData.ParentCellX, nodeData.ParentCellZ);
            if (parentCell == null)
            {
                Debug.LogWarning($"[PedestrianWaypointManager] Parent cell ({nodeData.ParentCellX}, {nodeData.ParentCellZ}) not found for node {nodeData.Id}.");
                continue;
            }

            var node = new WaypointNode(
                new Vector3(nodeData.X, 0f, nodeData.Z),
                parentCell,
                nodeData.Type,
                nodeData.NetworkType
            );

            // Restore the saved ID rather than using the new GUID generated in the constructor
            node.Id = nodeData.Id;

            _allWaypoints.Add(node);
            nodeLookup[node.Id] = node;
        }

        // Second pass — restore connections
        foreach (var nodeData in saveData.PedestrianWaypoints.Nodes)
        {
            if (!nodeLookup.TryGetValue(nodeData.Id, out var node))
                continue;

            foreach (var connectionData in nodeData.Connections)
            {
                if (nodeLookup.TryGetValue(connectionData.TargetNodeId, out var targetNode))
                {
                    node.Connections.Add(new WaypointConnection(targetNode, connectionData.Cost));
                }
                else
                {
                    Debug.LogWarning($"[WaypointManager] Target node {connectionData.TargetNodeId} not found for connection.");
                }
            }
        }

        Debug.Log($"[WaypointManager] Loaded {_allWaypoints.Count} pedestrian waypoint nodes.");
    }

    // private void OnDrawGizmos()
    // {
    //     if (_allWaypoints.Count <= 0) return;

    //     // Draw waypoints
    //     foreach (WaypointNode node in _allWaypoints)
    //     {
    //         if (node.Type != WaypointType.PedestrianRoadCrossing) continue;
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawSphere(node.Position, 0.2f);
    //     }

    //     // Draw connections (optional)
    //     Gizmos.color = Color.green;
    //     // foreach (var kvp in _cellWaypoints)
    //     // {
    //     //     foreach (var node in kvp.Value)
    //     //     {
    //     //         foreach (var connection in node.Connections)
    //     //         {
    //     //             Gizmos.DrawLine(node.Position, connection.TargetWaypoint.Position);
    //     //         }
    //     //     }
    //     // }
    // }
}