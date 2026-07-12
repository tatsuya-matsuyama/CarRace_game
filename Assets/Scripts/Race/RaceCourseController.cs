using UnityEngine;

/// <summary>
/// シーン上へ手作業で置いたチェックポイントとスタートグリッドを、1つの固定コースとして管理します。
/// </summary>
public class RaceCourseController : MonoBehaviour
{
    [SerializeField] private RaceCourseData courseData;
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private Transform[] opponentStartPoints;
    [SerializeField] private Transform[] checkpoints;

    public RaceCourseData CourseData => courseData;
    public Transform PlayerStartPoint => playerStartPoint;
    public Transform[] OpponentStartPoints => opponentStartPoints;
    public Transform[] Checkpoints => checkpoints;
    public int CheckpointCount => checkpoints != null ? checkpoints.Length : 0;

    public Transform GetCheckpoint(int index)
    {
        return checkpoints != null && index >= 0 && index < checkpoints.Length ? checkpoints[index] : null;
    }
}
