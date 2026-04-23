using System.Collections.Generic;

public interface IWaypointNetwork
{
    List<WaypointNode> GetAllWaypoints();
    List<WaypointNode> GetCellWaypoints(GridCell cell);
    void GenerateWaypoints();
}