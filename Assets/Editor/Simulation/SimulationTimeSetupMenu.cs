using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Simulation.Time.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigRetail.Editor.Simulation
{
    /// <summary>
    /// Installs the clock host and HUD presenter into the open Gameplay scene.
    /// Safe to run repeatedly; existing components are reused.
    /// </summary>
    public static class SimulationTimeSetupMenu
    {
        [MenuItem("Big Retail/Simulation/Install Time Clock")]
        private static void InstallTimeClock()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(
                    "Exit Play Mode before installing Simulation Time.");
                return;
            }

            ConstructionToolbarDocumentHost documentHost =
                Object.FindAnyObjectByType<ConstructionToolbarDocumentHost>(
                    FindObjectsInactive.Exclude);

            if (documentHost == null)
            {
                Debug.LogError(
                    "Simulation Time requires a ConstructionToolbarDocumentHost "
                    + "in the open scene.");
                return;
            }

            GameObject toolbarObject =
                documentHost.gameObject;

            SimulationTimeRuntimeHost timeHost =
                GetOrAddComponent<SimulationTimeRuntimeHost>(
                    toolbarObject);

            SimulationClockPresenter presenter =
                GetOrAddComponent<SimulationClockPresenter>(
                    toolbarObject);

            SetObjectReference(
                presenter,
                "documentHost",
                documentHost);
            SetObjectReference(
                presenter,
                "timeHost",
                timeHost);

            timeHost.enabled = true;
            presenter.enabled = true;

            EditorSceneManager.MarkSceneDirty(
                toolbarObject.scene);
            Selection.activeObject = timeHost;

            Debug.Log(
                "Installed Simulation Time. Save the scene, then enter Play "
                + "Mode to test pause, 1x, 2x, 4x, and day rollover.",
                timeHost);
        }


        private static T GetOrAddComponent<T>(
            GameObject gameObject)
            where T : Component
        {
            T component =
                gameObject.GetComponent<T>();

            return component != null
                ? component
                : Undo.AddComponent<T>(gameObject);
        }

        private static void SetObjectReference(
            Object target,
            string propertyName,
            Object value)
        {
            Undo.RecordObject(
                target,
                "Install Simulation Time");

            SerializedObject serialized =
                new SerializedObject(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError(
                    $"Could not find serialized property '{propertyName}' on '{target.name}'.",
                    target);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
