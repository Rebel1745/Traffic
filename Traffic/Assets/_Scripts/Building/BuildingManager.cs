using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BuildingManager : MonoBehaviour, ISaveable
{
    public static BuildingManager Instance { get; private set; }

    public string SaveKey => "Buildings";

    [Header("Building Prefabs")]
    [SerializeField] private GameObject _housePrefab;
    [SerializeField] private GameObject _petrolStationPrefab;
    [SerializeField] private GameObject _carParkPrefab;

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
            BuildingSubState.House => _housePrefab,
            BuildingSubState.PetrolStation => _petrolStationPrefab,
            BuildingSubState.CarPark => _carParkPrefab,
            _ => _housePrefab
        };

        BuildingBase bb = prefab.GetComponent<BuildingBase>();
        xCells = bb.BuildingXCells;
        zCells = bb.BuildingZCells;
    }

    public List<EntityId> GetBuildingsByType(BuildingType type)
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
            Houses = new(),
            CarParks = new(),
            PetrolStations = new()
        };

        BuildingBase currentBuilding;
        BuildingHouse currentHouse;
        BuildingCarPark currentCarPark;
        BuildingPetrolStation currentPetrolStation;

        // get all the houses
        List<EntityId> houses = GetBuildingsByType(BuildingType.House);
        foreach (EntityId id in houses)
        {
            currentBuilding = GetBuilding(id);
            currentHouse = currentBuilding as BuildingHouse;

            BuildingHouseSaveData houseSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.Houses.Add(houseSaveData);
        }

        // get all the car parks
        List<EntityId> carParks = GetBuildingsByType(BuildingType.CarPark);
        foreach (EntityId id in carParks)
        {
            currentBuilding = GetBuilding(id);
            currentCarPark = currentBuilding as BuildingCarPark;

            BuildingCarParkSaveData carParkSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.CarParks.Add(carParkSaveData);
        }

        // get all the petrol stations
        List<EntityId> petrolStations = GetBuildingsByType(BuildingType.PetrolStation);
        foreach (EntityId id in petrolStations)
        {
            currentBuilding = GetBuilding(id);
            currentPetrolStation = currentBuilding as BuildingPetrolStation;

            BuildingPetrolStationSaveData petrolStationSaveData = new()
            {
                Id = currentBuilding.Id.ToString(),
                BuildingName = currentBuilding.BuildingName,
                CellX = currentBuilding.Cell.Position.x,
                CellZ = currentBuilding.Cell.Position.z,
                WidthX = currentBuilding.BuildingXCells,
                WidthZ = currentBuilding.BuildingZCells
            };

            buildings.PetrolStations.Add(petrolStationSaveData);
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

        // load houses
        foreach (BuildingHouseSaveData house in saveData.Buildings.Houses)
        {
            Vector3Int firstCell = new(house.CellX, 0, house.CellZ);
            EntityId houseId = EntityId.FromString(house.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(houseId, _housePrefab, firstCell, house.WidthX, house.WidthZ);
            newBuilding.LoadBuilding(houseId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = house.BuildingName;
        }

        // load car parks
        foreach (BuildingCarParkSaveData carPark in saveData.Buildings.CarParks)
        {
            Vector3Int firstCell = new(carPark.CellX, 0, carPark.CellZ);
            EntityId carParkId = EntityId.FromString(carPark.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(carParkId, _carParkPrefab, firstCell, carPark.WidthX, carPark.WidthZ);
            newBuilding.LoadBuilding(carParkId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = carPark.BuildingName;
        }

        // load petrol stations
        foreach (BuildingPetrolStationSaveData petrolStation in saveData.Buildings.PetrolStations)
        {
            Vector3Int firstCell = new(petrolStation.CellX, 0, petrolStation.CellZ);
            EntityId petrolStationId = EntityId.FromString(petrolStation.Id);

            BuildingBase newBuilding = PlaceAndRegisterBuilding(petrolStationId, _petrolStationPrefab, firstCell, petrolStation.WidthX, petrolStation.WidthZ);
            newBuilding.LoadBuilding(petrolStationId, GridManager.Instance.GetCell(firstCell));
            newBuilding.name = petrolStation.BuildingName;
        }

        Debug.Log($"[BuildingManager] Loaded {saveData.Buildings.Houses.Count} houses, {saveData.Buildings.CarParks.Count} car parks, and {saveData.Buildings.PetrolStations.Count} petrol stations.");
    }
}
