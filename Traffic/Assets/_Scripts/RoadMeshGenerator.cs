using UnityEngine;
using System.Collections.Generic;

public class RoadMeshGenerator
{
    private RoadConfig config;

    private const float GRID_WIDTH = 2.5f;

    public RoadMeshGenerator(RoadConfig config)
    {
        this.config = config;
    }

    public MeshData GenerateRoadNetwork(List<RoadSegment> segments, List<RoadNode> nodes)
    {
        MeshData meshData = new MeshData();

        // Generate straight segments
        foreach (RoadSegment segment in segments)
        {
            GenerateRoadSegment(meshData, segment);
        }

        // // Generate junctions
        foreach (RoadNode node in nodes)
        {
            GenerateJunction(meshData, node, true); // true = road surface
        }

        return meshData;
    }

    public MeshData GeneratePavementNetwork(List<RoadSegment> segments, List<RoadNode> nodes)
    {
        MeshData meshData = new MeshData();

        // Generate pavement along segments
        foreach (RoadSegment segment in segments)
        {
            GeneratePavementSegment(meshData, segment);
        }

        // Generate pavement at junctions
        foreach (RoadNode node in nodes)
        {
            GenerateJunction(meshData, node, false); // false = pavement
        }

        return meshData;
    }

    private void GenerateRoadSegment(MeshData meshData, RoadSegment segment)
    {
        if (Vector3.Distance(segment.startPos, segment.endPos) < 0.1f) return;

        float halfWidth = config.GetHalfRoadWidth(segment.lanesPerDirection);
        Vector3 direction = (segment.endPos - segment.startPos).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);

        // Calculate segment endpoints (shortened to account for junctions)
        Vector3 start = segment.startPos + direction * GRID_WIDTH;
        Vector3 end = segment.endPos - direction * GRID_WIDTH;

        // Define the four corners of the road (in x-z plane)
        Vector3 v0 = start - perpendicular * halfWidth;  // bottom-left
        Vector3 v1 = start + perpendicular * halfWidth;  // bottom-right
        Vector3 v2 = end + perpendicular * halfWidth;   // top-right
        Vector3 v3 = end - perpendicular * halfWidth;   // top-left

        Vector3 min = new Vector3(Mathf.Min(v0.x, v3.x), 0, Mathf.Min(v0.z, v3.z));
        Vector3 max = new Vector3(Mathf.Max(v1.x, v2.x), 0, Mathf.Max(v1.z, v2.z));

        // Add the cuboid
        meshData.AddCuboid(min, max, config.roadThickness);
    }

    private void GeneratePavementSegment(MeshData meshData, RoadSegment segment)
    {
        if (Vector3.Distance(segment.startPos, segment.endPos) < 0.1f) return;

        float roadHalfWidth = config.GetHalfRoadWidth(segment.lanesPerDirection);
        float pavementWidth = config.pavementWidth;
        float pavementThickness = config.pavementThickness;

        Vector3 direction = (segment.endPos - segment.startPos).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);

        // Calculate segment endpoints (shortened to account for junctions)
        Vector3 start = segment.startPos + direction * GRID_WIDTH;
        Vector3 end = segment.endPos - direction * GRID_WIDTH;

        // Left pavement
        Vector3 leftStart = start - perpendicular * (roadHalfWidth + pavementWidth);
        Vector3 leftEnd = end - perpendicular * (roadHalfWidth + pavementWidth);
        Vector3 leftInnerStart = start - perpendicular * roadHalfWidth;
        Vector3 leftInnerEnd = end - perpendicular * roadHalfWidth;

        Vector3 leftV0 = new Vector3(Mathf.Min(leftStart.x, leftEnd.x), 0, Mathf.Min(leftStart.z, leftEnd.z));
        Vector3 leftV1 = new Vector3(Mathf.Max(leftInnerStart.x, leftInnerEnd.x), 0, Mathf.Max(leftInnerStart.z, leftInnerEnd.z));

        meshData.AddCuboid(leftV0, leftV1, pavementThickness);

        // Right pavement
        Vector3 rightStart = start + perpendicular * roadHalfWidth;
        Vector3 rightEnd = end + perpendicular * roadHalfWidth;
        Vector3 rightOuterStart = start + perpendicular * (roadHalfWidth + pavementWidth);
        Vector3 rightOuterEnd = end + perpendicular * (roadHalfWidth + pavementWidth);

        Vector3 rightV0 = new Vector3(Mathf.Min(rightStart.x, rightEnd.x), 0, Mathf.Min(rightStart.z, rightEnd.z));
        Vector3 rightV1 = new Vector3(Mathf.Max(rightOuterStart.x, rightOuterEnd.x), 0, Mathf.Max(rightOuterStart.z, rightOuterEnd.z));

        meshData.AddCuboid(rightV0, rightV1, pavementThickness);
    }

    private void GenerateJunction(MeshData meshData, RoadNode node, bool isRoadSurface)
    {
        switch (node.nodeType)
        {
            case NodeType.DeadEnd:
                GenerateDeadEnd(meshData, node, isRoadSurface);
                break;
            case NodeType.Straight:
                // Straight connections don't need junction geometry
                break;
            case NodeType.Corner:
                //GenerateCorner(meshData, node, isRoadSurface);
                break;
            case NodeType.TJunction:
                //GenerateTJunction(meshData, node, isRoadSurface);
                break;
            case NodeType.Crossroad:
                //GenerateCrossroad(meshData, node, isRoadSurface);
                break;
        }
    }

    private void GenerateDeadEnd(MeshData meshData, RoadNode node, bool isRoadSurface)
    {
        if (node.connectedSegments.Count == 0) return;

        RoadSegment segment = node.connectedSegments[0];
        int lanes = segment.lanesPerDirection;

        // Determine which end of the segment this node is
        bool isStart = Vector3.Distance(node.position, segment.startPos) < 0.1f;
        Vector3 direction = isStart
            ? (segment.startPos - segment.endPos).normalized
            : (segment.endPos - segment.startPos).normalized;

        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);

        float halfRoadWidth = config.GetHalfRoadWidth(lanes);
        float halfTotalWidth = config.GetHalfTotalWidth(lanes);
        float pavementWidth = config.pavementWidth;

        // Dead end extends GRID_WIDTH away from the road
        Vector3 center = node.position;
        Vector3 back = center - direction * GRID_WIDTH;

        if (isRoadSurface) // we are creating a road
        {
            float roadThickness = config.roadThickness;

            // Road surface (front piece)
            Vector3 roadV0 = new Vector3(
                Mathf.Min(back.x - perpendicular.x * halfRoadWidth, center.x - perpendicular.x * halfRoadWidth),
                0,
                Mathf.Min(back.z - perpendicular.z * halfRoadWidth, center.z - perpendicular.z * halfRoadWidth)
            );
            Vector3 roadV1 = new Vector3(
                Mathf.Max(back.x + perpendicular.x * halfRoadWidth, center.x + perpendicular.x * halfRoadWidth),
                0,
                Mathf.Max(back.z + perpendicular.z * halfRoadWidth, center.z + perpendicular.z * halfRoadWidth)
            );

            meshData.AddCuboid(roadV0, roadV1, roadThickness);
        }
        else // we are creating a pavement
        {
            float pavementThickness = config.pavementThickness;

            // Left pavement
            Vector3 leftOuterStart = center - perpendicular * (halfRoadWidth + pavementWidth);
            Vector3 leftOuterEnd = back - perpendicular * (halfRoadWidth + pavementWidth);
            Vector3 leftInnerStart = center - perpendicular * halfRoadWidth;
            Vector3 leftInnerEnd = back - perpendicular * halfRoadWidth;

            Vector3 leftV0 = new Vector3(
                Mathf.Min(leftOuterStart.x, leftOuterEnd.x, leftInnerStart.x, leftInnerEnd.x),
                0,
                Mathf.Min(leftOuterStart.z, leftOuterEnd.z, leftInnerStart.z, leftInnerEnd.z)
            );
            Vector3 leftV1 = new Vector3(
                Mathf.Max(leftOuterStart.x, leftOuterEnd.x, leftInnerStart.x, leftInnerEnd.x),
                0,
                Mathf.Max(leftOuterStart.z, leftOuterEnd.z, leftInnerStart.z, leftInnerEnd.z)
            );

            meshData.AddCuboid(leftV0, leftV1, pavementThickness);

            // Right pavement
            Vector3 rightInnerStart = center + perpendicular * halfRoadWidth;
            Vector3 rightInnerEnd = back + perpendicular * halfRoadWidth;
            Vector3 rightOuterStart = center + perpendicular * (halfRoadWidth + pavementWidth);
            Vector3 rightOuterEnd = back + perpendicular * (halfRoadWidth + pavementWidth);

            Vector3 rightV0 = new Vector3(
                Mathf.Min(rightInnerStart.x, rightInnerEnd.x, rightOuterStart.x, rightOuterEnd.x),
                0,
                Mathf.Min(rightInnerStart.z, rightInnerEnd.z, rightOuterStart.z, rightOuterEnd.z)
            );
            Vector3 rightV1 = new Vector3(
                Mathf.Max(rightInnerStart.x, rightInnerEnd.x, rightOuterStart.x, rightOuterEnd.x),
                0,
                Mathf.Max(rightInnerStart.z, rightInnerEnd.z, rightOuterStart.z, rightOuterEnd.z)
            );

            meshData.AddCuboid(rightV0, rightV1, pavementThickness);

            // End pavement (covers the back of the dead end)
            Vector3 endV0 = new Vector3(
                Mathf.Min(leftOuterStart.x, rightOuterStart.x),
                0,
                Mathf.Min(leftOuterStart.z, rightOuterStart.z)
            );
            Vector3 endV1 = new Vector3(
                Mathf.Max(leftOuterStart.x, rightOuterStart.x),
                0,
                Mathf.Max(leftOuterStart.z, rightOuterStart.z)
            );

            // Extend the end pavement to include the full width of the pavement
            Vector3 endV0Extended = new Vector3(
                Mathf.Min(endV0.x, endV0.x - direction.x * pavementWidth),
                0,
                Mathf.Min(endV0.z, endV0.z - direction.z * pavementWidth)
            );
            Vector3 endV1Extended = new Vector3(
                Mathf.Max(endV1.x, endV1.x - direction.x * pavementWidth),
                0,
                Mathf.Max(endV1.z, endV1.z - direction.z * pavementWidth)
            );

            meshData.AddCuboid(endV0Extended, endV1Extended, pavementThickness);
        }
    }

    /*
        private void GenerateCorner(MeshData meshData, RoadNode node, bool isRoadSurface)
        {
            if (node.connectedSegments.Count < 2) return;

            RoadSegment seg1 = node.connectedSegments[0];
            RoadSegment seg2 = node.connectedSegments[1];
            int lanes = seg1.lanesPerDirection;

            // Get directions from node
            Vector3 dir1 = GetDirectionFromNode(node, seg1);
            Vector3 dir2 = GetDirectionFromNode(node, seg2);

            if (isRoadSurface)
            {
                GenerateCornerRoad(meshData, node.position, dir1, dir2, lanes);
            }
            else
            {
                GenerateCornerPavement(meshData, node.position, dir1, dir2, lanes);
            }
        }

        private void GenerateCornerRoad(MeshData meshData, Vector3 center, Vector3 dir1, Vector3 dir2, int lanes)
        {
            float halfWidth = config.GetHalfRoadWidth(lanes);
            Vector3 perp1 = Vector3.Cross(dir1, Vector3.up);
            Vector3 perp2 = Vector3.Cross(dir2, Vector3.up);

            // Inner corner point (where the roads meet on the inside)
            Vector3 innerCorner = center;

            // Outer corner points
            Vector3 outer1 = center + dir1 * halfWidth - perp1 * halfWidth;
            Vector3 outer2 = center + dir2 * halfWidth - perp2 * halfWidth;

            // Edge points
            Vector3 edge1Inner = center - perp1 * halfWidth;
            Vector3 edge1Outer = center + dir1 * halfWidth + perp1 * halfWidth;
            Vector3 edge2Inner = center - perp2 * halfWidth;
            Vector3 edge2Outer = center + dir2 * halfWidth + perp2 * halfWidth;

            // Generate curved corner
            int segments = config.cornerSegments;
            List<Vector3> curvePoints = GenerateCurvePoints(outer1, outer2, center, segments);

            // Create fan from inner corner to curve
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                float t1 = (float)i / (curvePoints.Count - 1);
                float t2 = (float)(i + 1) / (curvePoints.Count - 1);

                // Add height to all points
                Vector3 p1 = curvePoints[i] + Vector3.up * config.roadHeight;
                Vector3 p2 = curvePoints[i + 1] + Vector3.up * config.roadHeight;
                Vector3 p3 = innerCorner + Vector3.up * config.roadHeight;

                meshData.AddTriangle(p3, p1, p2,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(t1, 1),
                    new Vector2(t2, 1)
                );
            }

            // Fill in the sides
            // meshData.AddRect(edge1Inner + Vector3.up * config.roadHeight,
            //     innerCorner + Vector3.up * config.roadHeight,
            //     curvePoints[0] + Vector3.up * config.roadHeight,
            //     edge1Outer + Vector3.up * config.roadHeight,
            //     new Vector2(0, 0), new Vector2(0.5f, 0.5f),
            //     new Vector2(1, 0), new Vector2(1, 1),
            //     config.roadThickness);

            // meshData.AddRect(innerCorner + Vector3.up * config.roadHeight,
            //     edge2Inner + Vector3.up * config.roadHeight,
            //     edge2Outer + Vector3.up * config.roadHeight,
            //     curvePoints[curvePoints.Count - 1] + Vector3.up * config.roadHeight,
            //     new Vector2(0.5f, 0.5f), new Vector2(0, 0),
            //     new Vector2(1, 1), new Vector2(1, 0),
            //     config.roadThickness);
        }

        private void GenerateCornerPavement(MeshData meshData, Vector3 center, Vector3 dir1, Vector3 dir2, int lanes)
        {
            float halfRoadWidth = config.GetHalfRoadWidth(lanes);
            float halfTotalWidth = config.GetHalfTotalWidth(lanes);
            Vector3 perp1 = Vector3.Cross(dir1, Vector3.up);
            Vector3 perp2 = Vector3.Cross(dir2, Vector3.up);

            // Inner pavement (inside the corner)
            Vector3 innerRoadCorner = center;
            Vector3 innerPavementCorner = center - perp1 * halfRoadWidth - perp2 * halfRoadWidth;

            Vector3 innerEdge1Road = center - perp1 * halfRoadWidth;
            Vector3 innerEdge1Pave = center + dir1 * halfTotalWidth - perp1 * halfRoadWidth;
            Vector3 innerEdge2Road = center - perp2 * halfRoadWidth;
            Vector3 innerEdge2Pave = center + dir2 * halfTotalWidth - perp2 * halfRoadWidth;

            // Generate inner corner curve
            int segments = config.cornerSegments;
            List<Vector3> innerCurve = GenerateCurvePoints(innerEdge1Pave, innerEdge2Pave, center, segments);

            // Inner pavement fan
            for (int i = 0; i < innerCurve.Count - 1; i++)
            {
                // meshData.AddTriangle(
                //     innerRoadCorner, innerCurve[i], innerCurve[i + 1],
                //     new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(1, 1)
                // );
            }

            // Outer pavement (outside the corner)
            Vector3 outerRoadCorner1 = center + dir1 * halfRoadWidth + perp1 * halfRoadWidth;
            Vector3 outerRoadCorner2 = center + dir2 * halfRoadWidth + perp2 * halfRoadWidth;
            Vector3 outerPaveCorner1 = center + dir1 * halfTotalWidth + perp1 * halfTotalWidth;
            Vector3 outerPaveCorner2 = center + dir2 * halfTotalWidth + perp2 * halfTotalWidth;

            Vector3 outerEdge1Inner = center + perp1 * halfRoadWidth;
            Vector3 outerEdge1Outer = center + dir1 * halfTotalWidth + perp1 * halfTotalWidth;
            Vector3 outerEdge2Inner = center + perp2 * halfRoadWidth;
            Vector3 outerEdge2Outer = center + dir2 * halfTotalWidth + perp2 * halfTotalWidth;

            // Generate outer corner curve
            List<Vector3> outerCurve = GenerateCurvePoints(outerPaveCorner1, outerPaveCorner2, center, segments);

            // Outer pavement fan
            for (int i = 0; i < outerCurve.Count - 1; i++)
            {
                // meshData.AddTriangle(
                //     outerRoadCorner1, outerCurve[i], outerCurve[i + 1],
                //     new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(1, 1)
                // );
            }

            // Fill in the sides
            // meshData.AddQuad(innerEdge1Pave, innerPavementCorner, outerEdge1Inner, outerEdge1Outer,
            //     new Vector2(0, 0), new Vector2(0.5f, 0.5f),
            //     new Vector2(1, 0), new Vector2(1, 1));

            // meshData.AddQuad(innerEdge2Pave, innerPavementCorner, outerEdge2Inner, outerEdge2Outer,
            //     new Vector2(0, 0), new Vector2(0.5f, 0.5f),
            //     new Vector2(1, 0), new Vector2(1, 1));
        }

        private void GenerateTJunction(MeshData meshData, RoadNode node, bool isRoadSurface)
        {
            if (node.connectedSegments.Count < 3) return;

            RoadSegment seg1 = node.connectedSegments[0];
            RoadSegment seg2 = node.connectedSegments[1];
            RoadSegment seg3 = node.connectedSegments[2];
            int lanes = seg1.lanesPerDirection;

            // Get directions from node
            Vector3 dir1 = GetDirectionFromNode(node, seg1);
            Vector3 dir2 = GetDirectionFromNode(node, seg2);
            Vector3 dir3 = GetDirectionFromNode(node, seg3);

            if (isRoadSurface)
            {
                GenerateTJunctionRoad(meshData, node.position, dir1, dir2, dir3, lanes);
            }
            else
            {
                GenerateTJunctionPavement(meshData, node.position, dir1, dir2, dir3, lanes);
            }
        }

        private void GenerateTJunctionRoad(MeshData meshData, Vector3 center, Vector3 dir1, Vector3 dir2, Vector3 dir3, int lanes)
        {
            float halfWidth = config.GetHalfRoadWidth(lanes);
            Vector3 perp1 = Vector3.Cross(dir1, Vector3.up);
            Vector3 perp2 = Vector3.Cross(dir2, Vector3.up);
            Vector3 perp3 = Vector3.Cross(dir3, Vector3.up);

            // Calculate the three corner points
            Vector3 corner1 = center + dir1 * halfWidth - perp1 * halfWidth;
            Vector3 corner2 = center + dir2 * halfWidth - perp2 * halfWidth;
            Vector3 corner3 = center + dir3 * halfWidth - perp3 * halfWidth;

            // Calculate the three edge points
            Vector3 edge1 = center - perp1 * halfWidth;
            Vector3 edge2 = center - perp2 * halfWidth;
            Vector3 edge3 = center - perp3 * halfWidth;

            // Calculate the three outer points
            Vector3 outer1 = center + dir1 * halfWidth + perp1 * halfWidth;
            Vector3 outer2 = center + dir2 * halfWidth + perp2 * halfWidth;
            Vector3 outer3 = center + dir3 * halfWidth + perp3 * halfWidth;

            // Create the three triangles forming the T-junction
            meshData.AddTriangle(edge1, corner1, corner2,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge2, corner2, corner3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge3, corner3, corner1,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Add the outer triangle
            meshData.AddTriangle(outer1, outer2, outer3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private void GenerateTJunctionPavement(MeshData meshData, Vector3 center, Vector3 dir1, Vector3 dir2, Vector3 dir3, int lanes)
        {
            float halfRoadWidth = config.GetHalfRoadWidth(lanes);
            float halfTotalWidth = config.GetHalfTotalWidth(lanes);
            Vector3 perp1 = Vector3.Cross(dir1, Vector3.up);
            Vector3 perp2 = Vector3.Cross(dir2, Vector3.up);
            Vector3 perp3 = Vector3.Cross(dir3, Vector3.up);

            // Calculate the three corner points
            Vector3 corner1 = center + dir1 * halfRoadWidth - perp1 * halfRoadWidth;
            Vector3 corner2 = center + dir2 * halfRoadWidth - perp2 * halfRoadWidth;
            Vector3 corner3 = center + dir3 * halfRoadWidth - perp3 * halfRoadWidth;

            // Calculate the three edge points
            Vector3 edge1 = center - perp1 * halfRoadWidth;
            Vector3 edge2 = center - perp2 * halfRoadWidth;
            Vector3 edge3 = center - perp3 * halfRoadWidth;

            // Calculate the three outer points
            Vector3 outer1 = center + dir1 * halfTotalWidth + perp1 * halfTotalWidth;
            Vector3 outer2 = center + dir2 * halfTotalWidth + perp2 * halfTotalWidth;
            Vector3 outer3 = center + dir3 * halfTotalWidth + perp3 * halfTotalWidth;

            // Calculate the three inner pavement points
            Vector3 innerPave1 = center + dir1 * halfTotalWidth - perp1 * halfTotalWidth;
            Vector3 innerPave2 = center + dir2 * halfTotalWidth - perp2 * halfTotalWidth;
            Vector3 innerPave3 = center + dir3 * halfTotalWidth - perp3 * halfTotalWidth;

            // Create the three triangles forming the T-junction pavement
            // meshData.AddTriangle(edge1, corner1, innerPave1,
            //     new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // meshData.AddTriangle(edge2, corner2, innerPave2,
            //     new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge3, corner3, innerPave3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Add the outer triangle
            meshData.AddTriangle(outer1, outer2, outer3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private void GenerateCrossroad(MeshData meshData, RoadNode node, bool isRoadSurface)
        {
            if (node.connectedSegments.Count < 4) return;

            RoadSegment seg1 = node.connectedSegments[0];
            RoadSegment seg2 = node.connectedSegments[1];
            RoadSegment seg3 = node.connectedSegments[2];
            RoadSegment seg4 = node.connectedSegments[3];
            int lanes = seg1.lanesPerDirection;

            // Get directions from node
            Vector3 dir1 = GetDirectionFromNode(node, seg1);
            Vector3 dir2 = GetDirectionFromNode(node, seg2);
            Vector3 dir3 = GetDirectionFromNode(node, seg3);
            Vector3 dir4 = GetDirectionFromNode(node, seg4);

            if (isRoadSurface)
            {
                GenerateCrossroadRoad(meshData, node.position, dir1, dir2, dir3, dir4, lanes);
            }
            else
            {
                GenerateCrossroadPavement(meshData, node.position, dir1, dir2, dir3, dir4, lanes);
            }
        }

        private void GenerateCrossroadRoad(MeshData meshData, Vector3 center, Vector3 dir1, Vector3 dir2, Vector3 dir3, Vector3 dir4, int lanes)
        {
            float halfWidth = config.GetHalfRoadWidth(lanes);
            Vector3 perp1 = Vector3.Cross(dir1, Vector3.up);
            Vector3 perp2 = Vector3.Cross(dir2, Vector3.up);
            Vector3 perp3 = Vector3.Cross(dir3, Vector3.up);
            Vector3 perp4 = Vector3.Cross(dir4, Vector3.up);

            // Calculate the four corner points
            Vector3 corner1 = center + dir1 * halfWidth - perp1 * halfWidth;
            Vector3 corner2 = center + dir2 * halfWidth - perp2 * halfWidth;
            Vector3 corner3 = center + dir3 * halfWidth - perp3 * halfWidth;
            Vector3 corner4 = center + dir4 * halfWidth - perp4 * halfWidth;

            // Calculate the four edge points
            Vector3 edge1 = center - perp1 * halfWidth;
            Vector3 edge2 = center - perp2 * halfWidth;
            Vector3 edge3 = center - perp3 * halfWidth;
            Vector3 edge4 = center - perp4 * halfWidth;

            // Calculate the four outer points
            Vector3 outer1 = center + dir1 * halfWidth + perp1 * halfWidth;
            Vector3 outer2 = center + dir2 * halfWidth + perp2 * halfWidth;
            Vector3 outer3 = center + dir3 * halfWidth + perp3 * halfWidth;
            Vector3 outer4 = center + dir4 * halfWidth + perp4 * halfWidth;

            // Create the four triangles forming the crossroad
            meshData.AddTriangle(edge1, corner1, corner2,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge2, corner2, corner3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge3, corner3, corner4,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge4, corner4, corner1,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Add the outer triangle
            meshData.AddTriangle(outer1, outer2, outer3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(outer1, outer3, outer4,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private void GenerateCrossroadPavement(MeshData meshData, Vector3 center, Vector3 dir1, Vector3 dir2, Vector3 dir3, Vector3 dir4, int lanes)
        {
            float halfRoadWidth = config.GetHalfRoadWidth(lanes);
            float halfTotalWidth = config.GetHalfTotalWidth(lanes);
            Vector3 perp1 = Vector3.Cross(dir1, Vector3.up);
            Vector3 perp2 = Vector3.Cross(dir2, Vector3.up);
            Vector3 perp3 = Vector3.Cross(dir3, Vector3.up);
            Vector3 perp4 = Vector3.Cross(dir4, Vector3.up);

            // Calculate the four corner points
            Vector3 corner1 = center + dir1 * halfRoadWidth - perp1 * halfRoadWidth;
            Vector3 corner2 = center + dir2 * halfRoadWidth - perp2 * halfRoadWidth;
            Vector3 corner3 = center + dir3 * halfRoadWidth - perp3 * halfRoadWidth;
            Vector3 corner4 = center + dir4 * halfRoadWidth - perp4 * halfRoadWidth;

            // Calculate the four edge points
            Vector3 edge1 = center - perp1 * halfRoadWidth;
            Vector3 edge2 = center - perp2 * halfRoadWidth;
            Vector3 edge3 = center - perp3 * halfRoadWidth;
            Vector3 edge4 = center - perp4 * halfRoadWidth;

            // Calculate the four inner pavement points
            Vector3 innerPave1 = center + dir1 * halfTotalWidth - perp1 * halfTotalWidth;
            Vector3 innerPave2 = center + dir2 * halfTotalWidth - perp2 * halfTotalWidth;
            Vector3 innerPave3 = center + dir3 * halfTotalWidth - perp3 * halfTotalWidth;
            Vector3 innerPave4 = center + dir4 * halfTotalWidth - perp4 * halfTotalWidth;

            // Calculate the four outer points
            Vector3 outer1 = center + dir1 * halfTotalWidth + perp1 * halfTotalWidth;
            Vector3 outer2 = center + dir2 * halfTotalWidth + perp2 * halfTotalWidth;
            Vector3 outer3 = center + dir3 * halfTotalWidth + perp3 * halfTotalWidth;
            Vector3 outer4 = center + dir4 * halfTotalWidth + perp4 * halfTotalWidth;

            // Create the four triangles forming the crossroad pavement
            meshData.AddTriangle(edge1, corner1, innerPave1,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge2, corner2, innerPave2,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge3, corner3, innerPave3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(edge4, corner4, innerPave4,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Add the outer triangle
            meshData.AddTriangle(outer1, outer2, outer3,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            meshData.AddTriangle(outer1, outer3, outer4,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }*/

    private Vector3 GetDirectionFromNode(RoadNode node, RoadSegment segment)
    {
        Vector3 direction = (segment.endPos - segment.startPos).normalized;
        return Vector3.Cross(direction, Vector3.up).normalized;
    }

    private List<Vector3> GenerateCurvePoints(Vector3 start, Vector3 end, Vector3 center, int segments)
    {
        List<Vector3> points = new List<Vector3>();

        // Calculate the angle between the two vectors
        Vector3 toStart = start - center;
        Vector3 toEnd = end - center;

        float angle = Vector3.Angle(toStart, toEnd);

        // Calculate the radius of the curve
        float radius = Vector3.Distance(center, start);

        // Calculate the angle of the first point
        float angleStart = Vector3.SignedAngle(toStart, Vector3.right, Vector3.up);

        // Generate points along the curve
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angleAtT = angleStart + t * angle;

            // Calculate the point on the circle
            Vector3 point = center + new Vector3(
                Mathf.Cos(angleAtT * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Sin(angleAtT * Mathf.Deg2Rad) * radius
            );

            points.Add(point);
        }

        return points;
    }
}