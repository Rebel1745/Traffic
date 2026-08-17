using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RelationshipSaveData
{
    public string Type; // e.g., "Resident"
    public string SourceId;
    public List<string> TargetIds; // List of IDs
}
