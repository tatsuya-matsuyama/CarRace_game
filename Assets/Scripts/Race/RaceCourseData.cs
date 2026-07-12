using UnityEngine;

/// <summary>
/// あらかじめ作成したレースコースの名前・周回数・順位報酬を定義するデータです。
/// 実際のチェックポイントやスタート位置は、シーン上のRaceCourseControllerへ固定配置します。
/// </summary>
[CreateAssetMenu(fileName = "RaceCourse", menuName = "CarRace/Race Course Data")]
public class RaceCourseData : ScriptableObject
{
    [SerializeField] private string courseName = "コース名";
    [TextArea] [SerializeField] private string description;
    [Min(1)] [SerializeField] private int lapCount = 3;
    [Min(0)] [SerializeField] private int firstPlaceReward = 1000;
    [Min(0)] [SerializeField] private int secondPlaceReward = 600;
    [Min(0)] [SerializeField] private int thirdPlaceReward = 300;

    public string CourseName => courseName;
    public string Description => description;
    public int LapCount => lapCount;

    public int GetReward(int rank)
    {
        return rank switch
        {
            1 => firstPlaceReward,
            2 => secondPlaceReward,
            3 => thirdPlaceReward,
            _ => 0
        };
    }
}
