using UnityEngine;

/// <summary>
/// 参加車両ごとのチェックポイント通過順・周回数・ゴール状態を記録します。
/// </summary>
public class RaceProgressTracker : MonoBehaviour
{
    [SerializeField] private string racerName = "Racer";
    [SerializeField] private bool isPlayer;
    [SerializeField] private float checkpointReachDistance = 5f;

    private RaceCourseController course;
    private int nextCheckpointIndex;
    private int completedLaps;
    private bool hasFinished;
    private float finishedTime;

    public string RacerName => racerName;
    public bool IsPlayer => isPlayer;
    public int CompletedLaps => completedLaps;
    public int NextCheckpointIndex => nextCheckpointIndex;
    public bool HasFinished => hasFinished;
    public float FinishedTime => finishedTime;

    public void Initialize(RaceCourseController raceCourse, string displayName, bool player)
    {
        course = raceCourse;
        racerName = displayName;
        isPlayer = player;
        nextCheckpointIndex = 0;
        completedLaps = 0;
        hasFinished = false;
        finishedTime = 0f;
    }

    private void Update()
    {
        if (course == null || hasFinished || RaceManager.Instance == null || !RaceManager.Instance.IsRaceActive)
        {
            return;
        }

        Transform target = course.GetCheckpoint(nextCheckpointIndex);
        if (target == null || Vector3.Distance(transform.position, target.position) > checkpointReachDistance)
        {
            return;
        }

        nextCheckpointIndex++;
        if (nextCheckpointIndex < course.CheckpointCount)
        {
            return;
        }

        nextCheckpointIndex = 0;
        completedLaps++;
        if (completedLaps >= course.CourseData.LapCount)
        {
            hasFinished = true;
            finishedTime = Time.time;
            // この旧トラッカーは既存シーンを壊さず読み込むためだけに残しています。
            // 新規レースではCarRaceTrackerがTrigger通過とゴール通知を担当します。
        }
    }

    public float GetProgressScore()
    {
        if (course == null || course.CheckpointCount == 0)
        {
            return 0f;
        }

        Transform target = course.GetCheckpoint(nextCheckpointIndex);
        float distanceToNext = target != null ? Vector3.Distance(transform.position, target.position) : 0f;
        return completedLaps * course.CheckpointCount + nextCheckpointIndex - distanceToNext * 0.001f;
    }
}
