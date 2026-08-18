using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PedestrianSaveData
{
    public string Id;
    public string FirstName;
    public string LastName;
    public string CurrentVehicleId;
    public string CurrentWaypointId;
    public string TargetWaypointId;
    public List<GoalSaveData> Goals;
}
