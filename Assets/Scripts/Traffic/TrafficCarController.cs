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
    [Header("交通らしい間隔制御")]
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private float detectionRadius = 0.45f;
    [SerializeField] private float obstacleSlowdown = 0.15f;
    [SerializeField] private LayerMask obstacleLayers = ~0;

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

        // 前方の車や障害物を球形レイで確認し、見つけた時は急停止せず自然に減速します。
        // これにより複数台を同じ経路へ置いても、追突し続ける見た目を抑えられます。
        float speedMultiplier = HasObstacleAhead() ? obstacleSlowdown : 1f;
        transform.position += transform.forward * moveSpeed * speedMultiplier * Time.deltaTime;
    }

    private bool HasObstacleAhead()
    {
        Vector3 origin = transform.position + Vector3.up * 0.35f;
        return Physics.SphereCast(origin, detectionRadius, transform.forward, out RaycastHit hit, detectionDistance, obstacleLayers,
            QueryTriggerInteraction.Ignore) && hit.transform.root != transform.root;
    }
}
