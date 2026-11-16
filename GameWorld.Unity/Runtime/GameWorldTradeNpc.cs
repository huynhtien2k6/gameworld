using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldTradeNpc : MonoBehaviour, IGameWorldInteractable
    {
        [SerializeField] private string npcName = "NPC";
        [SerializeField] private string[] sellItems = { "Lúa mì", "Thịt", "Gạo" };
        [SerializeField] private int[] prices = { 10, 16, 20 };

        private GameWorldPlayerHud hud;

        private void Awake()
        {
            hud = FindObjectOfType<GameWorldPlayerHud>();
        }

        public void Interact()
        {
            if (hud == null)
            {
                Debug.Log($"{npcName}: Tôi bán đồ cho bạn.");
                return;
            }

            Debug.Log($"{npcName}: Chào bạn! Tôi có thể bán:");
            for (int i = 0; i < sellItems.Length; i++)
            {
                Debug.Log($"- {sellItems[i]}: {prices[i]} vàng");
            }
        }

        public void BuyItem(int index)
        {
            if (index < 0 || index >= sellItems.Length) return;
            if (hud == null) return;

            if (hud.Gold < prices[index])
            {
                Debug.Log($"{npcName}: Bạn không đủ vàng để mua {sellItems[index]}.");
                return;
            }

            hud.AddGold(-prices[index]);
            Debug.Log($"Bạn mua {sellItems[index]} với giá {prices[index]} vàng.");
        }
    }
}
































































