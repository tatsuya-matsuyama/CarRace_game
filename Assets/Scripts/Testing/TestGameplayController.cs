using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// テストシーン上で、クエスト報酬とショップ購入の一連の流れを確認するための補助コンポーネントです。
/// 実ゲームのUI実装時には、各ボタンを専用UIへ置き換えます。
/// </summary>
public class TestGameplayController : MonoBehaviour
{
    [Header("テストデータ")]
    [SerializeField] private QuestData testQuest;
    [SerializeField] private CarPartData testPart;

    [Header("連携先")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private PlayerCarStats playerCarStats;
    [SerializeField] private Text statusText;

    private void Start()
    {
        // ゴールドが変化するたびに、テストUIへ最新の状態を反映します。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged += HandleGoldChanged;
        }

        RefreshStatus();
    }

    private void OnDestroy()
    {
        // テストシーンを閉じるときにイベント購読を解除し、不要な参照を残しません。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= HandleGoldChanged;
        }
    }

    /// <summary>
    /// テストクエストを受注します。受注済み・達成済みなら状態表示だけ更新します。
    /// </summary>
    public void AcceptTestQuest()
    {
        bool accepted = QuestManager.Instance != null && QuestManager.Instance.AcceptQuest(testQuest);
        SetMessage(accepted ? "クエストを受注しました。次に「クエスト達成」を押してください。" : "このクエストは受注できません。");
    }

    /// <summary>
    /// 受注済みテストクエストを達成し、GameManager経由で報酬ゴールドを受け取ります。
    /// </summary>
    public void CompleteTestQuest()
    {
        bool completed = QuestManager.Instance != null && QuestManager.Instance.CompleteQuest(testQuest);
        SetMessage(completed ? "クエスト達成！ 報酬ゴールドを獲得しました。" : "先にクエストを受注してください。");
    }

    /// <summary>
    /// テスト用パーツを購入します。残高が不足している場合は購入に失敗します。
    /// </summary>
    public void BuyTestPart()
    {
        bool purchased = shopManager != null && shopManager.BuyPart(testPart);
        SetMessage(purchased ? "パーツ購入成功！ PlayerCarStatsの所持パーツへ追加されました。" : "購入できません。ゴールド不足または購入済みです。");
    }

    /// <summary>
    /// 現在の所持金と購入済みパーツ数をUIへ表示します。
    /// </summary>
    private void RefreshStatus()
    {
        if (statusText == null)
        {
            return;
        }

        int gold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
        int ownedParts = playerCarStats != null ? playerCarStats.OwnedParts.Count : 0;
        statusText.text = $"所持金: {gold} G\n所持パーツ: {ownedParts}\n\n"
            + "1. クエスト受注\n2. クエスト達成\n3. パーツ購入";
    }

    /// <summary>
    /// GameManagerの所持金変更イベントを受け取り、表示を更新します。
    /// </summary>
    private void HandleGoldChanged(int _)
    {
        RefreshStatus();
    }

    private void SetMessage(string message)
    {
        RefreshStatus();
        if (statusText != null)
        {
            statusText.text += "\n\n" + message;
        }
    }
}
