using UnityEngine;

[System.Serializable]
public struct PedestrianPumpDetails
{
    public Transform AlightPosition;
    public Transform[] PathToPump;
    public Transform[] PathToShop;
}