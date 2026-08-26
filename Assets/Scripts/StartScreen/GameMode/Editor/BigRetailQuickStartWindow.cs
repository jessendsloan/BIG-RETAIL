using BigRetail.Core.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.Session.Editor
{
    /// <summary>
    /// Starts an intentional development session before Play Mode, bypassing
    /// the player-facing menu without bypassing the real session model.
    /// </summary>
    public sealed class BigRetailQuickStartWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Development/Quick Start";

        private const string GameplayScenePath =
            "Assets/Scenes/Gameplay.unity";

        private const string FrankRoadsideScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        [MenuItem(MenuPath)]
        public static void Open()
        {
            BigRetailQuickStartWindow window =
                GetWindow<BigRetailQuickStartWindow>(
                    "Big Retail Quick Start");

            window.minSize = new Vector2(430f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "Big Retail Quick Start",
                EditorStyles.largeLabel);

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Choose the exact game moment you need to test. Quick Start "
                + "creates a real session and enters Play Mode without "
                + "visiting the main menu.",
                MessageType.Info);

            EditorGUILayout.Space(10f);

            bool launchBlocked =
                EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling;

            using (new EditorGUI.DisabledScope(launchBlocked))
            {
                DrawLaunchOption(
                    "Frank Opening Quick Start",
                    "Start at Frank's Roadside with the first campaign story "
                    + "beat active.",
                    "Start Frank Opening",
                    GameMode.Campaign,
                    FrankRoadsideScenePath);

                EditorGUILayout.Space(8f);

                DrawLaunchOption(
                    "Main Property Campaign Quick Start",
                    "Keep testing the existing Mr. BIG property assignment "
                    + "without changing the player-facing campaign route.",
                    "Start Main Property Campaign",
                    GameMode.Campaign,
                    GameplayScenePath);

                EditorGUILayout.Space(8f);

                DrawLaunchOption(
                    "Sandbox Quick Start",
                    "Use this for unrestricted construction, art alignment, "
                    + "and isolated systems testing.",
                    "Start Sandbox",
                    GameMode.Sandbox,
                    GameplayScenePath);
            }

            EditorGUILayout.Space(10f);

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox(
                    "Unity is compiling. Quick Start will be available when "
                    + "the compile finishes.",
                    MessageType.Warning);
            }
            else if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode is already running or starting.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Editor-only tool. It does not appear in player builds "
                    + "and does not modify the Gameplay scene.",
                    MessageType.None);
            }
        }

        private static void DrawLaunchOption(
            string title,
            string description,
            string buttonLabel,
            GameMode mode,
            string scenePath)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    description,
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(4f);

                if (GUILayout.Button(buttonLabel, GUILayout.Height(38f)))
                {
                    Launch(mode, scenePath);
                }
            }
        }

        private static void Launch(
            GameMode mode,
            string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling)
            {
                return;
            }

            SceneAsset targetScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (targetScene == null)
            {
                Debug.LogError(
                    $"Big Retail Quick Start could not find '{scenePath}'.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            DevelopmentSessionBootstrap.ClearRequest();

            try
            {
                EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
                DevelopmentSessionBootstrap.Arm(mode);
                EditorApplication.isPlaying = true;
            }
            catch
            {
                DevelopmentSessionBootstrap.ClearRequest();
                throw;
            }
        }
    }
}
