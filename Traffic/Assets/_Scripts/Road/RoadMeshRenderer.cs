using System;
using UnityEngine;

public class RoadMeshRenderer : MonoBehaviour
{
    public static RoadMeshRenderer Instance;

    [SerializeField] private MeshFilter _roadMeshFilter;
    [SerializeField] private MeshFilter _pavementMeshFilter;
    [SerializeField] private RoadConfig _config;

    private RoadMeshGenerator _generator;

    public event Action OnRoadMeshUpdated;

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
        GridManager.OnRoadGridUpdated += OnGridUpdated;
    }

    private void OnDisable()
    {
        // Subscribe to road grid updates
        GridManager.OnRoadGridUpdated -= OnGridUpdated;
    }

    private void Initialise()
    {
        _generator = new RoadMeshGenerator(_config);

        if (_roadMeshFilter == null)
        {
            GameObject roadObj = new GameObject("RoadMesh");
            roadObj.transform.parent = transform;
            _roadMeshFilter = roadObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = roadObj.AddComponent<MeshRenderer>();
            renderer.material = _config.roadMaterial;
        }

        if (_pavementMeshFilter == null)
        {
            GameObject pavementObj = new GameObject("PavementMesh");
            pavementObj.transform.parent = transform;
            _pavementMeshFilter = pavementObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = pavementObj.AddComponent<MeshRenderer>();
            renderer.material = _config.pavementMaterial;
        }
    }

    private void OnGridUpdated()
    {
        UpdateRoadMesh(true);
    }

    public void UpdateRoadMesh(bool fireEvent = true)
    {
        if (_roadMeshFilter == null || _pavementMeshFilter == null)
            return;

        MeshData roadMeshData = _generator.GenerateRoadNetwork(GridManager.Instance.GetGrid(), GridManager.Instance.GridOrigin, GridManager.Instance.CellSize);
        MeshData pavementMeshData = _generator.GeneratePavementNetwork(GridManager.Instance.GetGrid(), GridManager.Instance.GridOrigin, GridManager.Instance.CellSize);

        // Apply road mesh
        Mesh roadMesh = new Mesh();
        roadMesh.vertices = roadMeshData.vertices.ToArray();
        roadMesh.triangles = roadMeshData.triangles.ToArray();
        roadMesh.uv = roadMeshData.uvs.ToArray();
        roadMesh.RecalculateNormals();
        _roadMeshFilter.mesh = roadMesh;

        // Apply pavement mesh
        Mesh pavementMesh = new Mesh();
        pavementMesh.vertices = pavementMeshData.vertices.ToArray();
        pavementMesh.triangles = pavementMeshData.triangles.ToArray();
        pavementMesh.uv = pavementMeshData.uvs.ToArray();
        pavementMesh.RecalculateNormals();
        _pavementMeshFilter.mesh = pavementMesh;

        if (fireEvent) // this should only be false if we are loading a road because we will also be loading the waypoints
            OnRoadMeshUpdated?.Invoke();
    }

    public float GetLaneWidth()
    {
        return _config.laneWidth;
    }

    public float GetPavementWidth()
    {
        return _config.pavementWidth;
    }
}