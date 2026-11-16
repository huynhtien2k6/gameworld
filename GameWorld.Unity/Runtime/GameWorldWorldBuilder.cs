using UnityEngine;

namespace GameWorld.UnityClient
{
    public sealed class GameWorldWorldBuilder : MonoBehaviour
    {
        [SerializeField] private Vector3 villageCenter = new Vector3(0f, 0f, 0f);
        [SerializeField] private float zoneSize = 20f;

        private void Start()
        {
            BuildVillage();
            BuildForest();
            BuildMountain();
            BuildDesert();
        }

        private void BuildVillage()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "VillageGround";
            ground.transform.position = villageCenter;
            ground.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

            var house1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house1.name = "House_01";
            house1.transform.position = villageCenter + new Vector3(-5f, 1.5f, 2f);
            house1.transform.localScale = new Vector3(3.5f, 3f, 3.5f);
            var renderer1 = house1.GetComponent<Renderer>();
            if (renderer1 != null) renderer1.material.color = new Color(0.85f, 0.6f, 0.4f);

            var house2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house2.name = "House_02";
            house2.transform.position = villageCenter + new Vector3(5f, 1.5f, 2f);
            house2.transform.localScale = new Vector3(3.5f, 3f, 3.5f);
            var renderer2 = house2.GetComponent<Renderer>();
            if (renderer2 != null) renderer2.material.color = new Color(0.7f, 0.75f, 0.55f);

            var well = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            well.name = "VillageWell";
            well.transform.position = villageCenter + new Vector3(0f, 0.7f, -4f);
            well.transform.localScale = new Vector3(1.2f, 0.7f, 1.2f);

            CreateNpc("Bà Lan", villageCenter + new Vector3(-2f, 0.8f, 3f), new Color(0.95f, 0.8f, 0.5f));
            CreateNpc("Chủ quán", villageCenter + new Vector3(2f, 0.8f, 5f), new Color(0.8f, 0.55f, 0.35f));
            CreateNpc("Bác Năm", villageCenter + new Vector3(5f, 0.8f, -3f), new Color(0.55f, 0.72f, 0.42f));
        }

        private void BuildForest()
        {
            var forestCenter = villageCenter + new Vector3(0f, 0f, 30f);
            var forestGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            forestGround.name = "ForestGround";
            forestGround.transform.position = forestCenter;
            forestGround.transform.localScale = new Vector3(1.3f, 1f, 1.3f);

            for (int i = 0; i < 7; i++)
            {
                var tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = "Tree_" + i;
                tree.transform.position = forestCenter + new Vector3(Random.Range(-10f, 10f), 1.3f, Random.Range(-10f, 10f));
                tree.transform.localScale = new Vector3(0.5f, 1.4f, 0.5f);
                var renderer = tree.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.42f, 0.28f, 0.18f);

                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Crown_" + i;
                crown.transform.position = tree.transform.position + new Vector3(0f, 1.8f, 0f);
                crown.transform.localScale = new Vector3(2.2f, 1.8f, 2.2f);
                var crownRenderer = crown.GetComponent<Renderer>();
                if (crownRenderer != null) crownRenderer.material.color = new Color(0.3f, 0.55f, 0.28f);
            }
        }

        private void BuildMountain()
        {
            var mountainCenter = villageCenter + new Vector3(-30f, 0f, 0f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "MountainGround";
            ground.transform.position = mountainCenter;
            ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

            var peak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            peak.name = "MountainPeak";
            peak.transform.position = mountainCenter + new Vector3(0f, 4f, 0f);
            peak.transform.localScale = new Vector3(8f, 4.5f, 8f);
        }

        private void BuildDesert()
        {
            var desertCenter = villageCenter + new Vector3(30f, 0f, 0f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DesertGround";
            ground.transform.position = desertCenter;
            ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

            var dune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dune.name = "Dune";
            dune.transform.position = desertCenter + new Vector3(0f, 1f, 0f);
            dune.transform.localScale = new Vector3(10f, 2f, 10f);
            var renderer = dune.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.9f, 0.7f, 0.45f);
        }

        private void CreateNpc(string name, Vector3 position, Color color)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = name;
            npc.transform.position = position;
            npc.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            var renderer = npc.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;

            npc.AddComponent<GameWorldNpcMarker>();
            var marker = npc.GetComponent<GameWorldNpcMarker>();
            if (marker != null)
            {
                var field = typeof(GameWorldNpcMarker).GetField("npcName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(marker, name);
            }
        }
    }
}
