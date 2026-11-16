using System.Collections.Generic;
using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldQuestController : MonoBehaviour
    {
        [System.Serializable]
        public sealed class QuestDefinition
        {
            public string id;
            public string title;
            public string description;
            public string objectiveType;
            public int targetCount;
            public int rewardGold;
            public int rewardXp;
        }

        [System.Serializable]
        public sealed class QuestStatus
        {
            public string id;
            public string title;
            public int progress;
            public int target;
            public bool isCompleted;
        }

        [SerializeField] private List<QuestDefinition> quests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                id = "q1",
                title = "Cây lúa đầu tiên",
                description = "Gieo một ô lúa và thu hoạch thành công.",
                objectiveType = "Farm",
                targetCount = 1,
                rewardGold = 30,
                rewardXp = 10
            },
            new QuestDefinition
            {
                id = "q2",
                title = "Dọn sạch bầy slime",
                description = "Tiêu diệt 3 con slime trong rừng gần làng.",
                objectiveType = "Kill",
                targetCount = 3,
                rewardGold = 50,
                rewardXp = 20
            },
            new QuestDefinition
            {
                id = "q3",
                title = "Thuốc chữa cho làng",
                description = "Mua thuốc hoặc vật phẩm từ NPC thương nhân.",
                objectiveType = "Trade",
                targetCount = 1,
                rewardGold = 60,
                rewardXp = 25
            }
        };

        private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private GameWorldPlayerHud hud;

        public List<QuestStatus> GetQuests()
        {
            var result = new List<QuestStatus>();
            foreach (var quest in quests)
            {
                var current = progress.TryGetValue(quest.id, out var value) ? value : 0;
                result.Add(new QuestStatus
                {
                    id = quest.id,
                    title = quest.title,
                    progress = current,
                    target = quest.targetCount,
                    isCompleted = completed.Contains(quest.id)
                });
            }
            return result;
        }

        public bool IsQuestCompleted(string questId)
        {
            return completed.Contains(questId);
        }

        public void UpdateQuestProgress(string questId, int amount)
        {
            if (!progress.ContainsKey(questId)) return;

            var quest = quests.Find(x => x.id == questId);
            if (quest == null) return;

            progress[questId] = Mathf.Min(quest.targetCount, progress[questId] + amount);
            if (progress[questId] >= quest.targetCount)
            {
                CompleteQuest(quest);
            }
        }

        public void ReportProgress(string objectiveType, int amount = 1)
        {
            foreach (var quest in quests)
            {
                if (completed.Contains(quest.id)) continue;
                if (!string.Equals(quest.objectiveType, objectiveType, System.StringComparison.OrdinalIgnoreCase)) continue;

                progress[quest.id] = Mathf.Min(quest.targetCount, progress.TryGetValue(quest.id, out var val) ? val + amount : amount);
                if (progress[quest.id] >= quest.targetCount)
                {
                    CompleteQuest(quest);
                }
            }
        }

        private void CompleteQuest(QuestDefinition quest)
        {
            if (completed.Contains(quest.id)) return;

            completed.Add(quest.id);
            if (hud != null)
            {
                hud.AddGold(quest.rewardGold);
            }
            Debug.Log($"Nhiệm vụ hoàn thành: {quest.title}. Nhận {quest.rewardGold} vàng, {quest.rewardXp} XP.");
        }

        private void Awake()
        {
            hud = FindObjectOfType<GameWorldPlayerHud>();
            foreach (var quest in quests)
            {
                progress[quest.id] = 0;
            }
        }

        public void ResetProgress()
        {
            progress.Clear();
            completed.Clear();
            foreach (var quest in quests)
            {
                progress[quest.id] = 0;
            }
        }
    }
}
