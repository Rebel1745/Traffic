using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadWaypointManager : MonoBehaviour, IWaypointNetwork, ISaveable
{
    public static RoadWaypointManager Instance { get; private set; }

    public string SaveKey => "VehicleWaypoints";

    private Dictionary<EntityId, WaypointNode> _allWaypoints = new Dictionary<EntityId, WaypointNode>();
    private List<WaypointNode>[,] _cellWaypoints;

    // cell calculations
    private int _gridWidth;
    private int _gridHeight;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SaveManager.Instance.RegisterSaveable(this);
        RoadMeshRenderer.Instance.OnRoadMeshUpdated += RoadMeshUpdated;

        _gridWidth = GridManager.Instance.GridWidth;
        _gridHeight = GridManager.Instance.GridHeight;
    }

    private void OnDestroy()
    {
        RoadMeshRenderer.Instance.OnRoadMeshUpdated -= RoadMeshUpdated;
    }

    private void RoadMeshUpdated()
    {
        GenerateWaypoints();
    }

    private WaypointNode CreateWaypoint(GridCell cell, Vector3 position, WaypointType type, WaypointNetworkType networkType = WaypointNetworkType.Vehicle, WaypointNode laneNode = null, RoadDirection lightPos = RoadDirection.None)
    {
        return new(position, cell, type, networkType);
    }

    private void AddWaypoint(GridCell cell, WaypointNode waypoint)
    {
        _allWaypoints[waypoint.Id] = waypoint;

        if (_cellWaypoints[cell.Position.x, cell.Position.z] == null)
            _cellWaypoints[cell.Position.x, cell.Position.z] = new List<WaypointNode>();

        _cellWaypoints[cell.Position.x, cell.Position.z].Add(waypoint);
    }

    private WaypointNode CreateAndAddWaypoint(GridCell cell, Vector3 position, WaypointType type, WaypointNetworkType networkType = WaypointNetworkType.Vehicle, WaypointNode laneNode = null, RoadDirection lightPos = RoadDirection.None)
    {
        // create the waypoint
        WaypointNode newNode = CreateWaypoint(cell, position, type, networkType);

        // add the waypoint
        AddWaypoint(cell, newNode);

        return newNode;
    }

    private void AddWaypointConnection(WaypointNode source, WaypointNode target, float cost, bool twoWay = false)
    {
        source.Connections[target] = cost;

        if (twoWay)
            target.Connections[source] = cost;
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

        _hasNorth = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.North);
        _hasSouth = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.South);
        _hasEast = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.East);
        _hasWest = GridManager.Instance.HasRoadNeighbour(cell, RoadDirection.West);
    }

    public void GenerateWaypoints()
    {
        GridCell[,] grid = GridManager.Instance.GetGrid();

        _cellWaypoints = new List<WaypointNode>[GridManager.Instance.GridWidth, GridManager.Instance.GridHeight];
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

        CalculateEntryExitAndMidpointsForCell(cell);

        switch (cell.RoadType)
        {
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
    }

    private void CreateStraightWaypoints(GridCell cell)
    {
        if (_hasNorth && _hasSouth) // Vertical road
        {
            // Lane going North (traffic flows from South to North)
            WaypointNode southEntry = CreateAndAddWaypoint(cell, _southEntry, WaypointType.Entry);
            WaypointNode northExit = CreateAndAddWaypoint(cell, _northExit, WaypointType.Exit);

            // Connect entry to exit (one-way)
            AddWaypointConnection(southEntry, northExit, Vector3.Distance(_southEntry, _northExit));

            // Lane going South (traffic flows from North to South)
            WaypointNode northEntry = CreateAndAddWaypoint(cell, _northEntry, WaypointType.Entry);
            WaypointNode southExit = CreateAndAddWaypoint(cell, _southExit, WaypointType.Exit);

            // Connect entry to exit (one-way)
            AddWaypointConnection(northEntry, southExit, Vector3.Distance(_northEntry, _southExit));

            // Trafflic light loaction waypoints
            Vector3 wpLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, 0);
            Vector3 wpRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, 0);

            WaypointNode trafficLight1 = CreateAndAddWaypoint(cell, wpLeftLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, southEntry, RoadDirection.West);
            WaypointNode trafficLight2 = CreateAndAddWaypoint(cell, wpRightLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, northEntry, RoadDirection.East);

            trafficLight1.PairedCrossingWaypoint = trafficLight2;
            trafficLight2.PairedCrossingWaypoint = trafficLight1;
        }
        else if (_hasEast && _hasWest) // Horizontal road
        {
            // Lane going East (traffic flows from West to East)
            WaypointNode westEntry = CreateAndAddWaypoint(cell, _westEntry, WaypointType.Entry);
            WaypointNode eastExit = CreateAndAddWaypoint(cell, _eastExit, WaypointType.Exit);

            // Connect entry to exit (one-way)
            AddWaypointConnection(westEntry, eastExit, Vector3.Distance(_westEntry, _eastExit));

            // Lane going West (traffic flows from East to West)
            WaypointNode eastEntry = CreateAndAddWaypoint(cell, _eastEntry, WaypointType.Entry);
            WaypointNode westExit = CreateAndAddWaypoint(cell, _westExit, WaypointType.Exit);

            // Connect entry to exit (one-way)
            AddWaypointConnection(eastEntry, westExit, Vector3.Distance(_eastEntry, _westExit));

            // Trafflic light loaction waypoints
            Vector3 wpTopLight = _cellCentre + new Vector3(0, 0, -_halfCellSize + _halfPavementSize);
            Vector3 wpBottomLight = _cellCentre + new Vector3(0, 0, _halfCellSize - _halfPavementSize);

            WaypointNode trafficLight1 = CreateAndAddWaypoint(cell, wpTopLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, westEntry, RoadDirection.North);
            WaypointNode trafficLight2 = CreateAndAddWaypoint(cell, wpBottomLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, eastEntry, RoadDirection.South);

            trafficLight1.PairedCrossingWaypoint = trafficLight2;
            trafficLight2.PairedCrossingWaypoint = trafficLight1;
        }
    }

    private void CreateCornerWaypoints(GridCell cell)
    {
        // Corner cases
        if (_hasNorth && _hasEast) // Corner from North to East
        {
            // Lane going North to East
            WaypointNode northEntry = CreateAndAddWaypoint(cell, _northEntry, WaypointType.Entry);
            WaypointNode midpoint1 = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.Midpoint);
            WaypointNode eastExit = CreateAndAddWaypoint(cell, _eastExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(northEntry, midpoint1, Vector3.Distance(_northEntry, _midpointNE));
            AddWaypointConnection(midpoint1, eastExit, Vector3.Distance(_midpointNE, _eastExit));

            // Lane going East to North (reverse direction)
            WaypointNode eastEntry = CreateAndAddWaypoint(cell, _eastEntry, WaypointType.Entry);
            WaypointNode midpoint2 = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.Midpoint);
            WaypointNode northExit = CreateAndAddWaypoint(cell, _northExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(eastEntry, midpoint2, Vector3.Distance(_eastEntry, _midpointSW));
            AddWaypointConnection(midpoint2, northExit, Vector3.Distance(_midpointSW, _northExit));
        }
        else if (_hasNorth && _hasWest) // Corner from North to West
        {
            // Lane going North to West
            WaypointNode northEntry = CreateAndAddWaypoint(cell, _northEntry, WaypointType.Entry);
            WaypointNode midpoint1 = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.Midpoint);
            WaypointNode westExit = CreateAndAddWaypoint(cell, _westExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(northEntry, midpoint1, Vector3.Distance(_northEntry, _midpointSE));
            AddWaypointConnection(midpoint1, westExit, Vector3.Distance(_midpointSE, _westExit));

            // Lane going West to North (reverse direction)
            WaypointNode westEntry = CreateAndAddWaypoint(cell, _westEntry, WaypointType.Entry);
            WaypointNode midpoint2 = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.Midpoint);
            WaypointNode northExit = CreateAndAddWaypoint(cell, _northExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(westEntry, midpoint2, Vector3.Distance(_westEntry, _midpointNW));
            AddWaypointConnection(midpoint2, northExit, Vector3.Distance(_midpointNW, _northExit));
        }
        else if (_hasSouth && _hasEast) // Corner from South to East
        {
            // Lane going South to East
            WaypointNode southEntry = CreateAndAddWaypoint(cell, _southEntry, WaypointType.Entry);
            WaypointNode midpoint1 = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.Midpoint);
            WaypointNode eastExit = CreateAndAddWaypoint(cell, _eastExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(southEntry, midpoint1, Vector3.Distance(_southEntry, _midpointNW));
            AddWaypointConnection(midpoint1, eastExit, Vector3.Distance(_midpointNW, _eastExit));

            // Lane going East to South (reverse direction)
            WaypointNode eastEntry = CreateAndAddWaypoint(cell, _eastEntry, WaypointType.Entry);
            WaypointNode midpoint2 = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.Midpoint);
            WaypointNode southExit = CreateAndAddWaypoint(cell, _southExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(eastEntry, midpoint2, Vector3.Distance(_eastEntry, _midpointSE));
            AddWaypointConnection(midpoint2, southExit, Vector3.Distance(_midpointSE, _southExit));
        }
        else if (_hasSouth && _hasWest) // Corner from South to West
        {
            // Lane going South to West
            WaypointNode southEntry = CreateAndAddWaypoint(cell, _southEntry, WaypointType.Entry);
            WaypointNode midpoint1 = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.Midpoint);
            WaypointNode westExit = CreateAndAddWaypoint(cell, _westExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(southEntry, midpoint1, Vector3.Distance(_southEntry, _midpointSW));
            AddWaypointConnection(midpoint1, westExit, Vector3.Distance(_midpointSW, _westExit));

            // Lane going West to South (reverse direction)
            WaypointNode westEntry = CreateAndAddWaypoint(cell, _westEntry, WaypointType.Entry);
            WaypointNode midpoint2 = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.Midpoint);
            WaypointNode southExit = CreateAndAddWaypoint(cell, _southExit, WaypointType.Exit);

            // connections
            AddWaypointConnection(westEntry, midpoint2, Vector3.Distance(_westEntry, _midpointNE));
            AddWaypointConnection(midpoint2, southExit, Vector3.Distance(_midpointNE, _southExit));
        }
    }

    private void CreateTJunctionWaypoints(GridCell cell)
    {
        // first create all of the waypoints
        // we will only need three quarters, but it really doesn't matter, we won,t add the unwanted nodes to the list
        WaypointNode northEntry = CreateWaypoint(cell, _northEntry, WaypointType.Entry);
        WaypointNode southEntry = CreateWaypoint(cell, _southEntry, WaypointType.Entry);
        WaypointNode westEntry = CreateWaypoint(cell, _westEntry, WaypointType.Entry);
        WaypointNode eastEntry = CreateWaypoint(cell, _eastEntry, WaypointType.Entry);
        WaypointNode northExit = CreateWaypoint(cell, _northExit, WaypointType.Exit);
        WaypointNode southExit = CreateWaypoint(cell, _southExit, WaypointType.Exit);
        WaypointNode westExit = CreateWaypoint(cell, _westExit, WaypointType.Exit);
        WaypointNode eastExit = CreateWaypoint(cell, _eastExit, WaypointType.Exit);
        WaypointNode midpointNW = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.Midpoint);
        WaypointNode midpointNE = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.Midpoint);
        WaypointNode midpointSW = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.Midpoint);
        WaypointNode midpointSE = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.Midpoint);

        // Trafflic light loaction waypoints. Do this first so we can configure pedestrian only lights as we go
        Vector3 wpTopLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
        Vector3 wpTopRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
        Vector3 wpBottomLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);
        Vector3 wpBottomRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);

        WaypointNode trafficLight1 = CreateAndAddWaypoint(cell, wpTopLeftLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, westEntry, RoadDirection.NorthWest);
        WaypointNode trafficLight2 = CreateAndAddWaypoint(cell, wpTopRightLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, northEntry, RoadDirection.NorthEast);
        WaypointNode trafficLight3 = CreateAndAddWaypoint(cell, wpBottomLeftLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, southEntry, RoadDirection.SouthWest);
        WaypointNode trafficLight4 = CreateAndAddWaypoint(cell, wpBottomRightLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, eastEntry, RoadDirection.SouthEast);

        // T-Junction with North, East, and West (missing South)
        if (_hasNorth && _hasEast && _hasWest && !_hasSouth)
        {
            // Lane 1: North to East (left turn)
            AddWaypointConnection(northEntry, midpointNE, Vector3.Distance(_northEntry, _midpointNE));
            AddWaypointConnection(midpointNE, eastExit, Vector3.Distance(_midpointNE, _eastExit));

            // Lane 2: North to West (right turn)
            AddWaypointConnection(northEntry, midpointSE, Vector3.Distance(_northEntry, _midpointSE));
            AddWaypointConnection(midpointSE, westExit, Vector3.Distance(_midpointSE, _westExit));

            // Lane 3: East to North (right turn)
            AddWaypointConnection(eastEntry, midpointSW, Vector3.Distance(_eastEntry, _midpointSW));
            AddWaypointConnection(midpointSW, northExit, Vector3.Distance(_midpointSW, _northExit));

            // Lane 4: East to West (straight through)
            AddWaypointConnection(eastEntry, westExit, Vector3.Distance(_eastEntry, _westExit));

            // Lane 5: West to North (left turn)
            AddWaypointConnection(westEntry, midpointNW, Vector3.Distance(_westEntry, _midpointNW));
            AddWaypointConnection(midpointNW, northExit, Vector3.Distance(_midpointNW, _northExit));

            // Lane 6: West to East (straight through)
            AddWaypointConnection(westEntry, eastExit, Vector3.Distance(_westEntry, _eastExit));

            trafficLight3.PedestiranOnlyTrafficLight = true;
        }
        // T-Junction with North, East, and South (missing West)
        else if (_hasNorth && _hasEast && _hasSouth && !_hasWest)
        {
            // Lane 1: North to East (left turn)
            AddWaypointConnection(northEntry, midpointNE, Vector3.Distance(_northEntry, _midpointNE));
            AddWaypointConnection(midpointNE, eastExit, Vector3.Distance(_midpointNE, _eastExit));

            // Lane 2: North to South (straight through)
            AddWaypointConnection(northEntry, southExit, Vector3.Distance(_northEntry, _southExit));

            // Lane 3: East to South (left turn)
            AddWaypointConnection(eastEntry, midpointSE, Vector3.Distance(_eastEntry, _midpointSE));
            AddWaypointConnection(midpointSE, southExit, Vector3.Distance(_midpointSE, _southExit));

            // Lane 4: East to North (right turn)
            AddWaypointConnection(eastEntry, midpointSW, Vector3.Distance(_eastEntry, _midpointSW));
            AddWaypointConnection(midpointSW, northExit, Vector3.Distance(_midpointSW, _northExit));

            // Lane 5: South to North (straight through)
            AddWaypointConnection(southEntry, northExit, Vector3.Distance(_southEntry, _northExit));

            // Lane 6: South to East (right turn)
            AddWaypointConnection(southEntry, midpointNW, Vector3.Distance(_southEntry, _midpointNW));
            AddWaypointConnection(midpointNW, eastExit, Vector3.Distance(_midpointNW, _eastExit));

            trafficLight1.PedestiranOnlyTrafficLight = true;
        }
        // T-Junction with North, South, and West (missing East)
        else if (_hasNorth && _hasSouth && _hasWest && !_hasEast)
        {

            // Lane 1: North to South (straight through)
            AddWaypointConnection(northEntry, southExit, Vector3.Distance(_northEntry, _southExit));

            // Lane 2: North to West (right turn)
            AddWaypointConnection(northEntry, midpointSE, Vector3.Distance(_northEntry, _midpointSE));
            AddWaypointConnection(midpointSE, westExit, Vector3.Distance(_midpointSE, _westExit));

            // Lane 3: South to West (left turn)
            AddWaypointConnection(southEntry, midpointSW, Vector3.Distance(_southEntry, _midpointSW));
            AddWaypointConnection(midpointSW, westExit, Vector3.Distance(_midpointSW, _westExit));

            // Lane 4: South to North (straight through)
            AddWaypointConnection(southEntry, northExit, Vector3.Distance(_southEntry, _northExit));

            // Lane 5: West to North (left turn)
            AddWaypointConnection(westEntry, midpointNW, Vector3.Distance(_westEntry, _midpointNW));
            AddWaypointConnection(midpointNW, northExit, Vector3.Distance(_midpointNW, _northExit));

            // Lane 6: West to South (right turn)
            AddWaypointConnection(westEntry, midpointNE, Vector3.Distance(_westEntry, _midpointNE));
            AddWaypointConnection(midpointNE, southExit, Vector3.Distance(_midpointNE, _southExit));

            trafficLight4.PedestiranOnlyTrafficLight = true;
        }
        // T-Junction with East, South, and West (missing North)
        else if (_hasEast && _hasSouth && _hasWest && !_hasNorth)
        {
            // Lane 1: East to West (straight through)
            AddWaypointConnection(eastEntry, westExit, Vector3.Distance(_eastEntry, _westExit));

            // Lane 2: East to South (right turn)
            AddWaypointConnection(eastEntry, midpointSE, Vector3.Distance(_eastEntry, _midpointSE));
            AddWaypointConnection(midpointSE, southExit, Vector3.Distance(_midpointSE, _southExit));

            // Lane 3: South to West (right turn)
            AddWaypointConnection(southEntry, midpointSW, Vector3.Distance(_southEntry, _midpointSW));
            AddWaypointConnection(midpointSW, westExit, Vector3.Distance(_midpointSW, _westExit));

            // Lane 4: South to East (left turn)
            AddWaypointConnection(southEntry, midpointNW, Vector3.Distance(_southEntry, _midpointNW));
            AddWaypointConnection(midpointNW, eastExit, Vector3.Distance(_midpointNW, _eastExit));

            // Lane 5: West to East (straight through)
            AddWaypointConnection(westEntry, eastExit, Vector3.Distance(_westEntry, _eastExit));

            // Lane 6: West to South (right turn)
            AddWaypointConnection(westEntry, midpointNE, Vector3.Distance(_westEntry, _midpointNE));
            AddWaypointConnection(midpointNE, southExit, Vector3.Distance(_midpointNE, _southExit));

            trafficLight2.PedestiranOnlyTrafficLight = true;
        }

        if (_hasNorth) AddWaypoint(cell, northEntry);
        if (_hasSouth) AddWaypoint(cell, southEntry);
        if (_hasWest) AddWaypoint(cell, westEntry);
        if (_hasEast) AddWaypoint(cell, eastEntry);
        if (_hasNorth) AddWaypoint(cell, northExit);
        if (_hasSouth) AddWaypoint(cell, southExit);
        if (_hasWest) AddWaypoint(cell, westExit);
        if (_hasEast) AddWaypoint(cell, eastExit);
    }

    private void CreateCrossroadsWaypoints(GridCell cell)
    {
        // Crossroads with all four directions
        if (_hasNorth && _hasSouth && _hasEast && _hasWest)
        {
            // first create all of the waypoints
            WaypointNode northEntry = CreateAndAddWaypoint(cell, _northEntry, WaypointType.Entry);
            WaypointNode southEntry = CreateAndAddWaypoint(cell, _southEntry, WaypointType.Entry);
            WaypointNode westEntry = CreateAndAddWaypoint(cell, _westEntry, WaypointType.Entry);
            WaypointNode eastEntry = CreateAndAddWaypoint(cell, _eastEntry, WaypointType.Entry);
            WaypointNode northExit = CreateAndAddWaypoint(cell, _northExit, WaypointType.Exit);
            WaypointNode southExit = CreateAndAddWaypoint(cell, _southExit, WaypointType.Exit);
            WaypointNode westExit = CreateAndAddWaypoint(cell, _westExit, WaypointType.Exit);
            WaypointNode eastExit = CreateAndAddWaypoint(cell, _eastExit, WaypointType.Exit);
            WaypointNode midpointNW = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.Midpoint);
            WaypointNode midpointNE = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.Midpoint);
            WaypointNode midpointSW = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.Midpoint);
            WaypointNode midpointSE = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.Midpoint);

            // connect each entry waypoint to its oposite exit waypoint
            AddWaypointConnection(northEntry, southExit, Vector3.Distance(_northEntry, _southExit));
            AddWaypointConnection(southEntry, northExit, Vector3.Distance(_southEntry, _northExit));
            AddWaypointConnection(westEntry, eastExit, Vector3.Distance(_westEntry, _eastExit));
            AddWaypointConnection(eastEntry, westExit, Vector3.Distance(_eastEntry, _westExit));

            // connect each entry to its two possible midpoints
            AddWaypointConnection(northEntry, midpointNE, Vector3.Distance(_northEntry, _midpointNE));
            AddWaypointConnection(northEntry, midpointSE, Vector3.Distance(_northEntry, _midpointSE));
            AddWaypointConnection(southEntry, midpointNW, Vector3.Distance(_southEntry, _midpointNW));
            AddWaypointConnection(southEntry, midpointSW, Vector3.Distance(_southEntry, _midpointSW));
            AddWaypointConnection(eastEntry, midpointSW, Vector3.Distance(_eastEntry, _midpointSW));
            AddWaypointConnection(eastEntry, midpointSE, Vector3.Distance(_eastEntry, _midpointSE));
            AddWaypointConnection(westEntry, midpointNW, Vector3.Distance(_westEntry, _midpointNW));
            AddWaypointConnection(westEntry, midpointNE, Vector3.Distance(_westEntry, _midpointNE));

            // connect midpoints to their exit points
            AddWaypointConnection(midpointNE, eastExit, Vector3.Distance(_midpointNE, _eastExit));
            AddWaypointConnection(midpointSE, westExit, Vector3.Distance(_midpointSE, _westExit));
            AddWaypointConnection(midpointNW, eastExit, Vector3.Distance(_midpointNW, _eastExit));
            AddWaypointConnection(midpointSW, westExit, Vector3.Distance(_midpointSW, _westExit));
            AddWaypointConnection(midpointSW, northExit, Vector3.Distance(_midpointSW, _northExit));
            AddWaypointConnection(midpointSE, southExit, Vector3.Distance(_midpointSE, _southExit));
            AddWaypointConnection(midpointNW, northExit, Vector3.Distance(_midpointNW, _northExit));
            AddWaypointConnection(midpointNE, southExit, Vector3.Distance(_midpointNE, _southExit));

            // Trafflic light loaction waypoints
            Vector3 wpTopLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
            Vector3 wpTopRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, _halfCellSize - _halfPavementSize);
            Vector3 wpBottomLeftLight = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);
            Vector3 wpBottomRightLight = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, -_halfCellSize + _halfPavementSize);

            CreateAndAddWaypoint(cell, wpTopLeftLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, westEntry, RoadDirection.NorthWest);
            CreateAndAddWaypoint(cell, wpTopRightLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, northEntry, RoadDirection.NorthEast);
            CreateAndAddWaypoint(cell, wpBottomLeftLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, southEntry, RoadDirection.SouthWest);
            CreateAndAddWaypoint(cell, wpBottomRightLight, WaypointType.TrafficLightLocation, WaypointNetworkType.Vehicle, eastEntry, RoadDirection.SouthEast);
        }
    }

    private void CreateDeadEndWaypoints(GridCell cell)
    {
        Vector3 wpEntry = Vector3.zero, wpMidpoint1 = Vector3.zero, wpUTurn = Vector3.zero, wpMidpoint2 = Vector3.zero, wpExit = Vector3.zero;
        WaypointNode entry = null, midpoint1 = null, uTurn = null, midpoint2 = null, exit = null;

        if (_hasNorth)
        {
            entry = CreateAndAddWaypoint(cell, _northEntry, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(_laneCentre, 0, 0);
            wpUTurn = _cellCentre - new Vector3(0, 0, _quarterCellSize);
            wpMidpoint2 = _cellCentre + new Vector3(-_laneCentre, 0, 0);
            exit = CreateAndAddWaypoint(cell, _northExit, WaypointType.Exit);
        }
        else if (_hasSouth)
        {
            entry = CreateAndAddWaypoint(cell, _southEntry, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(-_laneCentre, 0, 0);
            wpUTurn = _cellCentre - new Vector3(0, 0, -_quarterCellSize);
            wpMidpoint2 = _cellCentre + new Vector3(_laneCentre, 0, 0);
            exit = CreateAndAddWaypoint(cell, _southExit, WaypointType.Exit);
        }
        else if (_hasEast)
        {
            entry = CreateAndAddWaypoint(cell, _eastEntry, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(0, 0, -_laneCentre);
            wpUTurn = _cellCentre - new Vector3(_quarterCellSize, 0, 0);
            wpMidpoint2 = _cellCentre + new Vector3(0, 0, _laneCentre);
            exit = CreateAndAddWaypoint(cell, _eastExit, WaypointType.Exit);
        }
        else if (_hasWest)
        {
            entry = CreateAndAddWaypoint(cell, _westEntry, WaypointType.Entry);
            wpMidpoint1 = _cellCentre + new Vector3(0, 0, _laneCentre);
            wpUTurn = _cellCentre - new Vector3(-_quarterCellSize, 0, 0);
            wpMidpoint2 = _cellCentre + new Vector3(0, 0, -_laneCentre);
            exit = CreateAndAddWaypoint(cell, _westExit, WaypointType.Exit);
        }

        midpoint1 = CreateAndAddWaypoint(cell, wpMidpoint1, WaypointType.Midpoint);
        uTurn = CreateAndAddWaypoint(cell, wpUTurn, WaypointType.UTurn);
        midpoint2 = CreateAndAddWaypoint(cell, wpMidpoint2, WaypointType.Midpoint);

        // Connect entry to exit
        AddWaypointConnection(entry, midpoint1, Vector3.Distance(wpEntry, wpMidpoint1));
        AddWaypointConnection(midpoint1, uTurn, Vector3.Distance(wpMidpoint1, wpUTurn));
        AddWaypointConnection(uTurn, midpoint2, Vector3.Distance(wpUTurn, wpMidpoint2));
        AddWaypointConnection(midpoint2, exit, Vector3.Distance(wpMidpoint2, wpExit));
    }

    public void ConnectAllCells()
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
                    ConnectWaypointsToNeighbor(waypoints, neighbor);
                }
            }
        }
    }

    private void ConnectWaypointsToNeighbor(List<WaypointNode> waypoints, GridCell neighbor)
    {
        List<WaypointNode> neighbourWaypoints = _cellWaypoints[neighbor.Position.x, neighbor.Position.z];

        if (neighbourWaypoints == null || neighbourWaypoints.Count == 0)
            return;

        List<WaypointNode> cellExitWaypoints = new List<WaypointNode>();
        List<WaypointNode> neighborEntryWaypoints = new List<WaypointNode>();

        // Get exit waypoints from current cell and entry waypoints from neighbor
        cellExitWaypoints = waypoints.Where(w => w.Type == WaypointType.Exit).ToList();
        neighborEntryWaypoints = neighbourWaypoints.Where(w => w.Type == WaypointType.Entry).ToList();

        // Connect exit waypoints to entry waypoints only if they are at the same position (or very close)
        foreach (WaypointNode exitWaypoint in cellExitWaypoints)
        {
            foreach (WaypointNode entryWaypoint in neighborEntryWaypoints)
            {
                // Check if the waypoints are at the same position (or very close)
                float distance = Vector3.Distance(exitWaypoint.Position, entryWaypoint.Position);

                // If the distance is very small (essentially zero), connect them
                if (distance < 0.01f) // Tolerance for floating point precision
                {
                    AddWaypointConnection(exitWaypoint, entryWaypoint, distance);
                }
            }
        }
    }

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

    #region Building waypoint setup
    public void AddHouseVehicleWaypoints(
        GridCell cell,
        Transform[] parkedWaypoints,
        Transform[] entryToParkedWaypoints,
        Transform entryWaypoint,
        Transform cellCheckWaypoint,
        out WaypointNode[] parkingSpotWaypoints,
        out WaypointNode vehicleEntryExitPropertyWaypoint
    )
    {
        // define the main vehicle nodes
        vehicleEntryExitPropertyWaypoint = CreateAndAddWaypoint(cell, entryWaypoint.position, WaypointType.VehiclePropertyEntryExit, WaypointNetworkType.Vehicle);

        List<WaypointNode> parkedNodeList = new();
        WaypointNode parkedNode = null;

        for (int i = 0; i < parkedWaypoints.Length; i++)
        {
            parkedNode = CreateAndAddWaypoint(cell, parkedWaypoints[i].position, WaypointType.VehicleParking, WaypointNetworkType.Vehicle);
            parkedNodeList.Add(parkedNode);
        }

        parkingSpotWaypoints = parkedNodeList.ToArray();

        WaypointNode currentNode, previousNode = vehicleEntryExitPropertyWaypoint;

        // loop through the path from the car to the the node before the door and connect them
        for (int i = 0; i < entryToParkedWaypoints.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, entryToParkedWaypoints[i].position, WaypointType.PropertyDriveway, WaypointNetworkType.Vehicle);

            // add connections in both directions
            AddWaypointConnection(previousNode, currentNode, 0f);
            AddWaypointConnection(currentNode, previousNode, 0f);

            previousNode = currentNode;
        }

        // then connect to the parked nodes
        foreach (WaypointNode node in parkedNodeList)
        {
            AddWaypointConnection(previousNode, node, 0f);
            AddWaypointConnection(node, previousNode, 0f);
        }

        // find and connect the property exit waypoint, to the waypoints exiting the adjoing cell to allow the vehicle to exit the property in multiple different directions
        List<WaypointNode> closestVehicleWaypoints = FindClosestVehicleNodesInCellFromPosition(cellCheckWaypoint.position, vehicleEntryExitPropertyWaypoint.Position, 3, WaypointType.Exit);

        // connect the property entry/exit node to the Vehicle node on the road
        foreach (WaypointNode node in closestVehicleWaypoints)
        {
            AddWaypointConnection(vehicleEntryExitPropertyWaypoint, node, 0f);
        }

        //find and connect the entry points of the cell connecting to the property entry way to allow the vehicle to approach from any direction
        closestVehicleWaypoints = FindClosestVehicleNodesInCellFromPosition(cellCheckWaypoint.position, vehicleEntryExitPropertyWaypoint.Position, 3, WaypointType.Entry);

        // connect the property entry/exit node to the Vehicle node on the road
        foreach (WaypointNode node in closestVehicleWaypoints)
        {
            AddWaypointConnection(node, vehicleEntryExitPropertyWaypoint, 100f);
        }
    }

    public void AddPetrolStationVehicleWaypoints(
        GridCell cell,
        Transform entry,
        VehiclePumpDetails[] pumps,
        Transform exit,
        Transform cellCheckEntry,
        Transform cellCheckExit,
        out WaypointNode propertyEntry,
        out WaypointNode[] pumpNodes
    )
    {
        // create waypoints for the entry and exit points of the petrol station
        propertyEntry = CreateAndAddWaypoint(cell, entry.position, WaypointType.VehiclePropertyEntry, WaypointNetworkType.Vehicle);
        WaypointNode propertyExit = CreateAndAddWaypoint(cell, exit.position, WaypointType.VehiclePropertyExit, WaypointNetworkType.Vehicle);

        List<WaypointNode> pumpList = new();

        foreach (VehiclePumpDetails p in pumps)
        {
            // create waypoints for each pump
            WaypointNode pumpEntry = CreateAndAddWaypoint(cell, p.PumpEntry.position, WaypointType.Midpoint, WaypointNetworkType.Vehicle);
            WaypointNode pump = CreateAndAddWaypoint(cell, p.Pump.position, WaypointType.PetrolStationPump, WaypointNetworkType.Vehicle);
            WaypointNode pumpExit = CreateAndAddWaypoint(cell, p.PumpExit.position, WaypointType.Midpoint, WaypointNetworkType.Vehicle);

            pumpList.Add(pump);

            // connect property entry to pump entry
            AddWaypointConnection(propertyEntry, pumpEntry, 0f);
            // connect pump entry to pump
            AddWaypointConnection(pumpEntry, pump, 0f);
            // connect pump to pump exit
            AddWaypointConnection(pump, pumpExit, 0f);
            // connect pump exit to property exit
            AddWaypointConnection(pumpExit, propertyExit, 0f);
        }

        pumpNodes = pumpList.ToArray();

        // now we need to connect the road to the property entry
        List<WaypointNode> closestVehicleWaypoints = FindClosestVehicleNodesInCellFromPosition(cellCheckEntry.position, propertyEntry.Position, 3, WaypointType.Entry);
        foreach (WaypointNode node in closestVehicleWaypoints)
        {
            AddWaypointConnection(node, propertyEntry, 100f);
        }

        // now connect the property exit to the road
        closestVehicleWaypoints = FindClosestVehicleNodesInCellFromPosition(cellCheckExit.position, propertyExit.Position, 3, WaypointType.Exit);
        foreach (WaypointNode node in closestVehicleWaypoints)
        {
            AddWaypointConnection(propertyExit, node, 100f);
        }
    }

    public void AddCarParkVehicleWaypoints(
            GridCell cell,
            Transform cellCheckEntry,
            Transform entry,
            Transform[] entryRoutes,
            Transform[] topParkingSpots,
            Transform[] exitRoutes,
            Transform[] bottomParkingSpots,
            Transform exit,
            Transform cellCheckExit,
            out WaypointNode entryWaypoint,
            out WaypointNode[] entryRouteWaypoints,
            out WaypointNode[] topParkingSpotWaypoints,
            out WaypointNode[] exitRouteWaypoints,
            out WaypointNode[] bottomParkingSpotWaypoints,
            out WaypointNode exitWaypoint
        )
    {
        // easy stuff first, start with the entry and exit waypoints
        entryWaypoint = CreateAndAddWaypoint(cell, entry.position, WaypointType.VehiclePropertyEntry, WaypointNetworkType.Vehicle);
        exitWaypoint = CreateAndAddWaypoint(cell, exit.position, WaypointType.VehiclePropertyExit, WaypointNetworkType.Vehicle);

        List<WaypointNode> entryRouteList = new(), topParkingSpotList = new(), exitRouteList = new(), bottomParkingSpotList = new();
        WaypointNode currentNode, previousNode;

        // connect the entry node to the entry route
        previousNode = entryWaypoint;

        for (int i = 0; i < entryRoutes.Length; i++)
        {
            // create the node
            currentNode = CreateAndAddWaypoint(cell, entryRoutes[i].position, WaypointType.Midpoint, WaypointNetworkType.Vehicle);
            entryRouteList.Add(currentNode);

            // connect the previous node to the current node
            AddWaypointConnection(previousNode, currentNode, 0.1f);

            // update the previous node
            previousNode = currentNode;
        }

        entryRouteWaypoints = entryRouteList.ToArray();

        // connect the entry route to the exit route
        for (int i = 0; i < exitRoutes.Length; i++)
        {
            // create the node
            currentNode = CreateAndAddWaypoint(cell, exitRoutes[i].position, WaypointType.Midpoint, WaypointNetworkType.Vehicle);
            exitRouteList.Add(currentNode);

            // connect the previous node to the current node
            AddWaypointConnection(previousNode, currentNode, 0.1f);

            // update the previous node
            previousNode = currentNode;
        }

        exitRouteWaypoints = exitRouteList.ToArray();

        // link the final waypoint in the exit route, to the exit carpark waypoint
        AddWaypointConnection(previousNode, exitWaypoint, 0.1f);

        // create the top parking spaces and connect them the corresponding route waypoint (each way)
        for (int i = 0; i < topParkingSpots.Length; i++)
        {
            // create the node
            currentNode = CreateAndAddWaypoint(cell, topParkingSpots[i].position, WaypointType.VehicleParking, WaypointNetworkType.Vehicle);
            topParkingSpotList.Add(currentNode);

            // connect the parking space to the entry route
            AddWaypointConnection(currentNode, entryRouteWaypoints[i], 0.1f, twoWay: true);
        }

        topParkingSpotWaypoints = topParkingSpotList.ToArray();

        // create the bottom parking spaces and connect them the corresponding route waypoint (each way)
        for (int i = 0; i < bottomParkingSpots.Length; i++)
        {
            // create the node
            currentNode = CreateAndAddWaypoint(cell, bottomParkingSpots[i].position, WaypointType.VehicleParking, WaypointNetworkType.Vehicle);
            bottomParkingSpotList.Add(currentNode);

            // connect the parking space to the exit route
            AddWaypointConnection(currentNode, exitRouteWaypoints[i], 0.1f, twoWay: true);
        }
        bottomParkingSpotWaypoints = bottomParkingSpotList.ToArray();

        // now we need to connect the road to the property entry
        List<WaypointNode> closestVehicleWaypoints = FindClosestVehicleNodesInCellFromPosition(cellCheckEntry.position, entryWaypoint.Position, 3, WaypointType.Entry);
        foreach (WaypointNode node in closestVehicleWaypoints)
        {
            AddWaypointConnection(node, entryWaypoint, 100f);
        }

        // now connect the property exit to the road
        closestVehicleWaypoints = FindClosestVehicleNodesInCellFromPosition(cellCheckExit.position, exitWaypoint.Position, 3, WaypointType.Exit);
        foreach (WaypointNode node in closestVehicleWaypoints)
        {
            AddWaypointConnection(exitWaypoint, node, 100f);
        }
    }
    #endregion

    private List<WaypointNode> FindClosestVehicleNodesInCellFromPosition(Vector3 cellCheckPosition, Vector3 position, int count, WaypointType type)
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

    public WaypointNode GetWaypointNodeFromPositionInCell(GridCell cell, Vector3 position)
    {
        List<WaypointNode> allNodes = GetCellWaypoints(cell);

        if (allNodes.Count > 0)
        {
            allNodes.Sort((a, b) =>
            {
                float distA = Utils.GetDistanceWithSetHeight(position, a.Position, 0f);
                float distB = Utils.GetDistanceWithSetHeight(position, b.Position, 0f);
                return distA.CompareTo(distB);
            });

            return allNodes.First();
        }

        return null;
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        var waypointData = new WaypointSaveData();

        foreach (var node in _allWaypoints.Values)
        {
            var nodeData = new WaypointNodeSaveData
            {
                Id = node.Id.ToString(),
                X = node.Position.x,
                Z = node.Position.z,
                Type = node.Type,
                NetworkType = node.NetworkType,
                ParentCellX = node.ParentCell.Position.x,
                ParentCellZ = node.ParentCell.Position.z,
                PairedCrossingWaypointId = node.PairedCrossingWaypoint?.Id.ToString(),
                LaneNodeForTrafficLightId = node.LaneNodeForTrafficLight?.Id.ToString(),
                LightPosition = node.LightPosition
            };

            foreach (var connection in node.Connections)
            {
                nodeData.Connections.Add(new WaypointConnectionSaveData
                {
                    TargetNodeId = connection.Key.Id.ToString(),
                    Cost = connection.Value
                });
            }

            waypointData.Nodes.Add(nodeData);
        }

        saveData.VehicleWaypoints = waypointData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.VehicleWaypoints == null)
        {
            Debug.LogWarning("[VehicleWaypointManager] No waypoint data in save file.");
            return;
        }

        _allWaypoints.Clear();
        _cellWaypoints = new List<WaypointNode>[_gridWidth, _gridHeight];

        int connectionCount = 0;

        var nodeLookup = new Dictionary<string, WaypointNode>();

        // First pass — create all nodes
        foreach (var nodeData in saveData.VehicleWaypoints.Nodes)
        {
            // Retrieve the parent cell from the grid
            var parentCell = GridManager.Instance.GetCell(nodeData.ParentCellX, nodeData.ParentCellZ);
            if (parentCell == null)
            {
                Debug.LogWarning($"[VehicleWaypointManager] Parent cell ({nodeData.ParentCellX}, {nodeData.ParentCellZ}) not found for node {nodeData.Id}.");
                continue;
            }

            if (_cellWaypoints[parentCell.Position.x, parentCell.Position.z] == null)
            {
                _cellWaypoints[parentCell.Position.x, parentCell.Position.z] = new List<WaypointNode>();
            }

            var node = CreateAndAddWaypoint(parentCell,
                new Vector3(nodeData.X, 0f, nodeData.Z),
                nodeData.Type,
                nodeData.NetworkType
            );

            // Restore the saved ID rather than using the new GUID generated in the constructor
            node.Id = EntityId.FromString(nodeData.Id);

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

            nodeLookup[node.Id.ToString()] = node;
        }

        // Second pass — restore connections
        foreach (WaypointNodeSaveData nodeData in saveData.VehicleWaypoints.Nodes)
        {
            if (!nodeLookup.TryGetValue(nodeData.Id, out WaypointNode node)) continue;

            foreach (WaypointConnectionSaveData connectionData in nodeData.Connections)
            {
                if (nodeLookup.TryGetValue(connectionData.TargetNodeId, out WaypointNode targetNode))
                {
                    AddWaypointConnection(node, targetNode, connectionData.Cost);
                    connectionCount++;
                }
                else
                {
                    Debug.LogWarning($"[VehicleWaypointManager] Target node {connectionData.TargetNodeId} not found for connection.");
                }
            }
        }

        // Third pass — resolve paired crossing waypoints (after all nodes are created)
        foreach (WaypointNode node in _allWaypoints.Values)
        {
            if (!string.IsNullOrEmpty(node.PairedCrossingWaypointId) &&
                nodeLookup.TryGetValue(node.PairedCrossingWaypointId, out WaypointNode pairedNode))
            {
                node.PairedCrossingWaypoint = pairedNode;
            }
            if (!string.IsNullOrEmpty(node.LaneNodeForTrafficLightId) &&
                nodeLookup.TryGetValue(node.LaneNodeForTrafficLightId, out WaypointNode laneNode))
            {
                node.LaneNodeForTrafficLight = laneNode;
            }
        }

        Debug.Log($"[VehicleWaypointManager] Loaded {_allWaypoints.Count} vehicle waypoint nodes and {connectionCount} connections.");
    }

    private void OnDrawGizmos()
    {
        if (_allWaypoints.Count <= 0) return;

        // Draw waypoints
        foreach (WaypointNode node in _allWaypoints.Values)
        {
            //if (node.NetworkType != WaypointNetworkType.Vehicle || node.Type == WaypointType.TrafficLightLocation) continue;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Utils.GetVectorWithSetHeight(node.Position, 0.5f), 0.2f);
            Gizmos.color = Color.green;
            foreach (var connection in node.Connections)
            {
                Gizmos.DrawLine(Utils.GetVectorWithSetHeight(node.Position, 0.5f), Utils.GetVectorWithSetHeight(connection.Key.Position, 0.5f));
            }
        }

    }
}