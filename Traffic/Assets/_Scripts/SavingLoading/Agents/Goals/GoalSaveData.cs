[System.Serializable]
public class GoalSaveData
{
    public string GoalType; // "Wait", "Drive", "Walk"
    public string Json;     // The serialized data for that specific goal
}