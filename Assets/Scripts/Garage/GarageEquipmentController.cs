using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ガレージで所持パーツを選択・装備し、車両性能を棒グラフで確認する画面です。
/// タイヤ、エンジン、ステアリング、ミッションの4枠を扱います。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GarageEquipmentController : MonoBehaviour
{
    private static readonly CarPartType[] EquipmentTypes =
    {
        CarPartType.Tire,
        CarPartType.Engine,
        CarPartType.Steering,
        CarPartType.Transmission
    };

    private readonly Dictionary<CarPartType, int> selectedIndices = new Dictionary<CarPartType, int>();
    private readonly Dictionary<string, Image> statBars = new Dictionary<string, Image>();

    private ArcadeCarController playerController;
    private PlayerCarStats playerStats;
    private bool playerInRange;
    private bool isGarageOpen;
    private GameObject panel;
    private GameObject prompt;
    private Text detailsText;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        CreateUi();
        panel.SetActive(false);
        prompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (isGarageOpen)
        {
            CloseGarage();
        }
        else
        {
            OpenGarage();
        }
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
        playerStats = other.GetComponent<PlayerCarStats>();
        if (!isGarageOpen)
        {
            prompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ArcadeCarController>() == null)
        {
            return;
        }

        playerInRange = false;
        prompt.SetActive(false);
        if (isGarageOpen)
        {
            CloseGarage();
        }
    }

    /// <summary>ガレージ画面を開き、街中の走行操作を停止します。</summary>
    public void OpenGarage()
    {
        if (isGarageOpen || playerStats == null)
        {
            return;
        }

        isGarageOpen = true;
        playerController?.SetControlEnabled(false);
        GameManager.Instance?.ChangeState(GameManager.GameState.Garage);
        panel.SetActive(true);
        prompt.SetActive(false);
        RefreshUi();
    }

    /// <summary>ガレージ画面を閉じ、探索と車両操作を復帰します。</summary>
    public void CloseGarage()
    {
        isGarageOpen = false;
        panel.SetActive(false);
        playerController?.SetControlEnabled(true);
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);
        if (playerInRange)
        {
            prompt.SetActive(true);
        }
    }

    /// <summary>指定スロットの所持パーツを順に選択して装備します。</summary>
    public void EquipNext(CarPartType partType)
    {
        if (playerStats == null)
        {
            return;
        }

        List<CarPartData> candidates = playerStats.OwnedParts.Where(part => part.PartType == partType).ToList();
        if (candidates.Count == 0)
        {
            detailsText.text = $"{GetSlotLabel(partType)}の所持パーツがありません。\nショップで購入してください。";
            return;
        }

        int nextIndex = (selectedIndices.TryGetValue(partType, out int index) ? index + 1 : 0) % candidates.Count;
        selectedIndices[partType] = nextIndex;
        playerStats.EquipPart(candidates[nextIndex]);
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (playerStats == null)
        {
            return;
        }

        detailsText.text = "装備中パーツ\n" + string.Join("\n", EquipmentTypes.Select(type =>
        {
            CarPartData part = playerStats.GetEquippedPart(type);
            return $"{GetSlotLabel(type)}: {(part != null ? part.PartName : "ノーマル")}";
        })) + "\n\n各ボタンで所持パーツを切り替えます。";

        SetBar("Grip", playerStats.CurrentGrip / 2.5f);
        SetBar("Accel", playerStats.CurrentAcceleration / 40f);
        SetBar("Handling", playerStats.CurrentHandling / 240f);
        SetBar("Speed", playerStats.CurrentMaxSpeed / 40f);
    }

    private void SetBar(string key, float normalizedValue)
    {
        if (!statBars.TryGetValue(key, out Image bar))
        {
            return;
        }

        RectTransform rect = bar.rectTransform;
        rect.sizeDelta = new Vector2(Mathf.Lerp(5f, 260f, Mathf.Clamp01(normalizedValue)), rect.sizeDelta.y);
    }

    private void CreateUi()
    {
        GameObject canvasObject = new GameObject("GarageEquipmentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        prompt = CreateTextObject(canvasObject.transform, "GaragePrompt", new Vector2(.28f, .05f), new Vector2(.72f, .12f), 24, "ガレージ: Eキーで装備変更");

        panel = new GameObject("GarageEquipmentPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(.12f, .14f);
        panelRect.anchorMax = new Vector2(.88f, .86f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.025f, .045f, .1f, .97f);

        Text title = CreateTextObject(panel.transform, "Title", new Vector2(.05f, .88f), new Vector2(.95f, .98f), 34, "GARAGE  -  PARTS SETTING").GetComponent<Text>();
        title.color = new Color(1f, .84f, .2f);

        // 左側は装備一覧専用にし、右側の棒グラフと重ならない領域に固定します。
        detailsText = CreateTextObject(panel.transform, "EquipmentDetails", new Vector2(.06f, .48f), new Vector2(.50f, .82f), 20, string.Empty).GetComponent<Text>();
        detailsText.alignment = TextAnchor.UpperLeft;

        for (int i = 0; i < EquipmentTypes.Length; i++)
        {
            CarPartType type = EquipmentTypes[i];
            Button button = CreateButton(panel.transform, GetSlotLabel(type) + "を切替", new Vector2(.06f, .23f - i * .09f), new Vector2(.50f, .30f - i * .09f));
            CarPartType capturedType = type;
            button.onClick.AddListener(() => EquipNext(capturedType));
        }

        CreateStatRow("Grip", "GRIP", .74f, new Color(.2f, .85f, .35f));
        CreateStatRow("Accel", "ACCEL", .61f, new Color(1f, .46f, .12f));
        CreateStatRow("Handling", "STEERING", .48f, new Color(.22f, .6f, 1f));
        CreateStatRow("Speed", "SPEED", .35f, new Color(1f, .86f, .18f));

        Button closeButton = CreateButton(panel.transform, "街へ戻る", new Vector2(.60f, .18f), new Vector2(.92f, .27f));
        closeButton.onClick.AddListener(CloseGarage);
    }

    private void CreateStatRow(string key, string label, float y, Color color)
    {
        CreateTextObject(panel.transform, label, new Vector2(.57f, y), new Vector2(.92f, y + .06f), 18, label);
        GameObject background = new GameObject(key + "BarBackground", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(panel.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(.57f, y - .05f);
        backgroundRect.anchorMax = new Vector2(.92f, y - .01f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(.1f, .12f, .16f, 1f);

        GameObject bar = new GameObject(key + "Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(background.transform, false);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, .5f);
        barRect.anchorMax = new Vector2(0f, .5f);
        barRect.pivot = new Vector2(0f, .5f);
        barRect.anchoredPosition = new Vector2(3f, 0f);
        barRect.sizeDelta = new Vector2(5f, 18f);
        Image image = bar.GetComponent<Image>();
        image.color = color;
        statBars[key] = image;
    }

    private static GameObject CreateTextObject(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, int fontSize, string textValue)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = textValue;
        return textObject;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        buttonObject.GetComponent<Image>().color = new Color(.1f, .38f, .78f, 1f);
        GameObject textObject = CreateTextObject(buttonObject.transform, "Text", Vector2.zero, Vector2.one, 19, label);
        return buttonObject.GetComponent<Button>();
    }

    private static string GetSlotLabel(CarPartType type)
    {
        return type switch
        {
            CarPartType.Tire => "タイヤ（グリップ）",
            CarPartType.Engine => "エンジン（加速）",
            CarPartType.Steering => "ステアリング（旋回）",
            CarPartType.Transmission => "ミッション（最高速）",
            _ => type.ToString()
        };
    }
}
