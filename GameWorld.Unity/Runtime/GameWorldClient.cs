using System.Collections;
using UnityEngine;

namespace GameWorld.UnityClient
{
    [DisallowMultipleComponent]
    public sealed class GameWorldClient : MonoBehaviour
    {
        private enum ScreenState { Authentication, Characters, World }

        [SerializeField] private string apiBaseUrl = "http://localhost:5162";
        private GameWorldApiClient api;
        private ScreenState screen;
        private bool registerMode;
        private bool busy;
        private string username = string.Empty;
        private string email = string.Empty;
        private string password = string.Empty;
        private string characterName = string.Empty;
        private string message = "Kết nối máy chủ để bắt đầu hành trình.";
        private int classId = 1;
        private long selectedCharacterId;
        private GameWorldApiClient.CharacterSummary[] characters = System.Array.Empty<GameWorldApiClient.CharacterSummary>();
        private GameWorldApiClient.GameplayState state;
        private Vector2 scroll;

        private void Awake()
        {
            api = gameObject.GetComponent<GameWorldApiClient>();
            if (api == null) api = gameObject.AddComponent<GameWorldApiClient>();
            api.ConfigureBaseUrl(apiBaseUrl);
            api.RestoreSession();
            if (GetComponent<PrototypeVillageScene>() == null) gameObject.AddComponent<PrototypeVillageScene>();
            if (!string.IsNullOrEmpty(api.AccessToken))
            {
                screen = ScreenState.Characters;
                message = "Phiên cũ đã được phục hồi. Đang tải nhân vật...";
                LoadCharacters();
            }
        }

        private void OnGUI()
        {
            var width = Mathf.Min(440f, Screen.width * 0.48f);
            var rect = new Rect(12f, 12f, width, Screen.height - 24f);
            GUILayout.BeginArea(rect, GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Space(8f);
            GUILayout.Label("THẾ GIỚI MỞ", HeaderStyle());
            GUILayout.Label("Bản Unity thử nghiệm · Làng Tân Thủ", GUILayout.Height(24f));
            GUILayout.Space(10f);

            if (screen == ScreenState.Authentication) DrawAuthentication();
            else if (screen == ScreenState.Characters) DrawCharacters();
            else DrawWorld();

            if (!string.IsNullOrEmpty(message))
            {
                GUILayout.Space(10f);
                GUILayout.Label(message, GUILayout.MinHeight(38f));
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawAuthentication()
        {
            GUILayout.Label(registerMode ? "Tạo tài khoản" : "Đăng nhập", GUILayout.Height(26f));
            username = TextField("Tên đăng nhập", username);
            if (registerMode) email = TextField("Email", email);
            password = PasswordField("Mật khẩu", password);
            using (new GUILayout.HorizontalScope())
            {
                if (Button(registerMode ? "Đăng ký" : "Đăng nhập", busy))
                {
                    busy = true;
                    message = "Đang kết nối máy chủ...";
                    if (registerMode) StartCoroutine(api.Register(username.Trim(), email.Trim(), password, OnRegistered, OnRequestFailed));
                    else StartCoroutine(api.Login(username.Trim(), password, OnLoggedIn, OnRequestFailed));
                }
                if (!busy && GUILayout.Button(registerMode ? "Quay lại" : "Tạo tài khoản", GUILayout.Height(36f)))
                {
                    registerMode = !registerMode;
                    message = string.Empty;
                }
            }
        }

        private void DrawCharacters()
        {
            GUILayout.Label("Chọn nhân vật", GUILayout.Height(26f));
            if (characters == null || characters.Length == 0)
            {
                GUILayout.Label("Bạn chưa có nhân vật nào. Hãy tạo nhân vật mới ở Làng Tân Thủ.");
            }
            else
            {
                foreach (var character in characters)
                {
                    var label = character.name + " · Cấp " + character.level + " · " + character.currentHP + " HP · " + character.gold + " vàng";
                    if (GUILayout.Button(label, GUILayout.Height(42f)))
                    {
                        selectedCharacterId = character.id;
                        busy = true;
                        message = "Đang tải thế giới...";
                        StartCoroutine(api.GetGameplayState(selectedCharacterId, OnWorldLoaded, OnRequestFailed));
                    }
                }
            }

            GUILayout.Space(14f);
            GUILayout.Label("Tạo nhân vật mới tại Làng Tân Thủ");
            characterName = TextField("Tên nhân vật", characterName);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Lớp", GUILayout.Width(60f));
            if (GUILayout.Toggle(classId == 1, "Chiến binh", "Button")) classId = 1;
            if (GUILayout.Toggle(classId == 2, "Thợ săn", "Button")) classId = 2;
            if (GUILayout.Toggle(classId == 3, "Pháp sư", "Button")) classId = 3;
            GUILayout.EndHorizontal();
            if (Button("Tạo nhân vật", busy))
            {
                if (string.IsNullOrWhiteSpace(characterName))
                {
                    message = "Tên nhân vật tối thiểu 3 ký tự.";
                    return;
                }

                busy = true;
                StartCoroutine(api.CreateCharacter(characterName.Trim(), classId, 1, _ => LoadCharacters(), OnRequestFailed));
            }
            if (!busy && GUILayout.Button("Đăng xuất", GUILayout.Height(32f)))
            {
                api.ClearSession();
                characters = System.Array.Empty<GameWorldApiClient.CharacterSummary>();
                screen = ScreenState.Authentication;
                message = "Đã đăng xuất.";
            }
        }

        private void OnLoggedIn(GameWorldApiClient.AuthResponse result)
        {
            busy = true;
            message = "Đang tải nhân vật...";
            LoadCharacters();
        }

        private void LoadCharacters()
        {
            StartCoroutine(api.GetCharacters(result =>
            {
                characters = result ?? System.Array.Empty<GameWorldApiClient.CharacterSummary>();
                screen = characters.Length == 0 ? ScreenState.Characters : ScreenState.Characters;
                busy = false;
                message = characters.Length == 0 ? "Tạo nhân vật đầu tiên của bạn." : "Mọi nhân vật mới đều bắt đầu ở Làng Tân Thủ.";
            }, OnRequestFailed));
        }

        private void OnWorldLoaded(GameWorldApiClient.GameplayState result)
        {
            state = result;
            screen = ScreenState.World;
            busy = false;
            message = "Chào mừng đến Làng Tân Thủ.";
        }

        private void LoadWorld()
        {
            if (selectedCharacterId <= 0)
            {
                screen = ScreenState.Characters;
                return;
            }

            busy = true;
            StartCoroutine(api.GetGameplayState(selectedCharacterId, OnWorldLoaded, OnRequestFailed));
        }

        private void DrawWorld()
        {
            if (state == null || state.character == null) return;
            var character = state.character;
            GUILayout.Label(character.name + " · Cấp " + character.level, GUILayout.Height(26f));
            GUILayout.Label("HP " + character.currentHP + "/" + character.maxHP + " · MP " + character.currentMP + " · Vàng " + character.gold);
            if (state.map != null) GUILayout.Label("Địa điểm: " + state.map.name);
            if (state.weather != null)
                GUILayout.Label("Mùa " + state.weather.season + " · " + WeatherName(state.weather.type) + " · Năng suất cây " + state.weather.cropModifier + "x");
            GUILayout.Space(8f);
            GUILayout.Label("Xoay góc nhìn bằng chuột ở phía bên phải màn hình.");
            GUILayout.Label("Hành động được máy chủ kiểm tra; cảnh 3D hiện là bản minh họa.", GUILayout.Height(34f));

            GUILayout.Label("Di chuyển");
            if (state.maps != null)
            {
                foreach (var map in state.maps)
                {
                    if (map.id == character.mapId) continue;
                    var label = map.name + " · cấp " + map.minLevel + " · phí " + map.travelCost;
                    if (Button(label, busy)) RunAction("travel", new TravelRequest { mapId = map.id });
                }
            }

            GUILayout.Label("Hành động");
            if (state.monsters != null && state.monsters.Length > 0)
            {
                var monster = state.monsters[0];
                if (Button("Chiến đấu: " + monster.name, busy)) RunAction("fight", new MonsterRequest { monsterId = monster.id });
            }
            if (state.crops != null && state.crops.Length > 0 && state.map != null && state.map.isSafeZone)
            {
                if (Button("Gieo trồng: " + state.crops[0].name, busy)) RunAction("plant", new CropRequest { cropId = state.crops[0].id });
            }
            if (state.plots != null)
            {
                foreach (var plot in state.plots)
                {
                    if (plot.isHarvested || !plot.ready) continue;
                    if (Button("Thu hoạch: " + plot.name, busy)) RunAction("harvest", new PlotRequest { plotId = plot.id });
                }
            }
            if (state.recipes != null && state.recipes.Length > 0)
            {
                if (Button("Nấu ăn: " + state.recipes[0].name, busy)) RunAction("cook", new RecipeRequest { recipeId = state.recipes[0].id });
            }
            if (Button("Nghỉ ngơi", busy)) RunAction("rest", null);
            if (Button("Làm mới trạng thái", busy)) LoadWorld();
            if (!busy && GUILayout.Button("Đổi nhân vật / đăng xuất", GUILayout.Height(34f)))
            {
                api.ClearSession();
                state = null;
                screen = ScreenState.Authentication;
            }

            GUILayout.Space(8f);
            GUILayout.Label("Túi đồ");
            if (state.inventory != null)
                foreach (var item in state.inventory)
                    GUILayout.Label(item.name + " × " + item.quantity + (item.isEquipped ? " · đang trang bị" : string.Empty));
        }

        private void OnRegistered(string result)
        {
            busy = false;
            registerMode = false;
            password = string.Empty;
            message = string.IsNullOrEmpty(result) ? "Đăng ký thành công. Hãy đăng nhập." : result;
        }

        private void OnLoggedIn(GameWorldApiClient.AuthResponse result)
        {
            busy = true;
            message = "Đang tải nhân vật...";
            LoadCharacters();
        }

        private void LoadCharacters()
        {
            StartCoroutine(api.GetCharacters(result =>
            {
                characters = result ?? System.Array.Empty<GameWorldApiClient.CharacterSummary>();
                screen = ScreenState.Characters;
                busy = false;
                message = characters.Length == 0 ? "Tạo nhân vật đầu tiên của bạn." : "Mọi nhân vật mới đều bắt đầu ở Làng Tân Thủ.";
            }, OnRequestFailed));
        }

        private void OnWorldLoaded(GameWorldApiClient.GameplayState result)
        {
            state = result;
            screen = ScreenState.World;
            busy = false;
            message = "Chào mừng đến Làng Tân Thủ.";
        }

        private void LoadWorld()
        {
            if (selectedCharacterId <= 0)
            {
                screen = ScreenState.Characters;
                return;
            }

            busy = true;
            StartCoroutine(api.GetGameplayState(selectedCharacterId, OnWorldLoaded, OnRequestFailed));
        }

        private void OnRequestFailed(string error)
        {
            busy = false;
            if (string.IsNullOrEmpty(api.AccessToken) && screen == ScreenState.World)
            {
                state = null;
                screen = ScreenState.Authentication;
            }
            message = error;
        }

        private static string TextField(string label, string value)
        {
            GUILayout.Label(label);
            return GUILayout.TextField(value, GUILayout.Height(34f));
        }

        private static string PasswordField(string label, string value)
        {
            GUILayout.Label(label);
            return GUILayout.PasswordField(value, '\u25cf', GUILayout.Height(34f));
        }

        private static bool Button(string label, bool disabled)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var previous = GUI.enabled;
                GUI.enabled = !disabled;
                var clicked = GUILayout.Button(label, GUILayout.Height(36f));
                GUI.enabled = previous;
                GUILayout.FlexibleSpace();
                return clicked;
            }
        }

        private static GUIStyle HeaderStyle()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            return style;
        }

        private static string WeatherName(string type)
        {
            switch (type)
            {
                case "Sunny": return "Trời nắng";
                case "Cloudy": return "Nhiều mây";
                case "Rain": return "Mưa";
                case "Fog": return "Sương mù";
                case "Snow": return "Tuyết";
                case "Sandstorm": return "Bão cát";
                default: return type;
            }
        }

        [System.Serializable] private sealed class TravelRequest { public long mapId; }
        [System.Serializable] private sealed class MonsterRequest { public long monsterId; }
        [System.Serializable] private sealed class CropRequest { public long cropId; }
        [System.Serializable] private sealed class PlotRequest { public long plotId; }
        [System.Serializable] private sealed class RecipeRequest { public long recipeId; }
    }
}




























































































































        private void RunAction(string action, object body = null)
        {
            busy = true;
            StartCoroutine(api.PostAction<GameWorldApiClient.ApiMessage>(selectedCharacterId, action, body, result =>
            {
                message = result?.message ?? "Đã hoàn tất.";
                LoadWorld();
            }, OnRequestFailed));
        }

        private void RunAction(string action)
        {
            RunAction(action, null);
        }

        private void OnRequestFailed(string error)
        {
            busy = false;
            if (string.IsNullOrEmpty(api.AccessToken) && screen == ScreenState.World)
            {
                state = null;
                screen = ScreenState.Authentication;
            }
            message = error;
        }

        private static string TextField(string label, string value)
        {
            GUILayout.Label(label);
            return GUILayout.TextField(value, GUILayout.Height(34f));
        }

        private static string PasswordField(string label, string value)
        {
            GUILayout.Label(label);
            return GUILayout.PasswordField(value, '\u25cf', GUILayout.Height(34f));
        }

        private static bool Button(string label, bool disabled)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var previous = GUI.enabled;
                GUI.enabled = !disabled;
                var clicked = GUILayout.Button(label, GUILayout.Height(36f));
                GUI.enabled = previous;
                GUILayout.FlexibleSpace();
                return clicked;
            }
        }

        private static GUIStyle HeaderStyle()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            return style;
        }

        private static string WeatherName(string type)
        {
            switch (type)
            {
                case "Sunny": return "Trời nắng";
                case "Cloudy": return "Nhiều mây";
                case "Rain": return "Mưa";
                case "Fog": return "Sương mù";
                case "Snow": return "Tuyết";
                case "Sandstorm": return "Bão cát";
                default: return type;
            }
        }

        [System.Serializable] private sealed class TravelRequest { public long mapId; }
        [System.Serializable] private sealed class MonsterRequest { public long monsterId; }
        [System.Serializable] private sealed class CropRequest { public long cropId; }
        [System.Serializable] private sealed class PlotRequest { public long plotId; }
        [System.Serializable] private sealed class RecipeRequest { public long recipeId; }
    }
}
