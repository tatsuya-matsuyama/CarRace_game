using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM、UI／環境SE、簡易エンジン音を統括するSingletonです。
/// 音源アセットをInspectorで登録すれば、ゲーム状態に応じて街とレースのBGMを切り替えます。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private SoundData exploreBgm;
    [SerializeField] private SoundData raceBgm;
    [SerializeField, Min(0f)] private float crossFadeSeconds = 1.2f;

    [Header("音量")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float seVolume = 1f;

    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private SoundData[] sounds;

    [Header("簡易エンジン音（任意）")]
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private SoundData engineLoop;
    [SerializeField, Range(.1f, 3f)] private float idlePitch = .8f;
    [SerializeField, Range(.1f, 3f)] private float maxSpeedPitch = 2f;

    private readonly Dictionary<string, SoundData> soundLookup = new Dictionary<string, SoundData>();
    private AudioSource fadeSource;
    private Coroutine fadeRoutine;
    private float requestedBgmVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        BuildSoundLookup();
        ApplyVolumeSettings();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    /// <summary>
    /// 指定BGMへクロスフェードします。同じClipを再生中なら何もしません。
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        PlayBGM(clip, 1f);
    }

    private void PlayBGM(AudioClip clip, float clipVolume)
    {
        if (clip == null || (bgmSource.clip == clip && bgmSource.isPlaying))
        {
            return;
        }

        requestedBgmVolume = Mathf.Clamp01(clipVolume) * bgmVolume;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(CrossFadeBgm(clip));
    }

    /// <summary>SoundDataに設定した音量・ピッチでBGMを再生します。</summary>
    public void PlayBGM(SoundData sound)
    {
        if (sound == null || sound.Clip == null)
        {
            return;
        }
        PlayBGM(sound.Clip, sound.Volume);
    }

    /// <summary>SEを重ねて1回再生します。PlayOneShotを使うため既存SEを止めません。</summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip != null)
        {
            seSource.PlayOneShot(clip);
        }
    }

    /// <summary>SoundDataに登録した音量・ピッチを反映してSEを再生します。</summary>
    public void PlaySE(SoundData sound)
    {
        if (sound == null || sound.Clip == null)
        {
            return;
        }

        seSource.pitch = sound.Pitch;
        seSource.PlayOneShot(sound.Clip, sound.Volume * seVolume);
        seSource.pitch = 1f;
    }

    /// <summary>登録済みSoundDataをIDで検索して再生します。</summary>
    public bool PlaySE(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId) || !soundLookup.TryGetValue(soundId, out SoundData sound))
        {
            return false;
        }
        PlaySE(sound);
        return true;
    }

    /// <summary>
    /// 0〜1の速度比から、簡易エンジン音のピッチを更新します。
    /// 車体に付けたVehicleEngineAudioを使う場合も、同じ補間値を利用します。
    /// </summary>
    public void SetEnginePitch(float normalizedSpeed)
    {
        if (engineSource == null)
        {
            return;
        }

        engineSource.pitch = Mathf.Lerp(idlePitch, maxSpeedPitch, Mathf.Clamp01(normalizedSpeed));
    }

    /// <summary>設定画面などからマスター・BGM・SE音量をまとめて変更します。</summary>
    public void SetVolumes(float master, float bgm, float se)
    {
        masterVolume = Mathf.Clamp01(master);
        bgmVolume = Mathf.Clamp01(bgm);
        seVolume = Mathf.Clamp01(se);
        ApplyVolumeSettings();
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        // レース中だけ専用曲へ切り替え、街・ガレージ・ショップでは探索曲へ戻します。
        PlayBGM(state == GameManager.GameState.Race ? raceBgm : exploreBgm);
    }

    private IEnumerator CrossFadeBgm(AudioClip nextClip)
    {
        fadeSource.clip = bgmSource.clip;
        fadeSource.volume = bgmSource.volume;
        fadeSource.pitch = bgmSource.pitch;
        fadeSource.loop = true;
        if (fadeSource.clip != null && bgmSource.isPlaying)
        {
            fadeSource.Play();
        }

        bgmSource.clip = nextClip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.pitch = 1f;
        bgmSource.Play();

        float elapsed = 0f;
        float duration = Mathf.Max(.01f, crossFadeSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(elapsed / duration);
            bgmSource.volume = requestedBgmVolume * ratio;
            fadeSource.volume = requestedBgmVolume * (1f - ratio);
            yield return null;
        }

        bgmSource.volume = requestedBgmVolume;
        fadeSource.Stop();
        fadeRoutine = null;
    }

    private void EnsureSources()
    {
        bgmSource ??= CreateSource("BGM Source", true, false);
        seSource ??= CreateSource("SE Source", false, false);
        fadeSource = CreateSource("BGM Fade Source", true, false);
        engineSource ??= CreateSource("Engine Source", true, true);
        if (engineSource.clip == null && engineLoop != null)
        {
            engineSource.clip = engineLoop.Clip;
            engineSource.volume = engineLoop.Volume;
            engineSource.pitch = engineLoop.Pitch;
        }
        if (engineSource.clip != null && !engineSource.isPlaying)
        {
            engineSource.Play();
        }
    }

    private AudioSource CreateSource(string objectName, bool loop, bool spatial)
    {
        GameObject sourceObject = new GameObject(objectName, typeof(AudioSource));
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = spatial ? 1f : 0f;
        return source;
    }

    private void BuildSoundLookup()
    {
        soundLookup.Clear();
        foreach (SoundData sound in sounds)
        {
            if (sound != null && !string.IsNullOrWhiteSpace(sound.SoundId))
            {
                soundLookup[sound.SoundId] = sound;
            }
        }
    }

    private void ApplyVolumeSettings()
    {
        AudioListener.volume = masterVolume;
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.volume = requestedBgmVolume;
        }
        if (seSource != null)
        {
            seSource.volume = seVolume;
        }
    }
}
