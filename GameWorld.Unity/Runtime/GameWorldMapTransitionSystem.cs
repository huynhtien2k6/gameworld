using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldMapTransitionSystem : MonoBehaviour
    {
        [SerializeField] private string currentMapName = "Làng Tân Thủ";
        [SerializeField] private Vector3 spawnPoint = new Vector3(0f, 1f, 0f);

        private readonly string[] mapNames = { "Làng Tân Thủ", "Rừng Ven Làng", "Đồi Núi", "Sa Mạc" };
        private int mapIndex;

        public string CurrentMapName => currentMapName;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = spawnPoint;
            }
        }

        public void TransitionToNextMap()
        {
            mapIndex = (mapIndex + 1) % mapNames.Length;
            currentMapName = mapNames[mapIndex];
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = spawnPoint;
            }

            Debug.Log($"Bạn đã chuyển sang: {currentMapName}");
        }

        public void TransitionToMap(string mapName)
        {
            for (int i = 0; i < mapNames.Length; i++)
            {
                if (string.Equals(mapNames[i], mapName, System.StringComparison.OrdinalIgnoreCase))
                {
                    mapIndex = i;
                    currentMapName = mapNames[i];
                    var player = GameObject.FindWithTag("Player");
                    if (player != null)
                    {
                        player.transform.position = spawnPoint;
                    }

                    Debug.Log($"Bạn đã chuyển sang: {currentMapName}");
                    return;
                }
            }

            Debug.LogWarning($"Không tìm thấy bản đồ: {mapName}");
        }
    }
}
