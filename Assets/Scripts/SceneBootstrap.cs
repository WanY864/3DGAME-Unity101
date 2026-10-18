using UnityEngine;

namespace Depthbound
{
    public enum SceneKind
    {
        MainMenu = 0,
        Game = 1
    }

    public class SceneBootstrap : MonoBehaviour
    {
        public SceneKind sceneKind;

        private void Awake()
        {
            RuntimePresentation.Configure();

            if (sceneKind == SceneKind.MainMenu)
            {
                MenuBuilder.Build();

                if (GetComponent<PixelPresentation>() == null)
                {
                    gameObject.AddComponent<PixelPresentation>();
                }
            }
            else
            {
                GameBuilder.Build();
            }
        }
    }

    public static class RuntimePresentation
    {
        public static void Configure()
        {
            RuntimeSettings.ApplyDisplay();
            RuntimeSettings.ApplyAudio();
            Application.runInBackground = false;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Disable;
            QualitySettings.shadowDistance = 30f;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowResolution =
                ShadowResolution.Low;
            QualitySettings.softParticles = false;
            Application.targetFrameRate = 120;
        }
    }

    public static class GameBuilder
    {
        public static void Build()
        {
            if (GameObject.Find("DepthboundRuntime") != null) return;

            GameObject runtime = new GameObject("DepthboundRuntime");
            runtime.AddComponent<ProceduralAudio>();
            runtime.AddComponent<HUDController>();
            runtime.AddComponent<RunMorality>();
            runtime.AddComponent<DungeonDirector>();
            runtime.AddComponent<PixelPresentation>();
            runtime.AddComponent<SceneFadeController>();
        }
    }
}
