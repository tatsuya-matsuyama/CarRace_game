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

    private Rigidbody carRigidbody;
    private PlayerCarStats carStats;
    private ArcadeCarController inputGate;
    private float throttleInput;
    private float steeringInput;
    private bool isDrifting;

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
    }

    private void Update()
    {
        bool canControl = inputGate == null || inputGate.IsControlEnabled;
        throttleInput = canControl ? Input.GetAxis("Vertical") : 0f;
        steeringInput = canControl ? Input.GetAxis("Horizontal") : 0f;
        isDrifting = canControl && Input.GetKey(KeyCode.Space);
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
}
