using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldMapPortal : MonoBehaviour, IGameWorldInteractable
    {
        [SerializeField] private string destinationName = "Bản đồ mới";
        [SerializeField] private Vector3 destinationPosition = new Vector3(0f, 1f, 0f);
        [SerializeField] private float radius = 2f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawLine(transform.position, destinationPosition);
        }

        public void Teleport(GameObject player)
        {
            if (player == null) return;
            player.transform.position = destinationPosition;
            Debug.Log($"Bạn đã dịch chuyển đến {destinationName}.");
        }

        public void Interact()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            Teleport(player);
        }
    }
}










































































