using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チョロQ HG3風の、右下に表示する青い単眼スピードメーターを更新します。
/// </summary>
public class Hg3SpeedometerController : MonoBehaviour
{
    [SerializeField] private ArcadeCarController carController;
    [SerializeField] private RectTransform needle;
    [SerializeField] private Text speedText;
    [SerializeField] private float maxSpeedKmh = 180f;

    private const float MinNeedleAngle = -135f;
    private const float MaxNeedleAngle = 135f;

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
        float ratio = speed / maxSpeedKmh;
        float angle = Mathf.Lerp(MinNeedleAngle, MaxNeedleAngle, ratio);

        if (needle != null)
        {
            needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        if (speedText != null)
        {
            speedText.text = speed.ToString("000") + "\nkm/h";
        }
    }
}
