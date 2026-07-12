using UnityEngine;

/// <summary>
/// WheelColliderを使い、短いホイールベースと大きな舵角でチョロQ風のクイックな走りを作る車両コントローラーです。
/// Spaceキーで後輪グリップを下げ、アーケード調のドリフトへ移行します。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ChoroQWheelController : MonoBehaviour
{
    [Header("WheelCollider")]
    [SerializeField] private WheelCollider frontLeftWheel;
    [SerializeField] private WheelCollider frontRightWheel;
    [SerializeField] private WheelCollider rearLeftWheel;
    [SerializeField] private WheelCollider rearRightWheel;

    [Header("チョロQ走行設定")]
    [SerializeField] private float motorTorque = 1100f;
    [SerializeField] private float brakeTorque = 1800f;
    [SerializeField] private float maxSteerAngle = 42f;
    [SerializeField] private float maxSpeedKmh = 120f;
    [SerializeField] private float baseGrip = 1.1f;
    [SerializeField] private float driftRearGrip = 0.45f;
    [SerializeField] private float centerOfMassHeight = 0.18f;
    [SerializeField] private float downforce = 30f;

    [Header("走行不能フェイルセーフ")]
    [Tooltip("アクセルを踏み続けてもこの秒数だけ動かなければ、壁への埋まりから自動で抜け出します。")]
    [SerializeField] private float stuckRecoverySeconds = 2.5f;
    [SerializeField] private float stuckSpeedThresholdKmh = 1f;

    private Rigidbody carRigidbody;
    private PlayerCarStats carStats;
    private ArcadeCarController inputGate;
    private float throttleInput;
    private float steeringInput;
    private bool isDrifting;
    private Vector3 safePosition;
    private Quaternion safeRotation;
    private float stuckTimer;

    /// <summary>診断UI・HUD用の現在速度です。</summary>
    public float CurrentSpeedKmh => carRigidbody != null ? carRigidbody.linearVelocity.magnitude * 3.6f : 0f;

    /// <summary>少なくとも1つの車輪が地面を捉えているかを返します。</summary>
    public bool HasGroundContact => HasAllWheels() &&
                                    (frontLeftWheel.isGrounded || frontRightWheel.isGrounded || rearLeftWheel.isGrounded || rearRightWheel.isGrounded);

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carStats = GetComponent<PlayerCarStats>();
        inputGate = GetComponent<ArcadeCarController>();
    }

    private void Start()
    {
        // あえて重心を少し高めに置き、急旋回時のロールとチョロQらしい不安定さを残します。
        Vector3 centerOfMass = carRigidbody.centerOfMass;
        centerOfMass.y = centerOfMassHeight;
        carRigidbody.centerOfMass = centerOfMass;
        safePosition = transform.position;
        safeRotation = transform.rotation;
    }

    private void Update()
    {
        // 走行可否はゲーム状態を正とします。UIを閉じたのに旧コントローラーのフラグだけが
        // falseで残っても、Exploreへ戻った時点で入力を必ず復旧できます。
        bool canControl = GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Explore;
        if (canControl && inputGate != null && !inputGate.IsControlEnabled)
        {
            inputGate.SetControlEnabled(true);
        }

        throttleInput = canControl ? Input.GetAxis("Vertical") : 0f;
        steeringInput = canControl ? Input.GetAxis("Horizontal") : 0f;
        isDrifting = canControl && Input.GetKey(KeyCode.Space);

        // Rキーはデバッグ・動作確認用の手動復帰です。落下や物理スタック時に即座に戻せます。
        if (Input.GetKeyDown(KeyCode.R))
        {
            RecoverToSafePosition();
        }
    }

    private void FixedUpdate()
    {
        if (!HasAllWheels())
        {
            return;
        }

        ApplySteering();
        ApplyMotorAndBrake();
        ApplyGrip();
        carRigidbody.AddForce(-transform.up * carRigidbody.linearVelocity.magnitude * downforce, ForceMode.Force);
        UpdateSafePositionAndRecoverFromStuck();
    }

    private void ApplySteering()
    {
        // 低速では大きく切れ、高速では少し抑えることで小回りと安定性を両立します。
        float effectiveMaxSpeed = GetEffectiveMaxSpeed();
        float speedRatio = Mathf.Clamp01(carRigidbody.linearVelocity.magnitude * 3.6f / effectiveMaxSpeed);
        float handlingMultiplier = carStats != null ? carStats.CurrentHandling / 120f : 1f;
        float steerAngle = steeringInput * Mathf.Lerp(maxSteerAngle, maxSteerAngle * 0.45f, speedRatio) * handlingMultiplier;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;
    }

    private void ApplyMotorAndBrake()
    {
        float speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        float effectiveMaxSpeed = GetEffectiveMaxSpeed();
        float accelerationMultiplier = carStats != null ? carStats.CurrentAcceleration / 18f : 1f;
        float motor = speedKmh < effectiveMaxSpeed ? throttleInput * motorTorque * accelerationMultiplier : 0f;

        rearLeftWheel.motorTorque = motor;
        rearRightWheel.motorTorque = motor;

        bool braking = Mathf.Approximately(throttleInput, 0f) && speedKmh > 1f;
        float brake = braking ? brakeTorque * 0.08f : 0f;
        frontLeftWheel.brakeTorque = brake;
        frontRightWheel.brakeTorque = brake;
        rearLeftWheel.brakeTorque = isDrifting ? brakeTorque * 0.35f : brake;
        rearRightWheel.brakeTorque = isDrifting ? brakeTorque * 0.35f : brake;
    }

    private void ApplyGrip()
    {
        float partGrip = carStats != null ? carStats.CurrentGrip : 1f;
        float driftBonus = carStats != null ? carStats.CurrentDrift : 0f;
        float normalGrip = baseGrip * partGrip;
        SetSidewaysGrip(frontLeftWheel, normalGrip);
        SetSidewaysGrip(frontRightWheel, normalGrip);

        float rearGrip = isDrifting ? Mathf.Max(0.15f, driftRearGrip - driftBonus * 0.1f) : normalGrip;
        SetSidewaysGrip(rearLeftWheel, rearGrip);
        SetSidewaysGrip(rearRightWheel, rearGrip);
    }

    private static void SetSidewaysGrip(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve friction = wheel.sidewaysFriction;
        friction.stiffness = stiffness;
        wheel.sidewaysFriction = friction;
    }

    private bool HasAllWheels()
    {
        return frontLeftWheel != null && frontRightWheel != null && rearLeftWheel != null && rearRightWheel != null;
    }

    private float GetEffectiveMaxSpeed()
    {
        // エンジンなどの装備補正がある場合は、ScriptableObjectから算出済みの値を優先します。
        // PlayerCarStatsはUnity単位（m/s）で保持しているため、表示・制限用にkm/hへ換算します。
        return carStats != null ? carStats.CurrentMaxSpeed * 3.6f : maxSpeedKmh;
    }

    private void UpdateSafePositionAndRecoverFromStuck()
    {
        bool isGrounded = frontLeftWheel.isGrounded || frontRightWheel.isGrounded || rearLeftWheel.isGrounded || rearRightWheel.isGrounded;
        float speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;

        // 正常に地面を走れている場所を最後の安全地点として保存します。
        if (isGrounded && transform.up.y > 0.4f && transform.position.y > -1f && speedKmh > stuckSpeedThresholdKmh)
        {
            safePosition = transform.position;
            safeRotation = transform.rotation;
            stuckTimer = 0f;
            return;
        }

        bool isTryingToMove = Mathf.Abs(throttleInput) > 0.7f;
        bool isStuck = isGrounded && isTryingToMove && speedKmh < stuckSpeedThresholdKmh;
        stuckTimer = isStuck ? stuckTimer + Time.fixedDeltaTime : 0f;

        // 地面の下へ落ちた、横転した、または壁へ押し付けたまま動けない場合に最後の安全地点へ戻します。
        if (transform.position.y < -5f || transform.up.y < 0.15f || stuckTimer >= stuckRecoverySeconds)
        {
            RecoverToSafePosition();
        }
    }

    private void RecoverToSafePosition()
    {
        carRigidbody.position = safePosition + Vector3.up * 0.5f;
        carRigidbody.rotation = safeRotation;
        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        stuckTimer = 0f;
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);
        inputGate?.SetControlEnabled(true);
    }
}
