using UnityEngine;

/// <summary>
/// プレイヤー車にアタッチして、基本性能と装備パーツによる補正を管理します。
/// </summary>
public class PlayerCarStats : MonoBehaviour
{
    [Header("装備パーツ")]
    [Tooltip("現在装備中のエンジンです。")]
    [SerializeField] private CarPartData equippedEngine;

    [Tooltip("現在装備中のタイヤです。")]
    [SerializeField] private CarPartData equippedTire;

    [Tooltip("現在装備中のシャーシです。")]
    [SerializeField] private CarPartData equippedChassis;

    [Header("基本ステータス")]
    [Tooltip("パーツ未装備時の最高速度です。")]
    [SerializeField] private float baseMaxSpeed = 20f;

    [Tooltip("パーツ未装備時の加速度です。")]
    [SerializeField] private float baseAcceleration = 18f;

    [Tooltip("パーツ未装備時の旋回性能です。")]
    [SerializeField] private float baseHandling = 120f;

    [Header("現在のステータス（実行時表示）")]
    [SerializeField] private float currentMaxSpeed;
    [SerializeField] private float currentAcceleration;
    [SerializeField] private float currentHandling;

    public CarPartData EquippedEngine => equippedEngine;
    public CarPartData EquippedTire => equippedTire;
    public CarPartData EquippedChassis => equippedChassis;
    public float CurrentMaxSpeed => currentMaxSpeed;
    public float CurrentAcceleration => currentAcceleration;
    public float CurrentHandling => currentHandling;

    private void Awake()
    {
        CalculateStats();
    }

    /// <summary>
    /// 基本ステータスへ、装備中の全パーツの補正値を合計して最終ステータスを計算します。
    /// 装備変更後にも呼び出すことで、最新の性能を反映できます。
    /// </summary>
    public void CalculateStats()
    {
        currentMaxSpeed = baseMaxSpeed;
        currentAcceleration = baseAcceleration;
        currentHandling = baseHandling;

        AddPartBonus(equippedEngine);
        AddPartBonus(equippedTire);
        AddPartBonus(equippedChassis);
    }

    /// <summary>
    /// 指定されたパーツの3種類の補正値を現在ステータスへ加算します。
    /// 未装備（null）のパーツは補正なしとして扱います。
    /// </summary>
    private void AddPartBonus(CarPartData part)
    {
        if (part == null)
        {
            return;
        }

        currentMaxSpeed += part.MaxSpeedBonus;
        currentAcceleration += part.AccelerationBonus;
        currentHandling += part.HandlingBonus;
    }

    /// <summary>
    /// エンジンを装備し直し、最終ステータスを再計算します。
    /// </summary>
    public void EquipEngine(CarPartData engine)
    {
        equippedEngine = engine;
        CalculateStats();
    }

    /// <summary>
    /// タイヤを装備し直し、最終ステータスを再計算します。
    /// </summary>
    public void EquipTire(CarPartData tire)
    {
        equippedTire = tire;
        CalculateStats();
    }

    /// <summary>
    /// シャーシを装備し直し、最終ステータスを再計算します。
    /// </summary>
    public void EquipChassis(CarPartData chassis)
    {
        equippedChassis = chassis;
        CalculateStats();
    }
}
