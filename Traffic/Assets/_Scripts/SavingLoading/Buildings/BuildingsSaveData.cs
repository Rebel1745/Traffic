using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingsSaveData
{
    public List<BuildingHouseSaveData> Houses = new();
    public List<BuildingCarParkSaveData> CarParks = new();
    public List<BuildingPetrolStationSaveData> PetrolStations = new();
}
