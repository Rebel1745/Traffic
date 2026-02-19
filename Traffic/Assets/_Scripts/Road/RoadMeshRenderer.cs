using System;
using UnityEngine;

public class RoadMeshRenderer : MonoBehaviour
{
    public static RoadMeshRenderer Instance;

    [SerializeField] private MeshFilter roadMeshFilter;
    [SerializeField] private MeshFilter pavementMeshFilter;
    [SerializeField] private RoadConfig config;

    private RoadMeshGenerator generator;

    public static event Action OnRoadMeshUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Initialise();
    }

    private void OnEnable()
    {
        // Subscribe to road grid updates
        GridManager.OnRoadGridUpdated += UpdateRoadMesh;
    }

    private void OnDisable()
    {
        // Subscribe to road grid updates
        GridManager.OnRoadGridUpdated -= UpdateRoadMesh;
    }

    private void Initialise()
    {
        generator = new RoadMeshGenerator(config);

        if (roadMeshFilter == null)
        {
            GameObject roadObj = new GameObject("RoadMesh");
            roadObj.transform.parent = transform;
            roadMeshFilter = roadObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = roadObj.AddComponent<MeshRenderer>();
            renderer.material = config.roadMaterial;
        }

        if (pavementMeshFilter == null)
        {
            GameObject pavementObj = new GameObject("PavementMesh");
            pavementObj.transform.parent = transform;
            pavementMeshFilter = pavementObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = pavementObj.AddComponent<MeshRenderer>();
            renderer.material = config.pavementMaterial;
        }
    }

    private void UpdateRoadMesh()
    {
        if (roadMeshFilter == null || pavementMeshFilter == null)
            return;

        MeshData roadMeshData = generator.GenerateRoadNetwork(GridManager.Instance.GetGrid(), GridManager.Instance.GridOrigin, GridManager.Instance.CellSize);
        MeshData pavementMeshData = generator.GeneratePavementNetwork(GridManager.Instance.GetGrid(), GridManager.Instance.GridOrigin, GridManager.Instance.CellSize);

        // Apply road mesh
        Mesh roadMesh = new Mesh();
        roadMesh.vertices = roadMeshData.vertices.ToArray();
        roadMesh.triangles = roadMeshData.triangles.ToArray();
        roadMesh.uv = roadMeshData.uvs.ToArray();
        roadMesh.RecalculateNormals();
        roadMeshFilter.mesh = roadMesh;

        // Apply pavement mesh
        Mesh pavementMesh = new Mesh();
        pavementMesh.vertices = pavementMeshData.vertices.ToArray();
        pavementMesh.triangles = pavementMeshData.triangles.ToArray();
        pavementMesh.uv = pavementMeshData.uvs.ToArray();
        pavementMesh.RecalculateNormals();
        pavementMeshFilter.mesh = pavementMesh;

        OnRoadMeshUpdated?.Invoke();
    }

    public float GetLaneWidth()
    {
        return config.laneWidth;
    }
}