using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    private Dictionary<EntityId, BuildingController> _allBuildings = new();

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

        BuildingController bc = buildingObj.GetComponent<BuildingController>();

        EntityId newId = EntityId.New();
        _allBuildings[newId] = bc;

        bc.SetupBuilding(newId, GridManager.Instance.GetCellAtWorldPosition(anchorWorldPos));
    }

    public BuildingController GetBuilding(EntityId entityId)
    {
        return _allBuildings[entityId];
    }
}
