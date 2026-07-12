using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// レース会場の入口で開くコース選択画面です。
/// あらかじめシーンへ配置した2コース以上を選び、Enter/Eでレースを開始します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class RaceLobbyController : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private RaceCourseController[] courses;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text lobbyText;

    private ArcadeCarController playerController;
    private bool playerInRange;
    private bool isLobbyOpen;
    private bool isShowingResult;
    private bool raceInProgress;
    private int selectedCourseIndex;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        lobbyPanel?.SetActive(false);
        interactionPrompt?.SetActive(false);
    }

    private void OnEnable()
    {
        if (raceManager != null)
        {
            raceManager.OnPlayerRaceFinished += ShowRaceResult;
        }
    }

    private void OnDisable()
    {
        if (raceManager != null)
        {
            raceManager.OnPlayerRaceFinished -= ShowRaceResult;
        }
    }

    private void Update()
    {
        if (!isLobbyOpen)
        {
            if (playerInRange && Input.GetKeyDown(KeyCode.E))
            {
                OpenLobby();
            }

            return;
        }

        if (isShowingResult)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                CloseLobby();
            }

            return;
        }

        // レース中はコース選択の入力を受け取らず、車両操作だけをRaceManagerへ渡します。
        if (raceInProgress)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectCourse(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectCourse(1);
        }
        else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            StartSelectedRace();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLobby();
        }
    }

    private void OpenLobby()
    {
        if (courses == null || courses.Length == 0 || raceManager == null || playerController == null)
        {
            return;
        }

        isLobbyOpen = true;
        isShowingResult = false;
        raceInProgress = false;
        playerController.SetControlEnabled(false);
        GameManager.Instance?.ChangeState(GameManager.GameState.Race);
        lobbyPanel?.SetActive(true);
        interactionPrompt?.SetActive(false);
        RefreshCourseText();
    }

    private void CloseLobby()
    {
        isLobbyOpen = false;
        isShowingResult = false;
        raceInProgress = false;
        lobbyPanel?.SetActive(false);
        raceManager?.EndRaceAndReturnToExplore();

        if (playerInRange)
        {
            interactionPrompt?.SetActive(true);
        }
    }

    private void SelectCourse(int direction)
    {
        selectedCourseIndex = (selectedCourseIndex + direction + courses.Length) % courses.Length;
        RefreshCourseText();
    }

    private void StartSelectedRace()
    {
        RaceCourseController course = courses[selectedCourseIndex];
        if (course == null)
        {
            return;
        }

        lobbyPanel?.SetActive(false);
        raceInProgress = true;
        raceManager.StartRace(course, playerController);
    }

    private void ShowRaceResult(int rank, int reward)
    {
        isShowingResult = true;
        raceInProgress = false;
        lobbyPanel?.SetActive(true);
        if (lobbyText != null)
        {
            lobbyText.text = $"RACE RESULT\n\n{rank} 位！\n獲得報酬: {reward} G\n\n[E] 街へ戻る";
        }
    }

    private void RefreshCourseText()
    {
        RaceCourseData data = courses[selectedCourseIndex].CourseData;
        if (lobbyText == null || data == null)
        {
            return;
        }

        lobbyText.text = $"RACE COURSE SELECT\n\n{data.CourseName}\n{data.Description}\n\n周回数: {data.LapCount}\n1位 {data.GetReward(1)}G / 2位 {data.GetReward(2)}G / 3位 {data.GetReward(3)}G\n\n[A / D] コース選択    [E] レース開始";
    }

    private void OnTriggerEnter(Collider other)
    {
        ArcadeCarController controller = other.GetComponent<ArcadeCarController>();
        if (controller == null)
        {
            return;
        }

        playerInRange = true;
        playerController = controller;
        if (!isLobbyOpen)
        {
            interactionPrompt?.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ArcadeCarController>() == null)
        {
            return;
        }

        playerInRange = false;
        interactionPrompt?.SetActive(false);
    }
}
