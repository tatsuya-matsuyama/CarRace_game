using UnityEngine;

/// <summary>
/// パーツ購入を受け付け、所持金とプレイヤーのインベントリを連携させるショップ管理クラスです。
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Tooltip("購入したパーツを追加するプレイヤー車のステータス管理コンポーネントです。")]
    [SerializeField] private PlayerCarStats playerCarStats;

    /// <summary>
    /// 指定されたパーツを購入します。
    /// パーツの重複所持と残高不足を確認してからゴールドを消費し、成功時だけインベントリへ追加します。
    /// </summary>
    public bool BuyPart(CarPartData part)
    {
        // 販売対象、所持金管理、購入先が揃っていない場合は購入処理を開始できません。
        if (part == null || GameManager.Instance == null || playerCarStats == null)
        {
            return false;
        }

        // すでに持っているパーツは購入せず、不要なゴールド消費を防ぎます。
        if (playerCarStats.HasPart(part))
        {
            return false;
        }

        // GameManagerが残高を確認し、足りなければfalseを返して所持金を変更しません。
        if (!GameManager.Instance.SpendGold(part.Price))
        {
            return false;
        }

        // 支払い成功後に、プレイヤーの所持パーツリストへ追加します。
        // AddOwnedPartが成功した時点で、購入処理は完了です。
        return playerCarStats.AddOwnedPart(part);
    }
}
