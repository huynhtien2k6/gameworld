using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldSeasonSystem : MonoBehaviour
    {
        public enum SeasonType
        {
            Spring,
            Summer,
            Autumn,
            Winter
        }

        [SerializeField] private SeasonType currentSeason = SeasonType.Spring;
        [SerializeField] private float seasonDuration = 90f;

        private float elapsed;

        public SeasonType CurrentSeason => currentSeason;

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= seasonDuration)
            {
                elapsed = 0f;
                currentSeason = NextSeason();
                Debug.Log($"Mùa mới: {CurrentSeasonLabel()}");
            }
        }

        private SeasonType NextSeason()
        {
            int next = ((int)currentSeason + 1) % 4;
            return (SeasonType)next;
        }

        public string CurrentSeasonLabel()
        {
            return currentSeason switch
            {
                SeasonType.Spring => "Xuân",
                SeasonType.Summer => "Hè",
                SeasonType.Autumn => "Thu",
                SeasonType.Winter => "Đông",
                _ => "Mùa"
            };
        }
    }
}
