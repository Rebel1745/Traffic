using UnityEngine;

public class VehicleData : MonoBehaviour
{
    [Header("Selected Details")]
    [SerializeField] private string _vehicleName;
    public string VehicleName => _vehicleName;
}
