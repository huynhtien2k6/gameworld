using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldQuestJournalUi : MonoBehaviour
    {
        [SerializeField] private GameWorldQuestController questController;
        [SerializeField] private KeyCode toggleKey = KeyCode.J;

        private bool visible;

        private void Awake()
        {
            if (questController == null)
            {
                questController = FindObjectOfType<GameWorldQuestController>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) && questController != null)
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible || questController == null) return;

            var box = new Rect(340f, 20f, 420f, 220f);
            GUI.Box(box, "Nhiệm vụ");

            var quests = questController.GetQuests();
            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                string label = quest.isCompleted ? $"[Hoàn tất] {quest.title}" : $"{quest.title} ({quest.progress}/{quest.target})";
                GUI.Label(new Rect(360f, 52f + i * 28f, 380f, 24f), label);
            }
        }
    }
}
