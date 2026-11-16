using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldCropGrowthByWeather : MonoBehaviour
    {
        [SerializeField] private float growthMultiplier = 1f;
        [SerializeField] private GameWorldWeatherSystem weatherSystem;
        [SerializeField] private GameWorldSeasonSystem seasonSystem;

        private void Awake()
        {
            if (weatherSystem == null)
            {
                weatherSystem = FindObjectOfType<GameWorldWeatherSystem>();
            }

            if (seasonSystem == null)
            {
                seasonSystem = FindObjectOfType<GameWorldSeasonSystem>();
            }
        }

        private void Update()
        {
            if (weatherSystem == null || seasonSystem == null) return;

            float multiplier = 1f;
            switch (weatherSystem.CurrentWeather)
            {
                case GameWorldWeatherSystem.WeatherType.Rain:
                    multiplier *= 1.3f;
                    break;
                case GameWorldWeatherSystem.WeatherType.Sunny:
                    multiplier *= 1.1f;
                    break;
                case GameWorldWeatherSystem.WeatherType.Fog:
                    multiplier *= 0.9f;
                    break;
                case GameWorldWeatherSystem.WeatherType.Snow:
                    multiplier *= 0.65f;
                    break;
                case GameWorldWeatherSystem.WeatherType.Sandstorm:
                    multiplier *= 0.55f;
                    break;
                case GameWorldWeatherSystem.WeatherType.Cloudy:
                    multiplier *= 1f;
                    break;
            }

            switch (seasonSystem.CurrentSeason)
            {
                case GameWorldSeasonSystem.SeasonType.Spring:
                    multiplier *= 1.2f;
                    break;
                case GameWorldSeasonSystem.SeasonType.Summer:
                    multiplier *= 1.1f;
                    break;
                case GameWorldSeasonSystem.SeasonType.Autumn:
                    multiplier *= 0.9f;
                    break;
                case GameWorldSeasonSystem.SeasonType.Winter:
                    multiplier *= 0.7f;
                    break;
            }

            growthMultiplier = multiplier;
        }

        public float GetGrowthMultiplier()
        {
            return growthMultiplier;
        }
    }
}
