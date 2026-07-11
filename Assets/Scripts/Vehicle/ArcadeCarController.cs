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

    [Tooltip("速度がこの値未満のときは旋回入力を無視します。")]
    [SerializeField] private float minimumTurningSpeed = 0.1f;

    private Rigidbody carRigidbody;
    private float throttleInput;
    private float steeringInput;

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
    /// 車が実際に前進または後退している場合だけヨー方向へ回転させます。
    /// 停車中のその場旋回を禁止し、小回りの利く車らしい挙動にします。
    /// </summary>
    private void ApplySteering()
    {
        float forwardSpeed = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
        if (Mathf.Abs(forwardSpeed) < minimumTurningSpeed || Mathf.Approximately(steeringInput, 0f))
        {
            return;
        }

        // 後退時はハンドル操作を反転させ、実車と同じ旋回方向になるようにします。
        float reverseCorrection = Mathf.Sign(forwardSpeed);
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
