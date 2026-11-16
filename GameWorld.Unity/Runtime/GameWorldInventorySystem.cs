using System.Collections.Generic;
using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldInventorySystem : MonoBehaviour
    {
        [System.Serializable]
        public sealed class InventoryItem
        {
            public string id;
            public string name;
            public int quantity;
            public int price;
        }

        private readonly List<InventoryItem> items = new List<InventoryItem>();
        private GameWorldPlayerHud hud;

        private void Awake()
        {
            hud = FindObjectOfType<GameWorldPlayerHud>();
            AddDefaultItems();
        }

        private void AddDefaultItems()
        {
            if (items.Count > 0) return;
            items.Add(new InventoryItem { id = "wheat", name = "Lúa mì", quantity = 5, price = 8 });
            items.Add(new InventoryItem { id = "meat", name = "Thịt", quantity = 2, price = 12 });
            items.Add(new InventoryItem { id = "herb", name = "Thảo dược", quantity = 1, price = 18 });
        }

        public IReadOnlyList<InventoryItem> Items => items;

        public void AddItem(string itemId, string itemName, int quantity, int price)
        {
            var existing = items.Find(x => x.id == itemId);
            if (existing != null)
            {
                existing.quantity += quantity;
                return;
            }

            items.Add(new InventoryItem { id = itemId, name = itemName, quantity = quantity, price = price });
        }

        public bool TryConsume(string itemId, int quantity = 1)
        {
            var item = items.Find(x => x.id == itemId);
            if (item == null || item.quantity < quantity) return false;
            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                items.Remove(item);
            }
            return true;
        }

        public void SellItem(string itemId, int quantity = 1)
        {
            var item = items.Find(x => x.id == itemId);
            if (item == null || item.quantity < quantity)
            {
                Debug.Log("Bạn không có đủ vật phẩm để bán.");
                return;
            }

            var price = item.price;
            if (!TryConsume(itemId, quantity))
            {
                Debug.Log("Bạn không có đủ vật phẩm để bán.");
                return;
            }

            if (hud != null)
            {
                hud.AddGold(quantity * price);
            }
            Debug.Log($"Bán thành công {quantity} {itemId} và nhận {quantity * price} vàng.");
        }

        public void SellItem(string itemId)
        {
            SellItem(itemId, 1);
        }

        public void ShowInventory()
        {
            if (items.Count == 0)
            {
                Debug.Log("Túi đồ trống.");
                return;
            }

            foreach (var item in items)
            {
                Debug.Log($"- {item.name}: {item.quantity}");
            }
        }
    }
}
