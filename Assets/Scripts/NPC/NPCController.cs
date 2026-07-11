using UnityEngine;

/// <summary>
/// プレイヤーの車が近づいたときに会話を開始するNPCです。
/// NPC本体にSphereColliderを追加し、Triggerとして利用します。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class NPCController : MonoBehaviour
{
    [Header("会話設定")]
    [Tooltip("プレイヤーがこのNPCに近づいたときに表示する会話文です。")]
    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines;

    [Tooltip("プレイヤーとして扱うGameObjectのタグです。通常はPlayerを使用します。")]
    [SerializeField] private string playerTag = "Player";

    private SphereCollider interactionCollider;
    private bool playerInRange;

    private void Awake()
    {
        interactionCollider = GetComponent<SphereCollider>();

        // Inspectorで設定し忘れても、会話検知用Colliderとして動作するようにします。
        interactionCollider.isTrigger = true;
    }

    private void Update()
    {
        // 会話中はDialogueManager側がEキーを次の行へ使うため、ここでは開始しません。
        if (DialogueManager.Instance != null && playerInRange &&
            !DialogueManager.Instance.IsDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            DialogueManager.Instance.StartDialogue(dialogueLines);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
        }
    }
}
