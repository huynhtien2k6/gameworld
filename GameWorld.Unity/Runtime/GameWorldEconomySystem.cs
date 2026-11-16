using System.Collections.Generic;
using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldEconomySystem : MonoBehaviour
    {
        [System.Serializable]
        public sealed class MarketItem
        {
            public string id;
            public string name;
            public int basePrice;
            public float priceMultiplier = 1f;
            public int stock;
        }

        [SerializeField] private List<MarketItem> marketItems = new List<MarketItem>
        {
            new MarketItem { id = "wheat", name = "Lúa mì", basePrice = 12, priceMultiplier = 1f, stock = 20 },
            new MarketItem { id = "meat", name = "Thịt", basePrice = 18, priceMultiplier = 1f, stock = 12 },
            new MarketItem { id = "herb", name = "Thảo dược", basePrice = 24, priceMultiplier = 1f, stock = 8 },
            new MarketItem { id = "bread", name = "Bánh mì", basePrice = 28, priceMultiplier = 1f, stock = 10 }
        };

        [SerializeField] private GameWorldSeasonSystem seasonSystem;
        [SerializeField] private GameWorldWeatherSystem weatherSystem;
        private GameWorldPlayerHud playerHud;
        private GameWorldInventorySystem inventorySystem;

        public List<MarketItem> GetAllItems()
        {
            return marketItems;
        }

        private void Awake()
        {
            if (seasonSystem == null) seasonSystem = FindObjectOfType<GameWorldSeasonSystem>();
            if (weatherSystem == null) weatherSystem = FindObjectOfType<GameWorldWeatherSystem>();
            playerHud = FindObjectOfType<GameWorldPlayerHud>();
            inventorySystem = FindObjectOfType<GameWorldInventorySystem>();
        }

        public void BuyItem(string itemId)
        {
            var item = marketItems.Find(x => x.id == itemId);
            if (item == null) return;

            int price = GetPrice(itemId);
            if (playerHud != null && playerHud.Gold < price)
            {
                Debug.Log("Không đủ vàng để mua vật phẩm.");
                return;
            }

            if (inventorySystem != null)
            {
                inventorySystem.AddItem(item.id, item.name, 1, price);
            }

            if (playerHud != null)
            {
                playerHud.AddGold(-price);
            }

            item.stock = Mathf.Max(0, item.stock - 1);
            Debug.Log($"Bạn đã mua {item.name} với giá {price} vàng.");
        }

        public void SellItem(string itemId)
        {
            var item = marketItems.Find(x => x.id == itemId);
            if (item == null) return;

            if (inventorySystem == null || !inventorySystem.TryConsume(itemId, 1))
            {
                Debug.Log("Bạn không có vật phẩm này để bán.");
                return;
            }

            int price = GetPrice(itemId);
            if (playerHud != null)
            {
                playerHud.AddGold(price);
            }

            item.stock += 1;
            Debug.Log($"Bạn đã bán {item.name} với giá {price} vàng.");
        }

        public int GetPrice(string itemId)
        {
            var item = marketItems.Find(x => x.id == itemId);
            if (item == null) return 0;

            float multiplier = item.priceMultiplier;
            if (seasonSystem != null)
            {
                switch (seasonSystem.CurrentSeason)
                {
                    case GameWorldSeasonSystem.SeasonType.Spring:
                        multiplier *= 1.05f;
                        break;
                    case GameWorldSeasonSystem.SeasonType.Summer:
                        multiplier *= 1.1f;
                        break;
                    case GameWorldSeasonSystem.SeasonType.Autumn:
                        multiplier *= 0.95f;
                        break;
                    case GameWorldSeasonSystem.SeasonType.Winter:
                        multiplier *= 0.85f;
                        break;
                }
            }

            if (weatherSystem != null)
            {
                switch (weatherSystem.CurrentWeather)
                {
                    case GameWorldWeatherSystem.WeatherType.Rain:
                        multiplier *= 1.18f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Sandstorm:
                        multiplier *= 1.3f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Snow:
                        multiplier *= 1.1f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Cloudy:
                        multiplier *= 0.95f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Fog:
                        multiplier *= 1f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Sunny:
                        multiplier *= 1.05f;
                        break;
                }
            }

            return Mathf.RoundToInt(item.basePrice * multiplier);
        }

        public void UpdatePrice(string itemId, float modifier)
        {
            var item = marketItems.Find(x => x.id == itemId);
            if (item == null) return;
            item.priceMultiplier = Mathf.Clamp(item.priceMultiplier * modifier, 0.5f, 3f);
        }

        public void ShowMarket()
        {
            Debug.Log("Thị trường:");
            foreach (var item in marketItems)
            {
                Debug.Log($"- {item.name}: {GetPrice(item.id)} vàng ({item.stock} trong kho)");
            }
        }
    }
}


































            }

            if (weatherSystem != null)
            {
                switch (weatherSystem.CurrentWeather)
                {
                    case GameWorldWeatherSystem.WeatherType.Rain:
                        multiplier *= 1.18f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Sandstorm:
                        multiplier *= 1.3f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Snow:
                        multiplier *= 1.1f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Cloudy:
                        multiplier *= 0.95f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Fog:
                        multiplier *= 1f;
                        break;
                    case GameWorldWeatherSystem.WeatherType.Sunny:
                        multiplier *= 1.05f;
                        break;
                }
            }

            return Mathf.RoundToInt(item.basePrice * multiplier);
        }

        public void UpdatePrice(string itemId, float modifier)
        {
            var item = marketItems.Find(x => x.id == itemId);
            if (item == null) return;
            item.priceMultiplier = Mathf.Clamp(item.priceMultiplier * modifier, 0.5f, 3f);
        }

        public void ShowMarket()
        {
            Debug.Log("Thị trường:");
            foreach (var item in marketItems)
            {
                Debug.Log($"- {item.name}: {GetPrice(item.id)} vàng ({item.stock} trong kho)");
            }
        }
    }
}
