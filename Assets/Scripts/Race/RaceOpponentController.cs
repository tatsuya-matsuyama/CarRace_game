using UnityEngine;

/// <summary>
/// レース専用コースをチェックポイント順に走るNPC車です。
/// 交通NPCとは分離し、レース順位用のRaceProgressTrackerを持ちます。
/// </summary>
[RequireComponent(typeof(RaceProgressTracker))]
public class RaceOpponentController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float turnSpeed = 7f;

    private RaceCourseController course;
    private RaceProgressTracker progress;

    public void Initialize(RaceCourseController raceCourse, string racerName, int seed)
    {
        course = raceCourse;
        progress = GetComponent<RaceProgressTracker>();
        progress.Initialize(course, racerName, false);
        moveSpeed += (seed % 5 - 2) * 0.35f;
    }

    private void Update()
    {
        if (course == null || progress == null || progress.HasFinished || RaceManager.Instance == null || !RaceManager.Instance.IsRaceActive)
        {
            return;
        }

        Transform target = course.GetCheckpoint(progress.NextCheckpointIndex);
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), turnSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}
