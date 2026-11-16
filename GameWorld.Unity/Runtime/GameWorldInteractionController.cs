using UnityEngine;

namespace GameWorld.UnityClient
{
    [DisallowMultipleComponent]
    public sealed class GameWorldInteractionController : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 2.75f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private void Update()
        {
            if (!Input.GetKeyDown(interactKey)) return;
            TryInteract();
        }

        private void TryInteract()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Collider[] hits = Physics.OverlapSphere(player.transform.position + Vector3.up * 0.8f, interactionRadius,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Collide);

            IGameWorldInteractable closest = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var interactable = hit.GetComponent<IGameWorldInteractable>();
                if (interactable == null) continue;

                var distance = Vector3.Distance(player.transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }

            if (closest == null)
            {
                Debug.Log("Không có vật thể nào có thể tương tác gần đây.");
                return;
            }

            closest.Interact();
        }
    }
}
