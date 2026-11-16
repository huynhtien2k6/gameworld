using System.Collections.Generic;
using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldDialogueSystem : MonoBehaviour
    {
        [System.Serializable]
        public sealed class DialogueNode
        {
            public string id;
            public string npcName;
            public string text;
            public List<string> responses = new List<string>();
        }

        [SerializeField] private List<DialogueNode> dialogueTree = new List<DialogueNode>
        {
            new DialogueNode
            {
                id = "start",
                npcName = "Bà Lan",
                text = "Chào bạn, ngươi đến từ Làng Tân Thủ sao? Ta có thể giúp ngươi bắt đầu trồng trọt.",
                responses = new List<string> { "Hỏi về mùa vụ", "Hỏi về mua bán", "Tạm biệt" }
            },
            new DialogueNode
            {
                id = "season",
                npcName = "Bà Lan",
                text = "Mùa nào cũng có lợi và bất lợi riêng. Mưa thì trồng nhanh, nắng thì ngập sương, đông thì cây chậm lớn.",
                responses = new List<string> { "Về làng", "Tạm biệt" }
            },
            new DialogueNode
            {
                id = "trade",
                npcName = "Bà Lan",
                text = "Ta có lúa mì, gạo và thảo dược. Giá cả luôn thay đổi theo mùa và thời tiết.",
                responses = new List<string> { "Về làng", "Tạm biệt" }
            }
        };

        private readonly Dictionary<string, DialogueNode> nodes = new Dictionary<string, DialogueNode>();
        private bool isOpen;

        private void Awake()
        {
            foreach (var node in dialogueTree)
            {
                if (!string.IsNullOrEmpty(node.id))
                {
                    nodes[node.id] = node;
                }
            }
        }

        public void StartDialogue(string npcName)
        {
            isOpen = true;
            var startNode = nodes.ContainsKey("start") ? nodes["start"] : dialogueTree[0];
            if (startNode != null)
            {
                Debug.Log($"{startNode.npcName}: {startNode.text}");
                for (int i = 0; i < startNode.responses.Count; i++)
                {
                    Debug.Log($"[{i + 1}] {startNode.responses[i]}");
                }
            }
        }

        public void ChooseResponse(int index)
        {
            if (!isOpen) return;
            var startNode = nodes.ContainsKey("start") ? nodes["start"] : dialogueTree[0];
            if (startNode == null) return;

            if (index >= 0 && index < startNode.responses.Count)
            {
                string choice = startNode.responses[index].Trim();
                if (choice.Contains("mùa") || choice.Contains("Mùa"))
                {
                    ShowNode("season");
                    return;
                }

                if (choice.Contains("mua") || choice.Contains("Mua") || choice.Contains("bán") || choice.Contains("Bán"))
                {
                    ShowNode("trade");
                    return;
                }

                CloseDialogue();
            }
        }

        private void ShowNode(string nodeId)
        {
            if (!nodes.TryGetValue(nodeId, out var node)) return;
            Debug.Log($"{node.npcName}: {node.text}");
            for (int i = 0; i < node.responses.Count; i++)
            {
                Debug.Log($"[{i + 1}] {node.responses[i]}");
            }
        }

        private void CloseDialogue()
        {
            isOpen = false;
            Debug.Log("Đã đóng hội thoại.");
        }
    }
}
