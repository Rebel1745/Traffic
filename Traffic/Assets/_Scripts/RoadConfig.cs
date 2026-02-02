using UnityEngine;

[CreateAssetMenu(fileName = "RoadConfig", menuName = "Road System/Road Config")]
public class RoadConfig : ScriptableObject
{
    [Header("Road Dimensions")]
    public float laneWidth = 3.5f;
    public float pavementWidth = 2.0f;

    [Header("Mesh Quality")]
    [Range(2, 32)]
    public int cornerSegments = 8;

    [Header("Elevation")]
    public float roadHeight = 0.1f; // Height of road surface above ground
    public float pavementHeight = 0.2f; // Height of pavement surface above ground
    public float roadThickness = 0.2f; // Thickness of road material (below ground)
    public float pavementThickness = 0.1f; // Thickness of pavement material (below ground)

    [Header("Snapping")]
    public float nodeSnapDistance = 1.0f;

    [Header("Materials")]
    public Material roadMaterial;
    public Material pavementMaterial;

    public float GetRoadWidth(int lanesPerDirection)
    {
        return laneWidth * lanesPerDirection * 2;
    }

    public float GetTotalWidth(int lanesPerDirection)
    {
        return GetRoadWidth(lanesPerDirection) + (pavementWidth * 2);
    }

    public float GetHalfRoadWidth(int lanesPerDirection)
    {
        return GetRoadWidth(lanesPerDirection) * 0.5f;
    }

    public float GetHalfTotalWidth(int lanesPerDirection)
    {
        return GetTotalWidth(lanesPerDirection) * 0.5f;
    }
}