#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Depthbound.Editor
{
    public static class DepthboundPlaytest
    {
        private const string RequiredEditorVersion = "6000.6.0f1";

        [MenuItem("Tools/Depthbound/Start Playtest")]
        public static void StartPlaytest()
        {
            if (Application.unityVersion != RequiredEditorVersion)
            {
                EditorUtility.DisplayDialog(
                    "Unity version required",
                    "Open this project in Unity " + RequiredEditorVersion +
                    " before testing. Current Editor: " + Application.unityVersion,
                    "OK");
                return;
            }

            // Setup may open scenes, so offer to save existing edits first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            DepthboundProjectSetup.EnsureProjectStructure(false);
            // Throws on a failed gate; never enter Play Mode after a failure.
            DepthboundBuild.ValidateReleaseGate();
            EditorSceneManager.OpenScene(
                DepthboundProjectSetup.MainMenuScene,
                OpenSceneMode.Single);

            Debug.Log(
                "Depthbound v" + DepthboundProjectSetup.ProductVersion +
                " playtest starting in Unity " + Application.unityVersion +
                ". This is not a runtime PASS. Record the tested Git SHA, " +
                "complete the nine-chamber run and review the Console.");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Depthbound/Start Playtest", true)]
        private static bool CanStartPlaytest()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling &&
                   !EditorApplication.isUpdating;
        }
    }
}
#endif
