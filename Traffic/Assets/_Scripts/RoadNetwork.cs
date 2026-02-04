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

        // Check for intersections with existing segments and split them if needed
        CheckAndSplitIntersectingSegments(start, end);

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

    private void CheckAndSplitIntersectingSegments(Vector3 newStart, Vector3 newEnd)
    {
        List<RoadSegment> segmentsToSplit = new List<RoadSegment>();

        foreach (RoadSegment segment in segments)
        {
            Vector3? intersectionPoint = GetLineIntersection(newStart, newEnd, segment.startPos, segment.endPos);

            if (intersectionPoint.HasValue)
            {
                // Check if intersection is not at the endpoints (to avoid duplicate nodes)
                float distToStart = Vector3.Distance(intersectionPoint.Value, segment.startPos);
                float distToEnd = Vector3.Distance(intersectionPoint.Value, segment.endPos);

                if (distToStart > config.nodeSnapDistance && distToEnd > config.nodeSnapDistance)
                {
                    segmentsToSplit.Add(segment);
                }
            }
        }

        // Split the segments
        foreach (RoadSegment segment in segmentsToSplit)
        {
            SplitRoadSegment(segment, GetLineIntersection(newStart, newEnd, segment.startPos, segment.endPos).Value);
        }
    }

    private void SplitRoadSegment(RoadSegment segment, Vector3 splitPoint)
    {
        // Create new node at split point
        RoadNode middleNode = FindOrCreateNode(splitPoint);

        // Create two new segments
        RoadSegment segment1 = new RoadSegment(segment.startPos, splitPoint, segment.direction);
        RoadSegment segment2 = new RoadSegment(splitPoint, segment.endPos, segment.direction);

        // Set up nodes for first segment
        segment1.startNode = segment.startNode;
        segment1.endNode = middleNode;

        // Set up nodes for second segment
        segment2.startNode = middleNode;
        segment2.endNode = segment.endNode;

        // Remove old segment connections
        segment.startNode.connectedSegments.Remove(segment);
        segment.endNode.connectedSegments.Remove(segment);

        // Add new segment connections
        segment1.startNode.connectedSegments.Add(segment1);
        segment1.endNode.connectedSegments.Add(segment1);

        segment2.startNode.connectedSegments.Add(segment2);
        segment2.endNode.connectedSegments.Add(segment2);

        // Update node types
        segment1.startNode.UpdateNodeType();
        segment1.endNode.UpdateNodeType();
        segment2.startNode.UpdateNodeType();
        segment2.endNode.UpdateNodeType();

        // Replace old segment with new ones
        segments.Remove(segment);
        segments.Add(segment1);
        segments.Add(segment2);
    }

    private Vector3? GetLineIntersection(Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End)
    {
        // Work in 2D (ignore Y axis)
        Vector2 p1 = new Vector2(line1Start.x, line1Start.z);
        Vector2 p2 = new Vector2(line1End.x, line1End.z);
        Vector2 p3 = new Vector2(line2Start.x, line2Start.z);
        Vector2 p4 = new Vector2(line2End.x, line2End.z);

        Vector2 d1 = p2 - p1;
        Vector2 d2 = p4 - p3;

        float cross = d1.x * d2.y - d1.y * d2.x;

        // Lines are parallel
        if (Mathf.Abs(cross) < 0.0001f)
        {
            return null;
        }

        Vector2 p3p1 = p1 - p3;
        float t1 = (p3p1.x * d2.y - p3p1.y * d2.x) / cross;
        float t2 = (p3p1.x * d1.y - p3p1.y * d1.x) / cross;

        // Check if intersection is within both line segments
        if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
        {
            Vector3 intersection = new Vector3(
                p1.x + t1 * d1.x,
                0, // Y coordinate (assuming roads are on ground plane)
                p1.y + t1 * d1.y
            );

            return intersection;
        }

        return null;
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
        RoadMeshGenerator_old generator = new RoadMeshGenerator_old(config);

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