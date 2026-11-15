using System.Linq;
using System.Text.RegularExpressions;
using LostKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostKingdom.Editor
{
    public static class MysticForestSceneBaker
    {
        const string ScenePath = "Assets/Scenes/MysticForest.unity";
        const string GeneratedPath = "Assets/Generated/MysticForest";
        static bool isBaking;

        [InitializeOnLoadMethod]
        static void SubscribeToSceneOpen()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += BakeOpenedLevelOne;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += BakeOpenedLevelOne;
        }

        static void BakeOpenedLevelOne()
        {
            if (isBaking || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
            if (GameObject.Find("Mystic Forest ravine") && ForestLevelUpgrade.NeedsRoadVisualUpgrade())
            {
                BakeRoadSurfaceActiveScene();
                return;
            }
            if (GameObject.Find("Mystic Forest ravine") && ForestLevelUpgrade.NeedsPoisonMushroomVisualUpgrade())
            {
                BakePoisonMushroomsActiveScene();
                return;
            }
            if (GameObject.Find("Mystic Forest ravine") && !ForestLevelUpgrade.NeedsTreeCanopyUpgrade()
                && !ForestLevelUpgrade.NeedsFloatingPlatformUpgrade()) return;
            BakeActiveScene();
        }

        [MenuItem("The Lost Kingdom/Bake Mystic Forest Road Surface")]
        public static void BakeRoadSurface()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            BakeRoadSurfaceActiveScene();
        }

        static void BakeRoadSurfaceActiveScene()
        {
            if (isBaking || EditorSceneManager.GetActiveScene().path != ScenePath) return;
            isBaking = true;
            var scene = EditorSceneManager.GetActiveScene();
            try
            {
                ForestLevelUpgrade.UpgradeForestRoadVisuals();
                PersistProceduralAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Mystic Forest road texture upgrade baked into Level 1.");
            }
            finally { isBaking = false; }
        }

        static void BakePoisonMushroomsActiveScene()
        {
            if (isBaking || EditorSceneManager.GetActiveScene().path != ScenePath) return;
            isBaking = true;
            var scene = EditorSceneManager.GetActiveScene();
            try
            {
                ForestLevelUpgrade.UpgradePoisonMushroomVisuals();
                PersistProceduralAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Mystic Forest poison mushrooms upgraded in Level 1.");
            }
            finally { isBaking = false; }
        }

        [MenuItem("The Lost Kingdom/Bake Mystic Forest Visual Upgrade")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            BakeActiveScene();
        }

        static void BakeActiveScene()
        {
            if (isBaking || EditorSceneManager.GetActiveScene().path != ScenePath) return;
            isBaking = true;
            var scene = EditorSceneManager.GetActiveScene();
            try
            {
            if (!GameObject.Find("Mystic Forest ravine"))
            {
                PrepareRuntimeMaterialCopies();
                ForestLevelUpgrade.Build();
            }
            ForestLevelUpgrade.FixWalkingColliders();
            ForestLevelUpgrade.UpgradeTreeCanopies();
            PersistProceduralAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Mystic Forest visual upgrade baked into Level 1.");
            }
            finally { isBaking = false; }
        }

        public static void PrepareRuntimeMaterialCopies()
        {
            var restoration = Object.FindAnyObjectByType<ForestRestoration>();
            if (!restoration) return;
            var sourceGround = restoration.ground;
            var sourceLeaves = restoration.leaves;
            restoration.ground = new Material(sourceGround) { name = "Mystic Forest runtime ground" };
            restoration.leaves = new Material(sourceLeaves) { name = "Mystic Forest runtime leaves" };
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == sourceGround) { materials[i] = restoration.ground; changed = true; }
                    else if (materials[i] == sourceLeaves) { materials[i] = restoration.leaves; changed = true; }
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        static void PersistProceduralAssets()
        {
            EnsureFolder("Assets", "Generated");
            EnsureFolder("Assets/Generated", "MysticForest");
            EnsureFolder(GeneratedPath, "Textures");
            EnsureFolder(GeneratedPath, "Materials");
            EnsureFolder(GeneratedPath, "Meshes");

            foreach (var material in Object.FindObjectsByType<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m && !AssetDatabase.Contains(m)).Distinct().ToArray())
            {
                foreach (string property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_OcclusionMap" })
                {
                    if (!material.HasProperty(property)) continue;
                    var texture = material.GetTexture(property);
                    if (texture && !AssetDatabase.Contains(texture))
                        AssetDatabase.CreateAsset(texture, AssetDatabase.GenerateUniqueAssetPath(GeneratedPath + "/Textures/" + SafeName(texture.name) + ".asset"));
                }
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(GeneratedPath + "/Materials/" + SafeName(material.name) + ".mat"));
            }

            foreach (var mesh in Object.FindObjectsByType<MeshFilter>().Select(f => f.sharedMesh).Concat(Object.FindObjectsByType<MeshCollider>().Select(c => c.sharedMesh)).Where(m => m && !AssetDatabase.Contains(m) && !IsBuiltInPrimitiveMesh(m)).Distinct().ToArray())
                AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(GeneratedPath + "/Meshes/" + SafeName(mesh.name) + ".asset"));
            AssetDatabase.SaveAssets();
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        static bool IsBuiltInPrimitiveMesh(Mesh mesh)
            => mesh.name == "Cube" || mesh.name == "Sphere" || mesh.name == "Capsule" || mesh.name == "Cylinder" || mesh.name == "Plane" || mesh.name == "Quad";

        static string SafeName(string value)
        {
            string safe = Regex.Replace(string.IsNullOrWhiteSpace(value) ? "Generated asset" : value, "[^A-Za-z0-9 _-]", "").Trim();
            return string.IsNullOrEmpty(safe) ? "Generated asset" : safe;
        }
    }
}
