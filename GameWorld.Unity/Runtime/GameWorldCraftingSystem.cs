using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldCraftingSystem : MonoBehaviour
    {
        [System.Serializable]
        public sealed class CraftRecipe
        {
            public string id;
            public string name;
            public List<string> ingredients = new List<string>();
            public int goldCost;
            public string outputItem;
        }

        [SerializeField] private List<CraftRecipe> recipes = new List<CraftRecipe>
        {
            new CraftRecipe
            {
                id = "bread",
                name = "Bánh mì",
                ingredients = new List<string> { "wheat", "wheat" },
                goldCost = 10,
                outputItem = "bread"
            },
            new CraftRecipe
            {
                id = "potion",
                name = "Thuốc hồi máu",
                ingredients = new List<string> { "herb", "herb" },
                goldCost = 20,
                outputItem = "potion"
            }
        };

        private GameWorldInventorySystem inventorySystem;
        private GameWorldPlayerHud playerHud;

        private void Awake()
        {
            inventorySystem = FindObjectOfType<GameWorldInventorySystem>();
            playerHud = FindObjectOfType<GameWorldPlayerHud>();
        }

        public void Craft(string recipeId)
        {
            var recipe = recipes.Find(x => x.id == recipeId);
            if (recipe == null)
            {
                Debug.LogWarning("Công thức không tồn tại.");
                return;
            }

            if (inventorySystem == null)
            {
                Debug.Log("Không tìm thấy hệ thống kho đồ.");
                return;
            }

            if (playerHud != null && playerHud.Gold < recipe.goldCost)
            {
                Debug.Log("Không đủ vàng để nấu công thức này.");
                return;
            }

            var required = new Dictionary<string, int>();
            foreach (var ingredient in recipe.ingredients)
            {
                if (!required.ContainsKey(ingredient))
                {
                    required[ingredient] = 0;
                }
                required[ingredient]++;
            }

            foreach (var requiredIngredient in required)
            {
                if (!inventorySystem.Items.Any(x => x.id == requiredIngredient.Key && x.quantity >= requiredIngredient.Value))
                {
                    Debug.Log($"Thiếu nguyên liệu: {requiredIngredient.Key} (cần {requiredIngredient.Value})");
                    return;
                }
            }

            foreach (var requiredIngredient in required)
            {
                inventorySystem.TryConsume(requiredIngredient.Key, requiredIngredient.Value);
            }

            if (playerHud != null)
            {
                playerHud.AddGold(-recipe.goldCost);
            }

            inventorySystem.AddItem(recipe.outputItem, recipe.name, 1, 25);
            Debug.Log($"Bạn đã chế tạo {recipe.name}.");
        }
    }
}


























































































































































































































































    }
}
