using System;
using UnityEngine;

/// <summary>
/// 各レース参加車両の正規チェックポイント通過、周回数、ゴール状態を記録します。
/// 順番外のチェックポイントは受理しないため、逆走やショートカットでは周回できません。
/// </summary>
public class CarRaceTracker : MonoBehaviour
{
    [Header("レース進行")]
    [SerializeField] private int currentLap;
    [SerializeField] private int lastCheckpointIndex = -1;
    [SerializeField] private string racerName = "Racer";
    [SerializeField] private bool isPlayer;

    private RaceCourseController course;
    private int checkpointCount;
    private int targetLapCount;
    private bool hasFinished;
    private float finishedTime;

    public int CurrentLap => currentLap;
    public int LastCheckpointIndex => lastCheckpointIndex;
    public int NextCheckpointIndex => checkpointCount > 0 ? (lastCheckpointIndex + 1 + checkpointCount) % checkpointCount : 0;
    public string RacerName => racerName;
    public bool IsPlayer => isPlayer;
    public bool HasFinished => hasFinished;
    public float FinishedTime => finishedTime;
    public RaceCourseController Course => course;
    public event Action<CarRaceTracker> OnLapCompleted;

    /// <summary>レース開始時に走行コースと参加者情報を初期化します。</summary>
    public void Initialize(RaceCourseController raceCourse, string displayName, bool player)
    {
        course = raceCourse;
        racerName = displayName;
        isPlayer = player;
        checkpointCount = course != null ? course.CheckpointCount : 0;
        targetLapCount = course != null && course.CourseData != null ? course.CourseData.LapCount : 0;
        currentLap = 0;
        lastCheckpointIndex = -1;
        hasFinished = false;
        finishedTime = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPassCheckpoint(other.GetComponent<Checkpoint>());
    }

    /// <summary>
    /// チェックポイントを通過しようとした時の共通処理です。
    /// AIのようにTransform移動する車も、このメソッドを呼ぶことでプレイヤーと同じ順番判定を使えます。
    /// </summary>
    public bool TryPassCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || course == null || hasFinished || checkpointCount <= 0)
        {
            return false;
        }

        // 同じ番号でも別コースのTriggerは受け付けません。
        if (checkpoint.Course != null && checkpoint.Course != course)
        {
            return false;
        }

        int expectedIndex = NextCheckpointIndex;
        if (checkpoint.checkpointIndex != expectedIndex)
        {
            return false;
        }

        // 最終チェックポイントを通過済みでスタートライン(0)へ戻った時だけ周回を進めます。
        bool completedLap = lastCheckpointIndex == checkpointCount - 1 && checkpoint.checkpointIndex == 0;
        lastCheckpointIndex = checkpoint.checkpointIndex;

        if (!completedLap)
        {
            return true;
        }

        currentLap++;
        OnLapCompleted?.Invoke(this);
        if (currentLap >= targetLapCount)
        {
            hasFinished = true;
            finishedTime = RaceManager.Instance != null ? RaceManager.Instance.RaceTimer : Time.time;
            RaceManager.Instance?.NotifyParticipantFinished(this);
        }

        return true;
    }

    /// <summary>順位計算用に、次の正規チェックポイントまでの平面距離を返します。</summary>
    public float GetDistanceToNextCheckpoint()
    {
        if (course == null || checkpointCount == 0)
        {
            return float.MaxValue;
        }

        Transform next = course.GetCheckpoint(NextCheckpointIndex);
        if (next == null)
        {
            return float.MaxValue;
        }

        Vector3 offset = next.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }
}
