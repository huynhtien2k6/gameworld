using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldApiClient : MonoBehaviour
    {
        private const string AccessTokenKey = "GameWorld.AccessToken";
        private const string RefreshTokenKey = "GameWorld.RefreshToken";

        [SerializeField] private string baseUrl = "http://localhost:5162";

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }

        public void ConfigureBaseUrl(string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) baseUrl = value.TrimEnd('/');
        }

        public void RestoreSession()
        {
            AccessToken = PlayerPrefs.GetString(AccessTokenKey, string.Empty);
            RefreshToken = PlayerPrefs.GetString(RefreshTokenKey, string.Empty);
        }

        public void ClearSession()
        {
            AccessToken = null;
            RefreshToken = null;
            PlayerPrefs.DeleteKey(AccessTokenKey);
            PlayerPrefs.DeleteKey(RefreshTokenKey);
        }

        private void PersistSession()
        {
            if (!string.IsNullOrEmpty(AccessToken)) PlayerPrefs.SetString(AccessTokenKey, AccessToken);
            else PlayerPrefs.DeleteKey(AccessTokenKey);

            if (!string.IsNullOrEmpty(RefreshToken)) PlayerPrefs.SetString(RefreshTokenKey, RefreshToken);
            else PlayerPrefs.DeleteKey(RefreshTokenKey);
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
            {
                PersistSession();
            }
        }

        private void OnApplicationQuit()
        {
            PersistSession();
        }

        public void SetSession(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            PersistSession();
        }

        public IEnumerator Register(string username, string email, string password, Action<string> completed, Action<string> failed)
        {
            var body = new RegisterRequest { username = username, email = email, password = password };
            yield return Send<ApiMessage>("POST", "/api/auth/register", body, false, false,
                result => completed?.Invoke(result.message), failed);
        }

        public IEnumerator Login(string username, string password, Action<AuthResponse> completed, Action<string> failed)
        {
            var body = new LoginRequest { username = username, password = password };
            yield return Send<AuthResponse>("POST", "/api/auth/login", body, false, false, result =>
            {
                AccessToken = result.token;
                RefreshToken = result.refreshToken;
                SetSession(AccessToken, RefreshToken);
                completed?.Invoke(result);
            }, failed);
        }

        public IEnumerator GetCharacters(Action<CharacterSummary[]> completed, Action<string> failed)
        {
            yield return SendArray<CharacterSummary>("/api/character/me", result => completed?.Invoke(result), failed);
        }

        public IEnumerator CreateCharacter(string name, int classId, int raceId, Action<ApiMessage> completed, Action<string> failed)
        {
            var body = new CreateCharacterRequest { name = name, classId = classId, raceId = raceId, mapId = 1 };
            yield return Send<ApiMessage>("POST", "/api/character/create", body, true, true, completed, failed);
        }

        public IEnumerator GetGameplayState(long characterId, Action<GameplayState> completed, Action<string> failed)
        {
            yield return Send<GameplayState>("GET", "/api/gameplay/" + characterId + "/state", null, true, true, completed, failed);
        }

        public IEnumerator PostAction<T>(long characterId, string action, object body, Action<T> completed, Action<string> failed)
        {
            yield return Send<T>("POST", "/api/gameplay/" + characterId + "/" + action, body, true, true, completed, failed);
        }

        private IEnumerator SendArray<T>(string path, Action<T[]> completed, Action<string> failed, bool allowRefresh = true)
        {
            using (var request = CreateRequest("GET", path, null, true))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (request.responseCode == 401 && allowRefresh && !string.IsNullOrEmpty(RefreshToken))
                    {
                        var refreshed = false;
                        yield return RefreshSession(value => refreshed = value);
                        if (refreshed)
                        {
                            yield return SendArray(path, completed, failed, false);
                            yield break;
                        }
                        ClearSession();
                        failed?.Invoke("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                        yield break;
                    }
                    failed?.Invoke(GetError(request));
                    yield break;
                }

                var text = request.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    completed?.Invoke(Array.Empty<T>());
                    yield break;
                }

                var items = JsonUtility.FromJson<T[]>(text);
                completed?.Invoke(items ?? Array.Empty<T>());
            }
        }
        [Serializable] private sealed class ArrayWrapper<T> { public T[] items; }

        private IEnumerator Send<T>(string method, string path, object body, bool authenticated, bool allowRefresh,
            Action<T> completed, Action<string> failed)
        {
            using (var request = CreateRequest(method, path, body, authenticated))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    var error = GetError(request);
                    var shouldRefresh = request.responseCode == 401 && authenticated && allowRefresh && !string.IsNullOrEmpty(RefreshToken);
                    if (!shouldRefresh)
                    {
                        failed?.Invoke(error);
                        yield break;
                    }

                    var refreshed = false;
                    yield return RefreshSession(value => refreshed = value);
                    if (!refreshed)
                    {
                        AccessToken = null;
                        RefreshToken = null;
                        failed?.Invoke("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                        yield break;
                    }

                    yield return Send(method, path, body, authenticated, false, completed, failed);
                    yield break;
                }

                var text = request.downloadHandler.text;
                completed?.Invoke(string.IsNullOrEmpty(text) ? default : JsonUtility.FromJson<T>(text));
            }
        }

        private IEnumerator RefreshSession(Action<bool> completed)
        {
            var body = new RefreshRequest { refreshToken = RefreshToken };
            using (var request = CreateRequest("POST", "/api/auth/refresh", body, false))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed?.Invoke(false);
                    yield break;
                }

                var result = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                AccessToken = result?.token;
                RefreshToken = result?.refreshToken;
                if (!string.IsNullOrEmpty(AccessToken)) SetSession(AccessToken, RefreshToken);
                completed?.Invoke(!string.IsNullOrEmpty(AccessToken));
            }
        }

        private UnityWebRequest CreateRequest(string method, string path, object body, bool authenticated)
        {
            var request = new UnityWebRequest(baseUrl.TrimEnd('/') + path, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15
            };
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (authenticated && !string.IsNullOrEmpty(AccessToken))
                request.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            return request;
        }

        private static string GetError(UnityWebRequest request)
        {
            var text = request.downloadHandler?.text;
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    var error = JsonUtility.FromJson<ApiMessage>(text);
                    if (!string.IsNullOrEmpty(error?.message)) return error.message;
                }
                catch (ArgumentException) { }
            }
            return request.error ?? "Không thể kết nối máy chủ (HTTP " + request.responseCode + ").";
        }

        [Serializable] private sealed class RegisterRequest { public string username; public string email; public string password; }
        [Serializable] private sealed class LoginRequest { public string username; public string password; }
        [Serializable] private sealed class RefreshRequest { public string refreshToken; }
        [Serializable] private sealed class CreateCharacterRequest { public string name; public int classId; public int raceId; public long mapId; }

        [Serializable] public sealed class ApiMessage { public string message; }
        [Serializable] public sealed class AuthResponse { public string token; public string refreshToken; public string username; }
        [Serializable] public sealed class CharacterSummary
        {
            public long id;
            public string name;
            public int level;
            public int currentHP;
            public int currentMP;
            public long mapId;
            public double gold;
        }
        [Serializable] public sealed class GameplayState
        {
            public CharacterState character;
            public MapState map;
            public WeatherState weather;
            public InventoryItem[] inventory;
            public PlotState[] plots;
            public MonsterState[] monsters;
            public CropState[] crops;
            public RecipeState[] recipes;
            public MapState[] maps;
        }
        [Serializable] public sealed class CharacterState
        {
            public long id;
            public string name;
            public int level;
            public long experience;
            public long experienceToNextLevel;
            public int currentHP;
            public int maxHP;
            public int currentMP;
            public long mapId;
            public double gold;
        }
        [Serializable] public sealed class MapState { public long id; public string name; public long regionId; public int minLevel; public double travelCost; public bool isSafeZone; }
        [Serializable] public sealed class WeatherState { public string type; public string season; public double cropModifier; public double movementModifier; public int intensity; }
        [Serializable] public sealed class InventoryItem { public long id; public long itemId; public string name; public int quantity; public bool isEquipped; }
        [Serializable] public sealed class PlotState { public long id; public long cropId; public string name; public int expectedYield; public bool ready; public bool isHarvested; }
        [Serializable] public sealed class MonsterState { public long id; public string name; public long mapId; public int level; public int hp; }
        [Serializable] public sealed class CropState { public long id; public string name; public int growthTimeMinutes; public int baseYield; }
        [Serializable] public sealed class RecipeState { public long id; public string name; public int resultItemId; public int quantity; }
    }
}
