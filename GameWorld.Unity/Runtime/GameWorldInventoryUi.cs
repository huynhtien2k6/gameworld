using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldInventoryUi : MonoBehaviour
    {
        [SerializeField] private GameWorldInventorySystem inventorySystem;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        private bool visible;

        private void Awake()
        {
            if (inventorySystem == null)
            {
                inventorySystem = FindObjectOfType<GameWorldInventorySystem>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
                if (visible)
                {
                    inventorySystem?.ShowInventory();
                }
            }
        }

        private void OnGUI()
        {
            if (!visible || inventorySystem == null) return;

            var box = new Rect(20f, 140f, 300f, 220f);
            GUI.Box(box, "Túi đồ");

            var y = 170f;
            foreach (var item in inventorySystem.Items)
            {
                GUI.Label(new Rect(30f, y, 240f, 24f), $"- {item.name}: {item.quantity}");
                y += 24f;
            }
        }
    }
}
