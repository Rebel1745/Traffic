using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

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

    public void PlaceAndRegisterBuilding(GameObject prefab, Vector3Int firstCell, int xWidth, int zWidth)
    {
        float cellSize = GridManager.Instance.CellSize;
        float pavementHeight = RoadMeshRenderer.Instance.GetPavementHeight();

        GameObject building = new GameObject("Building (" + firstCell.x + ", " + firstCell.z + ")");
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

        EntityId newId = EntityId.New();
        _allBuildings[newId] = bc;

        bc.InitialiseBuilding(newId, GridManager.Instance.GetCellAtWorldPosition(anchorWorldPos));
    }

    public BuildingBase GetBuilding(EntityId entityId)
    {
        return _allBuildings[entityId];
    }

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
}
