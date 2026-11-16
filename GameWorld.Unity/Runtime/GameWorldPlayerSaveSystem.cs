using System;
using System.IO;
using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldPlayerSaveSystem : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "gameworld-save.json";

        private GameWorldPlayerHud playerHud;
        private GameWorldInventorySystem inventorySystem;
        private GameWorldQuestController questController;

        private void Awake()
        {
            playerHud = FindObjectOfType<GameWorldPlayerHud>();
            inventorySystem = FindObjectOfType<GameWorldInventorySystem>();
            questController = FindObjectOfType<GameWorldQuestController>();
        }

        public void SaveGame()
        {
            var saveData = new SaveData
            {
                gold = playerHud != null ? playerHud.Gold : 100,
                health = playerHud != null ? playerHud.CurrentHealth : 100,
                maxHealth = playerHud != null ? playerHud.MaxHealth : 100,
                inventory = inventorySystem != null ? inventorySystem.Items : Array.Empty<GameWorldInventorySystem.InventoryItem>(),
                saveTime = DateTime.UtcNow
            };

            var path = Path.Combine(Application.persistentDataPath, saveFileName);
            var json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);
            Debug.Log($"Game saved to {path}");
        }

        public void LoadGame()
        {
            var path = Path.Combine(Application.persistentDataPath, saveFileName);
            if (!File.Exists(path))
            {
                Debug.Log("No save file found.");
                return;
            }

            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;

            if (playerHud != null)
            {
                playerHud.SetGold(data.gold);
                playerHud.SetHealth(data.health, data.maxHealth);
            }

            if (inventorySystem != null && data.inventory != null)
            {
                foreach (var item in data.inventory)
                {
                    inventorySystem.AddItem(item.id, item.name, item.quantity, item.price);
                }
            }

            Debug.Log($"Game loaded from {path} at {data.saveTime}");
        }

        [Serializable]
        private sealed class SaveData
        {
            public int gold;
            public int health;
            public int maxHealth;
            public GameWorldInventorySystem.InventoryItem[] inventory;
            public DateTime saveTime;
        }
    }
}
