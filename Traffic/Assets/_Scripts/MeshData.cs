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

    public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector2 uv0 = new Vector2(0, 0);
        Vector2 uv1 = new Vector2(1, 0);
        Vector2 uv2 = new Vector2(1, 1);
        Vector2 uv3 = new Vector2(0, 1);

        AddTriangle(v0, v1, v2, uv0, uv1, uv2);
        AddTriangle(v0, v2, v3, uv0, uv2, uv3);
    }

    public void AddCuboid(Vector3 v0, Vector3 v1, float thickness)
    {
        Vector3 min = Vector3.Min(v0, v1);
        Vector3 max = Vector3.Max(v0, v1);

        // Front face corners (z = 0)
        Vector3 fbl = new Vector3(min.x, min.y, 0);
        Vector3 fbr = new Vector3(max.x, min.y, 0);
        Vector3 ftr = new Vector3(max.x, max.y, 0);
        Vector3 ftl = new Vector3(min.x, max.y, 0);

        // Back face corners (z = thickness)
        Vector3 bbl = new Vector3(min.x, min.y, thickness);
        Vector3 bbr = new Vector3(max.x, min.y, thickness);
        Vector3 btr = new Vector3(max.x, max.y, thickness);
        Vector3 btl = new Vector3(min.x, max.y, thickness);

        // Front face (facing -Z)
        AddQuad(ftl, ftr, fbr, fbl);

        // Back face (facing +Z)
        AddQuad(btl, bbl, bbr, btr);

        // Bottom face (facing -Y)
        AddQuad(fbl, fbr, bbr, bbl);

        // Top face (facing +Y)
        AddQuad(btl, btr, ftr, ftl);

        // Left face (facing -X)
        AddQuad(ftl, fbl, bbl, btl);

        // Right face (facing +X)
        AddQuad(fbr, ftr, btr, bbr);
    }

    public void AddTrapezium(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // trapezium is made up of 2 triangles (like a quad)
        // Assuming v0, v1, v2, v3 are in order (counter-clockwise)
        Vector2 uv0 = new Vector2(0, 0);
        Vector2 uv1 = new Vector2(1, 0);
        Vector2 uv2 = new Vector2(1, 1);
        Vector2 uv3 = new Vector2(0, 1);

        // 1st triangle
        AddTriangle(v0, v1, v2, uv0, uv1, uv2);

        // 2nd triangle
        AddTriangle(v0, v2, v3, uv0, uv2, uv3);
    }

    public void AddTriangularPrism(Vector3 v0, Vector3 v1, Vector3 v2, float thickness)
    {
        // Top triangle vertices (y = thickness)
        Vector3 t0 = new Vector3(v0.x, v0.y + thickness, v0.z);
        Vector3 t1 = new Vector3(v1.x, v1.y + thickness, v1.z);
        Vector3 t2 = new Vector3(v2.x, v2.y + thickness, v2.z);

        // Bottom triangle vertices (y = 0)
        Vector3 b0 = new Vector3(v0.x, v0.y, v0.z);
        Vector3 b1 = new Vector3(v1.x, v1.y, v1.z);
        Vector3 b2 = new Vector3(v2.x, v2.y, v2.z);

        Vector2 uv0 = new Vector2(0, 0);
        Vector2 uv1 = new Vector2(1, 0);
        Vector2 uv2 = new Vector2(0.5f, 1);

        // Top triangle (facing +Y): CCW when viewed from outside (above)
        AddTriangle(t0, t1, t2, uv0, uv1, uv2); // CCW from above → correct

        // Bottom triangle (facing -Y): CCW when viewed from outside (below)
        AddTriangle(b0, b2, b1, uv0, uv2, uv1); // CCW from below → correct

        // 1st quad (connecting v0-v1 edge)
        AddQuad(b0, b1, t1, t0);

        // 2nd quad (connecting v1-v2 edge)
        AddQuad(b1, b2, t2, t1);

        // 3rd quad (connecting v2-v0 edge)
        AddQuad(b2, b0, t0, t2);
    }

    public void AddTrapezoidalPrism(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float thickness)
    {
        // Top trapezium vertices (y = thickness)
        Vector3 t0 = new Vector3(v0.x, v0.y + thickness, v0.z);
        Vector3 t1 = new Vector3(v1.x, v1.y + thickness, v1.z);
        Vector3 t2 = new Vector3(v2.x, v2.y + thickness, v2.z);
        Vector3 t3 = new Vector3(v3.x, v3.y + thickness, v3.z);

        // Bottom trapezium vertices (y = 0)
        Vector3 b0 = new Vector3(v0.x, v0.y, v0.z);
        Vector3 b1 = new Vector3(v1.x, v1.y, v1.z);
        Vector3 b2 = new Vector3(v2.x, v2.y, v2.z);
        Vector3 b3 = new Vector3(v3.x, v3.y, v3.z);

        Vector2 uv0 = new Vector2(0, 0);
        Vector2 uv1 = new Vector2(1, 0);
        Vector2 uv2 = new Vector2(1, 1);
        Vector2 uv3 = new Vector2(0, 1);

        // Top trapezium (facing +Y): CCW when viewed from above
        AddTriangle(t0, t1, t2, uv0, uv1, uv2);
        AddTriangle(t0, t2, t3, uv0, uv2, uv3);

        // Bottom trapezium (facing -Y): CCW when viewed from below
        AddTriangle(b0, b3, b2, uv0, uv3, uv2); // CCW from below → reverse order
        AddTriangle(b0, b2, b1, uv0, uv2, uv1); // CCW from below → reverse order

        // 1st quad (connecting v0-v1 edge)
        AddQuad(b0, b1, t1, t0);

        // 2nd quad (connecting v1-v2 edge)
        AddQuad(b1, b2, t2, t1);

        // 3rd quad (connecting v2-v3 edge)
        AddQuad(b2, b3, t3, t2);

        // 4th quad (connecting v3-v0 edge)
        AddQuad(b3, b0, t0, t3);
    }
}