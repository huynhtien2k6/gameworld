using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldDayNightCycle : MonoBehaviour
    {
        [SerializeField] private float cycleMinutes = 6f;
        [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 1f, 0f);
        [SerializeField] private float startHour = 7f;

        private float timeOfDay;

        public float TimeOfDay => timeOfDay;

        private void Update()
        {
            timeOfDay += Time.deltaTime / (cycleMinutes * 60f) * 24f;
            if (timeOfDay >= 24f) timeOfDay -= 24f;

            var sun = GetComponent<Light>();
            if (sun == null) return;

            float angle = (timeOfDay / 24f) * 360f - 90f;
            transform.rotation = Quaternion.Euler(angle, 30f, 0f);

            float intensity = Mathf.Clamp01(Mathf.Sin((timeOfDay / 24f) * Mathf.PI * 2f - Mathf.PI / 2f) * 1.5f + 0.2f);
            sun.intensity = intensity;
        }

        public string TimeLabel()
        {
            int hour = Mathf.FloorToInt(timeOfDay);
            int minute = Mathf.FloorToInt((timeOfDay - hour) * 60f);
            return $"{hour:00}:{minute:00}";
        }
    }
}
