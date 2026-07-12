using UnityEngine;

/// <summary>
/// 指定した経路をループ走行する、街中の交通NPC用コントローラーです。
/// </summary>
public class TrafficCarController : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float reachDistance = 1.5f;

    private int currentWaypointIndex;

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = targetWaypoint.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= reachDistance * reachDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}
