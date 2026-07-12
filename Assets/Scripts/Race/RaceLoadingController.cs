using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 街からレース専用コースへ切り替える際のロード演出を管理します。
/// 現在は同一シーン内の固定コースへ移動しますが、将来SceneManagerで別シーンを読む形にも置き換え可能です。
/// </summary>
public class RaceLoadingController : MonoBehaviour
{
    public static RaceLoadingController Instance { get; private set; }

    [SerializeField] private float loadingDuration = 1.5f;
    private GameObject loadingPanel;
    private Text loadingText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateLoadingUi();
        loadingPanel.SetActive(false);
    }

    public void TransitionToCourse(RaceCourseController course, Action onComplete)
    {
        StartCoroutine(ShowLoadingAndComplete(course, onComplete));
    }

    private IEnumerator ShowLoadingAndComplete(RaceCourseController course, Action onComplete)
    {
        loadingPanel.SetActive(true);
        loadingText.text = $"NOW LOADING\n\n{course.CourseData.CourseName}\n\nコースへ移動中...";
        yield return new WaitForSeconds(loadingDuration);
        onComplete?.Invoke();
        loadingPanel.SetActive(false);
    }

    private void CreateLoadingUi()
    {
        GameObject canvasObject = new GameObject("RaceLoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        loadingPanel = new GameObject("RaceLoadingPanel", typeof(RectTransform), typeof(Image));
        loadingPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = loadingPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        loadingPanel.GetComponent<Image>().color = new Color(.01f, .015f, .04f, 1f);

        GameObject textObject = new GameObject("LoadingText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(loadingPanel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(.15f, .3f);
        textRect.anchorMax = new Vector2(.85f, .7f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        loadingText = textObject.GetComponent<Text>();
        loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingText.fontSize = 36;
        loadingText.fontStyle = FontStyle.Bold;
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = new Color(1f, .85f, .2f);
    }
}
