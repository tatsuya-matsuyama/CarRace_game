using UnityEngine;

/// <summary>
/// BGMやSEの再生設定をアセットとして保持します。
/// Createメニューから作成し、AudioManagerや車両コンポーネントへ割り当てて使用します。
/// </summary>
[CreateAssetMenu(fileName = "NewSound", menuName = "CarRace/Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Tooltip("スクリプトから識別するための名前です。空欄ならアセット名を使用します。")]
    [SerializeField] private string soundId;
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [Range(.1f, 3f)] [SerializeField] private float pitch = 1f;
    [SerializeField] private bool loop;

    public string SoundId => string.IsNullOrWhiteSpace(soundId) ? name : soundId;
    public AudioClip Clip => clip;
    public float Volume => volume;
    public float Pitch => pitch;
    public bool Loop => loop;
}
