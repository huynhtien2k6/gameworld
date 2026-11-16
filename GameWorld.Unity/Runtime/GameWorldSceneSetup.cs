using UnityEngine;

namespace GameWorld.UnityClient
{
    public static class GameWorldSceneSetup
    {
        public static void CreateDefaultScene(GameObject root)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            var renderer = player.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.5f, 0.9f);
            }

            player.AddComponent<GameWorldPlayerController>();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(12f, 1f, 12f);

            var cameraObject = new GameObject("World Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 55f;
            var cameraController = cameraObject.AddComponent<GameWorldCameraController>();
            cameraController.BindTarget(player.transform);

            var npcA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            npcA.name = "Bà Lan";
            npcA.transform.position = new Vector3(-4f, 1f, 2f);
            npcA.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            npcA.AddComponent<GameWorldNpcMarker>().SendMessage("set_NpcName", "Bà Lan");
            var npcRenderer = npcA.GetComponent<Renderer>();
            if (npcRenderer != null) npcRenderer.material.color = new Color(0.95f, 0.76f, 0.4f);

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            enemy.name = "Slime";
            enemy.transform.position = new Vector3(5f, 0.8f, 5f);
            enemy.transform.localScale = new Vector3(1.3f, 1.1f, 1.3f);
            enemy.AddComponent<GameWorldEnemyMarker>();
            var enemyRenderer = enemy.GetComponent<Renderer>();
            if (enemyRenderer != null) enemyRenderer.material.color = new Color(0.3f, 0.8f, 0.5f);

            var light = new GameObject("Directional Light");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
        }
    }
}
