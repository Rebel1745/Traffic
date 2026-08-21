using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class PedestrianWaypointManager : WaypointManagerBase, ISaveable
{
    public static PedestrianWaypointManager Instance { get; private set; }

    public string SaveKey => "PedestrianWaypoints";

    // waypoint values
    private Vector3 _northWestFromNorth, _northWestFromWest;
    private Vector3 _northEastFromNorth, _northEastFromEast;
    private Vector3 _southWestFromSouth, _southWestFromWest;
    private Vector3 _southEastFromSouth, _southEastFromEast;

    private Vector3 _midpointNW, _midpointNE, _midpointSW, _midpointSE;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();

        SaveManager.Instance.RegisterSaveable(this);
        VehicleWaypointManager.Instance.OnRoadWaypointsUpdated += RoadWaypointsUpdated;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.UnregisterSaveable(this);
        VehicleWaypointManager.Instance.OnRoadWaypointsUpdated -= RoadWaypointsUpdated;
    }

    private void RoadWaypointsUpdated()
    {
        GenerateWaypoints();
    }

    protected override void GenerateWaypoints()
    {
        base.GenerateWaypoints();

        GridManager.Instance.ResetUpdated();
    }

    protected override void CalculateEntryExitAndMidpointsForCell(GridCell cell)
    {
        base.CalculateEntryExitAndMidpointsForCell(cell);

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
    }

    protected override void CreateStraightWaypoints(GridCell cell)
    {
        if (_hasNorth && _hasSouth) // Vertical road
        {
            // Left pavement
            Vector3 crossing = _cellCentre + new Vector3(-_halfCellSize + _halfPavementSize, 0, 0);
            WaypointNode north = CreateAndAddWaypoint(cell, _northWestFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint1 = CreateAndAddWaypoint(cell, crossing, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode south = CreateAndAddWaypoint(cell, _southWestFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect north to south
            AddWaypointConnection(north, crossingPoint1, twoWay: true);
            AddWaypointConnection(crossingPoint1, south, twoWay: true);

            // Right pavement
            crossing = _cellCentre + new Vector3(_halfCellSize - _halfPavementSize, 0, 0);
            north = CreateAndAddWaypoint(cell, _northEastFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint2 = CreateAndAddWaypoint(cell, crossing, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            south = CreateAndAddWaypoint(cell, _southEastFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect north to south
            AddWaypointConnection(north, crossingPoint2, twoWay: true);
            AddWaypointConnection(crossingPoint2, south, twoWay: true);

            // connect crossing points
            AddWaypointConnection(crossingPoint1, crossingPoint2, twoWay: true);

        }
        else if (_hasEast && _hasWest) // Horizontal road
        {
            // Top pavement
            Vector3 crossing = _cellCentre + new Vector3(0, 0, _halfCellSize - _halfPavementSize);
            WaypointNode west = CreateAndAddWaypoint(cell, _northWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint1 = CreateAndAddWaypoint(cell, crossing, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode east = CreateAndAddWaypoint(cell, _northEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect west to east
            AddWaypointConnection(west, crossingPoint1, twoWay: true);
            AddWaypointConnection(crossingPoint1, east, twoWay: true);

            // Bottom pavement
            crossing = _cellCentre + new Vector3(0, 0, -_halfCellSize + _halfPavementSize);
            west = CreateAndAddWaypoint(cell, _southWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode crossingPoint2 = CreateAndAddWaypoint(cell, crossing, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            east = CreateAndAddWaypoint(cell, _southEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // Connect west to east
            AddWaypointConnection(west, crossingPoint2, twoWay: true);
            AddWaypointConnection(crossingPoint2, east, twoWay: true);

            // connect crossing points
            AddWaypointConnection(crossingPoint1, crossingPoint2, twoWay: true);
        }
    }

    protected override void CreateCornerWaypoints(GridCell cell)
    {
        // Corner cases
        if (_hasNorth && _hasEast) // Corner from North to East
        {
            // start with the long corner pavement
            WaypointNode northWestFromNorth = CreateAndAddWaypoint(cell, _northWestFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSW = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromEast = CreateAndAddWaypoint(cell, _southEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(northWestFromNorth, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSW, southEastFromEast, twoWay: true);

            // short corner pavement
            WaypointNode northEastFromNorth = CreateAndAddWaypoint(cell, _northEastFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNE = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromEast = CreateAndAddWaypoint(cell, _northEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(northEastFromNorth, midpointNE, twoWay: true);
            AddWaypointConnection(midpointNE, northEastFromEast, twoWay: true);
        }
        else if (_hasNorth && _hasWest) // Corner from North to West
        {
            // start with the long corner pavement
            WaypointNode northEastFromNorth = CreateAndAddWaypoint(cell, _northEastFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSE = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromWest = CreateAndAddWaypoint(cell, _southWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(northEastFromNorth, midpointSE, twoWay: true);
            AddWaypointConnection(midpointSE, southWestFromWest, twoWay: true);

            // short corner pavement
            WaypointNode northWestFromNorth = CreateAndAddWaypoint(cell, _northWestFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNW = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northWestFromWest = CreateAndAddWaypoint(cell, _northWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(northWestFromNorth, midpointNW, twoWay: true);
            AddWaypointConnection(midpointNW, northWestFromWest, twoWay: true);
        }
        else if (_hasSouth && _hasEast) // Corner from South to East
        {
            // start with the long corner pavement
            WaypointNode southWestFromSouth = CreateAndAddWaypoint(cell, _southWestFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNW = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromEast = CreateAndAddWaypoint(cell, _northEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(southWestFromSouth, midpointNW, twoWay: true);
            AddWaypointConnection(midpointNW, northEastFromEast, twoWay: true);

            // short corner pavement
            WaypointNode southEastFromSouth = CreateAndAddWaypoint(cell, _southEastFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSE = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromEast = CreateAndAddWaypoint(cell, _southEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(southEastFromSouth, midpointSE, twoWay: true);
            AddWaypointConnection(midpointSE, southEastFromEast, twoWay: true);
        }
        else if (_hasSouth && _hasWest) // Corner from South to West
        {
            // start with the long corner pavement
            WaypointNode southEastFromSouth = CreateAndAddWaypoint(cell, _southEastFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNE = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northWestFromWest = CreateAndAddWaypoint(cell, _northWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(southEastFromSouth, midpointNE, twoWay: true);
            AddWaypointConnection(midpointNE, northWestFromWest, twoWay: true);

            // short corner pavement
            WaypointNode southWestFromSouth = CreateAndAddWaypoint(cell, _southWestFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSW = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromWest = CreateAndAddWaypoint(cell, _southWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

            // connections
            AddWaypointConnection(southWestFromSouth, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSW, southWestFromWest, twoWay: true);
        }
    }

    protected override void CreateTJunctionWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // first create all of the waypoints
        // we will only need three quarters, but it really doesn't matter, we won,t add the unwanted nodes to the list
        WaypointNode northWestFromNorth = CreateWaypoint(cell, _northWestFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode northWestFromWest = CreateWaypoint(cell, _northWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode northEastFromNorth = CreateWaypoint(cell, _northEastFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode northEastFromEast = CreateWaypoint(cell, _northEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southWestFromSouth = CreateWaypoint(cell, _southWestFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southWestFromWest = CreateWaypoint(cell, _southWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southEastFromSouth = CreateWaypoint(cell, _southEastFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode southEastFromEast = CreateWaypoint(cell, _southEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpointNW = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
        WaypointNode midpointNE = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
        WaypointNode midpointSW = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
        WaypointNode midpointSE = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);

        // pedestrian crossing connections
        AddWaypointConnection(midpointNW, midpointNE, twoWay: true);
        AddWaypointConnection(midpointNW, midpointSW, twoWay: true);
        AddWaypointConnection(midpointSE, midpointSW, twoWay: true);
        AddWaypointConnection(midpointSE, midpointNE, twoWay: true);

        // T-Junction with North, East, and West (missing South)
        if (_hasNorth && _hasEast && _hasWest && !_hasSouth)
        {
            // north west connections
            AddWaypointConnection(northWestFromNorth, midpointNW, twoWay: true);
            AddWaypointConnection(midpointNW, northWestFromWest, twoWay: true);
            // north east connections
            AddWaypointConnection(northEastFromNorth, midpointNE, twoWay: true);
            AddWaypointConnection(midpointNE, northEastFromEast, twoWay: true);
            // south west to south east
            AddWaypointConnection(southWestFromWest, southEastFromEast, twoWay: true);
        }
        // T-Junction with North, East, and South (missing West)
        else if (_hasNorth && _hasEast && _hasSouth && !_hasWest)
        {
            // north east connections
            AddWaypointConnection(northEastFromNorth, midpointNE, twoWay: true);
            AddWaypointConnection(midpointNE, northEastFromEast, twoWay: true);
            // south east
            AddWaypointConnection(southEastFromSouth, midpointSE, twoWay: true);
            AddWaypointConnection(midpointSE, southEastFromEast, twoWay: true);
            // north west to south west
            AddWaypointConnection(northWestFromNorth, southWestFromSouth, twoWay: true);
        }
        // T-Junction with North, South, and West (missing East)
        else if (_hasNorth && _hasSouth && _hasWest && !_hasEast)
        {
            // north west connections
            AddWaypointConnection(northWestFromNorth, midpointNW, twoWay: true);
            AddWaypointConnection(midpointNW, northWestFromWest, twoWay: true);
            // south west
            AddWaypointConnection(southWestFromSouth, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSW, southWestFromWest, twoWay: true);
            // north east to south east
            AddWaypointConnection(northEastFromNorth, southEastFromSouth, twoWay: true);
        }
        // T-Junction with East, South, and West (missing North)
        else if (_hasEast && _hasSouth && _hasWest && !_hasNorth)
        {
            // south west
            AddWaypointConnection(southWestFromSouth, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSW, southWestFromWest, twoWay: true);
            // south east
            AddWaypointConnection(southEastFromSouth, midpointSE, twoWay: true);
            AddWaypointConnection(midpointSE, southEastFromEast, twoWay: true);
            // north west to north east
            AddWaypointConnection(northWestFromWest, northEastFromEast, twoWay: true);
        }

        if (_hasNorth)
        {
            AddWaypoint(cell, northWestFromNorth);
            AddWaypoint(cell, northEastFromNorth);
        }
        if (_hasSouth)
        {
            AddWaypoint(cell, southWestFromSouth);
            AddWaypoint(cell, southEastFromSouth);
        }
        if (_hasWest)
        {
            AddWaypoint(cell, northWestFromWest);
            AddWaypoint(cell, southWestFromWest);
        }
        if (_hasEast)
        {
            AddWaypoint(cell, northEastFromEast);
            AddWaypoint(cell, southEastFromEast);
        }
    }

    protected override void CreateCrossroadsWaypoints(GridCell cell)
    {
        List<WaypointNode> waypoints = new List<WaypointNode>();

        // Crossroads with all four directions
        if (_hasNorth && _hasSouth && _hasEast && _hasWest)
        {
            // first create all of the waypoints
            WaypointNode northWestFromNorth = CreateAndAddWaypoint(cell, _northWestFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northWestFromWest = CreateAndAddWaypoint(cell, _northWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromNorth = CreateAndAddWaypoint(cell, _northEastFromNorth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode northEastFromEast = CreateAndAddWaypoint(cell, _northEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromSouth = CreateAndAddWaypoint(cell, _southWestFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southWestFromWest = CreateAndAddWaypoint(cell, _southWestFromWest, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromSouth = CreateAndAddWaypoint(cell, _southEastFromSouth, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode southEastFromEast = CreateAndAddWaypoint(cell, _southEastFromEast, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNW = CreateAndAddWaypoint(cell, _midpointNW, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode midpointNE = CreateAndAddWaypoint(cell, _midpointNE, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSW = CreateAndAddWaypoint(cell, _midpointSW, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);
            WaypointNode midpointSE = CreateAndAddWaypoint(cell, _midpointSE, WaypointType.PedestrianRoadCrossing, WaypointNetworkType.Pedestrian);

            // north west connections
            AddWaypointConnection(northWestFromNorth, midpointNW, twoWay: true);
            AddWaypointConnection(midpointNW, northWestFromWest, twoWay: true);
            // north east connections
            AddWaypointConnection(northEastFromNorth, midpointNE, twoWay: true);
            AddWaypointConnection(midpointNE, northEastFromEast, twoWay: true);
            // south west
            AddWaypointConnection(southWestFromSouth, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSW, southWestFromWest, twoWay: true);
            // south east
            AddWaypointConnection(southEastFromSouth, midpointSE, twoWay: true);
            AddWaypointConnection(midpointSE, southEastFromEast, twoWay: true);

            // pedestrian crossing connections
            AddWaypointConnection(midpointNW, midpointNE, twoWay: true);
            AddWaypointConnection(midpointNW, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSE, midpointSW, twoWay: true);
            AddWaypointConnection(midpointSE, midpointNE, twoWay: true);
        }
    }

    protected override void CreateDeadEndWaypoints(GridCell cell)
    {
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

        WaypointNode entry = CreateAndAddWaypoint(cell, entryPos, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpoint1 = CreateAndAddWaypoint(cell, midpoint1Pos, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode midpoint2 = CreateAndAddWaypoint(cell, midpoint2Pos, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode exit = CreateAndAddWaypoint(cell, exitPos, WaypointType.PedestrianWalkway, WaypointNetworkType.Pedestrian);

        // connections
        AddWaypointConnection(entry, midpoint1, twoWay: true);
        AddWaypointConnection(midpoint1, midpoint2, twoWay: true);
        AddWaypointConnection(midpoint2, exit, twoWay: true);
    }

    protected override void ConnectWaypointsToNeighbour(List<WaypointNode> waypoints, GridCell neighbour)
    {
        // Get the Neighbour's waypoints
        List<WaypointNode> neighbourWaypoints = _cellWaypoints[neighbour.Position.x, neighbour.Position.z];

        if (neighbourWaypoints == null || neighbourWaypoints.Count == 0)
            return;

        foreach (WaypointNode w1 in waypoints)
        {
            foreach (WaypointNode w2 in neighbourWaypoints)
            {
                // Check if the waypoints are at the same position (or very close)
                float distance = Vector3.Distance(w1.Position, w2.Position);

                // If the distance is very small (essentially zero), connect them
                if (distance < 1f) // Tolerance for floating point precision
                {
                    w1.Connections[w2] = distance;
                }
            }
        }
    }

    protected override void ConfigureTrafficLights(GridCell cell)
    {
        List<WaypointNode> cellWaypoints = GetCellWaypoints(cell);
        if (cellWaypoints == null || cellWaypoints.Count == 0) return;

        List<WaypointNode> pedestrianCrossingWaypoints = cellWaypoints.Where(w => w.Type == WaypointType.PedestrianRoadCrossing).ToList();
        List<WaypointNode> cellRoadWaypoints = VehicleWaypointManager.Instance.GetCellWaypoints(cell).Where(w => w.Type == WaypointType.TrafficLightLocation).ToList();

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

    #region Building waypoint setup
    public void AddHousePedestrianWaypoints(
        GridCell cell,
        Transform insideBuilding,
        Transform door,
        Transform propertyEntryExit,
        Transform[] propertyEntryToDoor,
        Transform[] vehicleEntryExit,
        Transform[] doorToVehicle,
        Transform entryExitCellCheck,
        out WaypointNode insideBuildingWaypoint,
        out WaypointNode doorWaypoint,
        out WaypointNode entryExitPropertyWaypoint,
        out WaypointNode[] entryExitVehicleWaypoints
    )
    {
        // define the main property nodes
        insideBuildingWaypoint = CreateAndAddWaypoint(cell, insideBuilding.position, WaypointType.InsideBuilding, WaypointNetworkType.Pedestrian);
        doorWaypoint = CreateAndAddWaypoint(cell, door.position, WaypointType.BuildingDoor, WaypointNetworkType.Pedestrian);
        entryExitPropertyWaypoint = CreateAndAddWaypoint(cell, propertyEntryExit.position, WaypointType.PropertyEntryExit, WaypointNetworkType.Pedestrian);

        // connect the inside building to the door
        AddWaypointConnection(insideBuildingWaypoint, doorWaypoint, twoWay: true);

        WaypointNode finalDoorToVehicleNode = doorWaypoint;
        WaypointNode currentNode, previousNode = doorWaypoint;

        // setup the path from the front door to the alight points for the parking spots
        for (int i = 0; i < doorToVehicle.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, doorToVehicle[i].position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);

            // connect the current waypoint to the previous one
            AddWaypointConnection(currentNode, previousNode, twoWay: true, cost: 0.1f);

            if (i == doorToVehicle.Length - 1)
                finalDoorToVehicleNode = currentNode;

            previousNode = currentNode;
        }

        List<WaypointNode> vehicleEntryExitNodes = new List<WaypointNode>();

        for (int i = 0; i < vehicleEntryExit.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, vehicleEntryExit[i].position, WaypointType.VehicleEntryExit, WaypointNetworkType.Pedestrian);
            vehicleEntryExitNodes.Add(currentNode);

            // if we have a path from the door to the car, connect to the last element in that path (connect to the front door if not)
            AddWaypointConnection(currentNode, finalDoorToVehicleNode, twoWay: true, cost: 0.1f);
        }

        entryExitVehicleWaypoints = vehicleEntryExitNodes.ToArray();

        previousNode = entryExitPropertyWaypoint;
        // loop throught the path from the entry/exit to the door and connect them
        for (int i = 0; i < propertyEntryToDoor.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, propertyEntryToDoor[i].position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);

            AddWaypointConnection(currentNode, previousNode, twoWay: true);

            previousNode = currentNode;
        }

        // now connect the last path node to the door
        currentNode = doorWaypoint;
        AddWaypointConnection(currentNode, previousNode, twoWay: true);

        // find the closest pedestrian walkway node to the entry/exit nodes position to allow the person to walk from the property into the world
        List<WaypointNode> closestPedestrianWaypoints = FindClosestNodesInCellFromPosition(entryExitCellCheck.position, entryExitPropertyWaypoint.Position, 2, new() { WaypointType.PedestrianWalkway, WaypointType.PedestrianRoadCrossing });

        // connect the property entry/exit node to the pedestrian walkway on the pavement
        foreach (WaypointNode node in closestPedestrianWaypoints)
        {
            AddWaypointConnection(entryExitPropertyWaypoint, node, twoWay: true);
        }
    }

    public void AddCarParkPedestrianWaypoints(
        GridCell cell,
        Transform entryPosition,
        Transform[] topAlightPositions,
        Transform[] topRoutePositions,
        Transform[] bottomAlightPositions,
        Transform[] bottomRoutePositions,
        Transform exitPosition,
        Transform entryCheck,
        Transform exitCheck,
        out WaypointNode entry,
        out WaypointNode[] topAlight,
        out WaypointNode[] topRoute,
        out WaypointNode[] bottomAlight,
        out WaypointNode[] bottomRoute,
        out WaypointNode exit
    )
    {
        // start with the entry and exit waypoints
        entry = CreateAndAddWaypoint(cell, entryPosition.position, WaypointType.PropertyEntry, WaypointNetworkType.Pedestrian);
        exit = CreateAndAddWaypoint(cell, exitPosition.position, WaypointType.PropertyExit, WaypointNetworkType.Pedestrian);

        List<WaypointNode> topRouteList = new(), topAlightList = new(), bottomRouteList = new(), bottomAlightList = new();
        WaypointNode currentNode, previousNode;

        // create the alight waypoints
        for (int i = 0; i < topAlightPositions.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, topAlightPositions[i].position, WaypointType.VehicleEntryExit, WaypointNetworkType.Pedestrian);
            topAlightList.Add(currentNode);
        }

        topAlight = topAlightList.ToArray();

        for (int i = 0; i < bottomAlightPositions.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, bottomAlightPositions[i].position, WaypointType.VehicleEntryExit, WaypointNetworkType.Pedestrian);
            bottomAlightList.Add(currentNode);
        }

        bottomAlight = bottomAlightList.ToArray();

        currentNode = topAlightList.First();

        // connect the entry node to the first alight node
        AddWaypointConnection(entry, currentNode, twoWay: true, cost: 0.1f);

        previousNode = currentNode;

        // create the top route, and connect starting at the entrance
        for (int i = 0; i < topRoutePositions.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, topRoutePositions[i].position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);
            topRouteList.Add(currentNode);

            AddWaypointConnection(previousNode, currentNode, twoWay: true, cost: 0.1f);
            previousNode = currentNode;
        }

        topRoute = topRouteList.ToArray();

        // connect the last waypoint in the top route (previousNode) to the first bottom alight waypoint
        currentNode = bottomAlightList.First();
        AddWaypointConnection(currentNode, previousNode, twoWay: true, cost: 0.1f);
        previousNode = currentNode;

        // connect the bottom route. 
        for (int i = 0; i < bottomRoutePositions.Length; i++)
        {
            currentNode = CreateAndAddWaypoint(cell, bottomRoutePositions[i].position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);
            bottomRouteList.Add(currentNode);

            AddWaypointConnection(previousNode, currentNode, twoWay: true, cost: 0.1f);
            previousNode = currentNode;
        }

        bottomRoute = bottomRouteList.ToArray();

        // connect the top route positions to the corresponding alight positions (ignoring the first alight position as that has already been handled)
        for (int i = 1; i < topAlight.Length; i++)
        {
            AddWaypointConnection(topAlight[i], topRoute[i], twoWay: true, cost: 0.1f);
        }

        // now do the same for the bottom
        for (int i = 0; i < bottomAlight.Length; i++)
        {
            AddWaypointConnection(bottomAlight[i], bottomRoute[i], twoWay: true, cost: 0.1f);
        }

        // connect the last node in the bottom route (previousNode) to the exit
        AddWaypointConnection(previousNode, exit, twoWay: true, cost: 0.1f);

        // find the closest pedestrian walkway node to the entry node position to allow the person to walk from the property into the world
        List<WaypointNode> closestPedestrianWaypoints = FindClosestNodesInCellFromPosition(entryCheck.position, entry.Position, 2, new() { WaypointType.PedestrianWalkway, WaypointType.PedestrianRoadCrossing });

        // connect the property entry/exit node to the pedestrian walkway on the pavement
        foreach (WaypointNode node in closestPedestrianWaypoints)
        {
            AddWaypointConnection(entry, node, twoWay: true);
        }

        // do the same for the exit waypoint
        closestPedestrianWaypoints = FindClosestNodesInCellFromPosition(exitCheck.position, exit.Position, 2, new() { WaypointType.PedestrianWalkway, WaypointType.PedestrianRoadCrossing });

        // connect the property entry/exit node to the pedestrian walkway on the pavement
        foreach (WaypointNode node in closestPedestrianWaypoints)
        {
            AddWaypointConnection(exit, node, twoWay: true);
        }
    }

    public void AddPetrolStationPedestrianWaypoints(
            GridCell cell,
            Transform insideBuilding,
            Transform frontDoor,
            Transform pointBeforeFrontDoor,
            PedestrianPumpDetails[] pedestrianPumps,
            out WaypointNode insideBuildingWaypoint,
            out WaypointNode[] alightWaypoints,
            out WaypointNode[] fillUpWaypoints
        )
    {
        // start with the basic nodes
        insideBuildingWaypoint = CreateAndAddWaypoint(cell, insideBuilding.position, WaypointType.InsideBuilding, WaypointNetworkType.Pedestrian);
        WaypointNode frontDoorNode = CreateAndAddWaypoint(cell, frontDoor.position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);
        WaypointNode pointBeforeFrontDoorNode = CreateAndAddWaypoint(cell, pointBeforeFrontDoor.position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);

        // connect the nodes
        AddWaypointConnection(insideBuildingWaypoint, frontDoorNode, twoWay: true, cost: 0.1f);
        AddWaypointConnection(frontDoorNode, pointBeforeFrontDoorNode, twoWay: true, cost: 0.1f);

        List<WaypointNode> alightWaypointList = new();
        List<WaypointNode> fillUpWaypointList = new();
        WaypointNode currentNode, previousNode;

        // now configure the path from the alight waypoint to the pump
        for (int i = 0; i < pedestrianPumps.Length; i++)
        {
            WaypointNode alightWaypoint = CreateAndAddWaypoint(cell, pedestrianPumps[i].AlightPosition.position, WaypointType.VehicleEntryExit, WaypointNetworkType.Pedestrian);
            alightWaypointList.Add(alightWaypoint);

            previousNode = alightWaypoint;

            // go through the path to the pump and connect them to each other
            for (int j = 0; j < pedestrianPumps[i].PathToPump.Length; j++)
            {
                currentNode = CreateAndAddWaypoint(cell, pedestrianPumps[i].PathToPump[j].position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);

                AddWaypointConnection(previousNode, currentNode, twoWay: true);

                previousNode = currentNode;
            }

            // the last path to pump waypoint is the fill up waypoint
            fillUpWaypointList.Add(previousNode);

            // now go through the path to the shop and connect them to the path to pump
            for (int k = 0; k < pedestrianPumps[i].PathToShop.Length; k++)
            {
                currentNode = CreateAndAddWaypoint(cell, pedestrianPumps[i].PathToShop[k].position, WaypointType.PropertyWalkway, WaypointNetworkType.Pedestrian);

                AddWaypointConnection(previousNode, currentNode, twoWay: true);

                previousNode = currentNode;
            }

            // connect the last path to shop waypoint, to the waypoint before the front door
            AddWaypointConnection(pointBeforeFrontDoorNode, previousNode, twoWay: true);
        }

        alightWaypoints = alightWaypointList.ToArray();
        fillUpWaypoints = fillUpWaypointList.ToArray();
    }

    public void AddStoreRoadsidePedestrianWaypoints(
        GridCell cell,
        Transform insideBuilding,
        Transform buildingEntrance,
        Transform propertyEntrance,
        Transform checkCell,
        out WaypointNode insideBuildingWaypoint,
        out WaypointNode buildingEntranceWaypoint,
        out WaypointNode propertyEntranceWaypoint
    )
    {
        insideBuildingWaypoint = CreateAndAddWaypoint(cell, insideBuilding.position, WaypointType.InsideBuilding, WaypointNetworkType.Pedestrian);
        buildingEntranceWaypoint = CreateAndAddWaypoint(cell, buildingEntrance.position, WaypointType.BuildingDoor, WaypointNetworkType.Pedestrian);
        propertyEntranceWaypoint = CreateAndAddWaypoint(cell, propertyEntrance.position, WaypointType.PropertyEntryExit, WaypointNetworkType.Pedestrian);

        AddWaypointConnection(insideBuildingWaypoint, buildingEntranceWaypoint, cost: 0.1f, twoWay: true);
        AddWaypointConnection(buildingEntranceWaypoint, propertyEntranceWaypoint, cost: 0.1f, twoWay: true);

        // find the closest pedestrian walkway node to the entry node position to allow the person to walk from the property into the world
        List<WaypointNode> closestPedestrianWaypoints = FindClosestNodesInCellFromPosition(checkCell.position, propertyEntranceWaypoint.Position, 2, new() { WaypointType.PedestrianWalkway, WaypointType.PedestrianRoadCrossing });

        // connect the property entry/exit node to the pedestrian walkway on the pavement
        foreach (WaypointNode node in closestPedestrianWaypoints)
        {
            AddWaypointConnection(propertyEntranceWaypoint, node, twoWay: true);
        }
    }
    #endregion

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
        WaypointSaveData waypointData = new();

        foreach (WaypointNode node in _allWaypoints.Values)
        {
            WaypointNodeSaveData nodeData = new()
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

        saveData.PedestrianWaypoints = waypointData;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.PedestrianWaypoints == null)
        {
            Debug.LogWarning("[PedestrianWaypointManager] No waypoint data in save file.");
            return;
        }

        _allWaypoints = new();
        _cellWaypoints = new List<WaypointNode>[_gridWidth, _gridHeight];

        var nodeLookup = new Dictionary<string, WaypointNode>();
        int connectionCount = 0;

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

            if (_cellWaypoints[parentCell.Position.x, parentCell.Position.z] == null)
            {
                _cellWaypoints[parentCell.Position.x, parentCell.Position.z] = new List<WaypointNode>();
            }

            var node = CreateWaypoint(
                parentCell,
                new Vector3(nodeData.X, 0f, nodeData.Z),
                nodeData.Type,
                nodeData.NetworkType
            );

            // Restore the saved ID rather than using the new GUID generated in the constructor
            node.Id = EntityId.FromString(nodeData.Id);

            AddWaypoint(parentCell, node);
            nodeLookup[node.Id.ToString()] = node;
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
                    AddWaypointConnection(node, targetNode, connectionData.Cost);
                    connectionCount++;
                }
                else
                {
                    Debug.LogWarning($"[WaypointManager] Target node {connectionData.TargetNodeId} not found for connection.");
                }
            }
        }

        Debug.Log($"[PedestrianWaypointManager] Loaded {_allWaypoints.Count} pedestrian waypoint nodes and {connectionCount} connections.");
    }

    private void OnDrawGizmos()
    {
        if (_allWaypoints == null) return;

        // Draw waypoints
        foreach (WaypointNode node in _allWaypoints.Values)
        {
            //if (node.NetworkType != WaypointNetworkType.Vehicle || node.Type == WaypointType.TrafficLightLocation) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Utils.GetVectorWithSetHeight(node.Position, 0.5f), 0.2f);
            Gizmos.color = Color.red;
            foreach (var connection in node.Connections)
            {
                Gizmos.DrawLine(Utils.GetVectorWithSetHeight(node.Position, 0.5f), Utils.GetVectorWithSetHeight(connection.Key.Position, 0.5f));
            }
        }
    }
}