using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();

    public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        int startIndex = vertices.Count;

        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        uvs.Add(uv0);
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);

        // First triangle
        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);

        // Second triangle
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
    }

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

    // Add method to create a 3D rectangle with thickness
    public void AddRect(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, float thickness)
    {
        int startIndex = vertices.Count;

        // Top face (positive thickness)
        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        // Bottom face (negative thickness)
        vertices.Add(v0 - Vector3.up * thickness);
        vertices.Add(v1 - Vector3.up * thickness);
        vertices.Add(v2 - Vector3.up * thickness);
        vertices.Add(v3 - Vector3.up * thickness);

        // UVs for top face
        uvs.Add(uv0);
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);

        // UVs for bottom face
        uvs.Add(uv0);
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);

        // Top face triangles
        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);

        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);

        // Bottom face triangles
        triangles.Add(startIndex + 4);
        triangles.Add(startIndex + 5);
        triangles.Add(startIndex + 6);

        triangles.Add(startIndex + 4);
        triangles.Add(startIndex + 6);
        triangles.Add(startIndex + 7);

        // Side faces
        // Front
        triangles.Add(startIndex);
        triangles.Add(startIndex + 4);
        triangles.Add(startIndex + 1);

        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 4);
        triangles.Add(startIndex + 5);

        // Right
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 5);
        triangles.Add(startIndex + 2);

        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 5);
        triangles.Add(startIndex + 6);

        // Back
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 6);
        triangles.Add(startIndex + 3);

        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 6);
        triangles.Add(startIndex + 7);

        // Left
        triangles.Add(startIndex);
        triangles.Add(startIndex + 4);
        triangles.Add(startIndex + 3);

        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 4);
        triangles.Add(startIndex + 7);
    }
}