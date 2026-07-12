using UnityEngine;

/// <summary>
/// テスト中の走行不能原因を画面上へ可視化する診断用コンポーネントです。
/// Focus / State / Control / Grounded の値から、入力・UI・物理のどこで止まったか判別できます。
/// </summary>
public class VehicleRuntimeDiagnostics : MonoBehaviour
{
    [SerializeField] private bool showDiagnostics = true;

    private ArcadeCarController arcadeController;
    private ChoroQWheelController wheelController;

    private void Awake()
    {
        arcadeController = GetComponent<ArcadeCarController>();
        wheelController = GetComponent<ChoroQWheelController>();
    }

    private void OnGUI()
    {
        if (!showDiagnostics)
        {
            return;
        }

        GameManager.GameState state = GameManager.Instance != null
            ? GameManager.Instance.CurrentState
            : GameManager.GameState.Explore;
        bool controlEnabled = arcadeController == null || arcadeController.IsControlEnabled;
        bool grounded = wheelController != null && wheelController.HasGroundContact;
        float speed = wheelController != null ? wheelController.CurrentSpeedKmh : 0f;

        GUI.Box(new Rect(18f, 18f, 315f, 125f), "走行診断（停止時に確認）");
        GUI.Label(new Rect(30f, 48f, 290f, 22f), $"Focus: {Application.isFocused}  State: {state}");
        GUI.Label(new Rect(30f, 70f, 290f, 22f), $"Control: {controlEnabled}  Grounded: {grounded}");
        GUI.Label(new Rect(30f, 92f, 290f, 22f), $"Speed: {speed:0.0} km/h");
        GUI.Label(new Rect(30f, 114f, 290f, 22f), "R: 安全地点へ復帰");
    }
}
