using System.Collections.Generic;
using UnityEngine;

public class RoadVisualiser : MonoBehaviour
{
    [Header("Visualisation Settings")]
    public Material LineMaterial;
    public Color ForwardLaneColor = Color.green;
    public Color BackwardLaneColor = Color.blue;
    public Color IntersectionColor = Color.yellow;
    public float LaneLineWidth = 0.3f;
    public float IntersectionSize = 1f;

    private RoadGraph roadGraph;
    private GameObject visualisationContainer;

    void Awake()
    {
        roadGraph = GetComponent<RoadGraph>();
        if (roadGraph == null)
        {
            Debug.LogError("RoadGraph component not found!");
            return;
        }
    }

    public void VisualiseAll()
    {
        ClearVisualisation();

        visualisationContainer = new GameObject("RoadVisualisation");
        visualisationContainer.transform.parent = transform;

        // Visualise all roads
        foreach (Road road in roadGraph.Roads)
        {
            VisualiseRoad(road);
        }

        // Visualise all intersections
        foreach (Intersection intersection in roadGraph.Intersections)
        {
            VisualiseIntersection(intersection);
        }
    }

    void VisualiseRoad(Road road)
    {
        GameObject roadContainer = new GameObject($"Road_{road.RoadID}");
        roadContainer.transform.parent = visualisationContainer.transform;

        // Visualise forward lanes (A to B)
        VisualiseLanes(road.LanesAtoB, ForwardLaneColor, roadContainer.transform);

        // Visualise backward lanes (B to A)
        VisualiseLanes(road.LanesBtoA, BackwardLaneColor, roadContainer.transform);
    }

    void VisualiseLanes(List<Lane> lanes, Color color, Transform parent)
    {
        foreach (Lane lane in lanes)
        {
            GameObject laneObj = new GameObject($"Lane_{lane.LaneID}");
            laneObj.transform.parent = parent;

            LineRenderer lineRenderer = laneObj.AddComponent<LineRenderer>();

            // Configure LineRenderer
            if (LineMaterial != null)
            {
                lineRenderer.material = LineMaterial;
            }
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = LaneLineWidth;
            lineRenderer.endWidth = LaneLineWidth;
            lineRenderer.positionCount = lane.Waypoints.Count;
            lineRenderer.useWorldSpace = true;

            // Set waypoint positions
            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                lineRenderer.SetPosition(i, lane.Waypoints[i]);
            }
        }
    }

    void VisualiseIntersection(Intersection intersection)
    {
        GameObject intersectionObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        intersectionObj.name = $"Intersection_{intersection.IntersectionID}";
        intersectionObj.transform.parent = visualisationContainer.transform;
        intersectionObj.transform.position = intersection.Position;
        intersectionObj.transform.localScale = new Vector3(IntersectionSize, 0.1f, IntersectionSize);

        Renderer renderer = intersectionObj.GetComponent<Renderer>();
        renderer.material.color = IntersectionColor;
    }

    public void ClearVisualisation()
    {
        if (visualisationContainer != null)
        {
            DestroyImmediate(visualisationContainer);
        }
    }

    // Optional: Draw gizmos in scene view
    void OnDrawGizmos()
    {
        if (roadGraph == null || roadGraph.Intersections.Count == 0) return;

        // Draw intersections
        Gizmos.color = IntersectionColor;
        foreach (Intersection intersection in roadGraph.Intersections)
        {
            Gizmos.DrawWireSphere(intersection.Position, IntersectionSize * 0.5f);
        }
    }
}