using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ショップ建物の入店判定と、ショップUIの開閉・購入操作を管理します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopInteractionController : MonoBehaviour
{
    [Header("ショップ設定")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private CarPartData partForSale;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text shopStatusText;

    private ArcadeCarController playerCarController;
    private bool playerInRange;
    private bool isShopOpen;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (isShopOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    /// <summary>
    /// ショップ画面を開き、車両入力を止めて購入情報を表示します。
    /// </summary>
    public void OpenShop()
    {
        if (!playerInRange || isShopOpen)
        {
            return;
        }

        isShopOpen = true;
        playerCarController?.SetControlEnabled(false);
        GameManager.Instance?.ChangeState(GameManager.GameState.Shop);

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        RefreshStatus("Eキーまたは閉じるボタンで街へ戻れます。");
    }

    /// <summary>
    /// ショップ画面を閉じ、プレイヤー車の操作を再開します。
    /// </summary>
    public void CloseShop()
    {
        if (!isShopOpen)
        {
            return;
        }

        isShopOpen = false;
        playerCarController?.SetControlEnabled(true);
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (interactionPrompt != null && playerInRange)
        {
            interactionPrompt.SetActive(true);
        }
    }

    /// <summary>
    /// 表示中のパーツを購入します。ShopManagerが残高確認とインベントリ追加を行います。
    /// </summary>
    public void BuyPart()
    {
        bool purchased = shopManager != null && shopManager.BuyPart(partForSale);
        RefreshStatus(purchased ? "購入しました！ PlayerCarStatsへ追加済みです。" : "購入できません。ゴールド不足または購入済みです。");
    }

    private void OnTriggerEnter(Collider other)
    {
        ArcadeCarController controller = other.GetComponent<ArcadeCarController>();
        if (controller == null)
        {
            return;
        }

        playerInRange = true;
        playerCarController = controller;
        if (!isShopOpen && interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ArcadeCarController>() == null)
        {
            return;
        }

        playerInRange = false;
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        // 入店中に押し出し・リスポーンなどで入口の外へ出た場合も、入力停止を残さず探索へ戻します。
        if (isShopOpen)
        {
            CloseShop();
        }
    }

    private void RefreshStatus(string message)
    {
        if (shopStatusText == null)
        {
            return;
        }

        int gold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
        string partName = partForSale != null ? partForSale.PartName : "パーツ未設定";
        int price = partForSale != null ? partForSale.Price : 0;
        shopStatusText.text = $"{partName}\n価格: {price} G\n所持金: {gold} G\n\n{message}";
    }
}
