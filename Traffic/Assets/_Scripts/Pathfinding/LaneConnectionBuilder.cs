using UnityEngine;

public class LaneConnectionBuilder
{
    public void BuildAllConnections(GridCell[,] grid)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                GridCell cell = grid[x, z];
                if (cell.CellType != CellType.Road || cell.LaneData == null)
                    continue;

                ConnectCellLanes(cell);
            }
        }
    }

    private void ConnectCellLanes(GridCell cell)
    {
        foreach (LaneSegment lane in cell.LaneData.Lanes)
        {
            // Find neighboring cell in the lane's direction
            GridCell neighbor = RoadGrid.Instance.GetNeighborInDirection(cell, lane.Direction);

            if (neighbor != null && neighbor.CellType == CellType.Road && neighbor.LaneData != null)
            {
                // Find compatible incoming lanes in the neighbor
                foreach (LaneSegment neighborLane in neighbor.LaneData.Lanes)
                {
                    // Check if lanes connect (end of current lane matches start of neighbor lane)
                    if (LanesConnect(lane, neighborLane))
                    {
                        float cost = Vector3.Distance(lane.EndWaypoint, neighborLane.StartWaypoint);
                        lane.OutgoingConnections.Add(new LaneConnection
                        {
                            TargetLane = neighborLane,
                            Cost = cost
                        });
                    }
                }
            }
        }
        //Debug.Log(cell.CellInfo);
    }

    private bool LanesConnect(LaneSegment from, LaneSegment to)
    {
        // Lanes connect if they're close and going the same direction
        float distance = Vector3.Distance(from.EndWaypoint, to.StartWaypoint);
        if (distance < 0.1f && from.Direction != to.Direction)
            Debug.Log($"{from.SegmentName} {from.Direction} doesn't connect to {to.SegmentName} {to.Direction}");

        return distance < 0.1f && from.Direction == to.Direction;
    }
}