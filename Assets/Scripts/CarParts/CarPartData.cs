using UnityEngine;

/// <summary>
/// 車に装備できるパーツの種類です。
/// </summary>
public enum CarPartType
{
    Engine,
    Tire,
    Chassis,
    Muffler,
    Body
}

/// <summary>
/// ショップやインベントリで扱う車パーツの定義データです。
/// ScriptableObjectとして保存することで、パーツごとのデータをアセットとして再利用できます。
/// </summary>
[CreateAssetMenu(fileName = "NewCarPart", menuName = "CarRace/Car Part Data")]
public class CarPartData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("パーツの種類です。")]
    [SerializeField] private CarPartType partType;

    [Tooltip("ゲーム内で表示するパーツ名です。")]
    [SerializeField] private string partName;

    [Tooltip("パーツの性能や特徴を説明する文章です。")]
    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Tooltip("ショップでの購入価格です。")]
    [Min(0)]
    [SerializeField] private int price;

    [Header("性能補正値")]
    [Tooltip("車の最高速度に加算する値です。")]
    [SerializeField] private float maxSpeedBonus;

    [Tooltip("車の加速度に加算する値です。")]
    [SerializeField] private float accelerationBonus;

    [Tooltip("車の旋回性能に加算する値です。")]
    [SerializeField] private float handlingBonus;

    [Tooltip("タイヤグリップに加算する値です。プラスで安定、マイナスでドリフトしやすくなります。")]
    [SerializeField] private float gripBonus;

    [Tooltip("ドリフト中の横滑りしやすさに加算する値です。")]
    [SerializeField] private float driftBonus;

    public CarPartType PartType => partType;
    public string PartName => partName;
    public string Description => description;
    public int Price => price;
    public float MaxSpeedBonus => maxSpeedBonus;
    public float AccelerationBonus => accelerationBonus;
    public float HandlingBonus => handlingBonus;
    public float GripBonus => gripBonus;
    public float DriftBonus => driftBonus;
}
