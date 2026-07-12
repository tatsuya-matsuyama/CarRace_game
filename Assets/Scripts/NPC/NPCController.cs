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

    [Tooltip("会話枠の名前プレートへ表示するNPC名です。")]
    [SerializeField] private string npcName = "フォレスト";

    [Tooltip("プレイヤーとして扱うGameObjectのタグです。通常はPlayerを使用します。")]
    [SerializeField] private string playerTag = "Player";

    private SphereCollider interactionCollider;
    private bool playerInRange;
    private bool dialogueWasActive;

    private void Awake()
    {
        interactionCollider = GetComponent<SphereCollider>();

        // Inspectorで設定し忘れても、会話検知用Colliderとして動作するようにします。
        interactionCollider.isTrigger = true;
    }

    private void Update()
    {
        if (DialogueManager.Instance == null)
        {
            return;
        }

        // 会話中はDialogueManager側がEキーを次の行へ使うため、ここでは開始しません。
        if (DialogueManager.Instance.IsDialogueActive)
        {
            dialogueWasActive = true;
            return;
        }

        // 最終行を閉じたEキーで、そのまま同じ会話を再開しないようにします。
        // 1フレーム待つことで、会話終了入力と会話開始入力を分離します。
        if (dialogueWasActive)
        {
            dialogueWasActive = false;
            return;
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            DialogueManager.Instance.StartDialogue(dialogueLines, npcName, transform);
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
