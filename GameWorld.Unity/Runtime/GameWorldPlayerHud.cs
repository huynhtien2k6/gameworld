using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldPlayerHud : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int gold = 100;

        private GameWorldWeatherSystem weatherSystem;
        private GameWorldDayNightCycle dayNightCycle;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int Gold => gold;

        public void ApplyDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }

        public void SetGold(int value)
        {
            gold = value;
        }

        public void SetHealth(int value, int maxValue)
        {
            currentHealth = Mathf.Clamp(value, 0, maxValue);
            maxHealth = maxValue;
        }

        private void Awake()
        {
            weatherSystem = FindObjectOfType<GameWorldWeatherSystem>();
            var sunlight = GameObject.Find("Directional Light");
            if (sunlight != null)
            {
                dayNightCycle = sunlight.GetComponent<GameWorldDayNightCycle>();
            }
        }

        private void OnGUI()
        {
            var rect = new Rect(16f, 16f, 320f, 150f);
            GUI.Box(rect, "");
            GUI.Label(new Rect(24f, 24f, 280f, 24f), $"HP: {currentHealth}/{maxHealth}");
            GUI.Label(new Rect(24f, 52f, 280f, 24f), $"Vàng: {gold}");

            var weatherText = weatherSystem != null ? weatherSystem.WeatherLabel() : "Nắng";
            var timeText = dayNightCycle != null ? dayNightCycle.TimeLabel() : "07:00";
            GUI.Label(new Rect(24f, 80f, 280f, 24f), $"Thời tiết: {weatherText}");
            GUI.Label(new Rect(24f, 108f, 280f, 24f), $"Thời gian: {timeText}");
        }
    }
}
























































































































































































































































































































































































































