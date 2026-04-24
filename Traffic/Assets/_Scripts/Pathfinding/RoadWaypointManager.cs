using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadWaypointManager : MonoBehaviour, IWaypointNetwork, ISaveable
{
    public static RoadWaypointManager Instance { get; private set; }

    public string SaveKey => "Waypoints";

    private Dictionary<GridCell, List<WaypointNode>> _cellWaypoints = new Dictionary<GridCell, List<WaypointNode>>();
    private List<WaypointNode> _allWaypoints = new List<WaypointNode>();

    // cell calculations
    private Vector3 _cellCentre;
    private float _laneCentre;
    private float _halfCellSize;
    private float _quarterCellSize;
    private float _halfPavementSize;

    // waypoint values
    private Vector3 _northEntry, _northExit;
    private Vector3 _southEntry, _southExit;
    private Vector3 _westEntry, _westExit;
    private Vector3 _eastEntry, _eastExit;
    private Vector3 _midpointNW, _midpointNE, _midpointSW, _midpointSE;
    private bool _hasNorth, _hasSouth, _hasWest, _hasEast;

    public event Action OnRoadWaypointsUpdated;

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

        _northEntry = _cellCentre + new Vector3(_laneCentre, 0, _halfCellSize);
        _northExit = _cellCentre + new Vector3(-_laneCentre, 0, _halfCellSize);

        _southEntry = _cellCentre + new Vector3(-_laneCentre, 0, -_halfCellSize);
        _southExit = _cellCentre + new Vector3(_laneCentre, 0, -_halfCellSize);

        _westEntry = _cellCentre + new Vector3(-_halfCellSize, 0, _laneCentre);
        _westExit = _cellCentre + new Vector3(-_halfCellSize, 0, -_laneCentre);

        _eastEntry = _cellCentre + new Vector3(_halfCellSize, 0, -_laneCentre);
        _eastExit = _cellCentre + new Vector3(_halfCellSize, 0, _laneCentre);

        _midpointNW = _cellCentre + new Vector3(-_laneCentre, 0, _laneCentre);
        _midpointNE = _cellCentre + new Vector3(_laneCentre, 0, _laneCentre);
        _midpointSW = _cellCentre + new Vector3(-_laneCentre, 0, -_laneCentre);
        _midpointSE = _cellCentre + new Vector3(_laneCentre, 0, -_laneCentre);

        _hasNorth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.North);
        _hasSouth = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.South);
        _hasEast = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.East);
        _hasWest = GridManager.Instance.HasRoadNeighbor(cell, RoadDirection.West);
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

        OnRoadWaypointsUpdated?.Invoke();
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
    }

    private List<WaypointNode> CreateStraightWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        if (_hasNorth && _hasSouth) // Vertical road
        {
            // Lane going North (traffic flows from South to North)
            WaypointNode southEntry = new WaypointNode(_southEntry, cell, WaypointType.Entry);
            WaypointNode northExit = new WaypointNode(_northExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            southEntry.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_southEntry, _northExit)));

            waypoints.Add(southEntry);
            waypoints.Add(northExit);

            // Lane going South (traffic flows from North to South)
            WaypointNode northEntry = new WaypointNode(_northEntry, cell, WaypointType.Entry);
            WaypointNode southExit = new WaypointNode(_southExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            northEntry.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_northEntry, _southExit)));

            waypoints.Add(northEntry);
            waypoints.Add(southExit);

            // Trafflic light loaction waypoints
            Vector3 wpLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, 0);
            Vector3 wpRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, 0);

            WaypointNode trafficLight1 = new WaypointNode(wpLeftLight, cell, WaypointType.TrafficLightLocation, southEntry, RoadDirection.West);
            WaypointNode trafficLight2 = new WaypointNode(wpRightLight, cell, WaypointType.TrafficLightLocation, northEntry, RoadDirection.East);

            trafficLight1.PairedCrossingWaypoint = trafficLight2;
            trafficLight2.PairedCrossingWaypoint = trafficLight1;

            waypoints.Add(trafficLight1);
            waypoints.Add(trafficLight2);

        }
        else if (_hasEast && _hasWest) // Horizontal road
        {
            // Lane going East (traffic flows from West to East)
            WaypointNode westEntry = new WaypointNode(_westEntry, cell, WaypointType.Entry);
            WaypointNode eastExit = new WaypointNode(_eastExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            westEntry.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_westEntry, _eastExit)));

            waypoints.Add(westEntry);
            waypoints.Add(eastExit);

            // Lane going West (traffic flows from East to West)
            WaypointNode eastEntry = new WaypointNode(_eastEntry, cell, WaypointType.Entry);
            WaypointNode westExit = new WaypointNode(_westExit, cell, WaypointType.Exit);

            // Connect entry to exit (one-way)
            eastEntry.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_eastEntry, _westExit)));

            waypoints.Add(eastEntry);
            waypoints.Add(westExit);

            // Trafflic light loaction waypoints
            Vector3 wpTopLight = _cellCentre + new Vector3(0, 0, -_halfCellSize + _halfPavementSize);
            Vector3 wpBottomLight = _cellCentre + new Vector3(0, 0, _halfCellSize - _halfPavementSize);

            WaypointNode trafficLight1 = new WaypointNode(wpTopLight, cell, WaypointType.TrafficLightLocation, westEntry, RoadDirection.North);
            WaypointNode trafficLight2 = new WaypointNode(wpBottomLight, cell, WaypointType.TrafficLightLocation, eastEntry, RoadDirection.South);

            trafficLight1.PairedCrossingWaypoint = trafficLight2;
            trafficLight2.PairedCrossingWaypoint = trafficLight1;

            waypoints.Add(trafficLight1);
            waypoints.Add(trafficLight2);
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
            // Lane going North to East
            WaypointNode northEntry = new WaypointNode(_northEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(_midpointNE, cell, WaypointType.Midpoint);
            WaypointNode eastExit = new WaypointNode(_eastExit, cell, WaypointType.Exit);

            // connections
            northEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(_northEntry, _midpointNE)));
            midpoint1.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNE, _eastExit)));

            // add waypoints
            waypoints.Add(northEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(eastExit);

            // Lane going East to North (reverse direction)
            WaypointNode eastEntry = new WaypointNode(_eastEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(_midpointSW, cell, WaypointType.Midpoint);
            WaypointNode northExit = new WaypointNode(_northExit, cell, WaypointType.Exit);

            // connections
            eastEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(_eastEntry, _midpointSW)));
            midpoint2.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointSW, _northExit)));

            // add waypoints
            waypoints.Add(eastEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(northExit);
        }
        else if (_hasNorth && _hasWest) // Corner from North to West
        {
            // Lane going North to West
            WaypointNode northEntry = new WaypointNode(_northEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(_midpointSE, cell, WaypointType.Midpoint);
            WaypointNode westExit = new WaypointNode(_westExit, cell, WaypointType.Exit);

            // connections
            northEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(_northEntry, _midpointSE)));
            midpoint1.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSE, _westExit)));

            // add waypoints
            waypoints.Add(northEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(westExit);

            // Lane going West to North (reverse direction)
            WaypointNode westEntry = new WaypointNode(_westEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(_midpointNW, cell, WaypointType.Midpoint);
            WaypointNode northExit = new WaypointNode(_northExit, cell, WaypointType.Exit);

            // connections
            westEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(_westEntry, _midpointNW)));
            midpoint2.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointNW, _northExit)));

            // add waypoints
            waypoints.Add(westEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(northExit);
        }
        else if (_hasSouth && _hasEast) // Corner from South to East
        {
            // Lane going South to East
            WaypointNode southEntry = new WaypointNode(_southEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(_midpointNW, cell, WaypointType.Midpoint);
            WaypointNode eastExit = new WaypointNode(_eastExit, cell, WaypointType.Exit);

            // connections
            southEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(_southEntry, _midpointNW)));
            midpoint1.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNW, _eastExit)));

            // add waypoints
            waypoints.Add(southEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(eastExit);

            // Lane going East to South (reverse direction)
            WaypointNode eastEntry = new WaypointNode(_eastEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(_midpointSE, cell, WaypointType.Midpoint);
            WaypointNode southExit = new WaypointNode(_southExit, cell, WaypointType.Exit);

            // connections
            eastEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(_eastEntry, _midpointSE)));
            midpoint2.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointSE, _southExit)));

            // add waypoints
            waypoints.Add(eastEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(southExit);
        }
        else if (_hasSouth && _hasWest) // Corner from South to West
        {
            // Lane going South to West
            WaypointNode southEntry = new WaypointNode(_southEntry, cell, WaypointType.Entry);
            WaypointNode midpoint1 = new WaypointNode(_midpointSW, cell, WaypointType.Midpoint);
            WaypointNode westExit = new WaypointNode(_westExit, cell, WaypointType.Exit);

            // connections
            southEntry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(_southEntry, _midpointSW)));
            midpoint1.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSW, _westExit)));

            // add waypoints
            waypoints.Add(southEntry);
            waypoints.Add(midpoint1);
            waypoints.Add(westExit);

            // Lane going West to South (reverse direction)
            WaypointNode westEntry = new WaypointNode(_westEntry, cell, WaypointType.Entry);
            WaypointNode midpoint2 = new WaypointNode(_midpointNE, cell, WaypointType.Midpoint);
            WaypointNode southExit = new WaypointNode(_southExit, cell, WaypointType.Exit);

            // connections
            westEntry.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(_westEntry, _midpointNE)));
            midpoint2.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointNE, _southExit)));

            // add waypoints
            waypoints.Add(westEntry);
            waypoints.Add(midpoint2);
            waypoints.Add(southExit);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateTJunctionWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // first create all of the waypoints
        // we will only need three quarters, but it really doesn't matter, we won,t add the unwanted nodes to the list
        WaypointNode northEntry = new WaypointNode(_northEntry, cell, WaypointType.Entry);
        WaypointNode southEntry = new WaypointNode(_southEntry, cell, WaypointType.Entry);
        WaypointNode westEntry = new WaypointNode(_westEntry, cell, WaypointType.Entry);
        WaypointNode eastEntry = new WaypointNode(_eastEntry, cell, WaypointType.Entry);
        WaypointNode northExit = new WaypointNode(_northExit, cell, WaypointType.Exit);
        WaypointNode southExit = new WaypointNode(_southExit, cell, WaypointType.Exit);
        WaypointNode westExit = new WaypointNode(_westExit, cell, WaypointType.Exit);
        WaypointNode eastExit = new WaypointNode(_eastExit, cell, WaypointType.Exit);
        WaypointNode midpointNW = new WaypointNode(_midpointNW, cell, WaypointType.Midpoint);
        WaypointNode midpointNE = new WaypointNode(_midpointNE, cell, WaypointType.Midpoint);
        WaypointNode midpointSW = new WaypointNode(_midpointSW, cell, WaypointType.Midpoint);
        WaypointNode midpointSE = new WaypointNode(_midpointSE, cell, WaypointType.Midpoint);

        // T-Junction with North, East, and West (missing South)
        if (_hasNorth && _hasEast && _hasWest && !_hasSouth)
        {
            // Lane 1: North to East (left turn)
            northEntry.Connections.Add(new WaypointConnection(midpointNE, Vector3.Distance(_northEntry, _midpointNE)));
            midpointNE.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNE, _eastExit)));

            // Lane 2: North to West (right turn)
            northEntry.Connections.Add(new WaypointConnection(midpointSE, Vector3.Distance(_northEntry, _midpointSE)));
            midpointSE.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSE, _westExit)));

            // Lane 3: East to North (right turn)
            eastEntry.Connections.Add(new WaypointConnection(midpointSW, Vector3.Distance(_eastEntry, _midpointSW)));
            midpointSW.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointSW, _northExit)));

            // Lane 4: East to West (straight through)
            eastEntry.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_eastEntry, _westExit)));

            // Lane 5: West to North (left turn)
            westEntry.Connections.Add(new WaypointConnection(midpointNW, Vector3.Distance(_westEntry, _midpointNW)));
            midpointNW.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointNW, _northExit)));

            // Lane 6: West to East (straight through)
            westEntry.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_westEntry, _eastExit)));
        }
        // T-Junction with North, East, and South (missing West)
        else if (_hasNorth && _hasEast && _hasSouth && !_hasWest)
        {
            // Lane 1: North to East (left turn)
            northEntry.Connections.Add(new WaypointConnection(midpointNE, Vector3.Distance(_northEntry, _midpointNE)));
            midpointNE.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNE, _eastExit)));

            // Lane 2: North to South (straight through)
            northEntry.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_northEntry, _southExit)));

            // Lane 3: East to South (left turn)
            eastEntry.Connections.Add(new WaypointConnection(midpointSE, Vector3.Distance(_eastEntry, _midpointSE)));
            midpointSE.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointSE, _southExit)));

            // Lane 4: East to North (right turn)
            eastEntry.Connections.Add(new WaypointConnection(midpointSW, Vector3.Distance(_eastEntry, _midpointSW)));
            midpointSW.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointSW, _northExit)));

            // Lane 5: South to North (straight through)
            southEntry.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_southEntry, _northExit)));

            // Lane 6: South to East (right turn)
            southEntry.Connections.Add(new WaypointConnection(midpointNW, Vector3.Distance(_southEntry, _midpointNW)));
            midpointNW.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNW, _eastExit)));
        }
        // T-Junction with North, South, and West (missing East)
        else if (_hasNorth && _hasSouth && _hasWest && !_hasEast)
        {

            // Lane 1: North to South (straight through)
            northEntry.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_northEntry, _southExit)));

            // Lane 2: North to West (right turn)
            northEntry.Connections.Add(new WaypointConnection(midpointSE, Vector3.Distance(_northEntry, _midpointSE)));
            midpointSE.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSE, _westExit)));

            // Lane 3: South to West (left turn)
            southEntry.Connections.Add(new WaypointConnection(midpointSW, Vector3.Distance(_southEntry, _midpointSW)));
            midpointSW.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSW, _westExit)));

            // Lane 4: South to North (straight through)
            southEntry.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_southEntry, _northExit)));

            // Lane 5: West to North (left turn)
            westEntry.Connections.Add(new WaypointConnection(midpointNW, Vector3.Distance(_westEntry, _midpointNW)));
            midpointNW.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointNW, _northExit)));

            // Lane 6: West to South (right turn)
            westEntry.Connections.Add(new WaypointConnection(midpointNE, Vector3.Distance(_westEntry, _midpointNE)));
            midpointNE.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointNE, _southExit)));
        }
        // T-Junction with East, South, and West (missing North)
        else if (_hasEast && _hasSouth && _hasWest && !_hasNorth)
        {
            // Lane 1: East to West (straight through)
            eastEntry.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_eastEntry, _westExit)));

            // Lane 2: East to South (right turn)
            eastEntry.Connections.Add(new WaypointConnection(midpointSE, Vector3.Distance(_eastEntry, _midpointSE)));
            midpointSE.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointSE, _southExit)));

            // Lane 3: South to West (right turn)
            southEntry.Connections.Add(new WaypointConnection(midpointSW, Vector3.Distance(_southEntry, _midpointSW)));
            midpointSW.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSW, _westExit)));

            // Lane 4: South to East (left turn)
            southEntry.Connections.Add(new WaypointConnection(midpointNW, Vector3.Distance(_southEntry, _midpointNW)));
            midpointNW.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNW, _eastExit)));

            // Lane 5: West to East (straight through)
            westEntry.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_westEntry, _eastExit)));

            // Lane 6: West to South (right turn)
            westEntry.Connections.Add(new WaypointConnection(midpointNE, Vector3.Distance(_westEntry, _midpointNE)));
            midpointNE.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointNE, _southExit)));
        }

        if (_hasNorth) waypoints.Add(northEntry);
        if (_hasSouth) waypoints.Add(southEntry);
        if (_hasWest) waypoints.Add(westEntry);
        if (_hasEast) waypoints.Add(eastEntry);
        if (_hasNorth) waypoints.Add(northExit);
        if (_hasSouth) waypoints.Add(southExit);
        if (_hasWest) waypoints.Add(westExit);
        if (_hasEast) waypoints.Add(eastExit);
        waypoints.Add(midpointNW);
        waypoints.Add(midpointNE);
        waypoints.Add(midpointSW);
        waypoints.Add(midpointSE);

        // Trafflic light loaction waypoints
        Vector3 wpTopLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
        Vector3 wpTopRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
        Vector3 wpBottomLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);
        Vector3 wpBottomRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);

        WaypointNode trafficLight1 = new WaypointNode(wpTopLeftLight, cell, WaypointType.TrafficLightLocation, westEntry, RoadDirection.NorthWest);
        WaypointNode trafficLight2 = new WaypointNode(wpTopRightLight, cell, WaypointType.TrafficLightLocation, northEntry, RoadDirection.NorthEast);
        WaypointNode trafficLight3 = new WaypointNode(wpBottomLeftLight, cell, WaypointType.TrafficLightLocation, southEntry, RoadDirection.SouthWest);
        WaypointNode trafficLight4 = new WaypointNode(wpBottomRightLight, cell, WaypointType.TrafficLightLocation, eastEntry, RoadDirection.SouthEast);

        if (_hasWest) waypoints.Add(trafficLight1);
        if (_hasNorth) waypoints.Add(trafficLight2);
        if (_hasSouth) waypoints.Add(trafficLight3);
        if (_hasEast) waypoints.Add(trafficLight4);

        return waypoints;
    }

    private List<WaypointNode> CreateCrossroadsWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // Crossroads with all four directions
        if (_hasNorth && _hasSouth && _hasEast && _hasWest)
        {
            // first create all of the waypoints
            WaypointNode northEntry = new WaypointNode(_northEntry, cell, WaypointType.Entry);
            WaypointNode southEntry = new WaypointNode(_southEntry, cell, WaypointType.Entry);
            WaypointNode westEntry = new WaypointNode(_westEntry, cell, WaypointType.Entry);
            WaypointNode eastEntry = new WaypointNode(_eastEntry, cell, WaypointType.Entry);
            WaypointNode northExit = new WaypointNode(_northExit, cell, WaypointType.Exit);
            WaypointNode southExit = new WaypointNode(_southExit, cell, WaypointType.Exit);
            WaypointNode westExit = new WaypointNode(_westExit, cell, WaypointType.Exit);
            WaypointNode eastExit = new WaypointNode(_eastExit, cell, WaypointType.Exit);
            WaypointNode midpointNW = new WaypointNode(_midpointNW, cell, WaypointType.Midpoint);
            WaypointNode midpointNE = new WaypointNode(_midpointNE, cell, WaypointType.Midpoint);
            WaypointNode midpointSW = new WaypointNode(_midpointSW, cell, WaypointType.Midpoint);
            WaypointNode midpointSE = new WaypointNode(_midpointSE, cell, WaypointType.Midpoint);

            // connect each entry waypoint to its oposite exit waypoint
            northEntry.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_northEntry, _southExit)));
            southEntry.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_southEntry, _northExit)));
            westEntry.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_westEntry, _eastExit)));
            eastEntry.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_eastEntry, _westExit)));

            // connect each entry to its two possible midpoints
            northEntry.Connections.Add(new WaypointConnection(midpointNE, Vector3.Distance(_northEntry, _midpointNE)));
            northEntry.Connections.Add(new WaypointConnection(midpointSE, Vector3.Distance(_northEntry, _midpointSE)));
            southEntry.Connections.Add(new WaypointConnection(midpointNW, Vector3.Distance(_southEntry, _midpointNW)));
            southEntry.Connections.Add(new WaypointConnection(midpointSW, Vector3.Distance(_southEntry, _midpointSW)));
            eastEntry.Connections.Add(new WaypointConnection(midpointSW, Vector3.Distance(_eastEntry, _midpointSW)));
            eastEntry.Connections.Add(new WaypointConnection(midpointSE, Vector3.Distance(_eastEntry, _midpointSE)));
            westEntry.Connections.Add(new WaypointConnection(midpointNW, Vector3.Distance(_westEntry, _midpointNW)));
            westEntry.Connections.Add(new WaypointConnection(midpointNE, Vector3.Distance(_westEntry, _midpointNE)));

            // connect midpoints to their exit points
            midpointNE.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNE, _eastExit)));
            midpointSE.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSE, _westExit)));
            midpointNW.Connections.Add(new WaypointConnection(eastExit, Vector3.Distance(_midpointNW, _eastExit)));
            midpointSW.Connections.Add(new WaypointConnection(westExit, Vector3.Distance(_midpointSW, _westExit)));
            midpointSW.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointSW, _northExit)));
            midpointSE.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointSE, _southExit)));
            midpointNW.Connections.Add(new WaypointConnection(northExit, Vector3.Distance(_midpointNW, _northExit)));
            midpointNE.Connections.Add(new WaypointConnection(southExit, Vector3.Distance(_midpointNE, _southExit)));

            // add the waypoints
            waypoints.Add(northEntry);
            waypoints.Add(southEntry);
            waypoints.Add(westEntry);
            waypoints.Add(eastEntry);
            waypoints.Add(northExit);
            waypoints.Add(southExit);
            waypoints.Add(westExit);
            waypoints.Add(eastExit);
            waypoints.Add(midpointNW);
            waypoints.Add(midpointNE);
            waypoints.Add(midpointSW);
            waypoints.Add(midpointSE);

            // Trafflic light loaction waypoints
            Vector3 wpTopLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
            Vector3 wpTopRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
            Vector3 wpBottomLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);
            Vector3 wpBottomRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);

            WaypointNode trafficLight1 = new WaypointNode(wpTopLeftLight, cell, WaypointType.TrafficLightLocation, westEntry, RoadDirection.NorthWest);
            WaypointNode trafficLight2 = new WaypointNode(wpTopRightLight, cell, WaypointType.TrafficLightLocation, northEntry, RoadDirection.NorthEast);
            WaypointNode trafficLight3 = new WaypointNode(wpBottomLeftLight, cell, WaypointType.TrafficLightLocation, southEntry, RoadDirection.SouthWest);
            WaypointNode trafficLight4 = new WaypointNode(wpBottomRightLight, cell, WaypointType.TrafficLightLocation, eastEntry, RoadDirection.SouthEast);

            waypoints.Add(trafficLight1);
            waypoints.Add(trafficLight2);
            waypoints.Add(trafficLight3);
            waypoints.Add(trafficLight4);
        }

        return waypoints;
    }

    private List<WaypointNode> CreateDeadEndWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();
        // return waypoints;

        Vector3 wpEntry = Vector3.zero, wpMidpoint1 = Vector3.zero, wpUTurn = Vector3.zero, wpMidpoint2 = Vector3.zero, wpExit = Vector3.zero;
        WaypointNode entry = null, midpoint1 = null, uTurn = null, midpoint2 = null, exit = null;

        if (_hasNorth)
        {
            entry = new WaypointNode(_northEntry, cell, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(_laneCentre, 0, 0);
            wpUTurn = _cellCentre - new Vector3(0, 0, _quarterCellSize);
            wpMidpoint2 = _cellCentre + new Vector3(-_laneCentre, 0, 0);
            exit = new WaypointNode(_northExit, cell, WaypointType.Exit);
        }
        else if (_hasSouth)
        {
            entry = new WaypointNode(_southEntry, cell, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(-_laneCentre, 0, 0);
            wpUTurn = _cellCentre - new Vector3(0, 0, -_quarterCellSize);
            wpMidpoint2 = _cellCentre + new Vector3(_laneCentre, 0, 0);
            exit = new WaypointNode(_southExit, cell, WaypointType.Exit);
        }
        else if (_hasEast)
        {
            entry = new WaypointNode(_eastEntry, cell, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(0, 0, -_laneCentre);
            wpUTurn = _cellCentre - new Vector3(_quarterCellSize, 0, 0);
            wpMidpoint2 = _cellCentre + new Vector3(0, 0, _laneCentre);
            exit = new WaypointNode(_eastExit, cell, WaypointType.Exit);
        }
        else if (_hasWest)
        {
            entry = new WaypointNode(_westEntry, cell, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(0, 0, _laneCentre);
            wpUTurn = _cellCentre - new Vector3(-_quarterCellSize, 0, 0);
            wpMidpoint2 = _cellCentre + new Vector3(0, 0, -_laneCentre);
            exit = new WaypointNode(_westExit, cell, WaypointType.Exit);
        }

        midpoint1 = new WaypointNode(wpMidpoint1, cell, WaypointType.Midpoint);
        uTurn = new WaypointNode(wpUTurn, cell, WaypointType.UTurn);
        midpoint2 = new WaypointNode(wpMidpoint2, cell, WaypointType.Midpoint);

        // Connect entry to exit
        entry.Connections.Add(new WaypointConnection(midpoint1, Vector3.Distance(wpEntry, wpMidpoint1)));
        midpoint1.Connections.Add(new WaypointConnection(uTurn, Vector3.Distance(wpMidpoint1, wpUTurn)));
        uTurn.Connections.Add(new WaypointConnection(midpoint2, Vector3.Distance(wpUTurn, wpMidpoint2)));
        midpoint2.Connections.Add(new WaypointConnection(exit, Vector3.Distance(wpMidpoint2, wpExit)));

        waypoints.Add(entry);
        waypoints.Add(midpoint1);
        waypoints.Add(uTurn);
        waypoints.Add(midpoint2);
        waypoints.Add(exit);

        return waypoints;
    }

    public void ConnectAllCells()
    {
        // After all cells have been created, connect neighboring cells
        foreach (var kvp in _cellWaypoints)
        {
            GridCell cell = kvp.Key;
            List<WaypointNode> waypoints = kvp.Value;
            ConnectToNeighboringCells(cell, waypoints);
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
                    ConnectWaypointsToNeighbor(cell, waypoints, neighbor, i);
                }
            }
        }
    }

    private void ConnectWaypointsToNeighbor(GridCell cell, List<WaypointNode> waypoints, GridCell neighbor, int direction)
    {
        if (!_cellWaypoints.ContainsKey(neighbor))
            return;

        List<WaypointNode> cellExitWaypoints = new List<WaypointNode>();
        List<WaypointNode> neighborEntryWaypoints = new List<WaypointNode>();

        // Get exit waypoints from current cell and entry waypoints from neighbor
        cellExitWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
        neighborEntryWaypoints = _cellWaypoints[neighbor].Where(w => w.Type == WaypointType.Entry).ToList();

        // Connect exit waypoints to entry waypoints only if they are at the same position (or very close)
        foreach (var exitWaypoint in cellExitWaypoints)
        {
            foreach (var entryWaypoint in neighborEntryWaypoints)
            {
                // Check if the waypoints are at the same position (or very close)
                float distance = Vector3.Distance(exitWaypoint.Position, entryWaypoint.Position);

                // If the distance is very small (essentially zero), connect them
                if (distance < 0.05f) // Tolerance for floating point precision
                {
                    exitWaypoint.Connections.Add(new WaypointConnection(entryWaypoint, distance));
                }
            }
        }
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
        var waypointData = new WaypointSaveData();

        foreach (var node in _allWaypoints)
        {
            var nodeData = new WaypointNodeSaveData
            {
                Id = node.Id,
                X = node.Position.x,
                Z = node.Position.z,
                Type = node.Type,
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

        saveData.waypoints = waypointData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.waypoints == null)
        {
            Debug.LogWarning("[WaypointManager] No waypoint data in save file.");
            return;
        }

        _allWaypoints.Clear();
        var nodeLookup = new Dictionary<string, WaypointNode>();

        // First pass — create all nodes
        foreach (var nodeData in saveData.waypoints.Nodes)
        {
            // Retrieve the parent cell from the grid
            var parentCell = GridManager.Instance.GetCell(nodeData.ParentCellX, nodeData.ParentCellZ);
            if (parentCell == null)
            {
                Debug.LogWarning($"[WaypointManager] Parent cell ({nodeData.ParentCellX}, {nodeData.ParentCellZ}) not found for node {nodeData.Id}.");
                continue;
            }

            var node = new WaypointNode(
                new Vector3(nodeData.X, 0f, nodeData.Z),
                parentCell,
                nodeData.Type
            );

            // Restore the saved ID rather than using the new GUID generated in the constructor
            node.Id = nodeData.Id;

            // Restore paired crossing waypoint reference (if any)
            if (!string.IsNullOrEmpty(nodeData.PairedCrossingWaypointId))
            {
                node.PairedCrossingWaypointId = nodeData.PairedCrossingWaypointId;  // Store ID for later resolution
            }

            // Restore traffic light lane waypoint reference (if any)
            if (!string.IsNullOrEmpty(nodeData.LaneNodeForTrafficLightId))
            {
                node.LaneNodeForTrafficLightId = nodeData.LaneNodeForTrafficLightId;  // Store ID for later resolution
                node.LightPosition = nodeData.LightPosition;
            }

            _allWaypoints.Add(node);
            nodeLookup[node.Id] = node;
        }

        // Second pass — restore connections
        foreach (var nodeData in saveData.waypoints.Nodes)
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

        // Third pass — resolve paired crossing waypoints (after all nodes are created)
        foreach (var node in _allWaypoints)
        {
            if (!string.IsNullOrEmpty(node.PairedCrossingWaypointId) &&
                nodeLookup.TryGetValue(node.PairedCrossingWaypointId, out var pairedNode))
            {
                node.PairedCrossingWaypoint = pairedNode;
            }
            if (!string.IsNullOrEmpty(node.LaneNodeForTrafficLightId) &&
                nodeLookup.TryGetValue(node.LaneNodeForTrafficLightId, out var laneNode))
            {
                node.LaneNodeForTrafficLight = laneNode;
            }
        }

        Debug.Log($"[WaypointManager] Loaded {_allWaypoints.Count} waypoint nodes.");
    }

    public Dictionary<string, WaypointNode> GetAllWaypointLookup()
    {
        var lookup = new Dictionary<string, WaypointNode>();
        foreach (var node in _allWaypoints)
        {
            lookup[node.Id] = node;
        }
        return lookup;
    }

    // private void OnDrawGizmos()
    // {
    //     if (_allWaypoints.Count == 0) return;

    //     foreach (WaypointNode node in _allWaypoints)
    //     {
    //         if (node.Type == WaypointType.TrafficLightLocation)
    //         {
    //             Gizmos.color = Color.yellow;
    //             Gizmos.DrawSphere(node.Position, 0.5f);
    //         }
    //     }
    // }
}