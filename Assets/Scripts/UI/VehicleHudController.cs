using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤー車の速度と推定エンジン回転数を表示する簡易HUDです。
/// </summary>
public class VehicleHudController : MonoBehaviour
{
    [SerializeField] private ArcadeCarController carController;
    [SerializeField] private Text speedText;
    [SerializeField] private Text tachometerText;
    [SerializeField] private float idleRpm = 900f;
    [SerializeField] private float maxRpm = 8000f;

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

        float speed = carController.CurrentSpeedKmh;
        // アクセル入力と速度を組み合わせ、アーケードゲーム向けの分かりやすい回転数を算出します。
        float speedRatio = Mathf.InverseLerp(0f, 120f, speed);
        float throttleRatio = Mathf.Abs(carController.ThrottleInput);
        float rpm = Mathf.Lerp(idleRpm, maxRpm, Mathf.Clamp01(speedRatio * 0.7f + throttleRatio * 0.5f));

        if (speedText != null)
        {
            speedText.text = $"SPEED\n{speed:000} km/h";
        }

        if (tachometerText != null)
        {
            tachometerText.text = $"TACHO\n{rpm:0000} rpm";
        }
    }
}
