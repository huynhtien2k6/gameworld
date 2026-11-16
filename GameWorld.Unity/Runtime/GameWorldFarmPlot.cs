using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldFarmPlot : MonoBehaviour, IGameWorldInteractable
    {
        [SerializeField] private string cropName = "Lúa mì";
        [SerializeField] private int growthStage = 0;
        [SerializeField] private int maxStage = 3;
        [SerializeField] private float growInterval = 4f;
        [SerializeField] private bool planted;

        private float nextGrowAt;

        private void Start()
        {
            nextGrowAt = Time.time + growInterval;
        }

        private void Update()
        {
            if (!planted) return;
            if (Time.time < nextGrowAt) return;

            nextGrowAt = Time.time + growInterval;
            growthStage = Mathf.Min(maxStage, growthStage + 1);
        }

        public void Plant()
        {
            if (planted) return;
            planted = true;
            growthStage = 1;
            nextGrowAt = Time.time + growInterval;
            Debug.Log($"Bạn đã gieo {cropName}.");
        }

        public bool CanHarvest()
        {
            return planted && growthStage >= maxStage;
        }

        public void Harvest()
        {
            if (!CanHarvest()) return;
            planted = false;
            growthStage = 0;
            Debug.Log($"Bạn thu hoạch {cropName} thành công.");
        }

        public void Interact()
        {
            if (!planted)
            {
                Plant();
                return;
            }

            if (CanHarvest())
            {
                Harvest();
                return;
            }

            Debug.Log($"{cropName} đang phát triển ở giai đoạn {growthStage}/{maxStage}.");
        }
    }
}
































































































