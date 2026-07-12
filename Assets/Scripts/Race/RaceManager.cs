using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// レース開始、20台のNPC生成、順位計算、3位までの報酬を管理するSingletonです。
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [SerializeField] private GameObject opponentPrefab;
    [SerializeField] private int opponentCount = 20;
    [SerializeField] private float countdownSeconds = 3f;

    private readonly List<RaceProgressTracker> participants = new List<RaceProgressTracker>();
    private readonly List<GameObject> spawnedOpponents = new List<GameObject>();
    private RaceCourseController currentCourse;
    private ArcadeCarController playerController;
    private RaceProgressTracker playerProgress;

    public bool IsRaceActive { get; private set; }
    public IReadOnlyList<RaceProgressTracker> Participants => participants;
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

    public void StartRace(RaceCourseController course, ArcadeCarController player)
    {
        if (IsRaceActive || course == null || course.CourseData == null || course.CheckpointCount < 2 || player == null)
        {
            return;
        }

        currentCourse = course;
        playerController = player;
        StartCoroutine(BeginRaceAfterCountdown());
    }

    private IEnumerator BeginRaceAfterCountdown()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Race);
        playerController.SetControlEnabled(false);
        PrepareParticipants();
        yield return new WaitForSeconds(countdownSeconds);

        IsRaceActive = true;
        playerController.SetControlEnabled(true);
    }

    private void PrepareParticipants()
    {
        ClearSpawnedOpponents();
        participants.Clear();

        Transform playerStart = currentCourse.PlayerStartPoint;
        if (playerStart != null)
        {
            playerController.transform.SetPositionAndRotation(playerStart.position, playerStart.rotation);
            Rigidbody playerBody = playerController.GetComponent<Rigidbody>();
            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }
        }

        playerProgress = playerController.GetComponent<RaceProgressTracker>();
        if (playerProgress == null)
        {
            playerProgress = playerController.gameObject.AddComponent<RaceProgressTracker>();
        }

        playerProgress.Initialize(currentCourse, "PLAYER", true);
        participants.Add(playerProgress);

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
            participants.Add(opponent.GetComponent<RaceProgressTracker>());
            spawnedOpponents.Add(opponent);
        }
    }

    public void NotifyParticipantFinished(RaceProgressTracker participant)
    {
        if (!IsRaceActive || participant == null || !participant.IsPlayer)
        {
            return;
        }

        List<RaceProgressTracker> standings = GetStandings();
        int rank = standings.IndexOf(participant) + 1;
        int reward = currentCourse.CourseData.GetReward(rank);
        if (reward > 0)
        {
            GameManager.Instance?.AddGold(reward);
        }

        IsRaceActive = false;
        playerController.SetControlEnabled(false);
        OnPlayerRaceFinished?.Invoke(rank, reward);
    }

    public List<RaceProgressTracker> GetStandings()
    {
        return participants
            .OrderBy(participant => participant.HasFinished ? 0 : 1)
            .ThenBy(participant => participant.HasFinished ? participant.FinishedTime : 0f)
            .ThenByDescending(participant => participant.GetProgressScore())
            .ToList();
    }

    public void EndRaceAndReturnToExplore()
    {
        IsRaceActive = false;
        playerController?.SetControlEnabled(true);
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);
        ClearSpawnedOpponents();
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
}
