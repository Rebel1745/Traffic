using UnityEngine;

[RequireComponent(typeof(AgentController))] // Ensures it's always on the same object
public class PedestrianData : MonoBehaviour
{
    [Header("Personal Info")]
    [SerializeField] private string _firstName;
    public string FirstName => _firstName;
    [SerializeField] private string _lastName;
    public string LastName => _lastName;

    // You can make these public properties to read, but keep fields private
    public string FullName => $"{_firstName} {_lastName}";

    public void SetNames(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
    }
}