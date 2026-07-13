using UnityEngine;

/// <summary>
/// プレイヤー車の後方から追従する、レースゲーム向けの固定3人称カメラです。
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("追従対象")]
    [Tooltip("追従するプレイヤー車です。未設定時はPlayerタグのGameObjectを自動検索します。")]
    [SerializeField] private Transform target;

    [Header("カメラ位置")]
    [Tooltip("プレイヤー車を基準にした、カメラの後方・上方へのオフセットです。")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -7f);

    [Tooltip("カメラが注視するプレイヤー車の高さです。")]
    [SerializeField] private float lookAtHeight = 1f;

    [Tooltip("車を追従する位置補間の速さです。大きいほど素早く追従します。")]
    [SerializeField] private float followSpeed = 8f;

    private void Awake()
    {
        ResolveTargetIfNeeded();
    }

    private void LateUpdate()
    {
        ResolveTargetIfNeeded();
        if (target == null)
        {
            return;
        }

        // 車のローカル後方に一定距離を保つことで、旋回時も常に車の後ろから見下ろします。
        Vector3 desiredPosition = target.TransformPoint(offset);
        float interpolation = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, interpolation);

        // 注視点を少し上へずらし、車体と前方の道路を同時に見やすくします。
        Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
    }

    /// <summary>レース開始時など、追従対象を明示的に切り替えるために使用します。</summary>
    public void SetTarget(Transform newTarget, bool snapToTarget = false)
    {
        target = newTarget;
        if (!snapToTarget || target == null)
        {
            return;
        }

        transform.position = target.TransformPoint(offset);
        transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * lookAtHeight - transform.position, Vector3.up);
    }

    private void ResolveTargetIfNeeded()
    {
        if (target != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }
}
