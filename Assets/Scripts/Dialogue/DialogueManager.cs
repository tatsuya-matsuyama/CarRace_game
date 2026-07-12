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

    [Tooltip("会話相手の名前を表示するuGUI Textです。")]
    [SerializeField] private Text speakerNameText;

    [Header("イベントカメラ")]
    [Tooltip("会話相手を映すカメラ位置のオフセットです。")]
    [SerializeField] private Vector3 eventCameraOffset = new Vector3(3.5f, 2.2f, -3.5f);

    [Tooltip("会話相手を注視する高さです。")]
    [SerializeField] private float eventCameraLookHeight = 0.8f;

    private string[] currentDialogue;
    private int currentLineIndex;
    private ArcadeCarController playerCarController;
    private Camera eventCamera;
    private ThirdPersonCamera thirdPersonCamera;
    private Vector3 cameraPositionBeforeDialogue;
    private Quaternion cameraRotationBeforeDialogue;
    private bool cameraWasControlledByChaseCamera;

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
        StartDialogue(dialogueLines, "？？？", null);
    }

    /// <summary>
    /// NPC名と注視対象を受け取り、HG風の会話イベントを開始します。
    /// </summary>
    public void StartDialogue(string[] dialogueLines, string speakerName, Transform focusTarget)
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

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }

        // 会話中はプレイヤー車の入力を止め、イベント画面中に走り出さないようにします。
        playerCarController = FindFirstObjectByType<ArcadeCarController>();
        playerCarController?.SetControlEnabled(false);
        GameManager.Instance?.ChangeState(GameManager.GameState.Dialogue);

        BeginEventCamera(focusTarget);
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

        playerCarController?.SetControlEnabled(true);
        GameManager.Instance?.ChangeState(GameManager.GameState.Explore);
        EndEventCamera();
    }

    /// <summary>
    /// 通常の追従カメラを停止し、NPCの側面から見せる会話用カメラへ切り替えます。
    /// </summary>
    private void BeginEventCamera(Transform focusTarget)
    {
        if (focusTarget == null)
        {
            return;
        }

        eventCamera = Camera.main;
        if (eventCamera == null)
        {
            return;
        }

        cameraPositionBeforeDialogue = eventCamera.transform.position;
        cameraRotationBeforeDialogue = eventCamera.transform.rotation;
        thirdPersonCamera = eventCamera.GetComponent<ThirdPersonCamera>();
        cameraWasControlledByChaseCamera = thirdPersonCamera != null && thirdPersonCamera.enabled;
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        eventCamera.transform.position = focusTarget.TransformPoint(eventCameraOffset);
        Vector3 lookTarget = focusTarget.position + Vector3.up * eventCameraLookHeight;
        eventCamera.transform.rotation = Quaternion.LookRotation(lookTarget - eventCamera.transform.position, Vector3.up);
    }

    /// <summary>
    /// 会話終了時に、会話前の追従カメラ位置と制御状態を復元します。
    /// </summary>
    private void EndEventCamera()
    {
        if (eventCamera == null)
        {
            return;
        }

        eventCamera.transform.position = cameraPositionBeforeDialogue;
        eventCamera.transform.rotation = cameraRotationBeforeDialogue;
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = cameraWasControlledByChaseCamera;
        }

        eventCamera = null;
        thirdPersonCamera = null;
    }
}
