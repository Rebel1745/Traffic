using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PedestrianManager : MonoBehaviour, ISaveable
{
    public static PedestrianManager Instance { get; private set; }

    public string SaveKey => "Pedestrians";

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

    private void Start()
    {
        SaveManager.Instance.RegisterSaveable(this);
        InputManager.OnLeftClickPressed += HandleLeftClickPressed;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.UnregisterSaveable(this);
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

    public AgentController AddAndRegisterPerson(EntityId id, WaypointNode spawnWaypoint, Vector3 spawnPosition, WaypointNode targetWaypoint)
    {
        // 1. Generate the ID
        if (id.Equals(EntityId.None))
            id = EntityId.New();

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
        pc.Initialise(AgentType.Person, id, spawnWaypoint, targetWaypoint);

        // 4. Register in the dictionary
        _allPedestrians[id] = pc;

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
        WaypointNode randomNode = GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway);
        if (randomNode == null)
        {
            Debug.LogError($"No random location found {PedestrianWaypointManager.Instance.GetAllWaypoints().Count}");
            return;
        }

        string goalName = "Walk to random node at " + randomNode.Position;

        agent.AddGoal(new WalkToWaypointGoal(randomNode, goalName));
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
        EntityId buildingId = RelationshipManager.Instance.GetHomeBuildingsForPerson(agent.Id).FirstOrDefault();

        if (!buildingId.IsValid) Debug.LogError("Home building not found");

        BuildingBase bc = BuildingManager.Instance.GetBuilding(buildingId);

        if (bc == null) Debug.LogError("Building Controller not found");

        WaypointNode homeNode = null;

        if (bc is BuildingHouse house)
            homeNode = house.DoorWaypoint;
        else Debug.LogError("This building is not a house");

        if (homeNode == null) Debug.LogError("Inside building node not found");

        return homeNode;
    }

    public void GoToStore(AgentController agent)
    {
        EntityId id = BuildingManager.Instance.GetBuildingsByType(BuildingSubState.Restaurant).First();
        if (!id.IsValid)
        {
            Debug.Log("Store id not found");
            return;
        }

        BuildingStoreRoadside bb = BuildingManager.Instance.GetBuilding(id) as BuildingStoreRoadside;
        if (bb == null)
        {
            Debug.Log("Store not found");
            return;
        }

        agent.AddGoal(new WalkToWaypointGoal(bb.InsideBuildingWaypoint));
    }

    public void GoOnADriveAndComeHome(AgentController agent)
    {
        agent.AddGoal(new WalkToAndEnterVehicleGoal());
        agent.AddGoal(new DriveToWaypointGoal(VehicleManager.Instance.GetRandomVehicleWaypoint(WaypointType.Entry), "Driving to random waypoint"));
        agent.AddGoal(new DriveHomeGoal());
        agent.AddGoal(new ExitVehicleGoal());
        agent.AddGoal(new WalkToFrontDoorGoal());
    }

    public void DriveToPetrolStationAndHome(AgentController agent)
    {
        List<EntityId> petrolStations = BuildingManager.Instance.GetBuildingsByType(BuildingSubState.PetrolStation);

        if (petrolStations.Count == 0) Debug.LogError("No petrol stations found");

        EntityId closestPetrolStation = BuildingManager.Instance.GetClosestBuildingToPosition(petrolStations, agent.transform.position);
        BuildingPetrolStation bp = BuildingManager.Instance.GetBuilding(closestPetrolStation) as BuildingPetrolStation;

        if (bp == null) Debug.LogError("The building does not have the petrol station script on it");

        agent.AddGoal(new WalkToAndEnterVehicleGoal());
        agent.AddGoal(new DriveToWaypointGoal(bp.PropertyEntryNode, "Driving to petrol station entrance"));
        agent.AddGoal(new DriveToAssignedPumpGoal(bp, "Driving to next available pump"));
        agent.AddGoal(new WaitGoal(2f));
        agent.AddGoal(new DriveHomeGoal());
        agent.AddGoal(new ExitVehicleGoal());
        agent.AddGoal(new WalkToFrontDoorGoal());
    }

    public void DriveToCarParkAndWalkAround(AgentController agent)
    {
        List<EntityId> carParks = BuildingManager.Instance.GetBuildingsByType(BuildingSubState.CarPark);

        if (carParks.Count == 0) Debug.LogError("No car parks found");

        EntityId closestCarPark = BuildingManager.Instance.GetClosestBuildingToPosition(carParks, agent.transform.position);
        BuildingCarPark cp = BuildingManager.Instance.GetBuilding(closestCarPark) as BuildingCarPark;

        if (cp == null) Debug.LogError("The building does not have the car park script on it");

        agent.AddGoal(new WalkToAndEnterVehicleGoal());
        agent.AddGoal(new DriveToWaypointGoal(cp.PropertyEntryNode, "Driving to car park entrance"));
        agent.AddGoal(new ParkInCarParkGoal(cp));
        agent.AddGoal(new WaitGoal(2f));
        agent.AddGoal(new ExitVehicleGoal());
        agent.AddGoal(new WalkToWaypointGoal(GetRandomPedestrianWaypoint(WaypointType.PedestrianWalkway), "Walking to random place"));
        agent.AddGoal(new WaitGoal(2f));
        agent.AddGoal(new WalkToAndEnterVehicleGoal());
        agent.AddGoal(new ExitCarParkGoal());
        agent.AddGoal(new DriveHomeGoal());
        agent.AddGoal(new ExitVehicleGoal());
        agent.AddGoal(new WalkToFrontDoorGoal());
    }

    public EntityId GetPersonsVehicle(EntityId personId)
        => RelationshipManager.Instance.GetVehiclesForPerson(personId).FirstOrDefault();

    public EntityId GetAlightWaypointId(EntityId waypointId)
        => RelationshipManager.Instance.GetAlightForParkingSpot(waypointId).FirstOrDefault();

    public EntityId GetHomeBuilding(EntityId personId)
        => RelationshipManager.Instance.GetHomeBuildingsForPerson(personId).FirstOrDefault();

    public void ReParentPedestrian(AgentController ac)
    {
        ac.transform.SetParent(transform);
    }

    public void PopulateSaveData(GameSaveData saveData)
    {
        saveData.Pedestrians = new();

        foreach (AgentController agent in _allPedestrians.Values)
        {
            PedestrianMovement pm = agent.GetComponent<PedestrianMovement>();
            PedestrianData pd = agent.GetComponent<PedestrianData>();

            PedestrianSaveData pedestrian = new()
            {
                Id = agent.Id.ToString(),
                FirstName = pd.FirstName,
                LastName = pd.LastName,
                CurrentVehicleId = pm.CurrentVehicle?.Id.ToString(),
                CurrentWaypointId = pm.CurrentWaypoint?.Id.ToString(),
                TargetWaypointId = pm.TargetWaypoint?.Id.ToString(),
                Goals = agent.SaveQueueToJson()
            };

            saveData.Pedestrians.Add(pedestrian);
        }
    }

    public void LoadFromSaveData(GameSaveData saveData)
    {
        if (saveData.Pedestrians == null)
        {
            Debug.LogWarning("[PedestrianManager] No Pedestrian data in save file. ");
            return;
        }

        // clear and delete all Pedestrian game objects from the world
        _allPedestrians.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        foreach (PedestrianSaveData p in saveData.Pedestrians)
        {
            EntityId pId = EntityId.FromString(p.Id);
            WaypointNode currentWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(p.CurrentWaypointId);

            if (currentWaypoint == null)
            {
                // one reason could be that we are in a car and thus have a vehicle waypoint
                currentWaypoint = VehicleWaypointManager.Instance.GetWaypointFromId(p.CurrentWaypointId);

                if (currentWaypoint == null)
                {
                    Debug.LogError($"We don't seem to have a valid waypoint for person {p.Id}. But why?");
                }
            }

            WaypointNode targetWaypoint = PedestrianWaypointManager.Instance.GetWaypointFromId(p.TargetWaypointId);

            AgentController person = AddAndRegisterPerson(pId, currentWaypoint, currentWaypoint.Position, targetWaypoint);
            PedestrianData pd = person.GetComponent<PedestrianData>();
            PedestrianMovement pm = person.GetComponent<PedestrianMovement>();

            pd.SetNames(p.FirstName, p.LastName);

            if (p.CurrentVehicleId != "")
            {
                AgentController vehicle = VehicleManager.Instance.GetVehicle(EntityId.FromString(p.CurrentVehicleId));

                if (vehicle.Id.IsValid)
                {
                    person.transform.parent = vehicle.transform;
                    person.ShowHideAgent(false);
                    pm.SetCurrentVehicle(vehicle);
                }
            }

            // load the goals
            person.LoadQueue(p.Goals);
        }
    }

    #endregion
}
