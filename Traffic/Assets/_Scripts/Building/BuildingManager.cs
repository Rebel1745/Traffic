using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BuildingManager : MonoBehaviour, ISaveable
{
    public static BuildingManager Instance { get; private set; }

    public string SaveKey => "Buildings";

    [Header("Building Prefabs")]
    [SerializeField] private GameObject _bakeryPrefab;
    [SerializeField] private GameObject _barPrefab;
    [SerializeField] private GameObject _carParkPrefab;
    [SerializeField] private GameObject _chickenShopPrefab;
    [SerializeField] private GameObject _coffeeShopPrefab;
    [SerializeField] private GameObject _drugStorePrefab;
    [SerializeField] private GameObject _fastFoodPrefab;
    [SerializeField] private GameObject _giftShopPrefab;
    [SerializeField] private GameObject _housePrefab;
    [SerializeField] private GameObject _musicShopPrefab;
    [SerializeField] private GameObject _petrolStationPrefab;
    [SerializeField] private GameObject _pizzaShopPrefab;
    [SerializeField] private GameObject _restaurantPrefab;

    private Dictionary<EntityId, BuildingBase> _allBuildings = new();

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
    }

    private void OnDestroy()
    {
        SaveManager.Instance.UnregisterSaveable(this);
    }

    public BuildingBase PlaceAndRegisterBuilding(EntityId id, GameObject prefab, Vector3Int firstCell, int xWidth, int zWidth)
    {
        float cellSize = GridManager.Instance.CellSize;
        float pavementHeight = RoadMeshRenderer.Instance.GetPavementHeight();

        GameObject building = new();
        building.transform.parent = this.transform;

        // add th building on top of the foundation
        GameObject buildingObj = Instantiate(prefab, Vector3.zero, prefab.transform.rotation);
        buildingObj.transform.parent = building.transform;

        Vector3 anchorWorldPos = GridManager.Instance.GridToWorldPosition(firstCell.x, firstCell.z);

        float xOffset = ((xWidth * cellSize) / 2f) - (cellSize / 2f);
        float zOffset = ((zWidth * cellSize) / 2f) - (cellSize / 2f);

        Vector3 finalPosition = new Vector3(
            anchorWorldPos.x + xOffset,
            anchorWorldPos.y, // Keep Y from grid world pos (usually terrain height or 0)
            anchorWorldPos.z + zOffset
        );

        building.transform.position = finalPosition;

        BuildingBase bc = buildingObj.GetComponent<BuildingBase>();

        building.name = bc.BuildingName + "(" + firstCell.x + ", " + firstCell.z + ")";

        _allBuildings[id] = bc;

        return bc;
    }

    public BuildingBase GetBuilding(EntityId entityId)
        => _allBuildings[entityId];

    public BuildingBase GetBuilding(string id)
        => GetBuilding(EntityId.FromString(id));

    public void GetBuildingPrefabDetailsFromSimulationState(out GameObject prefab, out int xCells, out int zCells)
    {
        prefab = SimulationManager.Instance.CurrentState.BuildingSubState switch
        {
            BuildingSubState.Bakery => _bakeryPrefab,
            BuildingSubState.Bar => _barPrefab,
            BuildingSubState.CarPark => _carParkPrefab,
            BuildingSubState.ChickenShop => _chickenShopPrefab,
            BuildingSubState.CoffeeShop => _coffeeShopPrefab,
            BuildingSubState.DrugStore => _drugStorePrefab,
            BuildingSubState.FastFood => _fastFoodPrefab,
            BuildingSubState.GiftShop => _giftShopPrefab,
            BuildingSubState.House => _housePrefab,
            BuildingSubState.MusicShop => _musicShopPrefab,
            BuildingSubState.PetrolStation => _petrolStationPrefab,
            BuildingSubState.PizzaShop => _pizzaShopPrefab,
            BuildingSubState.Restaurant => _restaurantPrefab,
            _ => _housePrefab
        };

        BuildingBase bb = prefab.GetComponent<BuildingBase>();
        xCells = bb.BuildingXCells;
        zCells = bb.BuildingZCells;
    }

    public List<EntityId> GetBuildingsByType(BuildingSubState type)
    {
        return _allBuildings.Values
        .Where(building => building.BuildingType == type)
        .Select(building => building.Id)
        .ToList();
    }

    public EntityId GetClosestBuildingToPosition(List<EntityId> buildings, Vector3 position)
    {
        EntityId closestId = buildings[0];
        float closestDistance = Mathf.Infinity;
        float currentDistance = 0f;
        BuildingBase building;

        foreach (EntityId id in buildings)
        {
            building = GetBuilding(id);
            currentDistance = Utils.GetDistanceWithSetHeight(building.transform.position, position, 0f);

            if (currentDistance < closestDistance)
            {
                closestId = id;
                closestDistance = currentDistance;
            }
        }

        return closestId;
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        BuildingsSaveData buildings = new()
        {
            Bakeries = new(),
            Bars = new(),
            CarParks = new(),
            ChickenShops = new(),
            CoffeeShops = new(),
            DrugStores = new(),
            FastFoodShops = new(),
            GiftShops = new(),
            Houses = new(),
            MusicShops = new(),
            PetrolStations = new(),
            PizzaShops = new(),
            Restaurants = new()
        };

        List<EntityId> buildingList;
        BuildingBase currentBuilding;
        BuildingCarPark currentCarPark;
        BuildingHouse currentHouse;
        BuildingPetrolStation currentPetrolStation;
        BuildingStoreRoadside currentStore;

        // get all the bakeries
        buildingList = GetBuildingsByType(BuildingSubState.Bakery);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.Bakeries.Add(storeSaveData);
        }

        // get all the bars
        buildingList = GetBuildingsByType(BuildingSubState.Bar);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.Bars.Add(storeSaveData);
        }

        // get all the car parks
        buildingList = GetBuildingsByType(BuildingSubState.CarPark);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentCarPark = currentBuilding as BuildingCarPark;

            BuildingCarParkSaveData carParkSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.CarParks.Add(carParkSaveData);
        }

        // get all the chicken shops
        buildingList = GetBuildingsByType(BuildingSubState.ChickenShop);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.ChickenShops.Add(storeSaveData);
        }

        // get all the CoffeeShops
        buildingList = GetBuildingsByType(BuildingSubState.CoffeeShop);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.CoffeeShops.Add(storeSaveData);
        }

        // get all the DrugStores
        buildingList = GetBuildingsByType(BuildingSubState.DrugStore);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.DrugStores.Add(storeSaveData);
        }

        // get all the FastFood shops
        buildingList = GetBuildingsByType(BuildingSubState.FastFood);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.FastFoodShops.Add(storeSaveData);
        }

        // get all the GiftShops
        buildingList = GetBuildingsByType(BuildingSubState.GiftShop);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.GiftShops.Add(storeSaveData);
        }

        // get all the houses
        buildingList = GetBuildingsByType(BuildingSubState.House);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentHouse = currentBuilding as BuildingHouse;

            BuildingHouseSaveData houseSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.Houses.Add(houseSaveData);
        }

        // get all the MusicShops
        buildingList = GetBuildingsByType(BuildingSubState.MusicShop);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.MusicShops.Add(storeSaveData);
        }

        // get all the petrol stations
        buildingList = GetBuildingsByType(BuildingSubState.PetrolStation);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentPetrolStation = currentBuilding as BuildingPetrolStation;

            BuildingPetrolStationSaveData petrolStationSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.PetrolStations.Add(petrolStationSaveData);
        }

        // get all the PizzaShops
        buildingList = GetBuildingsByType(BuildingSubState.PizzaShop);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.PizzaShops.Add(storeSaveData);
        }

        // get all the Restaurants
        buildingList = GetBuildingsByType(BuildingSubState.Restaurant);
        foreach (EntityId id in buildingList)
        {
            currentBuilding = GetBuilding(id);
            currentStore = currentBuilding as BuildingStoreRoadside;

            BuildingStoreRoadsideSaveData storeSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingType = currentBuilding.BuildingType,
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.Restaurants.Add(storeSaveData);
        }

        saveData.Buildings = buildings;
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.Buildings == null)
        {
            Debug.LogWarning("[BuildingManager] No building data in save file.");
            return;
        }

        // clear the groups and delete all building game objects from the world
        _allBuildings.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.Bakeries)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _bakeryPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.Bars)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _barPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingCarParkSaveData carPark in saveData.Buildings.CarParks)
        {
            Vector3Int firstCell = new(carPark.CellX, 0, carPark.CellZ);
            EntityId carParkId = EntityId.FromString(carPark.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(carParkId, _carParkPrefab, firstCell, carPark.WidthX, carPark.WidthZ);
            newBuilding.LoadBuilding(carParkId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = carPark.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.ChickenShops)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _chickenShopPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.CoffeeShops)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _coffeeShopPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.DrugStores)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _drugStorePrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.FastFoodShops)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _fastFoodPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.GiftShops)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _giftShopPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingHouseSaveData house in saveData.Buildings.Houses)
        {
            Vector3Int firstCell = new(house.CellX, 0, house.CellZ);
            EntityId houseId = EntityId.FromString(house.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(houseId, _housePrefab, firstCell, house.WidthX, house.WidthZ);
            newBuilding.LoadBuilding(houseId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = house.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.MusicShops)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _musicShopPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingPetrolStationSaveData petrolStation in saveData.Buildings.PetrolStations)
        {
            Vector3Int firstCell = new(petrolStation.CellX, 0, petrolStation.CellZ);
            EntityId petrolStationId = EntityId.FromString(petrolStation.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(petrolStationId, _petrolStationPrefab, firstCell, petrolStation.WidthX, petrolStation.WidthZ);
            newBuilding.LoadBuilding(petrolStationId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = petrolStation.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.PizzaShops)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _pizzaShopPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }

        foreach (BuildingStoreRoadsideSaveData store in saveData.Buildings.Restaurants)
        {
            Vector3Int firstCell = new(store.CellX, 0, store.CellZ);
            EntityId storeId = EntityId.FromString(store.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(storeId, _restaurantPrefab, firstCell, store.WidthX, store.WidthZ);
            newBuilding.LoadBuilding(storeId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = store.BuildingName;
        }
    }
}
