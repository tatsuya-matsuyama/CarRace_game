using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// レース中だけ表示する順位・周回・スタート信号のHUDです。
/// 会話や街用HUDとは分離し、レース進行に必要な情報を見やすく表示します。
/// </summary>
public class RaceHudController : MonoBehaviour
{
    private void OnGUI()
    {
        RaceManager manager = RaceManager.Instance;
        // ゴール後は結果画面へ任せるため、Destroy済みNPCを含む順位HUDを表示しません。
        if (manager == null || !manager.IsRaceActive || manager.CurrentCourse == null || manager.PlayerProgress == null)
        {
            return;
        }

        RaceProgressTracker player = manager.PlayerProgress;
        int lap = Mathf.Min(player.CompletedLaps + 1, manager.CurrentCourse.CourseData.LapCount);
        List<RaceProgressTracker> standings = manager.GetStandings();
        int rank = standings.IndexOf(player) + 1;

        GUI.Box(new Rect(20f, 20f, 245f, 78f), "RACE STATUS");
        GUI.Label(new Rect(36f, 49f, 220f, 24f), $"POSITION  {rank} / {standings.Count}");
        GUI.Label(new Rect(36f, 72f, 220f, 24f), $"LAP  {lap} / {manager.CurrentCourse.CourseData.LapCount}");

        if (!string.IsNullOrEmpty(manager.StartMessage))
        {
            GUI.Box(new Rect(Screen.width * .5f - 130f, 48f, 260f, 70f), manager.StartMessage);
        }
    }
}
