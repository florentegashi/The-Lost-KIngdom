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
        void OnDestroy() { if (Application.isPlaying) { Destroy(ground); Destroy(leaves); } }
    }
}


namespace LostKingdom
{
    // Reshapes the existing Level 1 into a ravine climb while reusing its player, collectibles, gate, and UI.
    public static class ForestLevelUpgrade
    {
        static readonly Dictionary<string, Material> RuntimeMaterials = new Dictionary<string, Material>();
        static Material stone, moss, poison, bark, cloth, leather, skin, hair;
        static Material forestSoil, forestLitter, wornTrail;
        const string ForestRoadMaterialName = "Ancient forest road surface";
        static AudioClip mysticForestAmbience;
        static LightFragment fragmentFour, fragmentFive;
        static readonly float[] Sections = { 100f, 112f, 124f, 139f };

        public static void Build()
        {
            if (GameObject.Find("Mystic Forest ravine"))
            {
                FixWalkingColliders();
                UpgradeForestRoadVisuals();
                UpgradePoisonMushroomVisuals();
                StartMysticAmbience();
                return;
            }
            stone = Material("Ancient stone", new Color(.25f, .32f, .34f));
            moss = Material("Moss green", new Color(.12f, .27f, .18f));
            forestSoil = Material("Forest soil variation", new Color(.16f, .13f, .09f));
            forestLitter = Material("Soft moss ground", new Color(.13f, .205f, .12f));
            wornTrail = Material("Worn forest earth", new Color(.27f, .22f, .15f));
            ApplyMottledTexture(forestSoil, new Color(.16f, .13f, .09f), 41);
            ApplyMottledTexture(forestLitter, new Color(.13f, .205f, .12f), 83);
            // The legacy ribbon remains in the scene for its route shape, but its renderer is hidden.
            poison = PinkPoisonMaterial();
            bark = Material("Adventurer leather", new Color(.2f, .11f, .075f));
            cloth = Material("Adventurer teal", new Color(.1f, .32f, .34f));
            leather = Material("Boot leather", new Color(.1f, .07f, .05f));
            skin = Material("Warm skin", new Color(.68f, .44f, .3f));
            hair = Material("Adventurer hair", new Color(.095f, .052f, .035f));
            BuildTerrain(); BuildRoute(); BuildShrineClimb(); BuildPoisonMushrooms(); BuildAdventurer(); BuildFloorDetails();
            BuildVisualDressing(); BuildKeyObjectVisuals(); BuildAtmosphere();
            FixWalkingColliders();
            UpgradeForestRoadVisuals();
            UpgradePoisonMushroomVisuals();
            StartMysticAmbience();
        }

        public static void UpgradePoisonMushroomVisuals()
        {
            poison = PinkPoisonMaterial();
            if (!skin) skin = Material("Warm skin", new Color(.68f, .44f, .3f));
            AddAdditionalPoisonMushrooms();
            foreach (var mushroom in Object.FindObjectsByType<PoisonMushroom>(FindObjectsInactive.Include))
            {
                foreach (var renderer in mushroom.GetComponentsInChildren<Renderer>(true))
                    if (renderer.gameObject.name.IndexOf("cap", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        renderer.sharedMaterial = poison;

                if (!mushroom.transform.Find("Pink mushroom glow"))
                {
                    var glow = new GameObject("Pink mushroom glow"); glow.transform.SetParent(mushroom.transform, false);
                    glow.transform.localPosition = Vector3.up * .62f;
                    var light = glow.AddComponent<Light>(); light.type = LightType.Point;
                    light.color = new Color(1f, .12f, .46f); light.intensity = .8f; light.range = 2.7f;
                    light.shadows = LightShadows.None;
                }
            }
        }

        public static bool NeedsPoisonMushroomVisualUpgrade()
        {
            for (int i = 1; i <= 6; i++)
                if (!GameObject.Find("Forest poison patch " + i.ToString("00"))) return true;
            foreach (var mushroom in Object.FindObjectsByType<PoisonMushroom>(FindObjectsInactive.Include))
            {
                if (!mushroom.transform.Find("Pink mushroom glow")) return true;
                foreach (var renderer in mushroom.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name.IndexOf("cap", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var material = renderer.sharedMaterial;
                    if (!material || material.name != "Poisonous bloom") return true;
                    Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                    if (color.r < .8f || color.g > .3f || !material.IsKeywordEnabled("_EMISSION")) return true;
                }
            }
            return false;
        }

        static void AddAdditionalPoisonMushrooms()
        {
            float[] zPositions = { 9f, 24f, 43f, 62f, 81f, 92f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                string name = "Forest poison patch " + (i + 1).ToString("00");
                if (GameObject.Find(name)) continue;
                float z = zPositions[i];
                float side = i % 2 == 0 ? -1f : 1f;
                float x = TrailX(z) + side * 7.6f;
                var cluster = CreatePoisonCluster(new Vector3(x, .01f, z));
                cluster.name = name;
            }
        }

        static void StartMysticAmbience()
        {
            if (!Application.isPlaying || GameObject.Find("Mystic Forest eerie ambience")) return;
            if (!mysticForestAmbience)
            {
                const int sampleRate = 22050, seconds = 12;
                int sampleCount = sampleRate * seconds;
                var samples = new float[sampleCount * 2];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = i / (float)sampleRate;
                    // A long breath-like rise and fall, with a quicker tremble underneath,
                    // keeps the loop eerie and changing instead of sounding like a static hum.
                    float breath = .5f + .5f * Mathf.Sin(2f * Mathf.PI * t / seconds - Mathf.PI * .5f);
                    float swell = .12f + .88f * Mathf.Pow(breath, 1.7f);
                    float tremble = .82f + .18f * (.5f + .5f * Mathf.Sin(2f * Mathf.PI * t / 3f));
                    float pitchWaver = 1f + .035f * Mathf.Sin(2f * Mathf.PI * t / 5f);
                    float drone = Mathf.Sin(2f * Mathf.PI * 35f * pitchWaver * t) * .3f
                        + Mathf.Sin(2f * Mathf.PI * 36.7f * pitchWaver * t) * .23f
                        + Mathf.Sin(2f * Mathf.PI * 52f * pitchWaver * t) * .15f
                        + Mathf.Sin(2f * Mathf.PI * 67f * pitchWaver * t) * .1f;
                    float ghost = Mathf.Sin(2f * Mathf.PI * (190f + 18f * Mathf.Sin(2f * Mathf.PI * t / 7f)) * t) * .026f
                        + Mathf.Sin(2f * Mathf.PI * (283f + 27f * Mathf.Sin(2f * Mathf.PI * t / 9f + 1f)) * t) * .018f;
                    float wind = Mathf.Sin(2f * Mathf.PI * 83f * t + .7f) * .035f
                        + Mathf.Sin(2f * Mathf.PI * 127f * t + 1.8f) * .027f
                        + Mathf.Sin(2f * Mathf.PI * 173f * t + 2.4f) * .021f
                        + Mathf.Sin(2f * Mathf.PI * 229f * t + .2f) * .016f
                        + Mathf.Sin(2f * Mathf.PI * 293f * t + 1.1f) * .012f
                        + Mathf.Sin(2f * Mathf.PI * 367f * t + 2.9f) * .009f;
                    float pulse = Mathf.Sin(2f * Mathf.PI * 2f * t) * .025f;
                    float value = (drone + wind + pulse + ghost) * swell * tremble;
                    samples[i * 2] = value;
                    samples[i * 2 + 1] = value * .96f + Mathf.Sin(2f * Mathf.PI * 173f * t + .5f) * .004f;
                }
                mysticForestAmbience = AudioClip.Create("Mystic Forest eerie ambience", sampleCount, 2, sampleRate, false);
                mysticForestAmbience.SetData(samples, 0);
            }
            var sourceObject = new GameObject("Mystic Forest eerie ambience");
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = mysticForestAmbience; source.loop = true; source.playOnAwake = false;
            source.spatialBlend = 0; source.volume = .52f; source.priority = 48;
            sourceObject.AddComponent<MysticForestAmbienceDistance>();
            source.Play();
        }

        static Material PinkPoisonMaterial()
        {
            var material = Material("Poisonous bloom", new Color(1f, .12f, .48f), new Color(2.4f, .12f, .72f));
            material.color = new Color(1f, .12f, .48f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, .12f, .48f));
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(2.4f, .12f, .72f));
            return material;
        }

        public static bool NeedsRoadVisualUpgrade()
        {
            var ground = GameObject.Find("Forest entrance ground natural surface");
            if (!ground) return false;
            var renderer = ground.GetComponent<MeshRenderer>();
            var filter = ground.GetComponent<MeshFilter>();
            var road = GameObject.Find("Continuous winding forest path");
            var roadRenderer = road ? road.GetComponent<Renderer>() : null;
            return !renderer || !filter || !filter.sharedMesh || filter.sharedMesh.subMeshCount != 1 ||
                !renderer.sharedMaterial || renderer.sharedMaterial.name != ForestRoadMaterialName ||
                !renderer.sharedMaterial.GetTexture("_BaseMap") || !renderer.sharedMaterial.GetTexture("_BumpMap") ||
                (roadRenderer && roadRenderer.enabled);
        }

        public static void UpgradeForestRoadVisuals()
        {
            var ground = GameObject.Find("Forest entrance ground natural surface");
            if (!ground) return;
            var filter = ground.GetComponent<MeshFilter>();
            var renderer = ground.GetComponent<MeshRenderer>();
            if (!filter || !filter.sharedMesh || !renderer) return;

            var roadMaterial = CreateForestRoadMaterial();
            if (filter.sharedMesh.subMeshCount != 1 || filter.sharedMesh.uv.Length != filter.sharedMesh.vertexCount)
            {
                var mesh = Object.Instantiate(filter.sharedMesh);
                mesh.name = "Forest entrance seamless road and verge mesh";
                var triangles = new List<int>();
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++) triangles.AddRange(mesh.GetTriangles(submesh));
                mesh.subMeshCount = 1;
                mesh.SetTriangles(triangles, 0, true);
                var vertices = mesh.vertices;
                var uv = new Vector2[vertices.Length];
                var bounds = mesh.bounds;
                float width = Mathf.Max(.001f, bounds.size.x), depth = Mathf.Max(.001f, bounds.size.z);
                for (int i = 0; i < vertices.Length; i++)
                    uv[i] = new Vector2((vertices[i].x - bounds.min.x) / width, (vertices[i].z - bounds.min.z) / depth);
                mesh.uv = uv;
                mesh.RecalculateTangents(); mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
            }
            renderer.sharedMaterial = roadMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            var path = GameObject.Find("Continuous winding forest path");
            if (path && path.TryGetComponent<Renderer>(out var pathRenderer)) pathRenderer.enabled = false;
            BuildTrailEdgeRoots();
        }

        static void BuildTrailEdgeRoots()
        {
            if (GameObject.Find("Forest trail edge roots")) return;
            var details = new GameObject("Forest trail edge roots").transform;
            var bark = Material("Rootwood detail", new Color(.16f, .095f, .065f));
            Random.InitState(63917);
            for (int i = 0; i < 18; i++)
            {
                float z = Random.Range(4.5f, 92f);
                float center = TrailX(z) + (Mathf.PerlinNoise(z * .035f, 31f) - .5f) * .9f;
                float halfWidth = 3.15f + Mathf.PerlinNoise(z * .09f, 17f) * .85f;
                float side = i % 2 == 0 ? -1f : 1f;
                float run = Random.Range(.55f, 1.15f);
                Vector3 start = new Vector3(center + side * (halfWidth - Random.Range(.55f, .95f)), .03f, z - run * .5f);
                Vector3 end = new Vector3(center + side * (halfWidth + .12f), .024f, z + run * .5f);
                CreateTaperedSegment("Low exposed trail root", details, start, end, Random.Range(.022f, .034f), .012f, bark, 5);
            }
            CombineVisualMeshes(details);
        }

        static Material CreateForestRoadMaterial()
        {
            var material = Material(ForestRoadMaterialName, Color.white);
            if (!material.GetTexture("_BaseMap") || !material.GetTexture("_BumpMap") || !material.GetTexture("_OcclusionMap"))
                GenerateForestRoadTextures(material);
            return material;
        }

        static void GenerateForestRoadTextures(Material material)
        {
            const int width = 1024, height = 1216;
            var albedo = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
            { name = "Forest road soil leaves and moss albedo", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var normal = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
            { name = "Forest road soil normal detail", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var occlusion = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
            { name = "Forest road contact variation", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var colors = new Color[width * height];
            var heights = new float[width * height];
            var aoPixels = new Color[width * height];
            const float left = -50f, near = -20f, worldWidth = 100f, worldDepth = 118f;
            for (int py = 0; py < height; py++)
            {
                float v = py / (float)(height - 1);
                float z = near + v * worldDepth;
                float roadCenter = TrailX(z) + (Mathf.PerlinNoise(z * .035f, 31f) - .5f) * .9f;
                float roadHalfWidth = 3.15f + Mathf.PerlinNoise(z * .09f, 17f) * .85f;
                float startFade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-1.4f, .8f, z));
                float endFade = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(95.7f, 97f, z));
                for (int px = 0; px < width; px++)
                {
                    float u = px / (float)(width - 1);
                    float x = left + u * worldWidth;
                    float broad = Mathf.PerlinNoise(x * .105f + 19.2f, z * .105f + 7.4f);
                    float patches = Mathf.PerlinNoise(x * .37f + 41.6f, z * .37f + 13.1f);
                    float grain = Mathf.PerlinNoise(x * 2.8f + 7.3f, z * 2.8f + 57.1f);
                    float grit = Mathf.PerlinNoise(x * 12.5f + 83.2f, z * 12.5f + 29.7f);
                    float edgeWarp = (Mathf.PerlinNoise(x * .48f + 3.2f, z * .48f + 77.7f) - .5f) * .82f;
                    float signedEdge = Mathf.Abs(x - roadCenter) + edgeWarp - roadHalfWidth;
                    float roadMask = (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.55f, .58f, signedEdge))) * startFade * endFade;
                    float vergeMoss = Mathf.SmoothStep(.66f, .88f, patches) * Mathf.SmoothStep(-1.4f, -.1f, -Mathf.Abs(signedEdge - .8f));
                    float floorMoss = Mathf.SmoothStep(.75f, .92f, broad) * .52f;
                    float mossMask = Mathf.Clamp01(Mathf.Max(vergeMoss * .72f, floorMoss));
                    float shade = Mathf.Clamp(.70f + broad * .27f + (patches - .5f) * .22f + (grain - .5f) * .13f, .48f, 1.12f);
                    Color forestFloor = Color.Lerp(new Color(.105f, .155f, .078f), new Color(.075f, .145f, .07f), mossMask) * shade;
                    Color soil = new Color(.205f, .125f, .074f) * Mathf.Clamp(.74f + broad * .30f + (patches - .5f) * .25f + (grain - .5f) * .19f, .48f, 1.22f);
                    float pebbleMask = Mathf.SmoothStep(.76f, .91f, grit) * Mathf.SmoothStep(.48f, .69f, grain) * roadMask * .42f;
                    float leafMask = Mathf.SmoothStep(.82f, .94f, patches) * Mathf.SmoothStep(.65f, .91f, grit) * roadMask * .52f;
                    float roadMoss = Mathf.SmoothStep(.77f, .92f, broad) * roadMask * .26f;
                    soil = Color.Lerp(soil, new Color(.25f, .235f, .19f) * shade, pebbleMask);
                    soil = Color.Lerp(soil, new Color(.31f, .205f, .095f) * shade, leafMask);
                    soil = Color.Lerp(soil, new Color(.105f, .17f, .085f) * shade, roadMoss);
                    Color color = Color.Lerp(forestFloor, soil, roadMask);
                    float wet = Mathf.Clamp01((.43f - broad) * .8f + (mossMask + roadMoss) * .3f);
                    int index = py * width + px;
                    colors[index] = new Color(color.r, color.g, color.b, Mathf.Lerp(.13f, .78f, wet));
                    heights[index] = (broad - .5f) * .025f + (grain - .5f) * .012f + pebbleMask * .012f + mossMask * .004f;
                    aoPixels[index] = new Color(Mathf.Clamp01(.60f + patches * .24f + grit * .12f), 0, 0, 1);
                }
            }
            var normalPixels = new Color[width * height];
            const float normalStrength = 7.5f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float dx = heights[y * width + Mathf.Min(width - 1, x + 1)] - heights[y * width + Mathf.Max(0, x - 1)];
                    float dz = heights[Mathf.Min(height - 1, y + 1) * width + x] - heights[Mathf.Max(0, y - 1) * width + x];
                    Vector3 n = new Vector3(-dx * normalStrength, -dz * normalStrength, 1).normalized;
                    normalPixels[index] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z, 1);
                }
            albedo.SetPixels(colors); albedo.Apply(true, true);
            normal.SetPixels(normalPixels); normal.Apply(true, true);
            occlusion.SetPixels(aoPixels); occlusion.Apply(true, true);
            string baseMap = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            material.SetTexture(baseMap, albedo);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
            if (material.HasProperty("_OcclusionMap")) material.SetTexture("_OcclusionMap", occlusion);
            material.color = Color.white;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .38f);
            if (material.HasProperty("_SmoothnessTextureChannel")) material.SetFloat("_SmoothnessTextureChannel", 1);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", .62f);
            if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", .42f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_OCCLUSIONMAP");
        }

        // The baked trail is visual only. One continuous entrance floor collider provides
        // collision support, so the original tile colliders must not remain underneath it.
        public static void FixWalkingColliders()
        {
            var floor = GameObject.Find("Forest entrance ground");
            if (floor)
            {
                var box = floor.GetComponent<BoxCollider>();
                if (box) box.enabled = true;
            }

            foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
                if (collider.gameObject.name == "Lantern trail") collider.enabled = false;

            // Keep the generated path render surface nearly flush with the floor collider.
            var path = GameObject.Find("Continuous winding forest path");
            var pathFilter = path ? path.GetComponent<MeshFilter>() : null;
            if (pathFilter && pathFilter.sharedMesh && pathFilter.sharedMesh.bounds.center.y > .02f)
            {
                var flushMesh = Object.Instantiate(pathFilter.sharedMesh);
                var vertices = flushMesh.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i].y = .012f;
                flushMesh.vertices = vertices; flushMesh.RecalculateBounds();
                flushMesh.name = "Winding forest path mesh";
                pathFilter.sharedMesh = flushMesh;
            }

            ConfigureShrinePlatformPositions();

            // At a 10 m/s run, a 1.6 m jump under -24 m/s² has about 7.3 m of
            // same-height air travel. The longest ordinary transfer is under 6 m;
            // the final shrine transfer is kept as the intended double jump.
            foreach (var meshCollider in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include))
            {
                string name = meshCollider.gameObject.name;
                if (name != "Ravine climbing ledge" && name != "Gate approach ledge" &&
                    name != "Shrine ascent ledge" && name != "Double jump cliff shelf" && name != "Mushroom shelf") continue;
                ConfigureFloatingPlatform(meshCollider);
            }

            // Already-repaired boxes are refreshed too, so repeated startup calls stay idempotent.
            foreach (var platform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (IsFloatingPlatform(platform.name)) ConfigureFloatingPlatform(platform.gameObject);
        }

        static bool IsFloatingPlatform(string name) => name == "Ravine climbing ledge" || name == "Gate approach ledge"
            || name == "Shrine ascent ledge" || name == "Double jump cliff shelf" || name == "Mushroom shelf";

        public static bool NeedsFloatingPlatformUpgrade()
        {
            bool found = false;
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!IsFloatingPlatform(transform.name)) continue;
                found = true;
                var filter = transform.GetComponent<MeshFilter>();
                var meshCollider = transform.GetComponent<MeshCollider>();
                var box = transform.GetComponent<BoxCollider>();
                if (!filter || !filter.sharedMesh || !filter.sharedMesh.name.Contains("flat floating island") ||
                    (meshCollider && meshCollider.enabled) || !box || !transform.Find("Mystic platform rim glow") ||
                    !transform.Find("Floating island motes") || !transform.Find("Floating island vegetation")) return true;
                Vector2 size = FloatingPlatformSize(transform.gameObject);
                if (Mathf.Abs(box.center.y + .06f) > .005f || Mathf.Abs(box.size.y - .12f) > .005f ||
                    Mathf.Abs(box.size.x - size.x) > .01f || Mathf.Abs(box.size.z - size.y) > .01f) return true;
            }
            if (!found) return false;
            var shrine = new List<Transform>();
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (transform.name == "Shrine ascent ledge" || transform.name == "Double jump cliff shelf") shrine.Add(transform);
            shrine.Sort((a, b) => a.position.z.CompareTo(b.position.z));
            float[] expectedZ = { 165.4f, 168.4f, 171.4f, 174.4f, 179f };
            for (int i = 0; i < Mathf.Min(shrine.Count, expectedZ.Length); i++)
                if (Mathf.Abs(shrine[i].position.z - expectedZ[i]) > .01f) return true;
            return false;
        }

        static void ConfigureShrinePlatformPositions()
        {
            var platforms = new List<Transform>();
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (transform.name == "Shrine ascent ledge" || transform.name == "Double jump cliff shelf") platforms.Add(transform);
            platforms.Sort((a, b) => a.position.z.CompareTo(b.position.z));
            float[] x = { .4f, -1.1f, 1.2f, -1.35f, 4.4f };
            float[] y = { 4.2f, 4.82f, 5.44f, 6.06f, 6.9f };
            float[] z = { 165.4f, 168.4f, 171.4f, 174.4f, 179f };
            for (int i = 0; i < Mathf.Min(platforms.Count, z.Length); i++)
                platforms[i].position = new Vector3(x[i], y[i], z[i]);
        }

        static Vector2 FloatingPlatformSize(GameObject platform)
        {
            string name = platform.name;
            float z = platform.transform.position.z;
            if (name == "Mushroom shelf") return new Vector2(2f, 2f);
            if (name == "Gate approach ledge") return new Vector2(3.2f, 3.1f);
            if (name == "Double jump cliff shelf") return new Vector2(3.3f, 3.3f);
            if (name == "Shrine ascent ledge") return new Vector2(3.45f, 2.7f);
            if (z < 124f) return new Vector2(3.15f, 3.3f);
            if (z < 139f) return new Vector2(3.35f, 3.2f);
            return new Vector2(3.3f, 3.3f);
        }

        static void ConfigureFloatingPlatform(MeshCollider meshCollider)
        {
            var platform = meshCollider.gameObject;
            var filter = platform.GetComponent<MeshFilter>();
            var renderer = platform.GetComponent<MeshRenderer>();
            if (!filter || !renderer || !meshCollider.sharedMesh) return;

            Vector2 size = FloatingPlatformSize(platform);
            // Rebuild old faceted landings as beveled rectangular islands with a broad, truly flat top.
            if (!filter.sharedMesh.name.Contains("flat floating island"))
            {
                var island = CreateFloatingIslandMesh(size.x, size.y);
                island.name = platform.name + " flat floating island";
                filter.sharedMesh = island;
                var mossSurface = platform.transform.Find("Flat moss landing surface");
                if (mossSurface)
                {
                    var top = mossSurface.GetComponent<MeshFilter>();
                    if (top) top.sharedMesh = CreateFloatingIslandTopMesh(size.x, size.y);
                }
            }

            var box = platform.GetComponent<BoxCollider>();
            if (!box) box = platform.AddComponent<BoxCollider>();
            box.center = new Vector3(0, -.06f, 0);
            box.size = new Vector3(size.x, .12f, size.y);
            meshCollider.enabled = false;
            DestroyGenerated(meshCollider);
            AddFloatingPlatformVisuals(platform.transform, size);
        }

        static void ConfigureFloatingPlatform(GameObject platform)
        {
            var filter = platform.GetComponent<MeshFilter>();
            var renderer = platform.GetComponent<MeshRenderer>();
            if (!filter || !renderer) return;
            Vector2 size = FloatingPlatformSize(platform);
            if (!filter.sharedMesh || !filter.sharedMesh.name.Contains("flat floating island"))
            {
                filter.sharedMesh = CreateFloatingIslandMesh(size.x, size.y);
                filter.sharedMesh.name = platform.name + " flat floating island";
                var top = platform.transform.Find("Flat moss landing surface")?.GetComponent<MeshFilter>();
                if (top) top.sharedMesh = CreateFloatingIslandTopMesh(size.x, size.y);
            }
            var box = platform.GetComponent<BoxCollider>();
            if (!box) box = platform.AddComponent<BoxCollider>();
            box.center = new Vector3(0, -.06f, 0);
            box.size = new Vector3(size.x, .12f, size.y);
            AddFloatingPlatformVisuals(platform.transform, size);
        }

        static Mesh CreateFloatingIslandMesh(float width, float depth)
        {
            const int sides = 8;
            var vertices = new Vector3[sides * 2 + 2];
            var triangles = new int[sides * 12];
            float corner = Mathf.Min(.06f, Mathf.Min(width, depth) * .025f);
            Vector2[] top = RoundedRectangle(width, depth, corner);
            Vector2[] baseRing = RoundedRectangle(width * 1.08f, depth * 1.08f, corner * 1.25f);
            for (int i = 0; i < sides; i++)
            {
                vertices[i] = new Vector3(top[i].x, 0, top[i].y);
                vertices[sides + i] = new Vector3(baseRing[i].x, -.9f, baseRing[i].y);
            }
            vertices[sides * 2] = Vector3.zero;
            vertices[sides * 2 + 1] = new Vector3(0, -.9f, 0);
            int ti = 0;
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles[ti++] = sides * 2; triangles[ti++] = next; triangles[ti++] = i;
                triangles[ti++] = i; triangles[ti++] = next; triangles[ti++] = sides + i;
                triangles[ti++] = next; triangles[ti++] = sides + next; triangles[ti++] = sides + i;
                triangles[ti++] = sides * 2 + 1; triangles[ti++] = sides + i; triangles[ti++] = sides + next;
            }
            var mesh = new Mesh { name = "flat floating island" };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateFloatingIslandTopMesh(float width, float depth)
        {
            const int sides = 8;
            Vector2[] outline = RoundedRectangle(width - .035f, depth - .035f, Mathf.Min(.06f, Mathf.Min(width, depth) * .025f));
            var vertices = new Vector3[sides + 1]; var triangles = new int[sides * 3];
            vertices[sides] = new Vector3(0, .008f, 0);
            for (int i = 0; i < sides; i++)
            {
                vertices[i] = new Vector3(outline[i].x, .008f, outline[i].y);
                triangles[i * 3] = sides; triangles[i * 3 + 1] = (i + 1) % sides; triangles[i * 3 + 2] = i;
            }
            var mesh = new Mesh { name = "Flat moss landing surface mesh" };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static Vector2[] RoundedRectangle(float width, float depth, float corner)
        {
            float x = width * .5f, z = depth * .5f;
            return new[] { new Vector2(-x + corner, -z), new Vector2(x - corner, -z),
                new Vector2(x, -z + corner), new Vector2(x, z - corner), new Vector2(x - corner, z),
                new Vector2(-x + corner, z), new Vector2(-x, z - corner), new Vector2(-x, -z + corner) };
        }

        static void AddFloatingPlatformVisuals(Transform platform, Vector2 size)
        {
            if (!platform.Find("Mystic platform rim glow"))
            {
                var rim = new GameObject("Mystic platform rim glow"); rim.transform.SetParent(platform, false);
                var line = rim.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true; line.positionCount = 8;
                float corner = Mathf.Min(.06f, Mathf.Min(size.x, size.y) * .025f);
                var points = RoundedRectangle(size.x - .04f, size.y - .04f, corner);
                for (int i = 0; i < points.Length; i++) line.SetPosition(i, new Vector3(points[i].x, -.025f, points[i].y));
                line.widthMultiplier = .025f; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var glow = Material("Floating platform edge glow", new Color(.24f, .66f, .58f), new Color(.08f, .48f, .39f));
                line.sharedMaterial = glow;
            }
            if (!platform.Find("Floating island motes"))
            {
                var particlesObject = new GameObject("Floating island motes"); particlesObject.transform.SetParent(platform, false);
                particlesObject.transform.localPosition = new Vector3(0, -.08f, 0);
                var particles = particlesObject.AddComponent<ParticleSystem>();
                var main = particles.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.025f, .08f); main.startSize = new ParticleSystem.MinMaxCurve(.018f, .035f);
                main.startColor = new Color(.44f, .9f, .75f, .72f); main.maxParticles = 12;
                var emission = particles.emission; emission.rateOverTime = .55f;
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(size.x * .92f, .08f, size.y * .92f);
                var particleRenderer = particlesObject.GetComponent<ParticleSystemRenderer>();
                var spark = Resources.Load<Material>("LostKingdomSpark"); if (spark) particleRenderer.sharedMaterial = spark;
                particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; particles.Play();
            }
            if (!platform.Find("Floating island vegetation"))
            {
                var vegetation = new GameObject("Floating island vegetation").transform;
                vegetation.SetParent(platform, false);
                var green = Material("Fern jade", new Color(.09f, .31f, .21f));
                float x = size.x * .52f, z = size.y * .52f;
                CreateFern(new Vector3(platform.position.x - x, platform.position.y - .18f, platform.position.z - z), .38f, green, vegetation);
                CreateFern(new Vector3(platform.position.x + x, platform.position.y - .18f, platform.position.z + z), .38f, green, vegetation);
                CreateFern(new Vector3(platform.position.x + x, platform.position.y - .18f, platform.position.z - z), .34f, green, vegetation);
                CreateFern(new Vector3(platform.position.x - x, platform.position.y - .18f, platform.position.z + z), .34f, green, vegetation);
            }
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
            BuildTreeVisuals(rootBark, fernGreen, fernLight);

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

        public static void UpgradeTreeCanopies()
        {
            if (!NeedsTreeCanopyUpgrade()) return;
            BuildTreeVisuals(Material("Rootwood detail", new Color(.16f, .095f, .065f)),
                Material("Fern jade", new Color(.09f, .31f, .21f)), Material("Fern tips", new Color(.25f, .48f, .29f)));
        }

        public static bool NeedsTreeCanopyUpgrade()
        {
            foreach (var tree in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if ((tree.name == "PineTree" || tree.name == "Low-poly pine") && (tree.position.z < 98 || tree.position.z >= 182) && !tree.Find("Wind swept canopy"))
                    return true;
            return false;
        }

        static void BuildTreeVisuals(Material rootBark, Material fernGreen, Material fernLight)
        {
            Random.InitState(49271);
            foreach (var tree in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if ((tree.name != "PineTree" && tree.name != "Low-poly pine") || tree.position.z >= 98 && tree.position.z < 182) continue;
                if (!tree.Find("Wind swept canopy"))
                {
                    foreach (Transform child in tree)
                        if (child.name == "Faceted crown") child.gameObject.SetActive(false);
                    var canopy = new GameObject("Wind swept canopy").transform; canopy.SetParent(tree, false);
                    canopy.gameObject.AddComponent<ForestWindSway>();
                    for (int tier = 0; tier < 4; tier++)
                        CreateNeedleCrown(canopy, 3.0f + tier * 1.12f, 2.45f - tier * .48f, 2.5f - tier * .18f, tier, tier % 2 == 0 ? fernGreen : fernLight);
                    CombineVisualMeshes(canopy);
                }
                if (tree.Find("Root and branch detail")) continue;
                var detail = new GameObject("Root and branch detail").transform; detail.SetParent(tree, false);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f + Random.Range(-.18f, .18f);
                    Vector3 end = new Vector3(Mathf.Cos(a) * Random.Range(1.15f, 1.7f), -.04f, Mathf.Sin(a) * Random.Range(1.15f, 1.7f));
                    CreateTaperedSegment("Twisted exposed root", detail, new Vector3(0, .26f, 0), end, .26f, .055f, rootBark, 6);
                }
                for (int i = 0; i < 2; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 start = new Vector3(0, Random.Range(2.1f, 2.8f), 0);
                    Vector3 end = start + new Vector3(Mathf.Cos(a) * 1.55f, .65f, Mathf.Sin(a) * 1.55f);
                    CreateTaperedSegment("Gnarled pine bough", detail, start, end, .14f, .055f, rootBark, 5);
                }
                CombineVisualMeshes(detail);
            }
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
            var motes = new GameObject("Blue forest motes"); motes.transform.position = new Vector3(0, 3.5f, 80f);
            var particles = motes.AddComponent<ParticleSystem>();
            var main = particles.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f); main.startSpeed = new ParticleSystem.MinMaxCurve(.025f, .13f);
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .075f); main.startColor = new Color(.39f, .72f, .83f, .48f); main.maxParticles = 180;
            var emission = particles.emission; emission.rateOverTime = 16;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(86, 7, 166);
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            var sparkMaterial = Resources.Load<Material>("LostKingdomSpark"); if (sparkMaterial) particleRenderer.sharedMaterial = sparkMaterial;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particles.Play();
        }

        static void CreateFern(Vector3 position, float scale, Material material, Transform parent)
        {
            const int blades = 7, segments = 5;
            var vertices = new Vector3[blades * (segments + 1) * 2];
            // Each blade segment writes two quads, with front and back faces (12 indices).
            var triangles = new int[blades * segments * 12];
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

        static void CreateNeedleCrown(Transform parent, float y, float radius, float height, int tier, Material material)
        {
            const int sides = 9;
            var vertices = new Vector3[sides * 2 + 1];
            var triangles = new int[sides * 6];
            vertices[sides * 2] = new Vector3(0, height, 0);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides + tier * .23f;
                float r = radius * Random.Range(.84f, 1.12f);
                vertices[i] = new Vector3(Mathf.Cos(a) * r, Random.Range(-.18f, .18f), Mathf.Sin(a) * r);
                vertices[sides + i] = new Vector3(Mathf.Cos(a) * r * .5f, height * .48f, Mathf.Sin(a) * r * .5f);
                int next = (i + 1) % sides, t = i * 6;
                triangles[t] = i; triangles[t + 1] = next; triangles[t + 2] = sides + i;
                triangles[t + 3] = next; triangles[t + 4] = sides + next; triangles[t + 5] = sides + i;
            }
            // Add the crown tip faces as a second band.
            var expanded = new int[triangles.Length + sides * 3]; triangles.CopyTo(expanded, 0);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides, t = triangles.Length + i * 3;
                expanded[t] = sides + i; expanded[t + 1] = sides + next; expanded[t + 2] = sides * 2;
            }
            var mesh = new Mesh { name = "Irregular evergreen bough mesh" };
            mesh.vertices = vertices; mesh.triangles = expanded; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject("Layered evergreen boughs", typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false); obj.transform.localPosition = new Vector3(0, y, 0);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            obj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            obj.AddComponent<ForestWindSway>();
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
                var filter = renderer.GetComponent<MeshFilter>(); if (filter && filter.sharedMesh) DestroyGenerated(filter.sharedMesh);
                DestroyGenerated(renderer.gameObject);
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
                BuildWindingTrail();
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
                    float width = section == 2 ? 3.35f : section == 3 ? 3.3f : 3.15f;
                    float depth = section == 2 ? 3.2f : 3.3f;
                    var rock = CreateRock("Ravine climbing ledge", new Vector3(x, top, z), width, depth);
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
            DestroyGenerated(wisp.GetComponent<Collider>());
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
            float[] z = { 165.4f, 168.4f, 171.4f, 174.4f, 179.0f };
            float[] y = { 4.2f, 4.82f, 5.44f, 6.06f, 6.9f };
            for (int i = 0; i < z.Length; i++)
            {
                var rock = CreateRock(i == 4 ? "Double jump cliff shelf" : "Shrine ascent ledge", new Vector3(x[i], y[i], z[i]), i == 4 ? 3.3f : 3.45f, i == 4 ? 3.3f : 2.7f);
                rock.transform.SetParent(route, true);
                if (i == 2) CreateMushroomSide(route, x[i], y[i], z[i], 1);
            }
            var dais = GameObject.Find("Crystal dais");
            if (dais) { var cylinder = dais.GetComponent<Collider>(); if (cylinder) cylinder.enabled = true; }
        }

        static GameObject CreateRock(string name, Vector3 topCenter, float width, float depth)
        {
            // Rounded rectangular island: a broad, flat top tapers into a rocky underside.
            var rock = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            rock.transform.position = topCenter;
            rock.GetComponent<MeshFilter>().sharedMesh = CreateFloatingIslandMesh(width, depth);
            rock.GetComponent<MeshRenderer>().sharedMaterial = stone;
            var landing = rock.GetComponent<BoxCollider>(); landing.size = new Vector3(width, .12f, depth); landing.center = new Vector3(0, -.06f, 0);
            var mossPatch = new GameObject("Flat moss landing surface", typeof(MeshFilter), typeof(MeshRenderer)); mossPatch.transform.SetParent(rock.transform, false);
            mossPatch.GetComponent<MeshFilter>().sharedMesh = CreateFloatingIslandTopMesh(width, depth);
            mossPatch.GetComponent<MeshRenderer>().sharedMaterial = moss;
            AddFloatingPlatformVisuals(rock.transform, new Vector2(width, depth));
            return rock;
        }

        static void CreateFloor(string name, Vector3 position, Vector3 size, Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = name; floor.transform.position = position; floor.transform.localScale = size;
            var floorRenderer = floor.GetComponent<Renderer>(); floorRenderer.sharedMaterial = material; floorRenderer.enabled = false;
            CreateNaturalSurface(name + " natural surface", position, size, material);
        }

        static void CreateNaturalSurface(string name, Vector3 center, Vector3 size, Material baseMaterial)
        {
            int nx = Mathf.Max(2, Mathf.CeilToInt(size.x / 2.2f));
            int nz = Mathf.Max(2, Mathf.CeilToInt(size.z / 2.2f));
            var vertices = new Vector3[(nx + 1) * (nz + 1)];
            var uv = new Vector2[vertices.Length];
            var submesh = new List<int>[3] { new List<int>(), new List<int>(), new List<int>() };
            float left = center.x - size.x * .5f, near = center.z - size.z * .5f;
            float top = center.y + size.y * .5f + .012f;
            for (int z = 0; z <= nz; z++)
                for (int x = 0; x <= nx; x++)
                {
                    float wx = left + size.x * x / nx;
                    float wz = near + size.z * z / nz;
                    float pathDistance = Mathf.Abs(wx - TrailX(wz));
                    float mask = Mathf.Clamp01((pathDistance - 5f) / 5f);
                    float noise = Mathf.PerlinNoise(wx * .075f + 3.7f, wz * .075f + 11.2f);
                    float broad = Mathf.PerlinNoise(wx * .023f + 19f, wz * .023f + 5f);
                    float variation = (noise - .5f) * .045f + (broad - .5f) * .035f;
                    int index = z * (nx + 1) + x;
                    vertices[index] = new Vector3(wx, top + variation * mask, wz);
                    uv[index] = new Vector2(wx * .18f, wz * .18f);
                }
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    int a = z * (nx + 1) + x, b = a + 1, c = a + nx + 1, d = c + 1;
                    float wx = left + size.x * (x + .5f) / nx, wz = near + size.z * (z + .5f) / nz;
                    float patch = Mathf.PerlinNoise(wx * .105f + 7f, wz * .105f + 2f);
                    int layer = patch < .23f ? 1 : patch > .79f ? 2 : 0;
                    submesh[layer].Add(a); submesh[layer].Add(c); submesh[layer].Add(b);
                    submesh[layer].Add(b); submesh[layer].Add(c); submesh[layer].Add(d);
                }
            var mesh = new Mesh { name = name + " mesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vertices; mesh.uv = uv; mesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) mesh.SetTriangles(submesh[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var surface = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            surface.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { baseMaterial, forestSoil, forestLitter };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void ApplyMottledTexture(Material material, Color tint, int seed)
        {
            const int resolution = 128;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true) { name = material.name + " procedural grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)resolution, v = y / (float)resolution;
                    float broad = Mathf.PerlinNoise(u * 5.5f + seed, v * 5.5f + seed * .37f);
                    float grain = Mathf.PerlinNoise(u * 24f + seed * .13f, v * 24f + seed);
                    float flecks = Mathf.PerlinNoise(u * 83f + seed, v * 83f + seed * .71f);
                    float value = Mathf.Clamp01(.48f + (broad - .5f) * .66f + (grain - .5f) * .48f + (flecks - .5f) * .22f);
                    pixels[y * resolution + x] = tint * Mathf.Lerp(.58f, 1.42f, value);
                }
            texture.SetPixels(pixels); texture.Apply(true, true);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            material.color = Color.white;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .16f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .16f);
        }

        static void BuildWindingTrail()
        {
            // The continuous entrance floor is the only collider for this walking area.
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
                if (renderer.gameObject.name == "Lantern trail")
                {
                    renderer.enabled = false;
                    foreach (var collider in renderer.GetComponents<Collider>()) collider.enabled = false;
                }
            // End the trail before the ravine so this visual surface cannot suggest a shortcut over the gap.
            const int segments = 90;
            var vertices = new Vector3[(segments + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float z = i * (97f / segments);
                float center = TrailX(z) + (Mathf.PerlinNoise(z * .035f, 31f) - .5f) * .9f;
                float halfWidth = 3.15f + Mathf.PerlinNoise(z * .09f, 17f) * .85f;
                int index = i * 2;
                vertices[index] = new Vector3(center - halfWidth, .012f, z);
                vertices[index + 1] = new Vector3(center + halfWidth, .012f, z);
                uv[index] = new Vector2(0, z * .22f); uv[index + 1] = new Vector2(1, z * .22f);
                if (i == segments) continue;
                int t = i * 6;
                triangles[t] = index; triangles[t + 1] = index + 2; triangles[t + 2] = index + 1;
                triangles[t + 3] = index + 1; triangles[t + 4] = index + 2; triangles[t + 5] = index + 3;
            }
            var mesh = new Mesh { name = "Winding forest path mesh" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var path = new GameObject("Continuous winding forest path", typeof(MeshFilter), typeof(MeshRenderer));
            path.GetComponent<MeshFilter>().sharedMesh = mesh; path.GetComponent<MeshRenderer>().sharedMaterial = wornTrail;
            path.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void AddCliffMass(Vector3 position, Vector3 size)
        {
            var mass = GameObject.CreatePrimitive(PrimitiveType.Sphere); mass.name = "Ravine rock face"; mass.transform.position = position; mass.transform.localScale = size;
            mass.GetComponent<Renderer>().sharedMaterial = stone; DestroyGenerated(mass.GetComponent<Collider>());
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
            var leaf = Material("Copper forest leaves", new Color(.28f, .19f, .105f));
            var stoneDust = Material("Weathered path stones", new Color(.24f, .28f, .27f));
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
                    leafObj.GetComponent<Renderer>().sharedMaterial = moss; DestroyGenerated(leafObj.GetComponent<Collider>());
                }
            }
            // Scatter a restrained mix of leaf litter and embedded stones away from the walkable trail.
            for (int i = 0; i < 520; i++)
            {
                float z = Random.Range(2f, 184f), x = Random.Range(-46f, 46f);
                if ((z >= 98 && z < 182) || Mathf.Abs(x - TrailX(z)) < 5.4f) continue;
                bool pebble = i % 4 == 0;
                var detail = GameObject.CreatePrimitive(pebble ? PrimitiveType.Sphere : PrimitiveType.Cube);
                detail.name = pebble ? "Mossy forest pebble" : "Fallen copper leaf";
                detail.transform.position = new Vector3(x, pebble ? .035f : .018f, z);
                detail.transform.rotation = Quaternion.Euler(pebble ? Random.Range(-12f, 12f) : Random.Range(0f, 18f), Random.Range(0f, 360f), pebble ? 0 : Random.Range(-18f, 18f));
                float size = Random.Range(.12f, .4f);
                detail.transform.localScale = pebble ? new Vector3(size * 1.7f, size * .55f, size) : new Vector3(size * 1.8f, .035f, size * .65f);
                detail.GetComponent<Renderer>().sharedMaterial = pebble ? stoneDust : leaf;
                DestroyGenerated(detail.GetComponent<Collider>());
            }
        }

        static void BuildPoisonMushrooms()
        {
            CreatePoisonCluster(new Vector3(-17, 0, 27));
            CreatePoisonCluster(new Vector3(24, 0, 69));
        }

        static GameObject CreatePoisonCluster(Vector3 position)
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
                DestroyGenerated(stem.GetComponent<Collider>());
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere); cap.name = "Pink glowing poison cap";
                cap.transform.SetParent(cluster.transform, false); cap.transform.localPosition = new Vector3(offsets[i], height, i == 1 ? .08f : -.12f);
                cap.transform.localScale = i == 1 ? new Vector3(.56f, .25f, .52f) : new Vector3(.42f, .2f, .4f);
                cap.GetComponent<Renderer>().sharedMaterial = poison; DestroyGenerated(cap.GetComponent<Collider>());
            }
            return cluster;
        }

        public static void PoisonEffect(Transform player)
        {
            if (!player) return;
            var effect = new GameObject("Poison spore burst"); effect.transform.SetParent(player, false); effect.transform.localPosition = Vector3.up * .9f;
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main; main.loop = false; main.duration = .35f; main.startLifetime = .7f; main.startSpeed = 1.2f; main.startSize = .12f; main.startColor = new Color(1f, .12f, .5f, .85f);
            var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .35f;
            var material = Resources.Load<Material>("LostKingdomSpark"); if (material) particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            particles.Play(); DestroyGenerated(effect, 1.5f);
        }

        static void Add(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos; obj.transform.localScale = scale; obj.layer = 2;
            obj.GetComponent<Renderer>().sharedMaterial = material; DestroyGenerated(obj.GetComponent<Collider>());
        }
        static void DestroyGenerated(Object target, float delay = 0)
        {
            if (!target) return;
            if (Application.isPlaying)
            {
                if (delay > 0) Object.Destroy(target, delay); else Object.Destroy(target);
            }
            else Object.DestroyImmediate(target);
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
    public sealed class ForestWindSway : MonoBehaviour
    {
        float phase;
        void Awake() { phase = Random.value * Mathf.PI * 2f; }
        void Update()
        {
            float time = Time.time * .68f + phase;
            transform.localRotation = Quaternion.Euler(Mathf.Sin(time) * 1.7f, 0, Mathf.Cos(time * .83f) * 2.1f);
        }
    }

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Collider))]
    public class PoisonMushroom : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        void OnTriggerEnter(Collider other)
        {
            if (!GameManager.Instance || !GameManager.Instance.IsPlaying) return;
            var player = other.GetComponentInParent<PlayerMovement>();
            if (!player) return;
            GameManager.Instance.AddPoison(player.transform);
            StartCoroutine(GlowBurst());
        }
        IEnumerator GlowBurst()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            for (float t = 0; t < .42f; t += Time.deltaTime)
            {
                float strength = Mathf.Lerp(3.5f, 1.1f, t / .42f);
                foreach (var renderer in renderers)
                {
                    renderer.GetPropertyBlock(block); block.SetColor(EmissionColor, new Color(1f, .08f, .42f) * strength); renderer.SetPropertyBlock(block);
                }
                yield return null;
            }
            foreach (var renderer in renderers) renderer.SetPropertyBlock(null);
        }
    }

    // Keeps the forest's eerie drone loud in the ravine and soft near its crystal sanctuary.
    public sealed class MysticForestAmbienceDistance : MonoBehaviour
    {
        AudioSource source;
        Transform crystal;
        float currentVolume;
        const float QuietVolume = .035f;
        const float LoudVolume = .52f;
        const float DistanceForFullVolume = 75f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            currentVolume = LoudVolume;
        }

        void Update()
        {
            if (!crystal)
            {
                var crystalObject = GameObject.Find("Crystal of Light");
                if (!crystalObject) crystalObject = GameObject.Find("Crystal dais");
                if (crystalObject) crystal = crystalObject.transform;
            }
            var player = FindAnyObjectByType<PlayerMovement>();
            float target = LoudVolume;
            if (player && crystal)
            {
                float distance = Vector3.Distance(player.transform.position, crystal.position);
                float away = Mathf.Clamp01(distance / DistanceForFullVolume);
                target = Mathf.Lerp(QuietVolume, LoudVolume, away * away);
            }
            currentVolume = Mathf.MoveTowards(currentVolume, target, Time.deltaTime * .2f);
            if (source) source.volume = currentVolume;
        }
    }
}
