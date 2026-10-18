using UnityEngine;

namespace Depthbound
{
    public static class RuntimeSettings
    {
        private const string SensitivityKey = "Depthbound.MouseScale";
        private const string VolumeKey = "Depthbound.MasterVolume";
        private const string FullscreenKey = "Depthbound.Fullscreen";
        private const string CameraShakeKey = "Depthbound.CameraShakeScale";

        private static bool loaded;
        private static float mouseScale = 1f;
        private static float masterVolume = 0.85f;
        private static bool fullscreen;
        private static bool displayAppliedThisSession;
        private static float cameraShakeScale = 1f;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCache()
        {
            loaded = false;
            displayAppliedThisSession = false;
            mouseScale = 1f;
            masterVolume = 0.85f;
            fullscreen = false;
            cameraShakeScale = 1f;
        }

        public static float MouseScale
        {
            get
            {
                EnsureLoaded();
                return mouseScale;
            }
        }

        public static float MasterVolume
        {
            get
            {
                EnsureLoaded();
                return masterVolume;
            }
        }

        public static bool Fullscreen
        {
            get
            {
                EnsureLoaded();
                return fullscreen;
            }
        }

        public static float CameraShakeScale
        {
            get
            {
                EnsureLoaded();
                return cameraShakeScale;
            }
        }

        public static void AdjustMouseScale(float delta)
        {
            EnsureLoaded();
            mouseScale = Mathf.Clamp(mouseScale + delta, 0.50f, 1.80f);
            PlayerPrefs.SetFloat(SensitivityKey, mouseScale);
            PlayerPrefs.Save();
        }

        public static void AdjustMasterVolume(float delta)
        {
            EnsureLoaded();
            masterVolume = Mathf.Clamp01(masterVolume + delta);
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public static void ApplyAudio()
        {
            EnsureLoaded();
            AudioListener.volume = masterVolume;
        }

        public static void AdjustCameraShake(float delta)
        {
            EnsureLoaded();
            cameraShakeScale = Mathf.Clamp(
                cameraShakeScale + delta,
                0f,
                1.25f);

            PlayerPrefs.SetFloat(
                CameraShakeKey,
                cameraShakeScale);
            PlayerPrefs.Save();
        }

        public static void ToggleFullscreen()
        {
            EnsureLoaded();
            fullscreen = !fullscreen;

            PlayerPrefs.SetInt(
                FullscreenKey,
                fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            displayAppliedThisSession = false;
            ApplyDisplay();
        }

        public static void ApplyDisplay()
        {
            EnsureLoaded();

            bool modeAlreadyMatches =
                fullscreen
                    ? Screen.fullScreenMode == FullScreenMode.FullScreenWindow
                    : Screen.fullScreenMode == FullScreenMode.Windowed;

            if (displayAppliedThisSession)
            {
                return;
            }

            if (fullscreen)
            {
                Resolution desktop = Screen.currentResolution;

                bool alreadyCorrect =
                    Screen.fullScreenMode == FullScreenMode.FullScreenWindow &&
                    Screen.width == desktop.width &&
                    Screen.height == desktop.height;

                if (!alreadyCorrect)
                {
                    Screen.SetResolution(
                        desktop.width,
                        desktop.height,
                        FullScreenMode.FullScreenWindow);
                }
            }
            else
            {
                bool alreadyCorrect =
                    Screen.fullScreenMode == FullScreenMode.Windowed &&
                    Screen.width == 1280 &&
                    Screen.height == 720;

                if (!alreadyCorrect)
                {
                    Screen.SetResolution(
                        1280,
                        720,
                        FullScreenMode.Windowed);
                }
            }

            displayAppliedThisSession = true;
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;

            mouseScale = Mathf.Clamp(
                PlayerPrefs.GetFloat(SensitivityKey, 1f),
                0.50f,
                1.80f);

            masterVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(VolumeKey, 0.85f));

            fullscreen =
                PlayerPrefs.GetInt(FullscreenKey, 0) == 1;

            cameraShakeScale = Mathf.Clamp(
                PlayerPrefs.GetFloat(CameraShakeKey, 1f),
                0f,
                1.25f);

            loaded = true;
        }
    }
}
