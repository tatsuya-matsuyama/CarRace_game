using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエストの受注状態と報酬の受け取りを管理するデータです。
/// InspectorからQuestManagerへ直接登録して使用できます。
/// </summary>
[Serializable]
public class QuestData
{
    [Tooltip("ゲーム内で表示するクエスト名です。")]
    [SerializeField] private string questName;

    [Tooltip("クエスト達成時に受け取るゴールドです。")]
    [Min(0)]
    [SerializeField] private int rewardGold;

    [Tooltip("達成済みかどうかを表すフラグです。")]
    [SerializeField] private bool isCompleted;

    public string QuestName => questName;
    public int RewardGold => rewardGold;
    public bool IsCompleted => isCompleted;

    /// <summary>
    /// 報酬の二重受け取りを防ぐため、達成済みフラグを更新します。
    /// </summary>
    public void MarkCompleted()
    {
        isCompleted = true;
    }
}

/// <summary>
/// 受注中クエストを管理し、達成時にGameManager経由で報酬を渡します。
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("受注中クエスト")]
    [SerializeField] private List<QuestData> activeQuests = new List<QuestData>();

    public IReadOnlyList<QuestData> ActiveQuests => activeQuests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// NPCなどから渡されたクエストを受注リストへ追加します。
    /// 同じクエストや達成済みクエストは重複して受注しません。
    /// </summary>
    public bool AcceptQuest(QuestData quest)
    {
        if (quest == null || quest.IsCompleted || activeQuests.Contains(quest))
        {
            return false;
        }

        activeQuests.Add(quest);
        return true;
    }

    /// <summary>
    /// 受注中のクエストを達成します。
    /// 報酬はGameManagerに加算させ、成功後に受注リストから取り除きます。
    /// </summary>
    public bool CompleteQuest(QuestData quest)
    {
        if (quest == null || quest.IsCompleted || !activeQuests.Contains(quest) || GameManager.Instance == null)
        {
            return false;
        }

        // 先に達成済みにすることで、同じクエストの報酬を重複して受け取るのを防ぎます。
        quest.MarkCompleted();
        GameManager.Instance.AddGold(quest.RewardGold);
        activeQuests.Remove(quest);
        return true;
    }
}
