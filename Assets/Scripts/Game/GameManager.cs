using System;
using UnityEngine;

/// <summary>
/// ゲーム全体の状態とプレイヤーの所持金を管理する司令塔です。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Explore,
        Dialogue,
        Shop
    }

    [Header("ゲーム状態")]
    [SerializeField] private GameState currentState = GameState.Explore;

    [Header("所持金")]
    [Tooltip("ゲーム開始時の所持金です。")]
    [Min(0)]
    [SerializeField] private int initialGold;

    private int gold;

    /// <summary>所持金が変化した直後に、最新の所持金を通知します。</summary>
    public event Action<int> OnGoldChanged;

    /// <summary>ゲーム状態が変化した直後に、最新の状態を通知します。</summary>
    public event Action<GameState> OnGameStateChanged;

    public int Gold => gold;
    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gold = initialGold;
    }

    /// <summary>
    /// ゲームの状態を変更し、UIや入力制御などの購読先へ通知します。
    /// </summary>
    public void ChangeState(GameState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState = nextState;
        OnGameStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// クエスト報酬などでゴールドを加算します。
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// ショップ購入などでゴールドを消費します。残高不足なら何も変更せずfalseを返します。
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount < 0 || gold < amount)
        {
            return false;
        }

        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }
}
