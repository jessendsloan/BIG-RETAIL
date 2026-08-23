using System;
using BigRetail.Map.Construction;
using BigRetail.Map.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.Editor.StoreLayouts
{
    /// <summary>
    /// Creates the first gameplay-compatible Frank Roadside authoring scene
    /// without hand-editing a large serialized scene file.
    /// </summary>
    public static class FrankRoadsideSceneScaffold
    {
        private const string SourceScenePath =
            "Assets/Scenes/Gameplay.unity";

        private const string DestinationScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string FrankMapId =
            "bigretail.map.frank_roadside";


        [MenuItem(
            "Big Retail/Map Workshop/Create or Validate Frank Roadside Scaffold")]
        public static void CreateOrValidate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    SourceScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"The source gameplay scene is missing at "
                    + $"'{SourceScenePath}'.");
            }

            SceneAsset destination =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    DestinationScenePath);

            if (destination == null)
            {
                if (!AssetDatabase.CopyAsset(
                        SourceScenePath,
                        DestinationScenePath))
                {
                    throw new InvalidOperationException(
                        $"Unity could not copy '{SourceScenePath}' to "
                        + $"'{DestinationScenePath}'.");
                }

                AssetDatabase.ImportAsset(
                    DestinationScenePath,
                    ImportAssetOptions.ForceSynchronousImport);
            }

            Scene scene =
                EditorSceneManager.OpenScene(
                    DestinationScenePath,
                    OpenSceneMode.Single);

            GridMapAuthoring mapAuthoring =
                FindRequiredInScene<GridMapAuthoring>(scene);
            GridMapHost mapHost =
                FindRequiredInScene<GridMapHost>(scene);
            GameObject mapVisuals =
                FindRequiredGameObject(scene, "MapVIsuals");

            ValidateMapVisualsPath(mapVisuals);

            SetString(
                mapAuthoring,
                "mapId",
                FrankMapId);
            SetEnum(
                mapHost,
                "landPolicyKind",
                LocationLandPolicyKind.FixedFootprint);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(
                    scene,
                    DestinationScenePath))
            {
                throw new InvalidOperationException(
                    $"Unity could not save '{DestinationScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            VerifySavedScene();

            Debug.Log(
                "Frank Roadside scene scaffold is ready. Its map ID is "
                + $"'{FrankMapId}', it uses the fixed-footprint land policy, "
                + "and the safe art handoff remains Map/Grid/MapVIsuals.");
        }


        public static void CreateForAutomation()
        {
            CreateOrValidate();
        }


        private static void VerifySavedScene()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    DestinationScenePath,
                    OpenSceneMode.Single);
            GridMapAuthoring mapAuthoring =
                FindRequiredInScene<GridMapAuthoring>(scene);
            GridMapHost mapHost =
                FindRequiredInScene<GridMapHost>(scene);

            SerializedObject authoringData =
                new SerializedObject(mapAuthoring);
            SerializedObject hostData =
                new SerializedObject(mapHost);

            string savedMapId =
                FindRequiredProperty(
                    authoringData,
                    "mapId").stringValue;
            int savedLandPolicy =
                FindRequiredProperty(
                    hostData,
                    "landPolicyKind").enumValueIndex;

            if (!string.Equals(
                    savedMapId,
                    FrankMapId,
                    StringComparison.Ordinal)
                || savedLandPolicy
                    != (int)LocationLandPolicyKind.FixedFootprint)
            {
                throw new InvalidOperationException(
                    "Frank Roadside did not retain its location identity "
                    + "and fixed-footprint policy after saving.");
            }
        }


        private static T FindRequiredInScene<T>(
            Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                T found =
                    roots[index].GetComponentInChildren<T>(true);

                if (found != null)
                {
                    return found;
                }
            }

            throw new InvalidOperationException(
                $"'{scene.path}' is missing required component "
                + $"'{typeof(T).Name}'.");
        }


        private static GameObject FindRequiredGameObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                Transform[] transforms =
                    roots[index].GetComponentsInChildren<Transform>(true);

                for (int childIndex = 0;
                     childIndex < transforms.Length;
                     childIndex++)
                {
                    if (transforms[childIndex].name == objectName)
                    {
                        return transforms[childIndex].gameObject;
                    }
                }
            }

            throw new InvalidOperationException(
                $"'{scene.path}' is missing required object "
                + $"'{objectName}'.");
        }


        private static void ValidateMapVisualsPath(
            GameObject mapVisuals)
        {
            Transform grid = mapVisuals.transform.parent;
            Transform map = grid != null
                ? grid.parent
                : null;

            if (grid == null
                || grid.name != "Grid"
                || map == null
                || map.name != "Map")
            {
                throw new InvalidOperationException(
                    "Frank Roadside requires the safe authoring path "
                    + "Map/Grid/MapVIsuals before the scene can be handed "
                    + "to environment art.");
            }
        }


        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            SerializedObject serializedObject =
                new SerializedObject(target);
            SerializedProperty property =
                FindRequiredProperty(
                    serializedObject,
                    propertyName);

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }


        private static void SetEnum<TEnum>(
            UnityEngine.Object target,
            string propertyName,
            TEnum value)
            where TEnum : Enum
        {
            SerializedObject serializedObject =
                new SerializedObject(target);
            SerializedProperty property =
                FindRequiredProperty(
                    serializedObject,
                    propertyName);

            property.enumValueIndex = Convert.ToInt32(value);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }


        private static SerializedProperty FindRequiredProperty(
            SerializedObject serializedObject,
            string propertyName)
        {
            return serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException(
                    $"'{serializedObject.targetObject.GetType().Name}' has "
                    + $"no serialized property named '{propertyName}'.");
        }
    }
}
