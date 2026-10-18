#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Depthbound.Editor
{
    public static class DepthboundBuild
    {
        [MenuItem("Tools/Depthbound/Build Windows x64")]
        public static void BuildWindows()
        {
            DepthboundProjectSetup.EnsureProjectStructure(false);

            if (!RunSanityCheck(true))
            {
                throw new BuildFailedException(
                    "Depthbound sanity check failed. Fix the reported project structure issues before building.");
            }

            Directory.CreateDirectory("Build");
            string[] scenes =
            {
                DepthboundProjectSetup.MainMenuScene,
                DepthboundProjectSetup.GameScene
            };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Build/Depthbound.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    "Depthbound Windows build failed with result " +
                    report.summary.result +
                    ". Check the Console for compiler/build errors.");
            }

            if (!File.Exists(report.summary.outputPath))
            {
                throw new BuildFailedException(
                    "Unity reported a successful build, but the executable was not found at " +
                    report.summary.outputPath);
            }

            Debug.Log(
                "Depthbound Windows build succeeded: " +
                report.summary.outputPath +
                "\nSize: " +
                (report.summary.totalSize / (1024f * 1024f)).ToString("0.0") +
                " MB");
        }

        [MenuItem("Tools/Depthbound/Run Project Sanity Check")]
        public static void SanityCheck()
        {
            RunSanityCheck(true);
        }

        [MenuItem("Tools/Depthbound/Validate Release Gate")]
        public static void ValidateReleaseGate()
        {
            if (!RunSanityCheck(true))
            {
                throw new BuildFailedException(
                    "Depthbound release gate failed. Fix every reported project issue before packaging.");
            }
        }

        private static bool RunSanityCheck(bool log)
        {
            DepthboundProjectSetup.EnsureProjectStructure(false);

            bool scenesOk =
                File.Exists(DepthboundProjectSetup.MainMenuScene) &&
                File.Exists(DepthboundProjectSetup.GameScene);

            bool prefabFileOk = File.Exists(DepthboundProjectSetup.EnemyPrefab);

            bool generatedAssetsOk =
                scenesOk &&
                prefabFileOk &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    DepthboundProjectSetup.MainMenuScene) != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    DepthboundProjectSetup.GameScene) != null &&
                !string.IsNullOrEmpty(
                    AssetDatabase.AssetPathToGUID(
                        DepthboundProjectSetup.MainMenuScene)) &&
                !string.IsNullOrEmpty(
                    AssetDatabase.AssetPathToGUID(
                        DepthboundProjectSetup.GameScene)) &&
                !string.IsNullOrEmpty(
                    AssetDatabase.AssetPathToGUID(
                        DepthboundProjectSetup.EnemyPrefab));

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DepthboundProjectSetup.EnemyPrefab);

            bool prefabComponentOk =
                enemyPrefab != null &&
                enemyPrefab.GetComponent<EnemyController>() != null;

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            bool buildSettingsOk =
                buildScenes.Length == 2 &&
                buildScenes[0].enabled &&
                buildScenes[1].enabled &&
                buildScenes[0].path == DepthboundProjectSetup.MainMenuScene &&
                buildScenes[1].path == DepthboundProjectSetup.GameScene;

            bool runtimeSystemsOk =
                typeof(FirstPersonController) != null &&
                typeof(PlayerCombat) != null &&
                typeof(PlayerHealth) != null &&
                typeof(HUDController) != null &&
                typeof(DungeonDirector) != null &&
                typeof(EnemyController) != null &&
                typeof(BossActMechanics) != null &&
                typeof(DungeonRelicPickup) != null &&
                typeof(RunMorality) != null &&
                typeof(MercyCompanion) != null &&
                typeof(PixelPresentation) != null &&
                typeof(PixelSpriteFactory) != null &&
                typeof(PixelBossAura) != null &&
                typeof(PixelPropFactory) != null &&
                typeof(PixelFlameFlicker) != null &&
                typeof(PixelWeaponFactory) != null &&
                typeof(PixelDungeonTextureFactory) != null &&
                typeof(ProceduralAudio) != null &&
                typeof(ProceduralWeaponMesh) != null &&
                typeof(ProceduralTextureFactory) != null &&
                typeof(CombatEffects) != null &&
                typeof(BladeWaveProjectile) != null &&
                typeof(BestiaryCatalog) != null &&
                typeof(TorchFlicker) != null &&
                typeof(RunePulse) != null &&
                typeof(BoonFocusVisual) != null &&
                typeof(DungeonDoor) != null &&
                typeof(SoulShard) != null &&
                typeof(RuneHazard) != null &&
                typeof(SceneFadeController) != null &&
                typeof(RuntimeSettings) != null &&
                typeof(UIFactory) != null;

            bool projectIdentityOk =
                PlayerSettings.productName == "Depthbound" &&
                PlayerSettings.companyName == "Wan Yan" &&
                PlayerSettings.bundleVersion == DepthboundProjectSetup.ProductVersion &&
                PlayerSettings.defaultScreenWidth == 1280 &&
                PlayerSettings.defaultScreenHeight == 720 &&
                PlayerSettings.fullScreenMode == FullScreenMode.Windowed &&
                PlayerSettings.resizableWindow &&
                !PlayerSettings.runInBackground;

            bool allOk =
                scenesOk &&
                prefabFileOk &&
                generatedAssetsOk &&
                prefabComponentOk &&
                buildSettingsOk &&
                runtimeSystemsOk &&
                projectIdentityOk;

            if (log)
            {
                string report =
                    "Depthbound v" + DepthboundProjectSetup.ProductVersion + " release-gate sanity check\n" +
                    "Scenes: " + PassFail(scenesOk) + "\n" +
                    "Enemy prefab file: " + PassFail(prefabFileOk) + "\n" +
                    "Generated assets imported/GUID-backed: " + PassFail(generatedAssetsOk) + "\n" +
                    "EnemyController on prefab: " + PassFail(prefabComponentOk) + "\n" +
                    "Build settings/order: " + PassFail(buildSettingsOk) + "\n" +
                    "Runtime systems compiled: " + PassFail(runtimeSystemsOk) + "\n" +
                    "Project identity/version: " + PassFail(projectIdentityOk) + "\n" +
                    "Overall: " + PassFail(allOk) + "\n\n" +
                    "Manual playtest is still required for movement, Boost/Evade, melee timing, " +
                    "Perfect Block / Guard Break, creature archetypes, Mercy/Sin choices, " +
                    "20/30/50 boss reinforcement outcomes, Mercy Renew / Sin Blood Rush doctrines, three-act carry/reset rules, " +
                    "explicit nine-chamber encounter budgets, smoothed enemy scaling, act relics / rune variants, differentiated Warden movement and arena-control loops, impact tiers / attack interruption, " +
                    "pixel presentation, segmented HUD readability, pixel boss warnings / enrage aura, " +
                    "pixel room props / torch flames, two-wave room pacing, chamber-specific formations, " +
                    "wave-specific enemy mixes, archetype-specific mercy reinforcements, readable betrayer windups, " +
                    "pixel spawn sigils, room composition blocks, boss phases, Weapon Camera, camera-shake setting, " +
                    "fullscreen/windowed switching, " +
                    "spatial audio, room clear, boon focus / feedback, low-health heartbeat, " +
                    "death/victory atmosphere, scene transitions and Quit.";

                if (allOk) Debug.Log(report);
                else Debug.LogError(report);
            }

            return allOk;
        }

        private static string PassFail(bool value)
        {
            return value ? "PASS" : "FAIL";
        }
    }
}
#endif
