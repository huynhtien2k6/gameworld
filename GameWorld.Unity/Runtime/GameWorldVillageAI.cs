using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldVillageAI : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 4f;
        [SerializeField] private float wanderSpeed = 1.2f;
        [SerializeField] private float changeDirectionDelay = 3f;

        private Vector3 startPosition;
        private float nextDirectionChange;
        private Vector3 targetPoint;

        private void Start()
        {
            startPosition = transform.position;
            PickNewTarget();
        }

        private void Update()
        {
            if (Time.time >= nextDirectionChange)
            {
                PickNewTarget();
            }

            Vector3 direction = (targetPoint - transform.position).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.position += direction * wanderSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 2f * Time.deltaTime);
            }
        }

        private void PickNewTarget()
        {
            float x = Random.Range(-wanderRadius, wanderRadius);
            float z = Random.Range(-wanderRadius, wanderRadius);
            targetPoint = startPosition + new Vector3(x, 0f, z);
            nextDirectionChange = Time.time + changeDirectionDelay;
        }
    }
}
