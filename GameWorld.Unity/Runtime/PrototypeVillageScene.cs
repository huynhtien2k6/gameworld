using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class PrototypeVillageScene : MonoBehaviour
    {
        private Camera worldCamera;
        private Vector3 cameraTarget = new Vector3(0f, 0.8f, 0f);
        private float yaw = 35f;
        private float pitch = 28f;
        private float distance = 18f;

        private void Start()
        {
            CreateCamera();
            CreateLighting();
            CreateVillage();
            UpdateCamera();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0) && Input.mousePosition.x > Screen.width * 0.45f)
            {
                yaw += Input.GetAxis("Mouse X") * 3f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2f, 12f, 65f);
            }
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y, 10f, 28f);
            UpdateCamera();
        }

        private void CreateCamera()
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                var cameraObject = new GameObject("World Camera");
                cameraObject.tag = "MainCamera";
                worldCamera = cameraObject.AddComponent<Camera>();
            }
            worldCamera.fieldOfView = 50f;
            worldCamera.backgroundColor = new Color(0.55f, 0.72f, 0.82f);
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.72f, 0.75f, 0.72f);
            var lightObject = new GameObject("Village Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static void CreateVillage()
        {
            var grass = new Color(0.36f, 0.55f, 0.31f);
            var ground = CreatePrimitive(PrimitiveType.Plane, "Village Ground", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 1f, 8f), grass);
            ground.isStatic = true;
            CreateHouse(new Vector3(-5f, 0f, 3f), new Color(0.78f, 0.63f, 0.43f));
            CreateHouse(new Vector3(5f, 0f, 4f), new Color(0.72f, 0.72f, 0.56f));
            CreateHouse(new Vector3(-5f, 0f, -5f), new Color(0.68f, 0.53f, 0.4f));
            CreateTree(new Vector3(-10f, 0f, 0f));
            CreateTree(new Vector3(10f, 0f, -3f));
            CreateTree(new Vector3(8f, 0f, 9f));
            CreateTree(new Vector3(-9f, 0f, -9f));
            CreatePrimitive(PrimitiveType.Capsule, "Traveler Preview", new Vector3(0f, 1f, 0f), new Vector3(0.7f, 1f, 0.7f), new Color(0.25f, 0.42f, 0.72f));
            CreatePrimitive(PrimitiveType.Cylinder, "Village Well", new Vector3(3f, 0.45f, -2f), new Vector3(1.3f, 0.45f, 1.3f), new Color(0.55f, 0.57f, 0.52f));
        }

        private static void CreateHouse(Vector3 position, Color wallColor)
        {
            CreatePrimitive(PrimitiveType.Cube, "Cottage", position + new Vector3(0f, 1.25f, 0f), new Vector3(3.2f, 2.5f, 2.8f), wallColor);
            var roof = CreatePrimitive(PrimitiveType.Cube, "Cottage Roof", position + new Vector3(0f, 2.85f, 0f), new Vector3(3.8f, 0.35f, 3.4f), new Color(0.42f, 0.25f, 0.17f));
            roof.transform.rotation = Quaternion.Euler(0f, 0f, -10f);
            CreatePrimitive(PrimitiveType.Cube, "Cottage Door", position + new Vector3(0f, 0.65f, -1.42f), new Vector3(0.7f, 1.3f, 0.12f), new Color(0.32f, 0.2f, 0.12f));
        }

        private static void CreateTree(Vector3 position)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Tree Trunk", position + new Vector3(0f, 1f, 0f), new Vector3(0.35f, 1f, 0.35f), new Color(0.39f, 0.27f, 0.17f));
            var crown = CreatePrimitive(PrimitiveType.Sphere, "Tree Crown", position + new Vector3(0f, 2.7f, 0f), new Vector3(2f, 2.4f, 2f), new Color(0.29f, 0.48f, 0.25f));
            crown.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Color color)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = objectName;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            var renderer = gameObject.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (renderer != null && shader != null)
            {
                renderer.material = new Material(shader) { color = color };
            }
            return gameObject;
        }

        private void UpdateCamera()
        {
            if (worldCamera == null) return;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            worldCamera.transform.position = cameraTarget - rotation * Vector3.forward * distance;
            worldCamera.transform.rotation = rotation;
        }
    }
}
