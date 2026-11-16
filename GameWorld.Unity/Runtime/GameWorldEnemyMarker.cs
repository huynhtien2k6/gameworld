using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldEnemyMarker : MonoBehaviour, IGameWorldInteractable
    {
        [SerializeField] private string enemyName = "Quái vật";
        [SerializeField] private int level = 1;
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private int damage = 5;
        [SerializeField] private int goldReward = 15;

        private int currentHealth;
        private bool defeated;

        public string EnemyName => enemyName;
        public int Level => level;
        public bool IsAlive => !defeated && currentHealth > 0;
        public int Damage => damage;
        public int GoldReward => goldReward;

        private void Awake()
        {
            currentHealth = maxHealth;
            defeated = false;
        }

        public void Interact()
        {
            Debug.Log($"Thách đấu với {enemyName} cấp {level}.");
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            currentHealth -= amount;
            Debug.Log($"{enemyName} nhận {amount} sát thương. HP còn lại: {Mathf.Max(0, currentHealth)}");

            if (currentHealth <= 0)
            {
                Defeat();
            }
        }

        public void Defeat()
        {
            if (defeated) return;
            defeated = true;
            currentHealth = 0;
            Debug.Log($"{enemyName} đã ngã xuống. Bạn nhận {goldReward} vàng.");
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            defeated = false;
        }
    }
}








































































































































