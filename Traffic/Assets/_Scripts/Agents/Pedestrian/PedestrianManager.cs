using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianManager : MonoBehaviour
{
    public static PedestrianManager Instance { get; private set; }

    [SerializeField] private GameObject[] _pedestrianPrefabs;

    private Dictionary<EntityId, AgentController> _allPedestrians = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to input events
        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        InputManager.OnLeftClickPressed -= HandleLeftClickPressed;
    }

    private void HandleLeftClickPressed(Vector2 screenPosition)
    {
        // Only spawn Pedestrians when simulation is running
        if (SimulationManager.Instance.CurrentState.SimulationState != SimulationState.Pedestrians)
            return;

        //AddAndRegisterPerson();

        // Get a random valid spawn location
        // WaypointNode startWaypoint = GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway);
        // //WaypointNode startWaypoint = GetRandomPedestrianWaypoint(WaypointType.InsideBuilding);
        // if (startWaypoint == null)
        // {
        //     Debug.LogError("No valid spawn location found!");
        //     return;
        // }

        // // Get a random valid target
        // WaypointNode targetWaypoint = FindValidTarget(startWaypoint);
        // if (targetWaypoint == null)
        // {
        //     Debug.LogError("No valid target found for spawn location!");
        //     return;
        // }

        // PedestrianSpawner.Instance.SpawnPedestrian(startWaypoint, targetWaypoint);
    }

    private AgentController AddAndRegisterPerson()
    {
        WaypointNode randomWaypoint = GetRandomPedestrianWaypoint(WaypointType.None);
        return AddAndRegisterPerson(randomWaypoint, randomWaypoint.Position);
    }

    public AgentController AddAndRegisterPerson(WaypointNode spawnWaypoint, Vector3 spawnPosition)
    {
        // 1. Generate the ID
        EntityId newId = EntityId.New();

        // 2. Instantiate the GameObject
        // You can use Object.Instantiate with a prefab
        GameObject pedestrianPrefab = _pedestrianPrefabs[Random.Range(0, _pedestrianPrefabs.Length)];
        Vector3 spawnLocation = Utils.GetVectorWithSetHeight(spawnPosition, 0.2f);
        //Vector3 lookDirection = (Utils.GetVectorWithSetHeight(Camera.main.transform.position, 0.2f) - spawnLocation).normalized;
        GameObject pedestrian = Instantiate(pedestrianPrefab, spawnLocation, Quaternion.identity, transform);
        Vector3 lookDirection = Vector3.back;
        pedestrian.transform.rotation = Quaternion.LookRotation(lookDirection);
        AgentController pc = pedestrian.GetComponent<AgentController>();

        // 3. Assign the ID to the controller
        pc.Initialise(AgentType.Person, newId, spawnWaypoint);

        // 4. Register in the dictionary
        _allPedestrians[newId] = pc;

        // 5. Hook into the Destroy event to auto-cleanup
        // (See Step C below)

        return pc;
    }

    public WaypointNode FindValidTarget(WaypointNode startWaypoint, WaypointType type = WaypointType.None, int maxAttempts = 10)
    {
        // Try to find a valid target
        for (int i = 0; i < maxAttempts; i++)
        {
            WaypointNode candidate = GetRandomPedestrianWaypoint(type);
            if (candidate == startWaypoint) continue;

            // Check if path exists
            List<WaypointNode> path = AStarPathfinder.FindPath(startWaypoint, candidate);
            if (path != null && path.Count > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    public WaypointNode GetRandomPedestrianWaypoint(WaypointType type)
    {
        List<WaypointNode> allWaypoints = PedestrianWaypointManager.Instance.GetAllWaypoints();
        List<WaypointNode> specificNodes = allWaypoints;

        if (type != WaypointType.None)
            specificNodes = allWaypoints.Where(w => w.Type == type).ToList();

        if (specificNodes.Count == 0)
        {
            Debug.LogError($"No waypoints of type {type} found");
            return null;
        }

        return specificNodes[Random.Range(0, specificNodes.Count)];
    }

    #region Goals and Target Stuff
    public void GoToRandomWaypoint(AgentController agent)
    {
        WaypointNode randomNode = GetRandomWaypoint(agent, WaypointType.PedestrianWalkway);
        string goalName = "Walk to random node at " + randomNode.Position;

        if (randomNode != null)
            agent.AddGoal(new WalkToRandomGoal(randomNode, goalName));
        else Debug.LogError("No random location found");
    }

    public WaypointNode GetRandomWaypoint(AgentController agent, WaypointType type)
    {
        PedestrianMovement pm = agent.GetComponent<PedestrianMovement>();

        return FindValidTarget(pm.CurrentWaypoint, type: WaypointType.PedestrianWalkway);
    }

    public void GoHome(AgentController agent)
    {
        WaypointNode homeNode = GetHomeWaypoint(agent);

        string goalName = "Walked home to " + homeNode.Position;

        agent.InterruptAndAddGoal(new WalkHomeGoal(homeNode, goalName));
    }

    public WaypointNode GetHomeWaypoint(AgentController agent)
    {
        PedestrianMovement pm = agent.GetComponent<PedestrianMovement>();
        EntityId buildingId = RelationshipManager.Instance.GetHomeBuildings(agent.Id).FirstOrDefault();

        if (!buildingId.IsValid) Debug.LogError("Home building not found");

        BuildingBase bc = BuildingManager.Instance.GetBuilding(buildingId);

        if (bc == null) Debug.LogError("Building Controller not found");

        WaypointNode homeNode = null;

        if (bc is BuildingHouse house)
            homeNode = house.DoorWaypoint;
        else Debug.LogError("This building is not a house");

        if (homeNode == null) Debug.LogError("Inside building node not found");

        List<WaypointNode> newPath = AStarPathfinder.FindPath(pm.CurrentWaypoint, homeNode);

        if (newPath == null || newPath.Count == 0) Debug.LogError("Path to home node not found");

        return homeNode;
    }

    public void GoOnADriveAndComeHome(AgentController agent)
    {
        // first, do we have a car?
        EntityId vehicleId = GetPersonsVehicle(agent.Id);

        if (!vehicleId.IsValid) Debug.LogError("Owned vehicle not found");

        AgentController ac = VehicleManager.Instance.GetVehicle(vehicleId);

        if (ac == null) Debug.LogError("No agent found for vehicle");

        VehicleMovement vm = ac.GetComponent<VehicleMovement>();

        // what is the cars home parking spot?
        EntityId homeSpotId = VehicleManager.Instance.GetVehiclesHomeSpotId(vehicleId);

        if (!homeSpotId.IsValid) Debug.LogError("HomeSpot not found");

        // is the car in its spot?
        EntityId currentSpotId = VehicleManager.Instance.GetVehiclesCurrentSpotId(vehicleId);

        if (!currentSpotId.IsValid) Debug.LogError("CurrentSpot not found");

        if (!homeSpotId.Equals(currentSpotId)) Debug.LogError("HomeSpot is not the same as CurrentSpot");

        // what is the waypoint that connects to the cars spot?
        EntityId alightId = GetAlightWaypointId(homeSpotId);

        if (!alightId.IsValid) Debug.LogError("Alight waypoint not valid");

        WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightId);

        string goalName = "Walking to the alight waypoint";

        if (alightWaypoint != null)
            agent.AddGoal(new GFAD_WalkToAlightGoal(alightWaypoint, goalName));
        else Debug.LogWarning("Alight waypoint not found");
    }

    public EntityId GetPersonsVehicle(EntityId personId)
        => RelationshipManager.Instance.GetVehicles(personId).FirstOrDefault();

    public EntityId GetAlightWaypointId(EntityId homeParkingSpotId)
        => RelationshipManager.Instance.GetAlight(homeParkingSpotId).FirstOrDefault();

    public void ReParentPedestrian(AgentController ac)
    {
        ac.transform.SetParent(transform);
    }

    #endregion
}
