using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldWeatherSystem : MonoBehaviour
    {
        public enum WeatherType
        {
            Sunny,
            Cloudy,
            Rain,
            Fog,
            Snow,
            Sandstorm
        }

        [SerializeField] private WeatherType currentWeather = WeatherType.Sunny;
        [SerializeField] private float weatherDuration = 25f;
        [SerializeField] private float transitionSpeed = 1.5f;

        private float elapsed;
        private Color skyTint = Color.white;
        private float lightIntensity = 1f;

        public WeatherType CurrentWeather => currentWeather;

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= weatherDuration)
            {
                elapsed = 0f;
                currentWeather = NextWeather();
            }

            UpdateLighting();
        }

        private WeatherType NextWeather()
        {
            var values = System.Enum.GetValues(typeof(WeatherType));
            int index = (int)currentWeather;
            index = (index + 1) % values.Length;
            return (WeatherType)values.GetValue(index);
        }

        private void UpdateLighting()
        {
            var sun = FindObjectOfType<Light>();
            if (sun == null) return;

            switch (currentWeather)
            {
                case WeatherType.Sunny:
                    skyTint = new Color(0.66f, 0.82f, 1f);
                    lightIntensity = 1.2f;
                    break;
                case WeatherType.Cloudy:
                    skyTint = new Color(0.75f, 0.77f, 0.83f);
                    lightIntensity = 0.8f;
                    break;
                case WeatherType.Rain:
                    skyTint = new Color(0.48f, 0.58f, 0.68f);
                    lightIntensity = 0.7f;
                    break;
                case WeatherType.Fog:
                    skyTint = new Color(0.78f, 0.82f, 0.86f);
                    lightIntensity = 0.5f;
                    break;
                case WeatherType.Snow:
                    skyTint = new Color(0.92f, 0.96f, 1f);
                    lightIntensity = 0.65f;
                    break;
                case WeatherType.Sandstorm:
                    skyTint = new Color(0.86f, 0.7f, 0.52f);
                    lightIntensity = 0.55f;
                    break;
            }

            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, skyTint, Time.deltaTime * transitionSpeed);
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(sun.intensity, lightIntensity, Time.deltaTime * transitionSpeed);
            }
        }

        public string WeatherLabel()
        {
            return currentWeather switch
            {
                WeatherType.Sunny => "Nắng",
                WeatherType.Cloudy => "Nhiều mây",
                WeatherType.Rain => "Mưa",
                WeatherType.Fog => "Sương mù",
                WeatherType.Snow => "Tuyết",
                WeatherType.Sandstorm => "Bão cát",
                _ => "Thời tiết"
            };
        }
    }
}
