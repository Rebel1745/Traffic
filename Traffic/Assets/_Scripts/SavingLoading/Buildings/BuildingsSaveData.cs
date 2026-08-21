using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingsSaveData
{
    public List<BuildingStoreRoadsideSaveData> Bakeries = new();
    public List<BuildingStoreRoadsideSaveData> Bars = new();
    public List<BuildingCarParkSaveData> CarParks = new();
    public List<BuildingStoreRoadsideSaveData> ChickenShops = new();
    public List<BuildingStoreRoadsideSaveData> CoffeeShops = new();
    public List<BuildingStoreRoadsideSaveData> DrugStores = new();
    public List<BuildingStoreRoadsideSaveData> FastFoodShops = new();
    public List<BuildingStoreRoadsideSaveData> GiftShops = new();
    public List<BuildingHouseSaveData> Houses = new();
    public List<BuildingStoreRoadsideSaveData> MusicShops = new();
    public List<BuildingPetrolStationSaveData> PetrolStations = new();
    public List<BuildingStoreRoadsideSaveData> PizzaShops = new();
    public List<BuildingStoreRoadsideSaveData> Restaurants = new();
}
