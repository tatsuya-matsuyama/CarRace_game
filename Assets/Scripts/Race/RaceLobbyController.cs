using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// レース会場の入口で開くコース選択画面を管理します。
/// コース一覧、選択中コースの詳細、レース開始を1画面にまとめます。
/// </summary>
[RequireComponent(typeof(Collider))]
public class RaceLobbyController : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private RaceCourseController[] courses;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject interactionPrompt;

    private ArcadeCarController playerController;
    private bool playerInRange;
    private bool isLobbyOpen;
    private bool isShowingResult;
    private bool raceInProgress;
    private int selectedCourseIndex;
    private GameObject selectionContent;
    private GameObject resultContent;
    private Text courseNameText;
    private Text japaneseNameText;
    private Text descriptionText;
    private Text difficultyText;
    private Text distanceText;
    private Text recordText;
    private Text resultText;
    private Image previewBackground;
    private readonly List<Image> courseListBackgrounds = new List<Image>();

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        EnsureReferences();
        CreateCourseSelectUi();
        lobbyPanel.SetActive(false);
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

        // レース中はロビーの操作を止め、走行操作はRaceManagerへ渡します。
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
        EnsureReferences();
        if (courses == null || courses.Length == 0 || raceManager == null || playerController == null)
        {
            return;
        }

        isLobbyOpen = true;
        isShowingResult = false;
        raceInProgress = false;
        playerController.SetControlEnabled(false);
        GameManager.Instance?.ChangeState(GameManager.GameState.Race);
        lobbyPanel.SetActive(true);
        interactionPrompt?.SetActive(false);
        selectionContent.SetActive(true);
        resultContent.SetActive(false);
        RefreshCourseUi();
    }

    private void CloseLobby()
    {
        isLobbyOpen = false;
        isShowingResult = false;
        raceInProgress = false;
        lobbyPanel.SetActive(false);
        raceManager?.EndRaceAndReturnToExplore();
        if (playerInRange)
        {
            interactionPrompt?.SetActive(true);
        }
    }

    private void SelectCourse(int direction)
    {
        if (courses == null || courses.Length == 0)
        {
            return;
        }

        selectedCourseIndex = (selectedCourseIndex + direction + courses.Length) % courses.Length;
        RefreshCourseUi();
    }

    private void SelectCourseByIndex(int index)
    {
        selectedCourseIndex = index;
        RefreshCourseUi();
    }

    private void StartSelectedRace()
    {
        if (courses == null || selectedCourseIndex < 0 || selectedCourseIndex >= courses.Length)
        {
            return;
        }

        RaceCourseController course = courses[selectedCourseIndex];
        if (course == null)
        {
            return;
        }

        lobbyPanel.SetActive(false);
        raceInProgress = true;
        raceManager.StartRace(course, playerController);
    }

    private void ShowRaceResult(int rank, int reward)
    {
        isShowingResult = true;
        raceInProgress = false;
        lobbyPanel.SetActive(true);
        selectionContent.SetActive(false);
        resultContent.SetActive(true);
        resultText.text = $"RACE RESULT\n\n{rank} 位！\n獲得報酬: {reward} G\n\n[E] 街へ戻る";
    }

    private void RefreshCourseUi()
    {
        if (courses == null || courses.Length == 0 || selectedCourseIndex >= courses.Length)
        {
            return;
        }

        RaceCourseData data = courses[selectedCourseIndex].CourseData;
        if (data == null)
        {
            return;
        }

        CoursePresentation presentation = GetPresentation(selectedCourseIndex);
        courseNameText.text = presentation.EnglishName;
        japaneseNameText.text = data.CourseName;
        descriptionText.text = data.Description;
        difficultyText.text = presentation.Difficulty;
        distanceText.text = presentation.Distance;
        recordText.text = presentation.Record;
        previewBackground.color = presentation.PreviewColor;

        for (int i = 0; i < courseListBackgrounds.Count; i++)
        {
            courseListBackgrounds[i].color = i == selectedCourseIndex
                ? presentation.AccentColor
                : new Color(.08f, .12f, .19f, .86f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ArcadeCarController controller = other.GetComponent<ArcadeCarController>();
        if (controller == null && other.CompareTag("Player"))
        {
            controller = other.GetComponentInChildren<ArcadeCarController>();
        }

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

    private void EnsureReferences()
    {
        if (raceManager == null)
        {
            raceManager = FindFirstObjectByType<RaceManager>();
        }

        if (courses == null || courses.Length == 0)
        {
            courses = FindObjectsByType<RaceCourseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        interactionPrompt ??= FindObjectIncludingInactive("RaceVenuePrompt");
    }

    private void CreateCourseSelectUi()
    {
        // 旧画面を再利用する場合も中身を全て作り直すため、古いボタン・テキストの重なりを残しません。
        lobbyPanel = FindObjectIncludingInactive("RaceCourseSelectPanel");
        if (lobbyPanel == null)
        {
            GameObject canvasObject = new GameObject("RaceCourseSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            lobbyPanel = new GameObject("RaceCourseSelectPanel", typeof(RectTransform), typeof(Image));
            lobbyPanel.transform.SetParent(canvasObject.transform, false);
        }

        RectTransform panelRect = lobbyPanel.GetComponent<RectTransform>();
        Stretch(panelRect, Vector2.zero, Vector2.one);
        Image panelImage = lobbyPanel.GetComponent<Image>() ?? lobbyPanel.AddComponent<Image>();
        panelImage.color = new Color(.01f, .02f, .05f, .97f);

        List<GameObject> oldChildren = new List<GameObject>();
        foreach (Transform child in lobbyPanel.transform)
        {
            oldChildren.Add(child.gameObject);
        }
        foreach (GameObject child in oldChildren)
        {
            Destroy(child);
        }

        selectionContent = CreatePanel(lobbyPanel.transform, "SelectionContent", new Vector2(.06f, .08f), new Vector2(.94f, .92f), Color.clear);
        Text title = CreateText(selectionContent.transform, "Title", new Vector2(0f, .89f), new Vector2(.36f, 1f), 44, "COURSE SELECT");
        title.alignment = TextAnchor.MiddleLeft;
        title.fontStyle = FontStyle.BoldAndItalic;
        Text subtitle = CreateText(selectionContent.transform, "Subtitle", new Vector2(0f, .85f), new Vector2(.42f, .90f), 15, "CHOOSE YOUR DESTINATION");
        subtitle.alignment = TextAnchor.MiddleLeft;
        subtitle.color = new Color(.58f, .66f, .76f);

        BuildCourseList(selectionContent.transform);
        BuildCourseDetail(selectionContent.transform);
        BuildResultUi();
    }

    private void BuildCourseList(Transform parent)
    {
        courseListBackgrounds.Clear();
        if (courses == null)
        {
            return;
        }

        for (int i = 0; i < courses.Length; i++)
        {
            RaceCourseData data = courses[i] != null ? courses[i].CourseData : null;
            float top = .72f - i * .16f;
            Button button = CreateButton(parent, "Course_" + i, new Vector2(0f, top), new Vector2(.31f, top + .12f), GetPresentation(i).EnglishName + "\n<size=14>" + (data != null ? data.CourseName : "NO DATA") + "</size>");
            courseListBackgrounds.Add(button.GetComponent<Image>());
            int capturedIndex = i;
            button.onClick.AddListener(() => SelectCourseByIndex(capturedIndex));
        }
    }

    private void BuildCourseDetail(Transform parent)
    {
        previewBackground = CreatePanel(parent, "CoursePreview", new Vector2(.38f, .55f), new Vector2(1f, .91f), new Color(.06f, .22f, .42f));
        CreateText(previewBackground.transform, "PreviewGrid", new Vector2(.08f, .10f), new Vector2(.92f, .90f), 30, "COURSE PREVIEW\n<size=16>FIXED RACE COURSE</size>");
        Image road = CreatePanel(previewBackground.transform, "PreviewRoad", new Vector2(.40f, 0f), new Vector2(.60f, 1f), new Color(.1f, .12f, .15f, .82f)).GetComponent<Image>();
        road.transform.SetAsFirstSibling();

        GameObject detail = CreatePanel(parent, "CourseDetail", new Vector2(.38f, .08f), new Vector2(1f, .50f), new Color(.04f, .07f, .12f, .95f));
        courseNameText = CreateText(detail.transform, "EnglishName", new Vector2(.06f, .70f), new Vector2(.73f, .91f), 34, string.Empty);
        courseNameText.alignment = TextAnchor.MiddleLeft;
        courseNameText.fontStyle = FontStyle.BoldAndItalic;
        japaneseNameText = CreateText(detail.transform, "JapaneseName", new Vector2(.06f, .62f), new Vector2(.72f, .72f), 17, string.Empty);
        japaneseNameText.alignment = TextAnchor.MiddleLeft;
        japaneseNameText.color = new Color(.65f, .7f, .78f);
        CreateText(detail.transform, "DifficultyLabel", new Vector2(.76f, .77f), new Vector2(.94f, .90f), 13, "DIFFICULTY");
        difficultyText = CreateText(detail.transform, "Difficulty", new Vector2(.76f, .64f), new Vector2(.94f, .80f), 24, string.Empty);
        difficultyText.color = new Color(1f, .8f, .2f);
        descriptionText = CreateText(detail.transform, "Description", new Vector2(.06f, .40f), new Vector2(.94f, .61f), 17, string.Empty);
        descriptionText.alignment = TextAnchor.UpperLeft;
        CreateInfoBox(detail.transform, "DISTANCE", out distanceText, new Vector2(.06f, .12f), new Vector2(.37f, .33f));
        CreateInfoBox(detail.transform, "COURSE RECORD", out recordText, new Vector2(.41f, .12f), new Vector2(.72f, .33f));
        Button entry = CreateButton(detail.transform, "Entry", new Vector2(.76f, .12f), new Vector2(.94f, .33f), "ENTRY  ▶");
        entry.onClick.AddListener(StartSelectedRace);
    }

    private void BuildResultUi()
    {
        resultContent = CreatePanel(lobbyPanel.transform, "RaceResult", new Vector2(.31f, .27f), new Vector2(.69f, .73f), new Color(.03f, .06f, .12f, .98f));
        resultText = CreateText(resultContent.transform, "ResultText", new Vector2(.05f, .08f), new Vector2(.95f, .92f), 28, string.Empty);
        resultText.fontStyle = FontStyle.Bold;
        resultContent.SetActive(false);
    }

    private static void CreateInfoBox(Transform parent, string label, out Text value, Vector2 min, Vector2 max)
    {
        GameObject box = CreatePanel(parent, label + "Box", min, max, new Color(.08f, .11f, .17f, 1f));
        Text labelText = CreateText(box.transform, "Label", new Vector2(.08f, .53f), new Vector2(.92f, .91f), 12, label);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = new Color(.55f, .63f, .73f);
        value = CreateText(box.transform, "Value", new Vector2(.08f, .08f), new Vector2(.92f, .60f), 23, string.Empty);
        value.alignment = TextAnchor.MiddleLeft;
        value.fontStyle = FontStyle.BoldAndItalic;
    }

    private static GameObject FindObjectIncludingInactive(string objectName)
    {
        foreach (GameObject gameObject in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (gameObject.name == objectName)
            {
                return gameObject;
            }
        }
        return null;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Stretch(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return gameObject;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string label)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);
        Stretch(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = gameObject.GetComponent<Image>();
        image.color = new Color(.08f, .12f, .19f, .9f);
        Button button = gameObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(.16f, .45f, .72f, 1f);
        colors.pressedColor = new Color(.05f, .25f, .42f, 1f);
        button.colors = colors;
        Text text = CreateText(gameObject.transform, "Text", Vector2.zero, Vector2.one, 20, label);
        text.fontStyle = FontStyle.Bold;
        return button;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, string value)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        Stretch(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Text text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.supportRichText = true;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static CoursePresentation GetPresentation(int index)
    {
        return index % 2 == 0
            ? new CoursePresentation("SUNSET CIRCUIT", "★☆☆", "4.2 km", "--'--''---", new Color(.05f, .25f, .48f), new Color(.07f, .45f, .72f, .92f))
            : new CoursePresentation("FOREST LOOP", "★★☆", "3.8 km", "--'--''---", new Color(.25f, .18f, .05f), new Color(.66f, .29f, .08f, .92f));
    }

    private readonly struct CoursePresentation
    {
        public readonly string EnglishName;
        public readonly string Difficulty;
        public readonly string Distance;
        public readonly string Record;
        public readonly Color PreviewColor;
        public readonly Color AccentColor;

        public CoursePresentation(string englishName, string difficulty, string distance, string record, Color previewColor, Color accentColor)
        {
            EnglishName = englishName;
            Difficulty = difficulty;
            Distance = distance;
            Record = record;
            PreviewColor = previewColor;
            AccentColor = accentColor;
        }
    }
}
