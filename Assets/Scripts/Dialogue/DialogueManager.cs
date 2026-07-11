using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPCとの会話を管理するSingletonです。
/// Canvas上のPanelとTextをInspectorから割り当てて使用します。
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("会話UI")]
    [Tooltip("会話中に表示するPanelです。")]
    [SerializeField] private GameObject dialoguePanel;

    [Tooltip("会話文を表示するuGUI Textです。")]
    [SerializeField] private Text dialogueText;

    private string[] currentDialogue;
    private int currentLineIndex;

    /// <summary>
    /// 会話中かどうかをNPC側から確認するためのプロパティです。
    /// </summary>
    public bool IsDialogueActive => currentDialogue != null;

    private void Awake()
    {
        // シーン内にManagerを1つだけ残し、どのNPCからでも同じManagerを参照できるようにします。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CloseDialogue();
    }

    private void Update()
    {
        if (IsDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            ShowNextLine();
        }
    }

    /// <summary>
    /// NPCから受け取った会話文を保存し、最初の行を表示します。
    /// </summary>
    public void StartDialogue(string[] dialogueLines)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }

        currentDialogue = dialogueLines;
        currentLineIndex = 0;
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.text = currentDialogue[currentLineIndex];
        }

        // 会話中はプレイヤー車の移動を制限します。
        // 例: ArcadeCarControllerの canControl / isInputEnabled などのフラグをfalseに設定します。
        // 会話終了時には同じフラグをtrueへ戻してください。
    }

    /// <summary>
    /// Eキー入力に応じて次の行を表示し、最後の行の後はUIを閉じます。
    /// </summary>
    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex >= currentDialogue.Length)
        {
            CloseDialogue();
            return;
        }

        if (dialogueText != null)
        {
            dialogueText.text = currentDialogue[currentLineIndex];
        }
    }

    /// <summary>
    /// 会話状態を解除し、Panelを非表示にします。
    /// </summary>
    private void CloseDialogue()
    {
        currentDialogue = null;
        currentLineIndex = 0;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 会話終了時は、ここでArcadeCarControllerの移動許可フラグをtrueに戻します。
    }
}
