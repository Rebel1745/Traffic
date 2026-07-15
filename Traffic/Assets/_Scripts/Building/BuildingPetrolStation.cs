using UnityEngine;

public class BuildingPetrolStation : BuildingBase
{
    protected override int MaximumOccupancy => throw new System.NotImplementedException();

    protected override int CurrentOccupancy => throw new System.NotImplementedException();

    protected override int MaximumVehicleOccupancy => throw new System.NotImplementedException();

    protected override int CurrentVehicleOccupancy => throw new System.NotImplementedException();

    public override AgentController AddPersonToBuilding()
    {
        throw new System.NotImplementedException();
    }

    public override AgentController AddVehicleToBuilding()
    {
        throw new System.NotImplementedException();
    }

    public override void InitialiseBuilding(EntityId entityId, GridCell cell)
    {
        throw new System.NotImplementedException();
    }

    public override void PopulateBuilding()
    {
        throw new System.NotImplementedException();
    }

    protected override int GetGridCols()
    {
        throw new System.NotImplementedException();
    }

    protected override int GetGridRows()
    {
        throw new System.NotImplementedException();
    }

    protected override float GetGridSize()
    {
        throw new System.NotImplementedException();
    }
}
