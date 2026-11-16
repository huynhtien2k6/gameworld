using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldLevelSystem : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int experience = 0;
        [SerializeField] private int experienceToNextLevel = 100;
        [SerializeField] private int vitality = 10;
        [SerializeField] private int attack = 10;
        [SerializeField] private int defense = 10;

        public int Level => level;
        public int Experience => experience;
        public int ExperienceToNextLevel => experienceToNextLevel;

        private void Start()
        {
            UpdateLevelProgression();
        }

        public void GainExperience(int amount)
        {
            if (amount <= 0) return;
            experience += amount;

            while (experience >= experienceToNextLevel)
            {
                experience -= experienceToNextLevel;
                level += 1;
                experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.35f);
                vitality += 3;
                attack += 2;
                defense += 2;
                Debug.Log($"Level up! Bạn đã lên cấp {level}.");
            }

            UpdateLevelProgression();
        }

        public void AddStats(int vitalityBonus, int attackBonus, int defenseBonus)
        {
            vitality += vitalityBonus;
            attack += attackBonus;
            defense += defenseBonus;
        }

        private void UpdateLevelProgression()
        {
            Debug.Log($"Level {level} | XP {experience}/{experienceToNextLevel} | HP {vitality * 10} | ATK {attack} | DEF {defense}");
        }
    }
}
