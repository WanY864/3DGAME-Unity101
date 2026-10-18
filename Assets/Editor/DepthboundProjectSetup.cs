#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Depthbound.Editor
{
    public static class DepthboundProjectSetup
    {
        public const string ProductVersion = "0.7.4";
        public const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        public const string GameScene = "Assets/Scenes/Game.unity";
        public const string EnemyPrefab = "Assets/Resources/Enemy.prefab";

        [InitializeOnLoadMethod]
        private static void AutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                EnsureProjectStructure(false);
            };
        }

        [MenuItem("Tools/Depthbound/Setup or Repair Project")]
        public static void SetupFromMenu()
        {
            EnsureProjectStructure(true);
        }

        public static void EnsureProjectStructure(bool openMenuScene)
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources");

            bool scenesWereMissing = !File.Exists(MainMenuScene) || !File.Exists(GameScene);

            CreateEnemyPrefabIfMissing();
            CreateSceneIfMissing(MainMenuScene, SceneKind.MainMenu);
            CreateSceneIfMissing(GameScene, SceneKind.Game);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScene, true),
                new EditorBuildSettingsScene(GameScene, true)
            };

            PlayerSettings.companyName = "Wan Yan";
            PlayerSettings.productName = "Depthbound";
            PlayerSettings.bundleVersion = ProductVersion;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            EnsureLegacyInputManager();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if ((openMenuScene || scenesWereMissing) && !EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            }
        }

        private static void EnsureLegacyInputManager()
        {
            Object[] playerSettingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettingsAssets == null || playerSettingsAssets.Length == 0) return;

            SerializedObject settings = new SerializedObject(playerSettingsAssets[0]);
            SerializedProperty inputHandler = settings.FindProperty("activeInputHandler");
            if (inputHandler != null && inputHandler.intValue != 0)
            {
                inputHandler.intValue = 0;
                settings.ApplyModifiedPropertiesWithoutUndo();
                Debug.LogWarning("Depthbound enabled the legacy Input Manager because the prototype uses UnityEngine.Input. If Unity asks to restart the Editor, accept the restart.");
            }
        }

        private static void CreateEnemyPrefabIfMissing()
        {
            if (File.Exists(EnemyPrefab)) return;

            GameObject enemy = new GameObject("Enemy");
            enemy.AddComponent<EnemyController>();
            PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefab);
            Object.DestroyImmediate(enemy);
        }

        private static void CreateSceneIfMissing(string path, SceneKind kind)
        {
            if (File.Exists(path)) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject(kind == SceneKind.MainMenu ? "MainMenuBootstrap" : "GameBootstrap");
            SceneBootstrap sceneBootstrap = bootstrap.AddComponent<SceneBootstrap>();
            sceneBootstrap.sceneKind = kind;
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
#endif
