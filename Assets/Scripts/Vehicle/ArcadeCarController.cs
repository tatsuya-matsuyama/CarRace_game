using UnityEngine;

/// <summary>
/// Rigidbody を使った、カジュアルなアーケード向け車両コントローラーです。
/// WheelCollider は使用せず、前進力と旋回トルクだけで軽快な操作感を作ります。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour
{
    [Header("走行性能")]
    [Tooltip("アクセルを押したときに車体へ加える加速度です。")]
    [SerializeField] private float acceleration = 18f;

    [Tooltip("前進方向の最高速度です（m/s）。")]
    [SerializeField] private float maxSpeed = 20f;

    [Tooltip("左右入力に対する旋回速度です（度/秒）。")]
    [SerializeField] private float turnSpeed = 120f;

    [Header("安定性")]
    [Tooltip("車体の重心を下げる量です。値を大きくすると横転しにくくなります。")]
    [SerializeField] private float centerOfMassHeight = -0.5f;

    private Rigidbody carRigidbody;
    private float throttleInput;
    private float steeringInput;
    private bool isControlEnabled = true;

    /// <summary>現在の速度です。HUD表示用にkm/hへ換算しています。</summary>
    public float CurrentSpeedKmh => carRigidbody != null ? carRigidbody.linearVelocity.magnitude * 3.6f : 0f;

    /// <summary>現在のアクセル入力です。タコメーター表示用に公開します。</summary>
    public float ThrottleInput => throttleInput;
    public bool IsControlEnabled => isControlEnabled;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // 重心を車体の中心より下げることで、コーナーや段差で横転しにくくします。
        // Rigidbody の centerOfMass はローカル座標なので、車体の大きさに依存せず調整できます。
        Vector3 loweredCenterOfMass = carRigidbody.centerOfMass;
        loweredCenterOfMass.y = centerOfMassHeight;
        carRigidbody.centerOfMass = loweredCenterOfMass;
    }

    private void Update()
    {
        if (!isControlEnabled)
        {
            throttleInput = 0f;
            steeringInput = 0f;
            return;
        }

        // 入力は Update で取得し、物理演算は FixedUpdate に任せます。
        // GetAxis は W/S・上下矢印、A/D・左右矢印を標準で扱えます。
        throttleInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        ApplyAcceleration();
        ApplySteering();
        LimitForwardSpeed();
    }

    /// <summary>
    /// 会話やショップ画面の表示中に、プレイヤー車の入力を一時的に有効・無効化します。
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        isControlEnabled = enabled;
        if (!enabled)
        {
            throttleInput = 0f;
            steeringInput = 0f;
        }
    }

    /// <summary>
    /// 入力方向へ加速度を加えます。入力が負なら同じ仕組みで後退します。
    /// </summary>
    private void ApplyAcceleration()
    {
        if (Mathf.Approximately(throttleInput, 0f))
        {
            return;
        }

        carRigidbody.AddForce(
            transform.forward * (throttleInput * acceleration),
            ForceMode.Acceleration);
    }

    /// <summary>
    /// 停車中を含め、常にヨー方向へ回転させます。
    /// その場旋回を許可することで、狭い場所でも向きを変えやすいアーケードらしい操作感にします。
    /// </summary>
    private void ApplySteering()
    {
        float forwardSpeed = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
        if (Mathf.Approximately(steeringInput, 0f))
        {
            return;
        }

        // 後退中だけハンドル操作を反転させ、実車と同じ旋回方向になるようにします。
        // 停車中は通常方向で回転するため、その場旋回が可能です。
        float reverseCorrection = forwardSpeed < 0f ? -1f : 1f;
        float yawDegrees = steeringInput * turnSpeed * reverseCorrection * Time.fixedDeltaTime;
        Quaternion nextRotation = carRigidbody.rotation * Quaternion.Euler(0f, yawDegrees, 0f);
        carRigidbody.MoveRotation(nextRotation);
    }

    /// <summary>
    /// 前後方向の速度だけを制限します。ジャンプ中の上下速度や、ドリフトの横滑りは残します。
    /// </summary>
    private void LimitForwardSpeed()
    {
        float forwardSpeed = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
        float limitedSpeed = Mathf.Clamp(forwardSpeed, -maxSpeed, maxSpeed);
        Vector3 forwardVelocity = transform.forward * forwardSpeed;
        Vector3 sidewaysAndVerticalVelocity = carRigidbody.linearVelocity - forwardVelocity;
        carRigidbody.linearVelocity = transform.forward * limitedSpeed + sidewaysAndVerticalVelocity;
    }
}
