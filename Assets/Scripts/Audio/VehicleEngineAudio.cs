using UnityEngine;

/// <summary>
/// プレイヤー車へ付ける3Dエンジン音・衝突音コンポーネントです。
/// 音源をInspectorへ割り当てるだけで、車速に応じたピッチ変化を行います。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VehicleEngineAudio : MonoBehaviour
{
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private SoundData engineLoop;
    [SerializeField] private SoundData impactSound;
    [SerializeField, Min(1f)] private float maxSpeedKmh = 120f;
    [SerializeField, Range(.1f, 3f)] private float idlePitch = .8f;
    [SerializeField, Range(.1f, 3f)] private float maxPitch = 2f;
    [SerializeField, Min(1f)] private float impactMinSpeedKmh = 12f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (engineSource == null)
        {
            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.spatialBlend = 1f;
            engineSource.rolloffMode = AudioRolloffMode.Linear;
            engineSource.minDistance = 3f;
            engineSource.maxDistance = 35f;
        }

        if (engineLoop != null)
        {
            engineSource.clip = engineLoop.Clip;
            engineSource.volume = engineLoop.Volume;
            engineSource.pitch = engineLoop.Pitch;
        }
        engineSource.loop = true;
        if (engineSource.clip != null)
        {
            engineSource.Play();
        }
    }

    private void Update()
    {
        float speedKmh = body.linearVelocity.magnitude * 3.6f;
        float speedRatio = Mathf.Clamp01(speedKmh / maxSpeedKmh);
        engineSource.pitch = Mathf.Lerp(idlePitch, maxPitch, speedRatio);
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude * 3.6f;
        if (impactSound == null || impactSpeed < impactMinSpeedKmh)
        {
            return;
        }

        // 衝突速度で音量を変え、低速の接触では耳障りにならないよう抑えます。
        float volume = Mathf.Lerp(.25f, impactSound.Volume, Mathf.Clamp01(impactSpeed / 80f));
        AudioSource.PlayClipAtPoint(impactSound.Clip, transform.position, volume);
    }
}
