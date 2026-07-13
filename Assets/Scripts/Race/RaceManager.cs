using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// レース開始、参加車両、チェックポイント順位、ラップタイム、報酬を一括管理するSingletonです。
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("レース設定")]
    [SerializeField] private GameObject opponentPrefab;
    [SerializeField] private int opponentCount = 20;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField, Min(.02f)] private float rankingUpdateInterval = .1f;

    [Header("TextMeshPro HUD（任意）")]
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI currentLapTimeText;
    [SerializeField] private TextMeshProUGUI bestLapTimeText;

    private readonly List<CarRaceTracker> participants = new List<CarRaceTracker>();
    private readonly List<GameObject> spawnedOpponents = new List<GameObject>();
    private List<CarRaceTracker> standings = new List<CarRaceTracker>();
    private RaceCourseController currentCourse;
    private ArcadeCarController playerController;
    private CarRaceTracker playerTracker;
    private Vector3 playerPositionBeforeRace;
    private Quaternion playerRotationBeforeRace;
    private float rankingElapsed;

    /// <summary>レース開始からの総時間です。</summary>
    public float RaceTimer { get; private set; }
    /// <summary>プレイヤーが現在走っているラップの時間です。</summary>
    public float CurrentLapTimer { get; private set; }
    /// <summary>プレイヤーの最速ラップです。未記録時はInfinityです。</summary>
    public float BestLapTime { get; private set; } = float.PositiveInfinity;
    public bool IsRaceActive { get; private set; }
    public IReadOnlyList<CarRaceTracker> Participants => participants;
    public CarRaceTracker PlayerTracker => playerTracker;
    public RaceCourseController CurrentCourse => currentCourse;
    public string StartMessage { get; private set; } = string.Empty;
    public event Action<int, int> OnPlayerRaceFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!IsRaceActive)
        {
            return;
        }

        RaceTimer += Time.deltaTime;
        CurrentLapTimer += Time.deltaTime;
        rankingElapsed += Time.deltaTime;
        if (rankingElapsed >= rankingUpdateInterval)
        {
            rankingElapsed = 0f;
            RecalculateStandings();
        }

        UpdateRaceUi();
    }

    public void StartRace(RaceCourseController course, ArcadeCarController player)
    {
        if (IsRaceActive || course == null || course.CourseData == null || course.CheckpointCount < 2 || player == null)
        {
            Debug.LogError($"[RaceManager] レース開始を中止しました。Active={IsRaceActive}, Course={course != null}, Data={course != null && course.CourseData != null}, Checkpoints={(course != null ? course.CheckpointCount : 0)}, Player={player != null}");
            return;
        }

        currentCourse = course;
        playerController = player;
        playerPositionBeforeRace = player.transform.position;
        playerRotationBeforeRace = player.transform.rotation;

        // ロード演出の完了コールバックに依存せず、開始操作の直後に専用コースを有効化します。
        // これによりロードUI側で問題が起きても、街に取り残される状態を防ぎます。
        ActivateCourseAndMovePlayerToGrid();
        Debug.Log($"[RaceManager] コースへ移動: {course.CourseData.CourseName} / {player.transform.position}");

        RaceLoadingController loader = RaceLoadingController.Instance;
        if (loader == null)
        {
            loader = new GameObject("RaceLoadingController").AddComponent<RaceLoadingController>();
        }

        loader.TransitionToCourse(course, () => StartCoroutine(BeginRaceAfterCountdown()));
    }

    private IEnumerator BeginRaceAfterCountdown()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Race);
        playerController.SetControlEnabled(false);
        SetRaceWorldVisibility(true);
        PrepareParticipants();
        for (int count = Mathf.CeilToInt(countdownSeconds); count > 0; count--)
        {
            StartMessage = $"READY {count}";
            yield return new WaitForSeconds(1f);
        }

        RaceTimer = 0f;
        CurrentLapTimer = 0f;
        BestLapTime = float.PositiveInfinity;
        rankingElapsed = rankingUpdateInterval;
        IsRaceActive = true;
        StartMessage = "GO!";
        playerController.SetControlEnabled(true);
        RecalculateStandings();
        UpdateRaceUi();
        yield return new WaitForSeconds(1f);
        StartMessage = string.Empty;
    }

    private void PrepareParticipants()
    {
        ClearSpawnedOpponents();
        participants.Clear();

        ActivateCourseAndMovePlayerToGrid();

        playerTracker = playerController.GetComponent<CarRaceTracker>();
        if (playerTracker == null)
        {
            playerTracker = playerController.gameObject.AddComponent<CarRaceTracker>();
        }
        playerTracker.OnLapCompleted -= HandlePlayerLapCompleted;
        playerTracker.OnLapCompleted += HandlePlayerLapCompleted;
        playerTracker.Initialize(currentCourse, "PLAYER", true);
        participants.Add(playerTracker);

        Transform[] starts = currentCourse.OpponentStartPoints;
        int count = Mathf.Min(opponentCount, starts != null ? starts.Length : 0);
        for (int i = 0; i < count; i++)
        {
            if (opponentPrefab == null || starts[i] == null)
            {
                continue;
            }

            GameObject opponent = Instantiate(opponentPrefab, starts[i].position, starts[i].rotation);
            opponent.name = $"RaceNPC_{i + 1:00}";
            opponent.SetActive(true);
            RaceOpponentController controller = opponent.GetComponent<RaceOpponentController>();
            controller.Initialize(currentCourse, $"NPC {i + 1:00}", i);
            CarRaceTracker tracker = opponent.GetComponent<CarRaceTracker>();
            if (tracker != null)
            {
                participants.Add(tracker);
            }
            spawnedOpponents.Add(opponent);
        }
    }

    private void HandlePlayerLapCompleted(CarRaceTracker tracker)
    {
        // ゴールラップも含めて、通過直後のラップタイムを保存してから次周用にリセットします。
        if (CurrentLapTimer > 0f && CurrentLapTimer < BestLapTime)
        {
            BestLapTime = CurrentLapTimer;
        }
        CurrentLapTimer = 0f;
        UpdateRaceUi();
    }

    /// <summary>プレイヤーまたはAIがゴールした時に順位と報酬を確定します。</summary>
    public void NotifyParticipantFinished(CarRaceTracker participant)
    {
        if (!IsRaceActive || participant == null || !participant.IsPlayer)
        {
            return;
        }

        RecalculateStandings();
        int rank = standings.IndexOf(participant) + 1;
        int reward = currentCourse.CourseData.GetReward(rank);
        if (reward > 0)
        {
            GameManager.Instance?.AddGold(reward);
        }

        IsRaceActive = false;
        StartMessage = "FINISH!";
        playerController.SetControlEnabled(false);
        OnPlayerRaceFinished?.Invoke(rank, reward);
    }

    // 旧テストシーンに残ったRaceProgressTrackerとのコンパイル互換用です。
    // 新規コースではCheckpoint + CarRaceTrackerだけを使用します。
    [Obsolete("RaceProgressTracker is legacy. Use CarRaceTracker instead.")]
    public void NotifyParticipantFinished(RaceProgressTracker legacyParticipant)
    {
    }

    /// <summary>
    /// 周回数、最後に正規通過したチェックポイント、次のチェックポイントまでの距離の順で順位を再計算します。
    /// </summary>
    public void RecalculateStandings()
    {
        participants.RemoveAll(tracker => tracker == null);
        standings = participants
            .OrderBy(tracker => tracker.HasFinished ? 0 : 1)
            .ThenBy(tracker => tracker.HasFinished ? tracker.FinishedTime : 0f)
            .ThenByDescending(tracker => tracker.CurrentLap)
            .ThenByDescending(tracker => tracker.LastCheckpointIndex)
            .ThenBy(tracker => tracker.GetDistanceToNextCheckpoint())
            .ToList();
    }

    public List<CarRaceTracker> GetStandings()
    {
        RecalculateStandings();
        return new List<CarRaceTracker>(standings);
    }

    /// <summary>TextMeshProのHUD参照を外部からまとめて設定します。</summary>
    public void ConfigureRaceUi(TextMeshProUGUI position, TextMeshProUGUI currentLap, TextMeshProUGUI bestLap)
    {
        positionText = position;
        currentLapTimeText = currentLap;
        bestLapTimeText = bestLap;
        UpdateRaceUi();
    }

    /// <summary>現在順位・現在ラップ・ベストラップをTextMeshPro HUDへ反映します。</summary>
    public void UpdateRaceUi()
    {
        if (playerTracker == null)
        {
            return;
        }

        if (positionText != null)
        {
            int rank = standings.IndexOf(playerTracker) + 1;
            positionText.text = $"POSITION  {Mathf.Max(1, rank)} / {participants.Count}";
        }
        if (currentLapTimeText != null)
        {
            currentLapTimeText.text = $"LAP TIME  {FormatTime(CurrentLapTimer)}";
        }
        if (bestLapTimeText != null)
        {
            bestLapTimeText.text = float.IsPositiveInfinity(BestLapTime)
                ? "BEST  --:--.---"
                : $"BEST  {FormatTime(BestLapTime)}";
        }
    }

    public void EndRaceAndReturnToExplore()
    {
        IsRaceActive = false;
        StartMessage = string.Empty;
        if (playerTracker != null)
        {
            playerTracker.OnLapCompleted -= HandlePlayerLapCompleted;
        }
        playerController?.SetControlEnabled(true);
        if (playerController != null)
        {
            playerController.transform.SetPositionAndRotation(playerPositionBeforeRace, playerRotationBeforeRace);
            ResetRigidbody(playerController.GetComponent<Rigidbody>());
        }

        SetRaceWorldVisibility(false);
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);
        ClearSpawnedOpponents();
        participants.Clear();
        standings.Clear();
        playerTracker = null;
        currentCourse = null;
    }

    private static void ResetRigidbody(Rigidbody body)
    {
        if (body == null)
        {
            return;
        }
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    /// <summary>選択コースだけを表示し、プレイヤーをスタートグリッドへ安全に移動します。</summary>
    private void ActivateCourseAndMovePlayerToGrid()
    {
        SetRaceWorldVisibility(true);
        if (currentCourse == null || playerController == null || currentCourse.PlayerStartPoint == null)
        {
            return;
        }

        Transform playerStart = currentCourse.PlayerStartPoint;
        playerController.transform.SetPositionAndRotation(playerStart.position, playerStart.rotation);
        ResetRigidbody(playerController.GetComponent<Rigidbody>());
    }

    private static string FormatTime(float time)
    {
        TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, time));
        return $"{span.Minutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
    }

    private void ClearSpawnedOpponents()
    {
        foreach (GameObject opponent in spawnedOpponents)
        {
            if (opponent != null)
            {
                Destroy(opponent);
            }
        }
        spawnedOpponents.Clear();
    }

    private void SetRaceWorldVisibility(bool raceVisible)
    {
        foreach (RaceCourseController course in FindObjectsByType<RaceCourseController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            course.gameObject.SetActive(raceVisible && course == currentCourse);
        }

        GameObject cityMap = GameObject.Find("CityMap");
        if (cityMap != null)
        {
            cityMap.SetActive(!raceVisible);
        }
    }
}
