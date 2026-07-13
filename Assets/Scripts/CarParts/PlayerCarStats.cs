using System.Collections.Generic;
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

    [Tooltip("現在装備中のマフラーです。")]
    [SerializeField] private CarPartData equippedMuffler;

    [Tooltip("現在装備中のボディです。")]
    [SerializeField] private CarPartData equippedBody;

    [Tooltip("現在装備中のステアリングです。旋回性能へ主に影響します。")]
    [SerializeField] private CarPartData equippedSteering;

    [Tooltip("現在装備中のミッションです。最高速度へ主に影響します。")]
    [SerializeField] private CarPartData equippedTransmission;

    [Header("所持パーツ")]
    [Tooltip("ショップで購入済みのパーツ一覧です。")]
    [SerializeField] private List<CarPartData> ownedParts = new List<CarPartData>();

    [Header("基本ステータス")]
    [Tooltip("パーツ未装備時の最高速度です。")]
    [SerializeField] private float baseMaxSpeed = 20f;

    [Tooltip("パーツ未装備時の加速度です。")]
    [SerializeField] private float baseAcceleration = 18f;

    [Tooltip("パーツ未装備時の旋回性能です。")]
    [SerializeField] private float baseHandling = 120f;

    [Tooltip("パーツ未装備時のタイヤグリップです。")]
    [SerializeField] private float baseGrip = 1f;

    [Tooltip("パーツ未装備時のドリフトしやすさです。")]
    [SerializeField] private float baseDrift = 0.5f;

    [Header("現在のステータス（実行時表示）")]
    [SerializeField] private float currentMaxSpeed;
    [SerializeField] private float currentAcceleration;
    [SerializeField] private float currentHandling;
    [SerializeField] private float currentGrip;
    [SerializeField] private float currentDrift;

    public CarPartData EquippedEngine => equippedEngine;
    public CarPartData EquippedTire => equippedTire;
    public CarPartData EquippedChassis => equippedChassis;
    public CarPartData EquippedMuffler => equippedMuffler;
    public CarPartData EquippedBody => equippedBody;
    public CarPartData EquippedSteering => equippedSteering;
    public CarPartData EquippedTransmission => equippedTransmission;
    public float BaseMaxSpeed => baseMaxSpeed;
    public float BaseAcceleration => baseAcceleration;
    public float BaseHandling => baseHandling;
    public float BaseGrip => baseGrip;
    public float CurrentMaxSpeed => currentMaxSpeed;
    public float CurrentAcceleration => currentAcceleration;
    public float CurrentHandling => currentHandling;
    public float CurrentGrip => currentGrip;
    public float CurrentDrift => currentDrift;
    public IReadOnlyList<CarPartData> OwnedParts => ownedParts;

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
        currentGrip = baseGrip;
        currentDrift = baseDrift;

        AddPartBonus(equippedEngine);
        AddPartBonus(equippedTire);
        AddPartBonus(equippedChassis);
        AddPartBonus(equippedMuffler);
        AddPartBonus(equippedBody);
        AddPartBonus(equippedSteering);
        AddPartBonus(equippedTransmission);
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
        currentGrip += part.GripBonus;
        currentDrift += part.DriftBonus;
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

    /// <summary>
    /// マフラーを装備し直し、加速や排気特性に関わる最終ステータスを再計算します。
    /// </summary>
    public void EquipMuffler(CarPartData muffler)
    {
        equippedMuffler = muffler;
        CalculateStats();
    }

    /// <summary>
    /// ボディを装備し直し、グリップやドリフト特性を含む最終ステータスを再計算します。
    /// </summary>
    public void EquipBody(CarPartData body)
    {
        equippedBody = body;
        CalculateStats();
    }

    /// <summary>
    /// ステアリングを装備し直し、旋回性能を含む最終ステータスを更新します。
    /// </summary>
    public void EquipSteering(CarPartData steering)
    {
        equippedSteering = steering;
        CalculateStats();
    }

    /// <summary>
    /// ミッションを装備し直し、最高速度を含む最終ステータスを更新します。
    /// </summary>
    public void EquipTransmission(CarPartData transmission)
    {
        equippedTransmission = transmission;
        CalculateStats();
    }

    /// <summary>
    /// ガレージUIからパーツ種別に応じた装備スロットへ反映します。
    /// 古いChassis/Muffler/Bodyも保持し、既存データを壊さず利用できます。
    /// </summary>
    public void EquipPart(CarPartData part)
    {
        if (part == null)
        {
            return;
        }

        switch (part.PartType)
        {
            case CarPartType.Engine: EquipEngine(part); break;
            case CarPartType.Tire: EquipTire(part); break;
            case CarPartType.Steering: EquipSteering(part); break;
            case CarPartType.Transmission: EquipTransmission(part); break;
            case CarPartType.Chassis: EquipChassis(part); break;
            case CarPartType.Muffler: EquipMuffler(part); break;
            case CarPartType.Body: EquipBody(part); break;
        }
    }

    /// <summary>指定種類で現在装備中のパーツを返します。</summary>
    public CarPartData GetEquippedPart(CarPartType partType)
    {
        return partType switch
        {
            CarPartType.Engine => equippedEngine,
            CarPartType.Tire => equippedTire,
            CarPartType.Steering => equippedSteering,
            CarPartType.Transmission => equippedTransmission,
            CarPartType.Chassis => equippedChassis,
            CarPartType.Muffler => equippedMuffler,
            CarPartType.Body => equippedBody,
            _ => null
        };
    }

    /// <summary>
    /// ショップで購入したパーツを所持リストへ追加します。
    /// 同じアセットの重複登録は行わず、追加できた場合だけtrueを返します。
    /// </summary>
    public bool AddOwnedPart(CarPartData part)
    {
        if (part == null || ownedParts.Contains(part))
        {
            return false;
        }

        ownedParts.Add(part);
        return true;
    }

    /// <summary>
    /// 指定したパーツをすでに所持しているか確認します。
    /// </summary>
    public bool HasPart(CarPartData part)
    {
        return part != null && ownedParts.Contains(part);
    }
}
