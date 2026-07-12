using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チョロQ風の丸型スピードメーターとタコメーターを更新するHUDコントローラーです。
/// </summary>
public class ArcadeGaugeController : MonoBehaviour
{
    [SerializeField] private ArcadeCarController carController;
    [SerializeField] private RectTransform speedNeedle;
    [SerializeField] private RectTransform tachometerNeedle;
    [SerializeField] private Text speedReadout;
    [SerializeField] private Text rpmReadout;
    [SerializeField] private float maxSpeedKmh = 180f;
    [SerializeField] private float maxRpm = 9000f;

    private const float MinNeedleAngle = -130f;
    private const float MaxNeedleAngle = 130f;

    private void Awake()
    {
        if (carController == null)
        {
            carController = FindFirstObjectByType<ArcadeCarController>();
        }
    }

    private void Update()
    {
        if (carController == null)
        {
            return;
        }

        float speed = Mathf.Clamp(carController.CurrentSpeedKmh, 0f, maxSpeedKmh);
        float speedRatio = speed / maxSpeedKmh;
        float throttleRatio = Mathf.Abs(carController.ThrottleInput);
        float rpm = Mathf.Lerp(900f, maxRpm, Mathf.Clamp01(speedRatio * 0.75f + throttleRatio * 0.45f));

        SetNeedleAngle(speedNeedle, speedRatio);
        SetNeedleAngle(tachometerNeedle, rpm / maxRpm);

        if (speedReadout != null)
        {
            speedReadout.text = speed.ToString("000") + "\nkm/h";
        }

        if (rpmReadout != null)
        {
            rpmReadout.text = (rpm / 1000f).ToString("0.0") + "\n×1000 RPM";
        }
    }

    /// <summary>
    /// 0から1の値をメーターの可動角度へ変換し、針を回転させます。
    /// </summary>
    private static void SetNeedleAngle(RectTransform needle, float ratio)
    {
        if (needle == null)
        {
            return;
        }

        float angle = Mathf.Lerp(MinNeedleAngle, MaxNeedleAngle, Mathf.Clamp01(ratio));
        needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}
