using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 街に置くレース会場・ペイント屋・ガレージの共通入場処理です。
/// プレイヤーが入口のTrigger内でEキーを押すと施設画面を開き、車両操作を停止します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class FacilityInteractionController : MonoBehaviour
{
    public enum FacilityType
    {
        RaceVenue,
        PaintShop,
        Garage
    }

    [Header("施設設定")]
    [SerializeField] private FacilityType facilityType;
    [SerializeField] private string facilityName = "施設";
    [TextArea]
    [SerializeField] private string facilityDescription;

    [Header("UI参照")]
    [SerializeField] private GameObject facilityPanel;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text facilityText;

    [Header("ペイント屋設定")]
    [SerializeField] private Color[] paintColors =
    {
        new Color(0.96f, 0.9f, 0.72f),
        new Color(0.15f, 0.35f, 0.8f),
        new Color(0.82f, 0.08f, 0.06f),
        new Color(0.12f, 0.55f, 0.22f)
    };
    [SerializeField] private Color stripeColor = new Color(0.95f, 0.95f, 0.95f);

    private ArcadeCarController playerController;
    private PlayerCarStats playerStats;
    private ChoroQCarVisual playerVisual;
    private bool playerInRange;
    private bool isOpen;
    private int paintIndex;

    private void Awake()
    {
        // 入口判定は物理衝突ではなく、接近だけを検知するTriggerとして使います。
        GetComponent<Collider>().isTrigger = true;
        facilityPanel?.SetActive(false);
        interactionPrompt?.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (!isOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenFacility();
            }

            return;
        }

        // ペイント屋ではA/Dキーで色を選び、その場で車体へ反映します。
        if (facilityType == FacilityType.PaintShop)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ChangePaint(-1);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangePaint(1);
            }
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseFacility();
        }
    }

    /// <summary>
    /// 施設画面を開き、車の操作を止めて施設種別に応じた内容を表示します。
    /// </summary>
    public void OpenFacility()
    {
        if (isOpen || !playerInRange)
        {
            return;
        }

        isOpen = true;
        playerController?.SetControlEnabled(false);
        GameManager.Instance?.ChangeState(GetGameState());
        facilityPanel?.SetActive(true);
        interactionPrompt?.SetActive(false);
        RefreshFacilityText();
    }

    /// <summary>
    /// 施設画面を閉じて探索状態と車の操作を復帰します。
    /// </summary>
    public void CloseFacility()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        facilityPanel?.SetActive(false);
        playerController?.SetControlEnabled(true);
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);

        if (playerInRange)
        {
            interactionPrompt?.SetActive(true);
        }
    }

    private void ChangePaint(int direction)
    {
        if (paintColors == null || paintColors.Length == 0)
        {
            return;
        }

        paintIndex = (paintIndex + direction + paintColors.Length) % paintColors.Length;
        playerVisual?.ApplyPaint(paintColors[paintIndex], stripeColor);
        RefreshFacilityText();
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
        playerVisual = other.GetComponent<ChoroQCarVisual>();
        if (!isOpen)
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

        // 入口の外へ出たのに操作停止だけ残らないよう、施設画面も必ず閉じます。
        // Triggerの設定ミスやリスポーン時にも探索状態へ安全に復帰できます。
        if (isOpen)
        {
            CloseFacility();
        }
    }

    private GameManager.GameState GetGameState()
    {
        return facilityType switch
        {
            FacilityType.RaceVenue => GameManager.GameState.Race,
            FacilityType.PaintShop => GameManager.GameState.Paint,
            _ => GameManager.GameState.Garage
        };
    }

    private void RefreshFacilityText()
    {
        if (facilityText == null)
        {
            return;
        }

        string body = facilityType switch
        {
            FacilityType.RaceVenue => "レースの準備中です。\n今後ここからレースイベントを受注できます。",
            FacilityType.PaintShop => $"カラー {paintIndex + 1}/{paintColors.Length}\nA / Dでボディカラーを変更できます。",
            FacilityType.Garage => GetGarageStatus(),
            _ => string.Empty
        };

        facilityText.text = $"{facilityName}\n\n{facilityDescription}\n\n{body}\n\n[E] 街へ戻る";
    }

    private string GetGarageStatus()
    {
        if (playerStats == null)
        {
            return "プレイヤー車を検出できません。";
        }

        return $"最高速: {playerStats.CurrentMaxSpeed:0.0}\n加速: {playerStats.CurrentAcceleration:0.0}\n旋回: {playerStats.CurrentHandling:0.0}\n所持パーツ: {playerStats.OwnedParts.Count}";
    }
}
