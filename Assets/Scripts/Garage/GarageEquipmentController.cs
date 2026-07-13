using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ガレージでパーツを選び、装備前後の車両性能を比較してから装備する画面です。
/// 画面は実行時に生成するため、テストシーンへ手作業でuGUIを配置する必要はありません。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GarageEquipmentController : MonoBehaviour
{
    private static readonly CarPartType[] EquipmentTypes =
    {
        CarPartType.Engine,
        CarPartType.Tire,
        CarPartType.Steering,
        CarPartType.Transmission
    };

    private readonly Dictionary<CarPartType, int> selectedIndices = new Dictionary<CarPartType, int>();
    private readonly Dictionary<string, StatBar> statBars = new Dictionary<string, StatBar>();

    private ArcadeCarController playerController;
    private PlayerCarStats playerStats;
    private bool playerInRange;
    private bool isGarageOpen;
    private CarPartType activeType = CarPartType.Engine;
    private GameObject panel;
    private GameObject prompt;
    private Text partNameText;
    private Text partDescriptionText;
    private Text selectionText;
    private Text activeCategoryText;
    private RawImage previewImage;
    private readonly Dictionary<CarPartType, Image> categoryBackgrounds = new Dictionary<CarPartType, Image>();
    private RenderTexture previewTexture;
    private GameObject previewRoot;
    private Camera previewCamera;

    private sealed class StatBar
    {
        public Image Current;
        public Image Preview;
        public Text Value;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        CreateUi();
        panel.SetActive(false);
        prompt.SetActive(false);
    }

    private void OnDestroy()
    {
        // Camera.targetTexture を参照したまま RenderTexture を解放すると、
        // Unity が "Releasing render texture that is set as Camera.targetTexture" を出します。
        // 先にカメラ側の参照を外してから、生成順と逆順で後始末します。
        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
            Destroy(previewCamera.gameObject);
        }

        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }

        if (previewRoot != null)
        {
            Destroy(previewRoot);
        }
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
        SelectCategory(activeType);
        CreatePreviewCar();
        RefreshUi();
    }

    /// <summary>画面を閉じ、探索と車両操作を復帰します。</summary>
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

    /// <summary>左側のカテゴリを選び、該当する所持パーツを比較対象にします。</summary>
    public void SelectCategory(CarPartType type)
    {
        activeType = type;
        List<CarPartData> candidates = GetCandidates(type);
        CarPartData equipped = playerStats != null ? playerStats.GetEquippedPart(type) : null;
        int equippedIndex = candidates.IndexOf(equipped);
        selectedIndices[type] = equippedIndex >= 0 ? equippedIndex : 0;
        RefreshUi();
    }

    /// <summary>選択中カテゴリの候補を前後へ切り替えます。ここではまだ装備を変更しません。</summary>
    public void ChangeSelection(int direction)
    {
        List<CarPartData> candidates = GetCandidates(activeType);
        if (candidates.Count == 0)
        {
            return;
        }

        int current = selectedIndices.TryGetValue(activeType, out int index) ? index : 0;
        selectedIndices[activeType] = (current + direction + candidates.Count) % candidates.Count;
        RefreshUi();
    }

    /// <summary>プレビュー中のパーツを確定装備し、車両の最終ステータスを再計算します。</summary>
    public void EquipSelectedPart()
    {
        CarPartData selectedPart = GetSelectedPart();
        if (selectedPart == null || playerStats == null)
        {
            return;
        }

        playerStats.EquipPart(selectedPart);
        RefreshUi();
    }

    private List<CarPartData> GetCandidates(CarPartType type)
    {
        return playerStats == null
            ? new List<CarPartData>()
            : playerStats.OwnedParts.Where(part => part != null && part.PartType == type).ToList();
    }

    private CarPartData GetSelectedPart()
    {
        List<CarPartData> candidates = GetCandidates(activeType);
        if (candidates.Count == 0)
        {
            return null;
        }

        int index = selectedIndices.TryGetValue(activeType, out int value) ? value : 0;
        index = Mathf.Clamp(index, 0, candidates.Count - 1);
        return candidates[index];
    }

    private void RefreshUi()
    {
        if (playerStats == null || partNameText == null)
        {
            return;
        }

        CarPartData selectedPart = GetSelectedPart();
        CarPartData equippedPart = playerStats.GetEquippedPart(activeType);
        List<CarPartData> candidates = GetCandidates(activeType);
        int selectedIndex = selectedIndices.TryGetValue(activeType, out int index) ? index : 0;

        activeCategoryText.text = GetSlotLabel(activeType).ToUpperInvariant();
        if (selectedPart == null)
        {
            partNameText.text = "NO PARTS OWNED";
            partDescriptionText.text = "このカテゴリの所持パーツはありません。ショップでパーツを購入してください。";
            selectionText.text = "0 / 0";
        }
        else
        {
            partNameText.text = selectedPart.PartName;
            partDescriptionText.text = string.IsNullOrWhiteSpace(selectedPart.Description)
                ? "性能を調整するカスタムパーツです。"
                : selectedPart.Description;
            selectionText.text = $"{selectedIndex + 1} / {candidates.Count}   {(selectedPart == equippedPart ? "EQUIPPED" : "PREVIEW")}";
        }

        foreach (KeyValuePair<CarPartType, Image> pair in categoryBackgrounds)
        {
            pair.Value.color = pair.Key == activeType
                ? new Color(.02f, .42f, .57f, .92f)
                : new Color(.08f, .12f, .19f, .88f);
        }

        // 選択パーツだけを仮に差し替えて比較値を作るため、未確定の選択では実際の車両性能を変更しません。
        float speed = playerStats.CurrentMaxSpeed + GetDifference(selectedPart, equippedPart, part => part.MaxSpeedBonus);
        float accel = playerStats.CurrentAcceleration + GetDifference(selectedPart, equippedPart, part => part.AccelerationBonus);
        float grip = playerStats.CurrentGrip + GetDifference(selectedPart, equippedPart, part => part.GripBonus);
        float handling = playerStats.CurrentHandling + GetDifference(selectedPart, equippedPart, part => part.HandlingBonus);

        SetBar("Speed", playerStats.CurrentMaxSpeed, speed, 40f);
        SetBar("Accel", playerStats.CurrentAcceleration, accel, 40f);
        SetBar("Grip", playerStats.CurrentGrip, grip, 2.5f);
        SetBar("Handling", playerStats.CurrentHandling, handling, 240f);
    }

    private static float GetDifference(CarPartData selected, CarPartData equipped, System.Func<CarPartData, float> selector)
    {
        return (selected != null ? selector(selected) : 0f) - (equipped != null ? selector(equipped) : 0f);
    }

    private void SetBar(string key, float current, float preview, float maximum)
    {
        if (!statBars.TryGetValue(key, out StatBar bar))
        {
            return;
        }

        SetBarWidth(bar.Current, current / maximum);
        SetBarWidth(bar.Preview, preview / maximum);
        bar.Preview.color = preview > current + .01f ? new Color(.2f, 1f, .45f, .75f) : new Color(1f, 1f, 1f, 0f);
        bar.Value.text = preview > current + .01f
            ? $"{Mathf.RoundToInt(current)}  <color=#55FF7A>▲ {Mathf.RoundToInt(preview)}</color>"
            : Mathf.RoundToInt(current).ToString();
    }

    private static void SetBarWidth(Image bar, float value)
    {
        bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private void CreateUi()
    {
        GameObject canvasObject = new GameObject("GarageEquipmentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        prompt = CreateTextObject(canvasObject.transform, "GaragePrompt", new Vector2(.30f, .055f), new Vector2(.70f, .11f), 24, "ガレージ: Eキーでカスタマイズ");

        panel = CreatePanel(canvasObject.transform, "GarageEquipmentPanel", Vector2.zero, Vector2.one, new Color(.015f, .03f, .07f, .96f));
        CreatePanel(panel.transform, "GarageGlow", new Vector2(.015f, .02f), new Vector2(.985f, .98f), new Color(.04f, .1f, .17f, .78f));

        Text title = CreateTextObject(panel.transform, "Title", new Vector2(.06f, .84f), new Vector2(.42f, .94f), 48, "GARAGE").GetComponent<Text>();
        title.alignment = TextAnchor.MiddleLeft;
        title.fontStyle = FontStyle.BoldAndItalic;
        title.color = new Color(.25f, .9f, 1f);
        Text subtitle = CreateTextObject(panel.transform, "Subtitle", new Vector2(.06f, .805f), new Vector2(.48f, .845f), 17, "CUSTOMIZE YOUR MACHINE").GetComponent<Text>();
        subtitle.alignment = TextAnchor.MiddleLeft;
        subtitle.color = new Color(.3f, .8f, .95f);

        for (int i = 0; i < EquipmentTypes.Length; i++)
        {
            CarPartType type = EquipmentTypes[i];
            float top = .70f - i * .115f;
            Button category = CreateButton(panel.transform, "Category_" + type, new Vector2(.06f, top), new Vector2(.30f, top + .095f), GetSlotLabel(type) + "\n<size=13>" + GetSlotDescription(type) + "</size>");
            Image background = category.GetComponent<Image>();
            categoryBackgrounds[type] = background;
            CarPartType captured = type;
            category.onClick.AddListener(() => SelectCategory(captured));
        }

        Button back = CreateButton(panel.transform, "Back", new Vector2(.06f, .12f), new Vector2(.22f, .18f), "←  街へ戻る");
        back.onClick.AddListener(CloseGarage);

        GameObject previewFrame = CreatePanel(panel.transform, "CarPreviewFrame", new Vector2(.34f, .18f), new Vector2(.64f, .80f), new Color(.01f, .04f, .09f, .9f));
        previewImage = new GameObject("CarPreview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        previewImage.transform.SetParent(previewFrame.transform, false);
        Stretch(previewImage.rectTransform, new Vector2(.03f, .07f), new Vector2(.97f, .93f));
        CreateTextObject(previewFrame.transform, "PreviewLabel", new Vector2(.08f, .90f), new Vector2(.92f, .98f), 15, "VEHICLE PREVIEW");

        GameObject statusPanel = CreatePanel(panel.transform, "MachineStatus", new Vector2(.68f, .46f), new Vector2(.94f, .80f), new Color(.025f, .055f, .11f, .93f));
        Text statusTitle = CreateTextObject(statusPanel.transform, "StatusTitle", new Vector2(.08f, .84f), new Vector2(.92f, .96f), 24, "MACHINE STATUS").GetComponent<Text>();
        statusTitle.alignment = TextAnchor.MiddleLeft;
        statusTitle.fontStyle = FontStyle.BoldAndItalic;
        CreateTextObject(statusPanel.transform, "Rank", new Vector2(.62f, .84f), new Vector2(.92f, .96f), 14, "RANK: C");
        CreateStatRow(statusPanel.transform, "Speed", "SPEED  最高速", .64f, new Color(.2f, .55f, 1f));
        CreateStatRow(statusPanel.transform, "Accel", "ACCELERATION  加速", .46f, new Color(1f, .45f, .12f));
        CreateStatRow(statusPanel.transform, "Grip", "GRIP  グリップ", .28f, new Color(.18f, .8f, .42f));
        CreateStatRow(statusPanel.transform, "Handling", "STEERING  旋回", .10f, new Color(1f, .82f, .18f));

        GameObject partPanel = CreatePanel(panel.transform, "PartDetail", new Vector2(.68f, .18f), new Vector2(.94f, .41f), new Color(.05f, .09f, .15f, .93f));
        activeCategoryText = CreateTextObject(partPanel.transform, "ActiveCategory", new Vector2(.08f, .78f), new Vector2(.92f, .94f), 15, string.Empty).GetComponent<Text>();
        activeCategoryText.alignment = TextAnchor.MiddleLeft;
        activeCategoryText.color = new Color(.25f, .85f, 1f);
        partNameText = CreateTextObject(partPanel.transform, "PartName", new Vector2(.08f, .57f), new Vector2(.92f, .80f), 25, string.Empty).GetComponent<Text>();
        partNameText.alignment = TextAnchor.MiddleLeft;
        partNameText.fontStyle = FontStyle.BoldAndItalic;
        partDescriptionText = CreateTextObject(partPanel.transform, "PartDescription", new Vector2(.08f, .23f), new Vector2(.92f, .58f), 15, string.Empty).GetComponent<Text>();
        partDescriptionText.alignment = TextAnchor.UpperLeft;
        selectionText = CreateTextObject(partPanel.transform, "Selection", new Vector2(.08f, .06f), new Vector2(.92f, .22f), 14, string.Empty).GetComponent<Text>();
        selectionText.alignment = TextAnchor.MiddleLeft;
        selectionText.color = new Color(.55f, .75f, .85f);

        Button previous = CreateButton(panel.transform, "PreviousPart", new Vector2(.35f, .10f), new Vector2(.43f, .16f), "<");
        previous.onClick.AddListener(() => ChangeSelection(-1));
        Button equip = CreateButton(panel.transform, "EquipPart", new Vector2(.44f, .10f), new Vector2(.56f, .16f), "装備する");
        equip.onClick.AddListener(EquipSelectedPart);
        Button next = CreateButton(panel.transform, "NextPart", new Vector2(.57f, .10f), new Vector2(.65f, .16f), ">");
        next.onClick.AddListener(() => ChangeSelection(1));
    }

    private void CreateStatRow(Transform parent, string key, string label, float y, Color color)
    {
        Text labelText = CreateTextObject(parent, key + "Label", new Vector2(.08f, y + .08f), new Vector2(.92f, y + .16f), 14, label).GetComponent<Text>();
        labelText.alignment = TextAnchor.MiddleLeft;
        Text value = CreateTextObject(parent, key + "Value", new Vector2(.58f, y + .08f), new Vector2(.92f, y + .16f), 14, "0").GetComponent<Text>();
        value.alignment = TextAnchor.MiddleRight;
        GameObject background = CreatePanel(parent, key + "Background", new Vector2(.08f, y), new Vector2(.92f, y + .06f), new Color(.01f, .02f, .04f, 1f));
        Image current = CreateFill(background.transform, key + "Current", color);
        Image preview = CreateFill(background.transform, key + "Preview", Color.clear);
        statBars[key] = new StatBar { Current = current, Preview = preview, Value = value };
    }

    private static Image CreateFill(Transform parent, string name, Color color)
    {
        GameObject fill = new GameObject(name, typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(parent, false);
        RectTransform rect = fill.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(.01f, 1f);
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);
        Image image = fill.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void CreatePreviewCar()
    {
        if (previewCamera != null || playerController == null)
        {
            return;
        }

        Transform sourceVisual = playerController.transform.Find("ChoroQVisual");
        if (sourceVisual == null)
        {
            return;
        }

        const int previewLayer = 29;
        previewRoot = new GameObject("GaragePreviewCar");
        previewRoot.hideFlags = HideFlags.DontSave;
        GameObject previewCar = Instantiate(sourceVisual.gameObject, previewRoot.transform);
        previewCar.transform.localPosition = new Vector3(0f, -.35f, 0f);
        previewCar.transform.localRotation = Quaternion.Euler(0f, 150f, 0f);
        previewCar.transform.localScale = Vector3.one * 1.65f;
        SetLayerRecursively(previewCar, previewLayer);

        GameObject cameraObject = new GameObject("GaragePreviewCamera", typeof(Camera));
        cameraObject.hideFlags = HideFlags.DontSave;
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(.015f, .04f, .08f);
        previewCamera.fieldOfView = 30f;
        previewCamera.transform.position = new Vector3(3.4f, 1.8f, -4.2f);
        previewCamera.transform.LookAt(previewRoot.transform.position + new Vector3(0f, .1f, 0f));
        previewTexture = new RenderTexture(768, 512, 16, RenderTextureFormat.ARGB32);
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;
        previewImage.texture = previewTexture;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        Stretch(panelObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panelObject;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Stretch(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(.08f, .12f, .19f, .88f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(.08f, .55f, .75f, 1f);
        colors.pressedColor = new Color(.02f, .32f, .48f, 1f);
        button.colors = colors;
        Text text = CreateTextObject(buttonObject.transform, "Text", Vector2.zero, Vector2.one, 19, label).GetComponent<Text>();
        text.fontStyle = FontStyle.Bold;
        text.supportRichText = true;
        return button;
    }

    private static GameObject CreateTextObject(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, string value)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Stretch(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.supportRichText = true;
        text.text = value;
        text.raycastTarget = false;
        return textObject;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string GetSlotLabel(CarPartType type)
    {
        return type switch
        {
            CarPartType.Engine => "エンジン",
            CarPartType.Tire => "タイヤ",
            CarPartType.Steering => "ステアリング",
            CarPartType.Transmission => "ミッション",
            _ => type.ToString()
        };
    }

    private static string GetSlotDescription(CarPartType type)
    {
        return type switch
        {
            CarPartType.Engine => "加速・最高速",
            CarPartType.Tire => "グリップ性能",
            CarPartType.Steering => "旋回性能",
            CarPartType.Transmission => "最高速度",
            _ => string.Empty
        };
    }
}
