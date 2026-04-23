using System.Collections.Generic;
using UnityEngine;

public class TrafficLightPlacementHandler : MonoBehaviour, IPlacementHandler
{
    [Header("Traffic Light Preview")]
    private GameObject _trafficLightPrefab;

    private List<GameObject> _previewLights = new List<GameObject>();
    private GridCell _lastPreviewCell = null;

    private void Start()
    {
        _trafficLightPrefab = TrafficLightManager.Instance.TrafficLightPrefab;
    }

    public void OnEnter() { }

    public void OnExit()
    {
        DisableTrafficLightVisuals();
    }

    public void OnUpdate()
    {
        UpdateTrafficLightPreview();
    }

    public void OnLeftClickPressed(Vector3 hitPoint) { /* drag start logic */ }

    public void OnLeftClickReleased(Vector3 hitPoint)
    {
        HandleTrafficLightPlacement();
    }

    public void OnRightClickPressed(Vector3 hitPoint)
    {
        HandleTrafficLightRemoval();
    }

    public void OnMouseMoved(Vector3 hitPoint) { /* preview line logic */ }

    private void UpdateTrafficLightPreview()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();

        if (!GridManager.Instance.IsValidGridPosition(gridPos))
        {
            ClearPreviewLights();
            _lastPreviewCell = null;
            return;
        }

        GridCell cell = GridManager.Instance.GetCell(gridPos);

        // Only show preview on road cells without traffic lights
        if (cell == null || cell.CellType != CellType.Road || cell.HasTrafficLights)
        {
            ClearPreviewLights();
            _lastPreviewCell = null;
            return;
        }

        // Only regenerate preview if we've moved to a different cell
        if (cell == _lastPreviewCell)
            return;

        _lastPreviewCell = cell;
        ClearPreviewLights();

        // Get valid TrafficLightLocation waypoints for the current substate
        List<WaypointNode> validWaypoints = GetValidWaypointsForSubState(cell);

        // Spawn a preview light at each valid waypoint
        foreach (WaypointNode waypoint in validWaypoints)
        {
            // Skip waypoints that already have a confirmed light
            if (waypoint.AssignedLight != null)
                continue;

            GameObject preview = Instantiate(_trafficLightPrefab, waypoint.Position, Quaternion.identity);

            // Make preview semi-transparent to distinguish from confirmed lights
            SetPreviewTransparency(preview, 0.5f);

            _previewLights.Add(preview);
        }
    }

    private void ClearPreviewLights()
    {
        foreach (GameObject preview in _previewLights)
        {
            if (preview != null)
                Destroy(preview);
        }
        _previewLights.Clear();
    }

    private void DisableTrafficLightVisuals()
    {
        ClearPreviewLights();
        _lastPreviewCell = null;
    }

    private void HandleTrafficLightPlacement()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();
        if (!GridManager.Instance.IsValidGridPosition(gridPos))
            return;

        GridCell cell = GridManager.Instance.GetCell(gridPos);
        if (cell == null || cell.CellType != CellType.Road)
            return;

        List<WaypointNode> validWaypoints = GetValidWaypointsForSubState(cell);
        if (validWaypoints.Count == 0)
            return;

        WaypointNode lastWaypoint = null;

        // Confirm all previewed lights for this cell
        foreach (WaypointNode waypoint in validWaypoints)
        {
            if (waypoint.AssignedLight != null)
                continue;

            lastWaypoint = waypoint;

            if (!cell.HasTrafficLights)
                TrafficLightManager.Instance.PlaceLightAtWaypoint(waypoint);
        }

        // if there are already traffic lights, load the settings screen then bail
        if (cell.HasTrafficLights)
        {
            UIManager.Instance.LoadTrafficLightGroupDetails(TrafficLightManager.Instance.FindGroupForWaypoint(lastWaypoint));
            return;
        }

        cell.HasTrafficLights = true;

        // hand off to the traffic light settings UI to allow for light timings and order to be changed
        UIManager.Instance.LoadTrafficLightGroupDetails(TrafficLightManager.Instance.FindGroupForWaypoint(lastWaypoint));

        // Refresh preview for the same cell (to hide confirmed previews)
        _lastPreviewCell = null;
    }

    private void HandleTrafficLightRemoval()
    {
        Vector3Int gridPos = GridManager.Instance.GetGridPositionFromMouse();
        if (!GridManager.Instance.IsValidGridPosition(gridPos))
            return;

        GridCell cell = GridManager.Instance.GetCell(gridPos);
        if (cell == null || cell.CellType != CellType.Road || !cell.HasTrafficLights)
            return;

        // Remove all confirmed lights on this cell
        TrafficLightManager.Instance.RemoveTrafficLightGroupFromCell(cell);

        // Refresh preview
        _lastPreviewCell = null;
    }

    private List<WaypointNode> GetValidWaypointsForSubState(GridCell cell)
    {
        TrafficLightSubState subState = SimulationManager.Instance.CurrentState.TrafficLightSubState;

        // Filter waypoints by type and substate
        return RoadWaypointManager.Instance.GetCellWaypoints(cell).FindAll(w =>
        {
            if (w.Type != WaypointType.TrafficLightLocation)
                return false;

            return subState switch
            {
                TrafficLightSubState.AddJunctionLights =>
                    cell.RoadType == RoadType.TJunction || cell.RoadType == RoadType.Crossroads,
                TrafficLightSubState.AddPedestrianCrossings =>
                    cell.RoadType == RoadType.Straight,
                _ => false
            };
        });
    }

    private void SetPreviewTransparency(GameObject previewObj, float alpha)
    {
        foreach (Renderer renderer in previewObj.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in renderer.materials)
            {
                // Enable transparency on the material
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                Color colour = mat.color;
                colour.a = alpha;
                mat.color = colour;
            }
        }
    }
}
