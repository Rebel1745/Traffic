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
            agent.AddGoal(new WalkToWaypointGoal(randomNode, goalName));
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

        agent.InterruptAndAddGoal(new WalkToWaypointGoal(homeNode, goalName));
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
        // get home building
        EntityId buildingId = GetHomeBuilding(agent.Id);
        if (!buildingId.IsValid) Debug.LogError("Home building not found");

        BuildingBase bb = BuildingManager.Instance.GetBuilding(buildingId);

        WaypointNode frontDoorWaypoint = null;

        if (bb is BuildingHouse house)
            frontDoorWaypoint = house.DoorWaypoint;
        else Debug.LogError("Building is not a house");

        // first, do we have a car?
        EntityId vehicleId = GetPersonsVehicle(agent.Id);

        if (!vehicleId.IsValid) Debug.LogError("Owned vehicle not found");

        AgentController vac = VehicleManager.Instance.GetVehicle(vehicleId);

        if (vac == null) Debug.LogError("No agent found for vehicle");

        // what is the cars home parking spot?
        EntityId homeSpotId = VehicleManager.Instance.GetVehiclesHomeSpotId(vehicleId);

        if (!homeSpotId.IsValid) Debug.LogError("HomeSpot not found");

        WaypointNode homeNode = RoadWaypointManager.Instance.GetWaypointFromId(homeSpotId);

        // is the car in its spot?
        EntityId currentSpotId = VehicleManager.Instance.GetVehiclesCurrentSpotId(vehicleId);

        if (!currentSpotId.IsValid) Debug.LogError("CurrentSpot not found");

        if (!homeSpotId.Equals(currentSpotId)) Debug.LogError("HomeSpot is not the same as CurrentSpot");

        // what is the waypoint that connects to the cars spot?
        EntityId alightId = GetAlightWaypointId(homeSpotId);

        if (!alightId.IsValid) Debug.LogError("Alight waypoint not valid");

        WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightId);

        if (alightWaypoint == null)
        {
            Debug.LogWarning("Alight waypoint not found");
            return;
        }

        agent.AddGoal(new WalkToWaypointGoal(alightWaypoint, "Walking to the alight waypoint"));
        agent.AddGoal(new EnterVehicleGoal(alightWaypoint, vac, "Entering vehicle"));
        agent.AddGoal(new DriveToWaypointGoal(vac, VehicleManager.Instance.GetRandomVehicleWaypoint(WaypointType.Entry), "Driving to random waypoint"));
        agent.AddGoal(new DriveToWaypointGoal(vac, homeNode, "Driving home"));
        agent.AddGoal(new ExitVehicleGoal(vac, "Exiting vehicle"));
        agent.AddGoal(new WalkToWaypointGoal(frontDoorWaypoint, "Walking to front door"));
    }

    public void DriveToPetrolStationAndHome(AgentController agent)
    {
        // get home building
        EntityId buildingId = GetHomeBuilding(agent.Id);
        if (!buildingId.IsValid) Debug.LogError("Home building not found");

        BuildingBase bb = BuildingManager.Instance.GetBuilding(buildingId);

        WaypointNode frontDoorWaypoint = null;

        if (bb is BuildingHouse house)
            frontDoorWaypoint = house.DoorWaypoint;
        else Debug.LogError("Building is not a house");

        // first, do we have a car?
        EntityId vehicleId = GetPersonsVehicle(agent.Id);

        if (!vehicleId.IsValid) Debug.LogError("Owned vehicle not found");

        AgentController vac = VehicleManager.Instance.GetVehicle(vehicleId);

        if (vac == null) Debug.LogError("No agent found for vehicle");

        // what is the cars home parking spot?
        EntityId homeSpotId = VehicleManager.Instance.GetVehiclesHomeSpotId(vehicleId);

        if (!homeSpotId.IsValid) Debug.LogError("HomeSpot not found");

        WaypointNode homeNode = RoadWaypointManager.Instance.GetWaypointFromId(homeSpotId);

        // is the car in its spot?
        EntityId currentSpotId = VehicleManager.Instance.GetVehiclesCurrentSpotId(vehicleId);

        if (!currentSpotId.IsValid) Debug.LogError("CurrentSpot not found");

        if (!homeSpotId.Equals(currentSpotId)) Debug.LogError("HomeSpot is not the same as CurrentSpot");

        // what is the waypoint that connects to the cars spot?
        EntityId alightId = GetAlightWaypointId(homeSpotId);

        if (!alightId.IsValid) Debug.LogError("Alight waypoint not valid");

        WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightId);

        if (alightWaypoint == null)
        {
            Debug.LogWarning("Alight waypoint not found");
            return;
        }

        List<EntityId> petrolStations = BuildingManager.Instance.GetBuildingsByType(BuildingType.PetrolStation);

        if (petrolStations.Count == 0) Debug.LogError("No petrol stations found");

        EntityId closestPetrolStation = BuildingManager.Instance.GetClosestBuildingToPosition(petrolStations, agent.transform.position);
        BuildingPetrolStation bp = BuildingManager.Instance.GetBuilding(closestPetrolStation) as BuildingPetrolStation;

        if (bp == null) Debug.LogError("The building does not have the petrol station script on it");

        agent.AddGoal(new WalkToWaypointGoal(alightWaypoint, "Walking to the alight waypoint"));
        agent.AddGoal(new EnterVehicleGoal(alightWaypoint, vac, "Entering vehicle"));
        agent.AddGoal(new DriveToWaypointGoal(vac, bp.PropertyEntryNode, "Driving to petrol station entrance"));
        agent.AddGoal(new DriveToAssignedPumpGoal(vac, bp, "Driving to next available pump"));
        agent.AddGoal(new WaitGoal(2f));
        agent.AddGoal(new DriveToWaypointGoal(vac, homeNode, "Driving home"));
        agent.AddGoal(new ExitVehicleGoal(vac, "Exiting vehicle"));
        agent.AddGoal(new WalkToWaypointGoal(frontDoorWaypoint, "Walking to front door"));
    }

    public void DriveToCarParkAndWalkAround(AgentController agent)
    {
        // get home building
        EntityId buildingId = GetHomeBuilding(agent.Id);
        if (!buildingId.IsValid) Debug.LogError("Home building not found");

        BuildingBase bb = BuildingManager.Instance.GetBuilding(buildingId);

        WaypointNode frontDoorWaypoint = null;

        if (bb is BuildingHouse house)
            frontDoorWaypoint = house.DoorWaypoint;
        else Debug.LogError("Building is not a house");

        // first, do we have a car?
        EntityId vehicleId = GetPersonsVehicle(agent.Id);

        if (!vehicleId.IsValid) Debug.LogError("Owned vehicle not found");

        AgentController vac = VehicleManager.Instance.GetVehicle(vehicleId);

        if (vac == null) Debug.LogError("No agent found for vehicle");

        // what is the cars home parking spot?
        EntityId homeSpotId = VehicleManager.Instance.GetVehiclesHomeSpotId(vehicleId);

        if (!homeSpotId.IsValid) Debug.LogError("HomeSpot not found");

        WaypointNode homeNode = RoadWaypointManager.Instance.GetWaypointFromId(homeSpotId);

        // is the car in its spot?
        EntityId currentSpotId = VehicleManager.Instance.GetVehiclesCurrentSpotId(vehicleId);

        if (!currentSpotId.IsValid) Debug.LogError("CurrentSpot not found");

        if (!homeSpotId.Equals(currentSpotId)) Debug.LogError("HomeSpot is not the same as CurrentSpot");

        // what is the waypoint that connects to the cars spot?
        EntityId alightId = GetAlightWaypointId(homeSpotId);

        if (!alightId.IsValid) Debug.LogError("Alight waypoint not valid");

        WaypointNode alightWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(alightId);

        if (alightWaypoint == null)
        {
            Debug.LogWarning("Alight waypoint not found");
            return;
        }

        List<EntityId> carParks = BuildingManager.Instance.GetBuildingsByType(BuildingType.CarPark);

        if (carParks.Count == 0) Debug.LogError("No car parks found");

        EntityId closestCarPark = BuildingManager.Instance.GetClosestBuildingToPosition(carParks, agent.transform.position);
        BuildingCarPark cp = BuildingManager.Instance.GetBuilding(closestCarPark) as BuildingCarPark;

        if (cp == null) Debug.LogError("The building does not have the petrol station script on it");

        WaypointNode randomWaypoint = GetRandomWaypoint(agent, WaypointType.PedestrianWalkway);

        agent.AddGoal(new WalkToWaypointGoal(alightWaypoint, "Walking to the alight waypoint"));
        agent.AddGoal(new EnterVehicleGoal(alightWaypoint, vac, "Entering vehicle"));
        agent.AddGoal(new DriveToWaypointGoal(vac, cp.PropertyEntryNode, "Driving to petrol station entrance"));
        agent.AddGoal(new DriveToCarParkGoal(vac, cp, "Driving to car park"));
        agent.AddGoal(new WaitGoal(2f));
        agent.AddGoal(new ExitVehicleGoal(vac, "Exiting vehicle"));
        agent.AddGoal(new WalkToWaypointGoal(randomWaypoint, "Walking to random place"));
    }

    public EntityId GetPersonsVehicle(EntityId personId)
        => RelationshipManager.Instance.GetVehicles(personId).FirstOrDefault();

    public EntityId GetAlightWaypointId(EntityId homeParkingSpotId)
        => RelationshipManager.Instance.GetAlight(homeParkingSpotId).FirstOrDefault();

    public EntityId GetHomeBuilding(EntityId personId)
        => RelationshipManager.Instance.GetHomeBuildings(personId).FirstOrDefault();

    public void ReParentPedestrian(AgentController ac)
    {
        ac.transform.SetParent(transform);
    }

    #endregion
}
