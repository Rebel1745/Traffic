using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoadNetwork : MonoBehaviour
{
    [SerializeField] private RoadConfig config;
    [SerializeField] private MeshFilter roadMeshFilter;
    [SerializeField] private MeshFilter pavementMeshFilter;

    private List<RoadSegment> segments = new List<RoadSegment>();
    private List<RoadNode> nodes = new List<RoadNode>();

    void Start()
    {
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

    public void AddRoadSegment(Vector3 start, Vector3 end)
    {
        // Determine direction
        RoadDirection direction = Mathf.Abs(end.x - start.x) > Mathf.Abs(end.z - start.z)
            ? RoadDirection.EastWest
            : RoadDirection.NorthSouth;

        RoadSegment newSegment = new RoadSegment(start, end, direction);

        // Find or create nodes
        newSegment.startNode = FindOrCreateNode(start);
        newSegment.endNode = FindOrCreateNode(end);

        // Connect segment to nodes
        newSegment.startNode.connectedSegments.Add(newSegment);
        newSegment.endNode.connectedSegments.Add(newSegment);

        // Update node types
        newSegment.startNode.UpdateNodeType();
        newSegment.endNode.UpdateNodeType();

        segments.Add(newSegment);

        // Regenerate mesh
        RegenerateMesh();
    }

    private RoadNode FindOrCreateNode(Vector3 position)
    {
        // Check if node already exists at this position
        RoadNode existingNode = nodes.FirstOrDefault(n =>
            Vector3.Distance(n.position, position) < config.nodeSnapDistance);

        if (existingNode != null)
        {
            return existingNode;
        }

        // Create new node
        RoadNode newNode = new RoadNode(position);
        nodes.Add(newNode);
        return newNode;
    }

    private void RegenerateMesh()
    {
        RoadMeshGenerator generator = new RoadMeshGenerator(config);

        MeshData roadMeshData = generator.GenerateRoadNetwork(segments, nodes);
        MeshData pavementMeshData = generator.GeneratePavementNetwork(segments, nodes);

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
    }
}