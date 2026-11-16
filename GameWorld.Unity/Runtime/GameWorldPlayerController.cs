using UnityEngine;

namespace GameWorld.UnityClient
{
    [DisallowMultipleComponent]
    public sealed class GameWorldPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private bool useThirdPersonCamera = true;

        private Vector3 lastMoveDirection;
        private CharacterController controller;
        private Vector3 moveInput;
        private Collider[] overlapBuffer = new Collider[12];

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<CharacterController>();
            }

            controller.center = new Vector3(0f, 1f, 0f);
            controller.radius = 0.45f;
            controller.height = 1.8f;
            controller.stepOffset = 0.35f;
            controller.enabled = true;

            if (cameraPivot == null)
            {
                var cameraObject = GameObject.Find("World Camera");
                if (cameraObject != null)
                {
                    cameraPivot = cameraObject.transform;
                }
            }
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveInput = Vector3.zero;

            if (cameraPivot != null && useThirdPersonCamera)
            {
                Vector3 forward = Vector3.ProjectOnPlane(cameraPivot.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cameraPivot.right, Vector3.up).normalized;

                if (forward.sqrMagnitude < 0.01f)
                {
                    forward = Vector3.forward;
                    right = Vector3.right;
                }

                moveInput = (forward * v + right * h).normalized;
            }
            else
            {
                moveInput = new Vector3(h, 0f, v).normalized;
            }

            if (moveInput.sqrMagnitude > 0.01f)
            {
                lastMoveDirection = moveInput;
                float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
                Quaternion desiredRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
            }

            if (controller != null)
            {
                Vector3 velocity = moveInput * moveSpeed;
                velocity.y = -5f;
                controller.Move(velocity * Time.deltaTime);
            }
            else
            {
                transform.position += moveInput * moveSpeed * Time.deltaTime;
            }

            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }
        }

        public void BindCamera(Transform targetCameraPivot)
        {
            cameraPivot = targetCameraPivot;
        }

        private void TryInteract()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.8f, interactionRadius, overlapBuffer,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Collide);

            GameObject nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < hits; i++)
            {
                var col = overlapBuffer[i];
                if (col == null || col.transform == transform) continue;

                var distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = col.gameObject;
                }
            }

            if (nearest == null)
            {
                Debug.Log("Không có NPC hoặc điểm tương tác gần đây.");
                return;
            }

            var npc = nearest.GetComponent<GameWorldNpcMarker>();
            if (npc != null)
            {
                npc.Interact();
                return;
            }

            var enemy = nearest.GetComponent<GameWorldEnemyMarker>();
            if (enemy != null)
            {
                enemy.Interact();
                return;
            }

            Debug.Log("Tương tác với: " + nearest.name);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, interactionRadius);
        }
    }
}



















































































































































































































































































































































































































































































































































































































































































































































































































































































































































            var npc = nearest.GetComponent<GameWorldNpcMarker>();
            if (npc != null)
            {
                npc.Interact();
                return;
            }

            var enemy = nearest.GetComponent<GameWorldEnemyMarker>();
            if (enemy != null)
            {
                enemy.Interact();
                return;
            }

            Debug.Log("Tương tác với: " + nearest.name);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, interactionRadius);
        }
    }
}
