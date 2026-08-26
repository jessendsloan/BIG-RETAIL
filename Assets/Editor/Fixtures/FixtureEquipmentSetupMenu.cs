using System;
using BigRetail.Construction.Unity.Fixtures;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Purchasing.Unity;
using BigRetail.Purchasing.Unity.UI;
using BigRetail.Receiving.Unity;
using BigRetail.Simulation.Time.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace BigRetail.Editor.Fixtures
{
    /// <summary>
    /// Installs the fixture equipment loop into Gameplay without requiring
    /// fragile manual scene-YAML edits.
    /// </summary>
    public static class FixtureEquipmentSetupMenu
    {
        private const string IntegrateMenuPath =
            "Big Retail/Fixtures/Integrate Fixture Equipment Into "
            + "Gameplay";

        private const string GameplayScenePath =
            "Assets/Scenes/Gameplay.unity";
        private const string EquipmentFolder =
            "Assets/Design/Equipment";
        private const string EquipmentCatalogPath =
            EquipmentFolder + "/FixtureEquipmentCatalog.asset";
        private const string FixtureCatalogPath =
            "Assets/Design/Fixtures/FixtureDefinitionCatalog.asset";
        private const string BigWholesaleSupplierPath =
            "Assets/Design/Purchasing/Suppliers/BIGWholesale.asset";
        private const string EquipmentCatalogUxmlPath =
            "Assets/UI/Purchasing/PC/EquipmentCatalogWorkspace.uxml";
        private const string PanelSettingsPath =
            "Assets/UI/Construction/PC/ConstructionToolbarPanelSettings.asset";


        [MenuItem(IntegrateMenuPath)]
        public static void IntegrateFixtureEquipmentIntoGameplay()
        {
            IntegrateFixtureEquipmentIntoScene(
                GameplayScenePath);
        }


        [MenuItem(IntegrateMenuPath, true)]
        public static bool CanIntegrateFixtureEquipmentIntoGameplay()
        {
            return CanEditSceneAssets();
        }


        public static void IntegrateFixtureEquipmentIntoScene(
            string scenePath)
        {
            RequireEditMode(
                "Fixture Equipment scene integration");

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException(
                    "A gameplay-compatible scene path is required.",
                    nameof(scenePath));
            }

            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);

            FixtureDefinitionAssetCatalog fixtureCatalog =
                AssetDatabase.LoadAssetAtPath<FixtureDefinitionAssetCatalog>(
                    FixtureCatalogPath)
                ?? throw new InvalidOperationException(
                    $"Fixture catalog is missing at '{FixtureCatalogPath}'.");
            FixtureEquipmentCatalogAsset equipmentCatalog =
                CreateOrUpdateEquipmentCatalog(fixtureCatalog);
            SupplierDefinitionAsset bigWholesaleSupplier =
                AssetDatabase.LoadAssetAtPath<SupplierDefinitionAsset>(
                    BigWholesaleSupplierPath)
                ?? throw new InvalidOperationException(
                    $"BIG Wholesale is missing at '{BigWholesaleSupplierPath}'.");
            VisualTreeAsset equipmentCatalogVisualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    EquipmentCatalogUxmlPath)
                ?? throw new InvalidOperationException(
                    $"Equipment Catalog UI is missing at '{EquipmentCatalogUxmlPath}'.");
            PanelSettings panelSettings =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    PanelSettingsPath)
                ?? throw new InvalidOperationException(
                    $"Panel settings are missing at '{PanelSettingsPath}'.");

            GridMapHost mapHost = FindRequired<GridMapHost>();
            FixtureRuntimeHost fixtureRuntimeHost =
                FindRequired<FixtureRuntimeHost>();
            FixturePlanogramRuntimeHost planogramRuntimeHost =
                FindRequired<FixturePlanogramRuntimeHost>();
            SimulationTimeRuntimeHost timeRuntimeHost =
                FindRequired<SimulationTimeRuntimeHost>();
            ReceivingAreaRuntimeHost receivingAreaRuntimeHost =
                FindRequired<ReceivingAreaRuntimeHost>();
            IsometricViewHost viewHost =
                FindRequired<IsometricViewHost>();
            FixtureConstructionToolController constructionTool =
                FindRequired<FixtureConstructionToolController>();
            FixtureDemolitionToolController demolitionTool =
                FindRequired<FixtureDemolitionToolController>();
            FixturePlacementPreviewView placementPreview =
                FindRequired<FixturePlacementPreviewView>();
            FixtureDefinitionPickerPresenter pickerPresenter =
                FindRequired<FixtureDefinitionPickerPresenter>();
            ConstructionToolbarDocumentHost toolbarDocumentHost =
                FindRequired<ConstructionToolbarDocumentHost>();
            ConstructionToolbarPresenter toolbarPresenter =
                FindRequired<ConstructionToolbarPresenter>();
            ConstructionToolCoordinator toolCoordinator =
                FindRequired<ConstructionToolCoordinator>();
            Tilemap coordinateTilemap = FindCoordinateTilemap(scene);

            FixtureEquipmentRuntimeHost equipmentRuntimeHost =
                GetOrAddComponent<FixtureEquipmentRuntimeHost>(
                    mapHost.gameObject);
            SetObjectReference(
                equipmentRuntimeHost,
                "equipmentCatalogAsset",
                equipmentCatalog);
            SetObjectReference(
                equipmentRuntimeHost,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
            SetObjectReference(
                equipmentRuntimeHost,
                "planogramRuntimeHost",
                planogramRuntimeHost);
            SetObjectReference(
                equipmentRuntimeHost,
                "timeRuntimeHost",
                timeRuntimeHost);
            SetObjectReference(
                equipmentRuntimeHost,
                "receivingAreaRuntimeHost",
                receivingAreaRuntimeHost);

            FixtureEquipmentPlanViewSystem planViewSystem =
                GetOrAddComponent<FixtureEquipmentPlanViewSystem>(
                    mapHost.gameObject);
            SetObjectReference(
                planViewSystem,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);
            SetObjectReference(
                planViewSystem,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
            SetObjectReference(planViewSystem, "viewHost", viewHost);
            SetObjectReference(
                planViewSystem,
                "coordinateTilemap",
                coordinateTilemap);

            FixtureEquipmentDeliveryViewSystem deliveryViewSystem =
                GetOrAddComponent<FixtureEquipmentDeliveryViewSystem>(
                    mapHost.gameObject);
            SetObjectReference(
                deliveryViewSystem,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);
            SetObjectReference(
                deliveryViewSystem,
                "receivingAreaRuntimeHost",
                receivingAreaRuntimeHost);
            SetObjectReference(deliveryViewSystem, "viewHost", viewHost);
            SetObjectReference(
                deliveryViewSystem,
                "coordinateTilemap",
                coordinateTilemap);
            SetObjectReference(
                deliveryViewSystem,
                "equipmentSupplierAsset",
                bigWholesaleSupplier);
            SetColor(
                deliveryViewSystem,
                "equipmentColor",
                new Color(0.82f, 0.2f, 0.15f, 1f));

            SetObjectReference(
                constructionTool,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);
            SetObjectReference(
                demolitionTool,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);
            SetObjectReference(
                placementPreview,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);
            SetObjectReference(
                pickerPresenter,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);
            SetObjectReference(
                toolbarPresenter,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);

            GameObject workspaceObject =
                FindSceneGameObject(scene, "EquipmentCatalogWorkspaceUI");

            if (workspaceObject == null)
            {
                workspaceObject =
                    new GameObject("EquipmentCatalogWorkspaceUI");
                workspaceObject.transform.SetParent(
                    toolbarDocumentHost.transform.parent,
                    false);
            }

            RemoveComponentIfPresent<PurchasingWorkspacePresenter>(
                workspaceObject);
            RemoveComponentIfPresent<PurchasingWorkspaceDocumentHost>(
                workspaceObject);

            workspaceObject.SetActive(false);
            workspaceObject.transform.SetAsLastSibling();

            PanelRenderer panelRenderer =
                GetOrAddComponent<PanelRenderer>(workspaceObject);
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.visualTreeAsset = equipmentCatalogVisualTree;
            panelRenderer.sortingOrder = 101;
            EditorUtility.SetDirty(panelRenderer);

            EquipmentCatalogWorkspaceDocumentHost equipmentDocumentHost =
                GetOrAddComponent<EquipmentCatalogWorkspaceDocumentHost>(
                    workspaceObject);
            SetObjectReference(
                equipmentDocumentHost,
                "panelRenderer",
                panelRenderer);

            EquipmentCatalogWorkspacePresenter equipmentPresenter =
                GetOrAddComponent<EquipmentCatalogWorkspacePresenter>(
                    workspaceObject);
            SetObjectReference(
                equipmentPresenter,
                "documentHost",
                equipmentDocumentHost);
            SetObjectReference(
                equipmentPresenter,
                "equipmentRuntimeHost",
                equipmentRuntimeHost);

            EquipmentCatalogGameplayOverlayController overlayController =
                GetOrAddComponent<EquipmentCatalogGameplayOverlayController>(
                    toolbarDocumentHost.gameObject);
            SetObjectReference(
                overlayController,
                "toolCoordinator",
                toolCoordinator);
            SetObjectReference(
                overlayController,
                "fixturePickerPresenter",
                pickerPresenter);
            SetObjectReference(
                overlayController,
                "equipmentWorkspace",
                workspaceObject);
            SetObjectReference(
                overlayController,
                "equipmentPresenter",
                equipmentPresenter);

            workspaceObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Integrated fixture planning, equipment ordering, shared "
                + "Receiving pallets, the Equipment Catalog, owned equipment, "
                + "installation, and storage into "
                + $"'{scenePath}'.",
                equipmentRuntimeHost);
        }

        public static void IntegrateForAutomation()
        {
            IntegrateFixtureEquipmentIntoGameplay();
        }


        private static FixtureEquipmentCatalogAsset
            CreateOrUpdateEquipmentCatalog(
                FixtureDefinitionAssetCatalog fixtureCatalog)
        {
            EnsureFolder("Assets/Design", "Equipment");
            FixtureEquipmentCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<FixtureEquipmentCatalogAsset>(
                    EquipmentCatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    FixtureEquipmentCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, EquipmentCatalogPath);
            }

            FixtureDefinitionAsset[] fixtures =
                new FixtureDefinitionAsset[fixtureCatalog.Count];
            int fixtureIndex = 0;

            foreach (FixtureDefinitionAsset fixture
                     in fixtureCatalog.EnumerateAssets())
            {
                fixtures[fixtureIndex++] = fixture;
            }

            Array.Sort(
                fixtures,
                (left, right) => string.CompareOrdinal(
                    left.Id.Value,
                    right.Id.Value));

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty entries =
                serialized.FindProperty("entries")
                ?? throw new InvalidOperationException(
                    "Equipment catalog has no serialized entries property.");
            entries.arraySize = fixtures.Length;

            for (int index = 0; index < fixtures.Length; index++)
            {
                FixtureDefinitionAsset fixture = fixtures[index];
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("fixtureDefinition")
                    .objectReferenceValue = fixture;
                entry.FindPropertyRelative("unitPriceCents")
                    .longValue = ResolvePriceCents(fixture.Id.Value);
                entry.FindPropertyRelative(
                        "deliveryLeadTimeGameMinutes")
                    .intValue = 120;
                entry.FindPropertyRelative("startingOwnedQuantity")
                    .intValue = 0;
                entry.FindPropertyRelative("categoryName")
                    .stringValue = ResolveCategoryName(fixture.Id.Value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static long ResolvePriceCents(string fixtureDefinitionId)
        {
            return fixtureDefinitionId switch
            {
                "STANDARD_SHELF" => 24000,
                "HALF_SHELF" => 16000,
                "BASIC_CHECKOUT_COUNTER" => 85000,
                "BACKSTOCK_SHELF" => 32000,
                _ => 25000
            };
        }

        private static string ResolveCategoryName(
            string fixtureDefinitionId)
        {
            return fixtureDefinitionId switch
            {
                "BASIC_CHECKOUT_COUNTER" => "Front End",
                "BACKSTOCK_SHELF" => "Operations",
                "HALF_SHELF" => "Sales Floor",
                "STANDARD_SHELF" => "Sales Floor",
                _ => "General"
            };
        }

        private static Tilemap FindCoordinateTilemap(Scene scene)
        {
            GameObject mapVisuals = FindSceneGameObject(scene, "MapVIsuals")
                ?? throw new InvalidOperationException(
                    "Gameplay is missing the MapVIsuals coordinate Tilemap.");
            return mapVisuals.GetComponent<Tilemap>()
                ?? throw new InvalidOperationException(
                    "MapVIsuals has no Tilemap component.");
        }

        private static GameObject FindSceneGameObject(
            Scene scene,
            string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child
                         in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static void EnsureFolder(
            string parent,
            string childName)
        {
            string path = parent + "/" + childName;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, childName);
            }
        }

        private static T FindRequired<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindAnyObjectByType<T>(
                    FindObjectsInactive.Include)
                ?? throw new InvalidOperationException(
                    $"Gameplay is missing '{typeof(T).Name}'.");
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            return gameObject.GetComponent<T>()
                ?? gameObject.AddComponent<T>();
        }


        private static void RemoveComponentIfPresent<T>(
            GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }


        private static bool CanEditSceneAssets()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && !EditorApplication.isCompiling;
        }


        private static void RequireEditMode(string operation)
        {
            if (CanEditSceneAssets())
            {
                return;
            }

            throw new InvalidOperationException(
                $"{operation} is only available in Edit Mode after Unity "
                + "finishes compiling.");
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException(
                    $"'{target.GetType().Name}' has no property '{propertyName}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetColor(
            UnityEngine.Object target,
            string propertyName,
            Color value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException(
                    $"'{target.GetType().Name}' has no property '{propertyName}'.");
            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
