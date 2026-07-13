using UnityEngine;

/// <summary>
/// コース上に置く透明な通過判定です。
/// checkpointIndex はスタート／ゴールラインを0、以降を走行順に連番で設定します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("コース内での通過順です。スタート／ゴールラインは 0 にします。")]
    [Min(0)] public int checkpointIndex;

    [Tooltip("このチェックポイントが属するレースコースです。別コースの判定を無視するために使用します。")]
    [SerializeField] private RaceCourseController course;

    public RaceCourseController Course => course;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    /// <summary>コース生成ツールやエディタ拡張から所属コースを設定します。</summary>
    public void SetCourse(RaceCourseController raceCourse)
    {
        course = raceCourse;
    }
}
