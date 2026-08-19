#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Core.Session
{
    /// <summary>
    /// Consumes the one-shot request created by Big Retail Quick Start before
    /// scene Awake methods run. Compiled only inside the Unity Editor.
    /// </summary>
    public static class DevelopmentSessionBootstrap
    {
        public const string ArmedEditorPreference =
            "BigRetail.DevelopmentQuickStart.Armed";

        public const string ModeEditorPreference =
            "BigRetail.DevelopmentQuickStart.Mode";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartRequestedSession()
        {
            if (!EditorPrefs.GetBool(ArmedEditorPreference, false))
            {
                return;
            }

            int rawMode = EditorPrefs.GetInt(
                ModeEditorPreference,
                (int)GameMode.Sandbox);

            ClearRequest();

            if (!Enum.IsDefined(typeof(GameMode), rawMode))
            {
                Debug.LogError(
                    $"Big Retail Quick Start received an unknown mode value: "
                    + $"{rawMode}.");
                return;
            }

            GameSessionHost.StartSessionInLoadedScene((GameMode)rawMode);
        }

        public static void Arm(GameMode mode)
        {
            if (!Enum.IsDefined(typeof(GameMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            EditorPrefs.SetInt(ModeEditorPreference, (int)mode);
            EditorPrefs.SetBool(ArmedEditorPreference, true);
        }

        public static void ClearRequest()
        {
            EditorPrefs.DeleteKey(ArmedEditorPreference);
            EditorPrefs.DeleteKey(ModeEditorPreference);
        }
    }
}
#endif
