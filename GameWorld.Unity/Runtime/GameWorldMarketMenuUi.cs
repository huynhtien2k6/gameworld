using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldMarketMenuUi : MonoBehaviour
    {
        [SerializeField] private GameWorldEconomySystem economySystem;
        [SerializeField] private GameWorldInventorySystem inventorySystem;
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        private bool visible;

        private void Awake()
        {
            if (economySystem == null) economySystem = FindObjectOfType<GameWorldEconomySystem>();
            if (inventorySystem == null) inventorySystem = FindObjectOfType<GameWorldInventorySystem>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            var box = new Rect(250f, 80f, 420f, 300f);
            GUI.Box(box, "Thị trường");

            if (economySystem == null)
            {
                GUI.Label(new Rect(270f, 120f, 380f, 24f), "Không có hệ thống kinh tế.");
                return;
            }

            var items = economySystem.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                GUI.Label(new Rect(270f, 120f + i * 28f, 280f, 24f), $"{item.name}: {economySystem.GetPrice(item.id)} vàng");
                if (GUI.Button(new Rect(520f, 120f + i * 28f, 120f, 24f), "Mua"))
                {
                    economySystem.BuyItem(item.id);
                }
            }
        }
    }
}
