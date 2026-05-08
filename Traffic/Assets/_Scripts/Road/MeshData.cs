using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();

    public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
    {
        int startIndex = vertices.Count;

        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);

        uvs.Add(uv0);
        uvs.Add(uv1);
        uvs.Add(uv2);

        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
    }

    public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2[] quadUVs)
    {
        Vector2 uvBottomLeft = quadUVs[0];
        Vector2 uvBottomRight = quadUVs[1];
        Vector2 uvTopRight = quadUVs[2];
        Vector2 uvTopLeft = quadUVs[3];

        AddTriangle(v0, v1, v2, uvBottomLeft, uvBottomRight, uvTopRight);
        AddTriangle(v0, v2, v3, uvBottomLeft, uvTopRight, uvTopLeft);
    }

    public void AddCuboid(Vector3 v0, Vector3 v1, float thickness, RoadType roadType, RoadDirection roadDirection, bool hasCustomUvs = false, Vector2[] customUVs = null)
    {
        Vector3 min = Vector3.Min(v0, v1);
        Vector3 max = Vector3.Max(v0, v1);
        Vector2[] roadUVs, defaultUVs;

        defaultUVs = new[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        if (hasCustomUvs)
            roadUVs = customUVs;
        // Get the correct UVs for this road type
        else if (roadType != RoadType.Empty)
            roadUVs = RoadMarkingUVs.GetUVsForRoadType(roadType, roadDirection);
        else roadUVs = defaultUVs;

        // Bottom quad corners (y = 0)
        Vector3 bbl = new Vector3(min.x, 0, min.z);           // bottom-bottom-left
        Vector3 bbr = new Vector3(max.x, 0, min.z);           // bottom-bottom-right
        Vector3 btr = new Vector3(max.x, 0, max.z);           // bottom-top-right
        Vector3 btl = new Vector3(min.x, 0, max.z);           // bottom-top-left

        // Top quad corners (y = thickness)
        Vector3 tbl = new Vector3(min.x, thickness, min.z);   // top-bottom-left
        Vector3 tbr = new Vector3(max.x, thickness, min.z);   // top-bottom-right
        Vector3 ttr = new Vector3(max.x, thickness, max.z);   // top-top-right
        Vector3 ttl = new Vector3(min.x, thickness, max.z);   // top-top-left

        // Bottom face (y = 0, facing -Y)
        AddQuad(bbl, bbr, btr, btl, defaultUVs);

        // Top face (y = thickness, facing +Y)
        AddQuad(tbl, ttl, ttr, tbr, roadUVs);

        // Front face (z = min.z, facing -Z)
        AddQuad(bbl, tbl, tbr, bbr, defaultUVs);

        // Back face (z = max.z, facing +Z)
        AddQuad(btr, ttr, ttl, btl, defaultUVs);

        // Left face (x = min.x, facing -X)
        AddQuad(btl, ttl, tbl, bbl, defaultUVs);

        // Right face (x = max.x, facing +X)
        AddQuad(bbr, tbr, ttr, btr, defaultUVs);
    }
}