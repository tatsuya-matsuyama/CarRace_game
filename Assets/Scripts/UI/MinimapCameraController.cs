using UnityEngine;

/// <summary>
/// プレイヤー車の上空に固定され、左下ミニマップ用の映像を描画するカメラコントローラーです。
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height = 45f;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // 真上から固定方位で見ることで、進行方向に影響されない分かりやすい地図にします。
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + Vector3.up * height;
    }
}
