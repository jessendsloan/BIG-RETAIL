using BigRetail.Characters.Rigging;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Customers;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigRetail.Editor.Customers
{
    /// <summary>
    /// Installs the first department-free customer loop into the open
    /// Gameplay scene without directly editing scene YAML.
    /// </summary>
    public static class OpeningDayCustomerSetupMenu
    {
        private const string PersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        private const string CustomerPopulationPath =
            "Assets/Art/Characters/Appearance/Population Definitions/Customer.asset";


        [MenuItem(
            "Big Retail/Customers/Install Opening Day Customer Loop")]
        public static void Install()
        {
            SceneDependencies dependencies = FindDependencies();

            if (!dependencies.IsComplete)
            {
                Debug.LogError(
                    "Opening-day customers require the map, floors, "
                    + "sidewalks, fixtures, merchandising runtime, "
                    + "isometric view, and grid target resolver in the "
                    + "open Gameplay scene.");
                return;
            }

            GameObject personPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PersonPrefabPath);

            NpcPopulationDefinition customerPopulation =
                AssetDatabase.LoadAssetAtPath<NpcPopulationDefinition>(
                    CustomerPopulationPath);

            if (personPrefab == null || customerPopulation == null)
            {
                Debug.LogError(
                    "The canonical Person prefab or Customer population "
                    + "definition is missing. Finish the NPC starter setup "
                    + "before installing opening-day customers.");
                return;
            }

            OpeningDayCustomerRuntimeHost customerHost =
                GetOrAddComponent<OpeningDayCustomerRuntimeHost>(
                    dependencies.MapHost.gameObject);

            SetObjectReference(
                customerHost,
                "mapHost",
                dependencies.MapHost);
            SetObjectReference(
                customerHost,
                "floorRuntimeHost",
                dependencies.FloorRuntimeHost);
            SetObjectReference(
                customerHost,
                "sidewalkRuntimeHost",
                dependencies.SidewalkRuntimeHost);
            SetObjectReference(
                customerHost,
                "fixtureRuntimeHost",
                dependencies.FixtureRuntimeHost);
            SetObjectReference(
                customerHost,
                "planogramRuntimeHost",
                dependencies.PlanogramRuntimeHost);
            SetObjectReference(
                customerHost,
                "viewHost",
                dependencies.ViewHost);
            SetObjectReference(
                customerHost,
                "coordinateTilemap",
                dependencies.CellTargetResolver.CoordinateTilemap);
            SetObjectReference(
                customerHost,
                "wallViewSystem",
                dependencies.WallViewSystem);
            SetObjectReference(
                customerHost,
                "customerPrefab",
                personPrefab);
            SetObjectReference(
                customerHost,
                "customerPopulation",
                customerPopulation);

            customerHost.enabled = true;
            EditorSceneManager.MarkSceneDirty(
                dependencies.MapHost.gameObject.scene);
            Selection.activeObject = customerHost;

            Debug.Log(
                "Installed the opening-day customer loop. Save Gameplay, "
                + "then enter Play Mode with a sidewalk entrance, a stocked "
                + "display, and a checkout. One customer will buy one item "
                + "at a time and add the real sale revenue to Store Cash.",
                customerHost);
        }


        private static SceneDependencies FindDependencies()
        {
            return new SceneDependencies(
                Object.FindAnyObjectByType<GridMapHost>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<FloorRuntimeHost>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<SidewalkRuntimeHost>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<FixtureRuntimeHost>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<FixturePlanogramRuntimeHost>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<IsometricViewHost>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<GridCellTargetResolver>(
                    FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<WallViewSystem>(
                    FindObjectsInactive.Exclude));
        }


        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();

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
                "Install Opening Day Customer Loop");

            SerializedObject serialized =
                new SerializedObject(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError(
                    $"Could not find serialized property "
                    + $"'{propertyName}' on '{target.name}'.",
                    target);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }


        private readonly struct SceneDependencies
        {
            public SceneDependencies(
                GridMapHost mapHost,
                FloorRuntimeHost floorRuntimeHost,
                SidewalkRuntimeHost sidewalkRuntimeHost,
                FixtureRuntimeHost fixtureRuntimeHost,
                FixturePlanogramRuntimeHost planogramRuntimeHost,
                IsometricViewHost viewHost,
                GridCellTargetResolver cellTargetResolver,
                WallViewSystem wallViewSystem)
            {
                MapHost = mapHost;
                FloorRuntimeHost = floorRuntimeHost;
                SidewalkRuntimeHost = sidewalkRuntimeHost;
                FixtureRuntimeHost = fixtureRuntimeHost;
                PlanogramRuntimeHost = planogramRuntimeHost;
                ViewHost = viewHost;
                CellTargetResolver = cellTargetResolver;
                WallViewSystem = wallViewSystem;
            }


            public GridMapHost MapHost { get; }

            public FloorRuntimeHost FloorRuntimeHost { get; }

            public SidewalkRuntimeHost SidewalkRuntimeHost { get; }

            public FixtureRuntimeHost FixtureRuntimeHost { get; }

            public FixturePlanogramRuntimeHost PlanogramRuntimeHost
            {
                get;
            }

            public IsometricViewHost ViewHost { get; }

            public GridCellTargetResolver CellTargetResolver { get; }

            public WallViewSystem WallViewSystem { get; }

            public bool IsComplete =>
                MapHost != null
                && FloorRuntimeHost != null
                && SidewalkRuntimeHost != null
                && FixtureRuntimeHost != null
                && PlanogramRuntimeHost != null
                && ViewHost != null
                && CellTargetResolver != null
                && WallViewSystem != null
                && CellTargetResolver.CoordinateTilemap != null;
        }
    }
}
