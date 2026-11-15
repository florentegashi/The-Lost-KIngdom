using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostKingdom
{
    public class ForestRestoration : MonoBehaviour
    {
        public Light sun;
        public Material ground, leaves;
        public GameObject restoredLights;
        public IEnumerator Restore()
        {
            Color oldAmbient = RenderSettings.ambientLight, oldFog = RenderSettings.fogColor;
            Color oldGround = ground.color, oldLeaves = leaves.color;
            float intensity = sun.intensity;
            restoredLights.SetActive(true);
            for (float elapsed = 0; elapsed < 4; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / 4);
                RenderSettings.ambientLight = Color.Lerp(oldAmbient, new Color(.55f, .65f, .52f), t);
                RenderSettings.fogColor = Color.Lerp(oldFog, new Color(.48f, .7f, .65f), t);
                RenderSettings.fogDensity = Mathf.Lerp(.0075f, .004f, t);
                Camera.main.backgroundColor = RenderSettings.fogColor;
                sun.intensity = Mathf.Lerp(intensity, 2.2f, t);
                sun.color = Color.Lerp(new Color(.55f, .7f, 1), new Color(1, .91f, .68f), t);
                ground.color = Color.Lerp(oldGround, new Color(.18f, .38f, .18f), t);
                leaves.color = Color.Lerp(oldLeaves, new Color(.22f, .58f, .3f), t);
                yield return null;
            }
        }
        void Awake()
        {
            // Runtime copies prevent restoration from modifying saved material assets in the editor.
            Material originalGround = ground, originalLeaves = leaves;
            ground = new Material(ground); leaves = new Material(leaves);
            foreach (var renderer in FindObjectsByType<Renderer>())
            {
                if (renderer.sharedMaterial == originalGround) renderer.sharedMaterial = ground;
                else if (renderer.sharedMaterial == originalLeaves) renderer.sharedMaterial = leaves;
            }
        }
        void OnDestroy() { Destroy(ground); Destroy(leaves); }
    }
}


namespace LostKingdom
{
    // Reshapes the existing Level 1 into a ravine climb while reusing its player, collectibles, gate, and UI.
    public static class ForestLevelUpgrade
    {
        static readonly Dictionary<string, Material> RuntimeMaterials = new Dictionary<string, Material>();
        static Material stone, moss, poison, bark, cloth, leather, skin, hair;
        static LightFragment fragmentFour, fragmentFive;
        static readonly float[] Sections = { 100f, 112f, 124f, 139f };

        public static void Build()
        {
            if (GameObject.Find("Mystic Forest ravine")) return;
            stone = Material("Ancient stone", new Color(.25f, .32f, .34f));
            moss = Material("Moss green", new Color(.12f, .27f, .18f));
            poison = Material("Poisonous bloom", new Color(.25f, .62f, .22f), new Color(.12f, .5f, .14f));
            bark = Material("Adventurer leather", new Color(.2f, .11f, .075f));
            cloth = Material("Adventurer teal", new Color(.1f, .32f, .34f));
            leather = Material("Boot leather", new Color(.1f, .07f, .05f));
            skin = Material("Warm skin", new Color(.68f, .44f, .3f));
            hair = Material("Adventurer hair", new Color(.095f, .052f, .035f));
            BuildTerrain(); BuildRoute(); BuildShrineClimb(); BuildPoisonMushrooms(); BuildAdventurer(); BuildFloorDetails();
            BuildVisualDressing(); BuildKeyObjectVisuals(); BuildAtmosphere();
        }

        // The following additions are visual-only: every generated object has no collider
        // and is kept under one scene hierarchy group, away from gameplay references.
        static void BuildVisualDressing()
        {
            var root = new GameObject("Mystic Forest visual dressing").transform;
            var rootBark = Material("Rootwood detail", new Color(.16f, .095f, .065f));
            var fernGreen = Material("Fern jade", new Color(.09f, .31f, .21f));
            var fernLight = Material("Fern tips", new Color(.25f, .48f, .29f));
            var flowerBlue = Material("Moonflower", new Color(.25f, .62f, .72f), new Color(.08f, .32f, .5f));
            var waterGlow = Material("Waterfall glow", new Color(.09f, .36f, .48f), new Color(.04f, .28f, .42f));
            var silhouette = Material("Distant silhouette", new Color(.035f, .075f, .12f));
            Random.InitState(49271);

            foreach (var tree in Object.FindObjectsByType<Transform>())
            {
                if (tree.name != "Low-poly pine" || tree.position.z >= 98 && tree.position.z < 182) continue;
                var detail = new GameObject("Root and branch detail").transform; detail.SetParent(tree, false);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f + Random.Range(-.18f, .18f);
                    Vector3 end = new Vector3(Mathf.Cos(a) * Random.Range(1.15f, 1.7f), -.04f, Mathf.Sin(a) * Random.Range(1.15f, 1.7f));
                    CreateTaperedSegment("Twisted exposed root", detail, new Vector3(0, .26f, 0), end, .26f, .055f, rootBark, 6);
                }
                // A few visible bark limbs break up the simple crown silhouette.
                for (int i = 0; i < 2; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 start = new Vector3(0, Random.Range(2.1f, 2.8f), 0);
                    Vector3 end = start + new Vector3(Mathf.Cos(a) * 1.55f, .65f, Mathf.Sin(a) * 1.55f);
                    CreateTaperedSegment("Gnarled pine bough", detail, start, end, .14f, .055f, rootBark, 5);
                }
                CombineVisualMeshes(detail);
            }

            // Low ferns and moonflowers form loose groups along the forest floor and path edge.
            for (int i = 0; i < 145; i++)
            {
                float z = Random.Range(5f, 184f);
                if (z >= 98f && z < 182f) continue;
                float side = Random.value < .5f ? -1 : 1;
                float x = TrailX(z) + side * Random.Range(5.2f, 13.5f);
                CreateFern(new Vector3(x, .015f, z), Random.Range(.72f, 1.25f), i % 4 == 0 ? fernLight : fernGreen, root);
                if (i % 3 == 0) CreateMoonflower(new Vector3(x + side * Random.Range(.5f, 1.5f), 0, z + Random.Range(-.6f, .6f)), flowerBlue, root);
            }

            // Faceted, non-colliding cliff accents, plus two restrained waterfall ribbons.
            for (int z = 106; z <= 158; z += 9)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 p = new Vector3(side * Random.Range(18.4f, 21.5f), Random.Range(.6f, 2.1f), z + Random.Range(-2f, 2f));
                    CreateFacetedRock(p, new Vector3(Random.Range(2.5f, 4.4f), Random.Range(2f, 3.6f), Random.Range(2.2f, 4f)), stone, root);
                }
            }
            CreateWaterfall(-19.7f, 121f, waterGlow, root);
            CreateWaterfall(19.7f, 146f, waterGlow, root);

            // A small horizon composition peeks above the existing northern ridge.
            BuildHorizonSilhouette(silhouette, root);
            CombineVisualMeshes(root);
        }

        static void BuildKeyObjectVisuals()
        {
            var rune = Material("Sanctuary rune glow", new Color(.25f, .83f, .9f), new Color(.12f, .55f, .72f));
            var warm = Material("Sanctuary gold inlay", new Color(.76f, .55f, .27f), new Color(.3f, .15f, .035f));
            var gate = Object.FindAnyObjectByType<MagicalGate>();
            if (gate)
            {
                var root = new GameObject("Sanctuary gate column inlays").transform;
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float y = 1.1f + i * 1.55f;
                        CreateDiamond(new Vector3(side * 4.3f, y, 161.65f), .28f, .46f, rune, root);
                    }
                    CreateDiamond(new Vector3(side * 4.3f, 8.05f, 161.65f), .52f, .74f, warm, root);
                }
                for (int i = -3; i <= 3; i++) CreateDiamond(new Vector3(i * .95f, 8.02f, 161.55f), .19f, .32f, rune, root);
            }

            // A thin octagonal plinth inlay and three secondary shards frame the existing Crystal.
            var dais = GameObject.Find("Crystal dais");
            if (dais)
            {
                var root = new GameObject("Crystal dais visual inlay").transform;
                float plinthTop = dais.transform.position.y + .27f;
                CreateRing(new Vector3(0, plinthTop, 179), 2.5f, .075f, warm, root);
                CreateRing(new Vector3(0, plinthTop + .02f, 179), 3.0f, .035f, rune, root);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI * 2f / 3f + .35f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * 3.6f, plinthTop + .4f, 179 + Mathf.Sin(a) * 3.6f);
                    CreateCrystalShard("Dais companion crystal", p, .28f, 1.05f, i == 1 ? warm : rune, root);
                }
            }

            // Add a delicate light ring around each existing pickup; trigger colliders stay intact.
            foreach (var fragment in Object.FindObjectsByType<LightFragment>())
            {
                var root = new GameObject("Fragment visual halo").transform; root.SetParent(fragment.transform, false);
                CreateRing(fragment.transform.position, .76f, .025f, rune, root);
                var renderer = root.gameObject.GetComponent<Renderer>(); if (renderer) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void BuildAtmosphere()
        {
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.085f, .145f, .18f); RenderSettings.fogDensity = .0075f;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.27f, .32f, .37f);
            var sun = GameObject.Find("Moon / restored sunlight");
            if (sun && sun.TryGetComponent<Light>(out var light))
            {
                light.color = new Color(.67f, .79f, 1f); light.intensity = .95f; light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(42, -28, 0);
            }
            var floor = GameObject.Find("Forest entrance ground");
            if (floor && floor.TryGetComponent<Renderer>(out var floorRenderer)) floorRenderer.sharedMaterial.color = new Color(.09f, .19f, .14f);
            var trail = GameObject.Find("Lantern trail");
            if (trail && trail.TryGetComponent<Renderer>(out var trailRenderer)) trailRenderer.sharedMaterial.color = new Color(.31f, .34f, .29f);
        }

        static void CreateFern(Vector3 position, float scale, Material material, Transform parent)
        {
            const int blades = 7, segments = 5;
            var vertices = new Vector3[blades * (segments + 1) * 2];
            var triangles = new int[blades * segments * 6];
            int vi = 0, ti = 0;
            for (int b = 0; b < blades; b++)
            {
                float angle = b * Mathf.PI * 2f / blades + Random.Range(-.2f, .2f);
                Vector3 side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 cross = Vector3.Cross(side, Vector3.up).normalized;
                float length = Random.Range(.65f, 1.05f) * scale;
                int first = vi;
                for (int s = 0; s <= segments; s++)
                {
                    float t = s / (float)segments;
                    Vector3 center = side * (t * length) + Vector3.up * (Mathf.Sin(t * Mathf.PI * .82f) * length * .62f);
                    float width = (1f - t) * .15f * scale + .008f;
                    vertices[vi++] = center - cross * width; vertices[vi++] = center + cross * width;
                    if (s == 0) continue;
                    int a = first + (s - 1) * 2, c = first + s * 2;
                    triangles[ti++] = a; triangles[ti++] = c; triangles[ti++] = a + 1;
                    triangles[ti++] = a + 1; triangles[ti++] = c; triangles[ti++] = c + 1;
                    triangles[ti++] = a + 1; triangles[ti++] = c; triangles[ti++] = a;
                    triangles[ti++] = c + 1; triangles[ti++] = c; triangles[ti++] = a + 1;
                }
            }
            var mesh = new Mesh { name = "Fern frond mesh" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject("Moonlit fern"); obj.transform.SetParent(parent, true); obj.transform.position = position; obj.transform.rotation = parent.rotation;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void CreateMoonflower(Vector3 position, Material material, Transform parent)
        {
            var flower = new GameObject("Moonflower cluster").transform; flower.SetParent(parent, true); flower.position = position;
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * .17f;
                CreateTaperedSegment("Moonflower stem", flower, new Vector3(x, 0, 0), new Vector3(x + .04f, .38f + i % 2 * .1f, 0), .024f, .012f, moss, 4);
                CreateCrystalShard("Moonflower", flower.TransformPoint(new Vector3(x + .04f, .39f + i % 2 * .1f, 0)), .09f, .16f, material, flower);
            }
            CombineVisualMeshes(flower);
        }

        static void CreateTaperedSegment(string name, Transform parent, Vector3 start, Vector3 end, float startRadius, float endRadius, Material material, int sides)
        {
            Vector3 axis = (end - start).normalized; Vector3 right = Vector3.Cross(axis, Vector3.up);
            if (right.sqrMagnitude < .001f) right = Vector3.Cross(axis, Vector3.forward);
            right.Normalize(); Vector3 forward = Vector3.Cross(axis, right).normalized;
            var vertices = new Vector3[sides * 2]; var triangles = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides; Vector3 radial = right * Mathf.Cos(a) + forward * Mathf.Sin(a);
                vertices[i] = start + radial * startRadius; vertices[sides + i] = end + radial * endRadius;
                int next = (i + 1) % sides, t = i * 6;
                triangles[t] = i; triangles[t + 1] = sides + i; triangles[t + 2] = next;
                triangles[t + 3] = next; triangles[t + 4] = sides + i; triangles[t + 5] = sides + next;
            }
            var mesh = new Mesh { name = name + " mesh" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateDiamond(Vector3 position, float halfWidth, float height, Material material, Transform parent)
        {
            var obj = new GameObject("Cut gemstone inlay"); obj.transform.SetParent(parent, true); obj.transform.position = position;
            var mesh = new Mesh { name = "Diamond inlay mesh" };
            mesh.vertices = new[] { Vector3.up * height, Vector3.right * halfWidth, Vector3.down * height, Vector3.left * halfWidth, Vector3.forward * .045f };
            mesh.triangles = new[] { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0, 1, 0, 4, 2, 1, 4, 3, 2, 4, 0, 3, 4 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateCrystalShard(string name, Vector3 position, float radius, float height, Material material, Transform parent)
        {
            var mesh = new Mesh { name = name + " mesh" };
            mesh.vertices = new[] { Vector3.up * height, new Vector3(radius, height * .18f, 0), new Vector3(0, height * .12f, radius), new Vector3(-radius, height * .18f, 0), new Vector3(0, height * .12f, -radius), Vector3.down * height * .28f };
            mesh.triangles = new[] { 0,1,2, 0,2,3, 0,3,4, 0,4,1, 1,5,2, 2,5,3, 3,5,4, 4,5,1 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(name); obj.transform.SetParent(parent, true); obj.transform.position = position; obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), Random.Range(-8f, 8f));
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateRing(Vector3 position, float radius, float width, Material material, Transform parent)
        {
            const int sides = 20; var vertices = new Vector3[sides * 2]; var triangles = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides; Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                vertices[i * 2] = radial * (radius - width * .5f); vertices[i * 2 + 1] = radial * (radius + width * .5f);
                int n = (i + 1) % sides, t = i * 6;
                triangles[t] = i * 2; triangles[t + 1] = n * 2; triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1; triangles[t + 4] = n * 2; triangles[t + 5] = n * 2 + 1;
            }
            var mesh = new Mesh { name = "Octagonal ring inlay mesh" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject("Octagonal ring inlay"); obj.transform.SetParent(parent, true); obj.transform.position = position;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateWaterfall(float x, float z, Material material, Transform parent)
        {
            var obj = new GameObject("Silver-blue waterfall ribbon"); obj.transform.SetParent(parent, true);
            obj.transform.position = new Vector3(x, 3.8f, z);
            var mesh = new Mesh { name = "Waterfall ribbon mesh" };
            mesh.vertices = new[] { new Vector3(-1.3f, 4.5f, 0), new Vector3(-.7f, -3.8f, -.12f), new Vector3(-.1f, 4.0f, -.05f), new Vector3(.25f, -3.7f, -.2f), new Vector3(.8f, 4.4f, -.08f), new Vector3(1.25f, -3.8f, -.18f) };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3, 2, 3, 4, 4, 3, 5, 2, 1, 0, 3, 1, 2, 4, 3, 2, 5, 3, 4 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void BuildHorizonSilhouette(Material material, Transform parent)
        {
            var far = new GameObject("Distant mountain and castle silhouette").transform; far.SetParent(parent, true);
            far.position = new Vector3(0, 0, 202);
            for (int i = 0; i < 5; i++)
            {
                float x = -43 + i * 21.5f;
                CreateConeVisual("Layered mountain peak", new Vector3(x, 17 + (i % 2) * 4, 7 + i * .8f), new Vector3(23, 28 + (i % 2) * 5, 11), material, far);
            }
            var castle = new GameObject("Eldoria distant castle silhouette").transform; castle.SetParent(far, false); castle.localPosition = new Vector3(0, 19, -1);
            Add(castle, "Keep silhouette", PrimitiveType.Cube, new Vector3(0, 4, 0), new Vector3(13, 10, 2), material);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 4f;
                Add(castle, "Castle tower silhouette", PrimitiveType.Cylinder, new Vector3(x, 7 + (i == 0 ? 2 : 0), 0), new Vector3(1.3f, 5, 1.3f), material);
                CreateConeVisual("Castle spire silhouette", new Vector3(x, 12 + (i == 0 ? 4 : 0), 0), new Vector3(2.1f, 4, 2.1f), material, castle);
            }
        }

        static void CreateConeVisual(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            const int sides = 7; var vertices = new Vector3[sides + 1]; var triangles = new int[sides * 3];
            vertices[0] = Vector3.up;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides; vertices[i + 1] = new Vector3(Mathf.Cos(a), -.65f, Mathf.Sin(a));
                int next = (i + 1) % sides; triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = next + 1;
            }
            var mesh = new Mesh { name = name + " mesh" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CreateFacetedRock(Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var vertices = new[]
            {
                new Vector3(-.38f,.08f,-.34f), new Vector3(.34f,.03f,-.4f), new Vector3(.43f,.12f,.24f), new Vector3(-.3f,.06f,.42f),
                new Vector3(-.2f,.48f,-.16f), new Vector3(.22f,.42f,-.12f), new Vector3(.16f,.39f,.2f), new Vector3(-.17f,.44f,.18f), Vector3.up * .61f
            };
            var triangles = new[] { 0,4,1, 1,4,5, 1,5,2, 2,5,6, 2,6,3, 3,6,7, 3,7,0, 0,7,4, 4,8,5, 5,8,6, 6,8,7, 7,8,4, 0,2,1, 0,3,2 };
            var mesh = new Mesh { name = "Ravine slate outcrop mesh" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject("Ravine slate outcrop"); obj.transform.SetParent(parent, true); obj.transform.position = position; obj.transform.localScale = scale;
            obj.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-14f, 14f));
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void CombineVisualMeshes(Transform root)
        {
            var sources = root.GetComponentsInChildren<MeshRenderer>();
            if (sources.Length < 2) return;
            var batches = new Dictionary<Material, List<CombineInstance>>();
            foreach (var renderer in sources)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh || !renderer.sharedMaterial) continue;
                if (!batches.TryGetValue(renderer.sharedMaterial, out var list)) batches[renderer.sharedMaterial] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
            }
            foreach (var batch in batches)
            {
                var mesh = new Mesh { name = "Combined visual detail" };
                mesh.CombineMeshes(batch.Value.ToArray(), true, true);
                var obj = new GameObject("Combined visual detail"); obj.transform.SetParent(root, false);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = batch.Key; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
            foreach (var renderer in sources)
            {
                var filter = renderer.GetComponent<MeshFilter>(); if (filter && filter.sharedMesh) Object.Destroy(filter.sharedMesh);
                Object.Destroy(renderer.gameObject);
            }
        }

        static void BuildTerrain()
        {
            var baseFloor = GameObject.Find("Mystic Forest ground");
            if (baseFloor)
            {
                var renderer = baseFloor.GetComponent<Renderer>();
                Material floorMaterial = renderer ? renderer.sharedMaterial : moss;
                baseFloor.SetActive(false);
                CreateFloor("Forest entrance ground", new Vector3(0, -1, 39), new Vector3(100, 2, 118), floorMaterial);
                // The upper shrine has a central chasm. The crystal can only be reached by climbing its ledges.
                CreateFloor("Shrine west shelf", new Vector3(-31, -1, 177), new Vector3(38, 2, 26), floorMaterial);
                CreateFloor("Shrine east shelf", new Vector3(31, -1, 177), new Vector3(38, 2, 26), floorMaterial);
                CreateFloor("Shrine north shelf", new Vector3(0, -1, 186), new Vector3(24, 2, 8), floorMaterial);
            }

            foreach (var item in Object.FindObjectsByType<LightFragment>())
            {
                if (item.name == "Light Fragment 4") fragmentFour = item;
                if (item.name == "Light Fragment 5") fragmentFive = item;
            }
            foreach (var item in Object.FindObjectsByType<Transform>())
            {
                Vector3 p = item.position;
                bool ravine = p.z >= 98 && p.z < 164;
                bool shrineChasm = p.z >= 164 && p.z < 182 && Mathf.Abs(p.x) < 12;
                if (!ravine && !shrineChasm) continue;
                if (item.name.Contains("Crystal") || item.name.Contains("Dais") || item.name.Contains("Aren") || item.name.Contains("Game systems")) continue;
                if (item.parent) { if (item.name == "Restored wisp") item.gameObject.SetActive(false); continue; }
                if (item.GetComponent<MagicalGate>() || item.name.Contains("Gate") || item.name.Contains("gate")
                    || item.name.Contains("Sanctuary") || item.name.Contains("Sealed stone door")) continue;
                if (item == baseFloor?.transform) continue;
                item.gameObject.SetActive(false);
            }

            // Dark, broken cliff faces frame the route and make the missing ground read as a ravine.
            for (int z = 103; z <= 159; z += 8)
            {
                float height = 11 + Mathf.Sin(z * .17f) * 2f;
                AddCliffMass(new Vector3(-27 - Mathf.Sin(z * .08f) * 3f, -1f, z), new Vector3(18, height, 13));
                AddCliffMass(new Vector3(27 + Mathf.Sin(z * .07f) * 3f, -1f, z + 3f), new Vector3(18, height + 2, 15));
            }

            // The gate remains the only passage through the old sanctuary wall.
            var dais = GameObject.Find("Crystal dais");
            if (dais) dais.transform.position = new Vector3(0, 8.4f, 179);
            var crystal = GameObject.Find("Crystal of Light");
            if (crystal) crystal.transform.position = new Vector3(0, 8.8f, 179);
        }

        static void BuildRoute()
        {
            var route = new GameObject("Mystic Forest ravine");
            for (int section = 0; section < Sections.Length; section++)
            {
                float start = Sections[section];
                float spacing = section == 0 ? 3.4f : section == 1 ? 3.5f : section == 2 ? 4.2f : 4.7f;
                int count = section == 3 ? 5 : section < 2 ? 3 : 4;
                float[] heights = section switch
                {
                    0 => new[] { .7f, 1.05f, .78f },
                    1 => new[] { .95f, 1.5f, 1.15f },
                    2 => new[] { 1.3f, 1.95f, 2.6f, 2.05f },
                    _ => new[] { 1.75f, 2.35f, 2.95f, 3.55f, 4.15f }
                };
                for (int i = 0; i < count; i++)
                {
                    float z = start + .1f + i * spacing;
                    float sideOffset = section < 2 ? .9f : 1.15f;
                    float x = TrailX(z) + ((i % 2 == 0) ? -sideOffset : sideOffset);
                    if (section == 3) x = new[] { 4.0f, 7.5f, 8.0f, 5.0f, 1.6f }[i];
                    float top = heights[i];
                    var rock = CreateRock("Ravine climbing ledge", new Vector3(x, top, z), section > 1 ? 2.7f : 3.1f, section > 1 ? 2.9f : 3.3f);
                    rock.transform.SetParent(route.transform, true);
                    if ((section == 0 && i == 1) || (section == 2 && i == 2) || (section == 3 && i == 3))
                        CreateMushroomSide(route.transform, x, top, z, i % 2 == 0 ? 1 : -1);
                }
            }

            CreateRock("Gate approach ledge", new Vector3(.6f, 4.15f, 161.3f), 3.4f, 3.2f).transform.SetParent(route.transform, true);
            AddCheckpoint(route.transform, "Ravine entrance", new Vector3(0, .15f, 0));
            AddCheckpoint(route.transform, "Lower ravine", new Vector3(TrailX(112.1f) - .9f, 1.2f, 112.1f));
            AddCheckpoint(route.transform, "Upper ravine", new Vector3(TrailX(139.1f) - 1.15f, 1.9f, 139.1f));
            AddCheckpoint(route.transform, "Sanctuary approach", new Vector3(.6f, 4.3f, 161.3f));
            if (fragmentFour) PlaceFragment(fragmentFour, TrailX(119.1f) - .9f, 1.15f, 119.1f);
            if (fragmentFive) PlaceFragment(fragmentFive, TrailX(148.5f) - 1.15f, 2.95f, 148.5f);
            var marker = new GameObject("Double jump wayfinder"); marker.transform.position = new Vector3(TrailX(112) + 4.5f, 3f, 113f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube); board.name = "SPACE twice sign"; board.transform.SetParent(marker.transform, false);
            board.transform.localScale = new Vector3(3.4f, 1.2f, .2f); board.GetComponent<Renderer>().sharedMaterial = stone;
            var label = new GameObject("Double jump instruction"); label.transform.SetParent(marker.transform, false); label.transform.localPosition = Vector3.back * .12f;
            var text = label.AddComponent<TextMesh>(); text.text = "SPACE  →  SPACE"; text.fontSize = 48; text.characterSize = .018f; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = Color.white;
        }

        static void AddCheckpoint(Transform parent, string label, Vector3 spawn)
        {
            var marker = new GameObject("Checkpoint - " + label);
            marker.transform.SetParent(parent, true); marker.transform.position = spawn + Vector3.up * .4f;
            var trigger = marker.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(2.8f, 1.2f, 2.8f);
            marker.AddComponent<ForestCheckpoint>().checkpointName = label;
            var wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere); wisp.name = "Checkpoint wisp";
            wisp.transform.SetParent(marker.transform, false); wisp.transform.localPosition = Vector3.up * .38f;
            wisp.transform.localScale = new Vector3(.24f, .44f, .24f);
            wisp.GetComponent<Renderer>().sharedMaterial = Material("Checkpoint glow", new Color(.25f, .86f, .69f), new Color(.08f, .48f, .31f));
            Object.Destroy(wisp.GetComponent<Collider>());
        }


        static void BuildShrineClimb()
        {
            // Sheer walls keep the upper chasm enclosed; missing ground makes every missed jump fatal.
            var route = GameObject.Find("Mystic Forest ravine").transform;
            for (int side = -1; side <= 1; side += 2)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Shrine chasm cliff wall";
                wall.transform.position = new Vector3(side * 12.2f, 3.5f, 173); wall.transform.localScale = new Vector3(2.5f, 11, 18);
                wall.GetComponent<Renderer>().sharedMaterial = stone;
            }
            float[] x = { .4f, -1.1f, 1.2f, -1.35f, 4.4f };
            float[] z = { 165.4f, 168.0f, 170.6f, 173.2f, 179.0f };
            float[] y = { 4.2f, 4.9f, 5.6f, 6.3f, 6.9f };
            for (int i = 0; i < z.Length; i++)
            {
                var rock = CreateRock(i == 4 ? "Double jump cliff shelf" : "Shrine ascent ledge", new Vector3(x[i], y[i], z[i]), i == 4 ? 2.2f : 3.0f, i == 4 ? 2.3f : 2.9f);
                rock.transform.SetParent(route, true);
                if (i == 2) CreateMushroomSide(route, x[i], y[i], z[i], 1);
            }
            var dais = GameObject.Find("Crystal dais");
            if (dais) { var cylinder = dais.GetComponent<Collider>(); if (cylinder) cylinder.enabled = true; }
        }

        static GameObject CreateRock(string name, Vector3 topCenter, float width, float depth)
        {
            // Broad, flat octagonal landing face with a shallow bevel; collider uses the same stable mesh.
            const int sides = 8;
            var vertices = new Vector3[sides * 2 + 2];
            var triangles = new int[sides * 12];
            vertices[sides * 2] = Vector3.zero;
            vertices[sides * 2 + 1] = new Vector3(0, -.9f, 0);
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides + Mathf.PI / 8f;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                vertices[i] = new Vector3(x * width * .48f, 0, z * depth * .48f);
                vertices[sides + i] = new Vector3(x * width * .54f, -.9f, z * depth * .54f);
            }
            int ti = 0;
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles[ti++] = sides * 2; triangles[ti++] = i; triangles[ti++] = next;
                triangles[ti++] = i; triangles[ti++] = sides + i; triangles[ti++] = next;
                triangles[ti++] = next; triangles[ti++] = sides + i; triangles[ti++] = sides + next;
                triangles[ti++] = sides * 2 + 1; triangles[ti++] = sides + next; triangles[ti++] = sides + i;
            }
            var mesh = new Mesh { name = name + " mesh", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var rock = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            rock.transform.position = topCenter; rock.GetComponent<MeshFilter>().sharedMesh = mesh; rock.GetComponent<MeshRenderer>().sharedMaterial = stone; rock.GetComponent<MeshCollider>().sharedMesh = mesh;
            var mossVertices = new Vector3[sides + 1]; var mossTriangles = new int[sides * 3];
            mossVertices[sides] = Vector3.up * .012f;
            for (int i = 0; i < sides; i++)
            {
                mossVertices[i] = vertices[i] * .86f + Vector3.up * .012f;
                int next = (i + 1) % sides;
                mossTriangles[i * 3] = sides; mossTriangles[i * 3 + 1] = i; mossTriangles[i * 3 + 2] = next;
            }
            var capMesh = new Mesh { name = "Flat moss landing surface mesh", vertices = mossVertices, triangles = mossTriangles };
            capMesh.RecalculateNormals(); capMesh.RecalculateBounds();
            var mossPatch = new GameObject("Flat moss landing surface", typeof(MeshFilter), typeof(MeshRenderer)); mossPatch.transform.SetParent(rock.transform, false);
            mossPatch.GetComponent<MeshFilter>().sharedMesh = capMesh; mossPatch.GetComponent<MeshRenderer>().sharedMaterial = moss;
            return rock;
        }

        static void CreateFloor(string name, Vector3 position, Vector3 size, Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = name; floor.transform.position = position; floor.transform.localScale = size;
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }
        static void AddCliffMass(Vector3 position, Vector3 size)
        {
            var mass = GameObject.CreatePrimitive(PrimitiveType.Sphere); mass.name = "Ravine rock face"; mass.transform.position = position; mass.transform.localScale = size;
            mass.GetComponent<Renderer>().sharedMaterial = stone; Object.Destroy(mass.GetComponent<Collider>());
        }
        static void PlaceFragment(LightFragment fragment, float x, float top, float z)
        {
            fragment.gameObject.SetActive(true); fragment.transform.position = new Vector3(x, top + 1.05f, z);
        }
        static void CreateMushroomSide(Transform route, float x, float top, float z, int side)
        {
            float shelfX = x + side * 3f;
            float shelfTop = top - .18f;
            var shelf = CreateRock("Mushroom shelf", new Vector3(shelfX, shelfTop, z + .15f), 1.9f, 1.9f);
            shelf.transform.SetParent(route, true);
            CreatePoisonCluster(new Vector3(shelfX, shelfTop, z + .15f));
        }

        static void BuildAdventurer()
        {
            var player = GameObject.Find("Aren"); if (!player) return;
            Add(player.transform, "Leather travel pack", PrimitiveType.Cube, new Vector3(0, 1.06f, -.38f), new Vector3(.52f, .66f, .3f), bark);
            foreach (string oldPart in new[] { "Aren cloak", "Aren hood", "Facing marker" })
            {
                var old = player.transform.Find(oldPart); if (old) old.gameObject.SetActive(false);
            }
            CreateProfilePart(player.transform, "Tailored adventurer jacket", Vector3.zero,
                new float[,] { { .45f, .23f, .14f }, { .72f, .25f, .15f }, { 1.02f, .27f, .17f }, { 1.3f, .36f, .19f }, { 1.52f, .3f, .17f }, { 1.62f, .18f, .13f } }, cloth);
            CreateProfilePart(player.transform, "Leather belt", Vector3.zero,
                new float[,] { { .69f, .253f, .153f }, { .75f, .263f, .16f }, { .81f, .26f, .16f } }, leather);
            CreateProfilePart(player.transform, "Face and head", Vector3.zero,
                new float[,] { { 1.48f, .035f, .07f }, { 1.55f, .145f, .15f }, { 1.7f, .205f, .19f }, { 1.84f, .19f, .18f }, { 1.96f, .11f, .12f }, { 2.0f, .02f, .035f } }, skin);
            CreateProfilePart(player.transform, "Hair", Vector3.zero,
                new float[,] { { 1.79f, .19f, .18f }, { 1.86f, .2f, .19f }, { 1.94f, .155f, .15f }, { 2.005f, .04f, .06f } }, hair);
            CreateProfilePart(player.transform, "Left sleeve", new Vector3(-.43f, 1.15f, 0),
                new float[,] { { -.48f, .085f, .09f }, { -.3f, .105f, .11f }, { 0f, .13f, .13f }, { .23f, .155f, .15f } }, cloth);
            CreateProfilePart(player.transform, "Right sleeve", new Vector3(.43f, 1.15f, 0),
                new float[,] { { -.48f, .085f, .09f }, { -.3f, .105f, .11f }, { 0f, .13f, .13f }, { .23f, .155f, .15f } }, cloth);
            CreateProfilePart(player.transform, "Left hand", new Vector3(-.43f, .64f, .02f),
                new float[,] { { -.1f, .065f, .055f }, { 0f, .105f, .075f }, { .12f, .085f, .065f } }, skin);
            CreateProfilePart(player.transform, "Right hand", new Vector3(.43f, .64f, .02f),
                new float[,] { { -.1f, .065f, .055f }, { 0f, .105f, .075f }, { .12f, .085f, .065f } }, skin);
            CreateProfilePart(player.transform, "Left trouser leg", new Vector3(-.2f, .48f, 0),
                new float[,] { { -.36f, .11f, .115f }, { -.2f, .13f, .14f }, { .08f, .145f, .15f }, { .34f, .16f, .16f } }, bark);
            CreateProfilePart(player.transform, "Right trouser leg", new Vector3(.2f, .48f, 0),
                new float[,] { { -.36f, .11f, .115f }, { -.2f, .13f, .14f }, { .08f, .145f, .15f }, { .34f, .16f, .16f } }, bark);
            var eye = Material("Adventurer eye", new Color(.035f, .025f, .02f));
            Add(player.transform, "Left eye", PrimitiveType.Sphere, new Vector3(-.075f, 1.72f, .176f), new Vector3(.045f, .055f, .035f), eye);
            Add(player.transform, "Right eye", PrimitiveType.Sphere, new Vector3(.075f, 1.72f, .176f), new Vector3(.045f, .055f, .035f), eye);
            Add(player.transform, "Nose", PrimitiveType.Sphere, new Vector3(0, 1.665f, .205f), new Vector3(.055f, .085f, .06f), skin);
            Add(player.transform, "Left boot", PrimitiveType.Cube, new Vector3(-.2f, .08f, .12f), new Vector3(.31f, .18f, .46f), leather);
            Add(player.transform, "Right boot", PrimitiveType.Cube, new Vector3(.2f, .08f, .12f), new Vector3(.31f, .18f, .46f), leather);

        }

        static void CreateProfilePart(Transform parent, string name, Vector3 position, float[,] profile, Material material)
        {
            const int sides = 12;
            int rings = profile.GetLength(0);
            var vertices = new Vector3[rings * sides]; var triangles = new int[(rings - 1) * sides * 6];
            for (int ring = 0; ring < rings; ring++)
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    vertices[ring * sides + i] = new Vector3(Mathf.Cos(a) * profile[ring, 1], profile[ring, 0], Mathf.Sin(a) * profile[ring, 2]);
                }
            int tri = 0;
            for (int ring = 0; ring < rings - 1; ring++)
                for (int i = 0; i < sides; i++)
                {
                    int a = ring * sides + i, b = ring * sides + (i + 1) % sides;
                    int c = (ring + 1) * sides + i, d = (ring + 1) * sides + (i + 1) % sides;
                    triangles[tri++] = a; triangles[tri++] = c; triangles[tri++] = b;
                    triangles[tri++] = b; triangles[tri++] = c; triangles[tri++] = d;
                }
            var mesh = new Mesh { name = name + " mesh", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(parent, false); part.transform.localPosition = position;
            part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<MeshRenderer>().sharedMaterial = material; part.layer = 2;
        }

        static void BuildFloorDetails()
        {
            Random.InitState(714);
            for (int i = 0; i < 220; i++)
            {
                float z = Random.Range(3f, 183f), x = Random.Range(-44f, 44f);
                if (z >= 98 && z < 164 || z >= 164 && z < 182 && Mathf.Abs(x) < 13) continue;
                if (Mathf.Abs(x - TrailX(z)) < 7) continue;
                var fern = new GameObject("Fern cluster"); fern.transform.position = new Vector3(x, 0, z);
                for (int blade = 0; blade < 4; blade++)
                {
                    var leafObj = GameObject.CreatePrimitive(PrimitiveType.Sphere); leafObj.name = "Fern frond";
                    leafObj.transform.SetParent(fern.transform, false); leafObj.transform.localPosition = new Vector3(0, .23f, 0);
                    leafObj.transform.localScale = new Vector3(.12f, Random.Range(.35f, .62f), .08f);
                    leafObj.transform.localRotation = Quaternion.Euler(Random.Range(-24f, 18f), blade * 90f, Random.Range(-18f, 18f));
                    leafObj.GetComponent<Renderer>().sharedMaterial = moss; Object.Destroy(leafObj.GetComponent<Collider>());
                }
            }
        }

        static void BuildPoisonMushrooms()
        {
            CreatePoisonCluster(new Vector3(-17, 0, 27));
            CreatePoisonCluster(new Vector3(24, 0, 69));
        }

        static void CreatePoisonCluster(Vector3 position)
        {
            var cluster = new GameObject("Glowing poisonous mushroom cluster"); cluster.transform.position = position;
            var trigger = cluster.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.center = Vector3.up * .47f; trigger.radius = .82f;
            var body = cluster.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            cluster.AddComponent<PoisonMushroom>();
            float[] offsets = { -.62f, 0f, .62f };
            for (int i = 0; i < offsets.Length; i++)
            {
                float height = i == 1 ? .8f : .52f;
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder); stem.name = "Pale mushroom stem";
                stem.transform.SetParent(cluster.transform, false); stem.transform.localPosition = new Vector3(offsets[i], height * .42f, i == 1 ? .08f : -.12f);
                stem.transform.localScale = new Vector3(.12f, height * .42f, .12f); stem.GetComponent<Renderer>().sharedMaterial = skin;
                Object.Destroy(stem.GetComponent<Collider>());
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere); cap.name = "Violet glowing cap";
                cap.transform.SetParent(cluster.transform, false); cap.transform.localPosition = new Vector3(offsets[i], height, i == 1 ? .08f : -.12f);
                cap.transform.localScale = i == 1 ? new Vector3(.56f, .25f, .52f) : new Vector3(.42f, .2f, .4f);
                cap.GetComponent<Renderer>().sharedMaterial = poison; Object.Destroy(cap.GetComponent<Collider>());
            }
        }

        public static void PoisonEffect(Transform player)
        {
            if (!player) return;
            var effect = new GameObject("Poison spore burst"); effect.transform.SetParent(player, false); effect.transform.localPosition = Vector3.up * .9f;
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main; main.loop = false; main.duration = .35f; main.startLifetime = .7f; main.startSpeed = 1.2f; main.startSize = .12f; main.startColor = new Color(.75f, .24f, 1f, .85f);
            var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .35f;
            var material = Resources.Load<Material>("LostKingdomSpark"); if (material) particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            particles.Play(); Object.Destroy(effect, 1.5f);
        }

        static void Add(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos; obj.transform.localScale = scale; obj.layer = 2;
            obj.GetComponent<Renderer>().sharedMaterial = material; Object.Destroy(obj.GetComponent<Collider>());
        }
        static Material Material(string name, Color color, Color? emission = null)
        {
            if (RuntimeMaterials.TryGetValue(name, out var cached) && cached) return cached;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name, color = color };
            if (emission.HasValue) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission.Value); }
            RuntimeMaterials[name] = mat;
            return mat;
        }
        static float TrailX(float z) => Mathf.Sin(z * .048f) * 13;
    }
}


namespace LostKingdom
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Collider))]
    public class PoisonMushroom : MonoBehaviour
    {
        bool consumed;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        void OnTriggerEnter(Collider other)
        {
            if (consumed || !GameManager.Instance) return;
            var player = other.GetComponentInParent<PlayerMovement>();
            if (!player) return;
            consumed = true;
            GameManager.Instance.AddPoison(player.transform);
            StartCoroutine(GlowBurst());
        }
        IEnumerator GlowBurst()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            for (float t = 0; t < .42f; t += Time.deltaTime)
            {
                float strength = Mathf.Lerp(4f, 1.2f, t / .42f);
                foreach (var renderer in renderers)
                {
                    renderer.GetPropertyBlock(block); block.SetColor(EmissionColor, new Color(.55f, .15f, 1f) * strength); renderer.SetPropertyBlock(block);
                }
                yield return null;
            }
            foreach (var renderer in renderers) renderer.SetPropertyBlock(null);
        }
    }
}
