using UnityEngine;

public class BuildingCarPark : BuildingBase
{
    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        Id = entityId;
        _cell = cell;

        InitialiseVehicleWaypoints();
    }

    private void InitialiseVehicleWaypoints()
    {

    }
}
