using System;
using System.Collections.Generic;

public interface IMovable
{
    // Event: Called when the current path is finished
    event Action OnArrivedAtDestination;

    // setup the movement details
    void Initialise(WaypointNode spawnWaypoint, WaypointNode targetWaypoint);

    // Action: Start moving along a list of waypoints
    void SetPath(List<WaypointNode> path);

    // Action: Stop immediately
    void Stop(bool forceStop = false);

    // Property: Get current waypoint (used for pathfinding start points)
    WaypointNode CurrentWaypoint { get; }
    WaypointNode TargetWaypoint { get; }
}