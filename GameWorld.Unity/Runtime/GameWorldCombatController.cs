using UnityEngine;

namespace GameWorld.UnityClient
{
    [DisallowMultipleComponent]
    public sealed class GameWorldCombatController : MonoBehaviour
    {
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackCooldown = 0.75f;
        [SerializeField] private int baseDamage = 8;
        [SerializeField] private KeyCode attackKey = KeyCode.Space;

        private float nextAttackAt;
        private GameWorldPlayerHud hud;

        private void Awake()
        {
            hud = FindObjectOfType<GameWorldPlayerHud>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(attackKey)) return;
            if (Time.time < nextAttackAt) return;

            nextAttackAt = Time.time + attackCooldown;
            AttackNearestEnemy();
        }

        private void AttackNearestEnemy()
        {
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.8f, attackRange,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Collide);

            GameWorldEnemyMarker nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<GameWorldEnemyMarker>();
                if (enemy == null || !enemy.IsAlive) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            if (nearest == null)
            {
                Debug.Log("Không có quái vật nào trong tầm đánh.");
                return;
            }

            nearest.TakeDamage(baseDamage);
            Debug.Log($"Bạn đánh {nearest.EnemyName} gây {baseDamage} sát thương.");

            if (!nearest.IsAlive)
            {
                if (hud != null)
                {
                    hud.AddGold(nearest.GoldReward);
                }
                else
                {
                    Debug.Log($"Bạn nhận {nearest.GoldReward} vàng.");
                }
            }
        }
    }
}
