using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public Lane CurrentLane;
    public int CurrentWaypointIndex = 0;
    public float Speed = 5f;
    public float TargetSpeed = 5f;
    public float Acceleration = 2f;
    public float Deceleration = 2f;
    public float WaypointReachDistance = 0.5f;

    void Update()
    {
        if (CurrentLane == null || CurrentLane.Waypoints == null || CurrentLane.Waypoints.Count == 0)
        {
            return;
        }

        // Check if we've reached the end of the lane
        if (CurrentWaypointIndex >= CurrentLane.Waypoints.Count)
        {
            CheckIntersection();
            return;
        }

        // Get target position
        Vector3 targetPos = CurrentLane.Waypoints[CurrentWaypointIndex];

        // Calculate direction to target
        Vector3 direction = (targetPos - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPos);

        // Accelerate/decelerate toward target speed
        if (Speed < TargetSpeed)
        {
            Speed += Acceleration * Time.deltaTime;
            Speed = Mathf.Min(Speed, TargetSpeed);
        }
        else if (Speed > TargetSpeed)
        {
            Speed -= Deceleration * Time.deltaTime;
            Speed = Mathf.Max(Speed, TargetSpeed);
        }

        // Move vehicle
        if (distanceToTarget > 0.1f)
        {
            // Apply steering
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // Move forward
            transform.position += direction * Speed * Time.deltaTime;
        }

        // Check if reached waypoint
        if (distanceToTarget < WaypointReachDistance)
        {
            CurrentWaypointIndex++;
        }
    }

    void CheckIntersection()
    {
        if (CurrentLane != null && CurrentLane.EndIntersection != null)
        {
            // Check if there's a turn available
            Lane nextLane = ChooseNextLane();
            if (nextLane != null)
            {
                CurrentLane = nextLane;
                CurrentWaypointIndex = 0;
            }
            else
            {
                // No lane available, destroy vehicle or handle as needed
                Debug.Log("Vehicle reached end with no connected lane");
                Destroy(gameObject);
            }
        }
        else
        {
            // No intersection, destroy vehicle
            Debug.Log("Vehicle reached end of lane");
            Destroy(gameObject);
        }
    }

    Lane ChooseNextLane()
    {
        // Simple logic: pick a random connected lane
        if (CurrentLane.ConnectedLanes != null && CurrentLane.ConnectedLanes.Count > 0)
        {
            return CurrentLane.ConnectedLanes[Random.Range(0, CurrentLane.ConnectedLanes.Count)];
        }
        return null;
    }
}