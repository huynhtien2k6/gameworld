using UnityEngine;

namespace GameWorld.UnityClient
{
    [DisallowMultipleComponent]
    public sealed class GameWorldBootstrapper : MonoBehaviour
    {
        [SerializeField] private string apiBaseUrl = "http://localhost:5162";
        [SerializeField] private bool createVillageScene = true;

        private void Awake()
        {
            var client = GetComponent<GameWorldClient>();
            if (client == null)
            {
                client = gameObject.AddComponent<GameWorldClient>();
            }

            var field = typeof(GameWorldClient).GetField("apiBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(client, apiBaseUrl);
            }

            if (createVillageScene && GetComponent<PrototypeVillageScene>() == null)
            {
                gameObject.AddComponent<PrototypeVillageScene>();
            }
        }
    }
}
