using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.Fixtures;
using BigRetail.Construction.Unity.History;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Editor.Fixtures
{
    /// <summary>
    /// Creates the first shelf definition and installs fixture placement into
    /// the open Gameplay scene without hand-editing scene YAML.
    /// Existing assets and components are preserved on repeated use.
    /// </summary>
    public static class FixtureStarterSetupMenu
    {
        private const string FixturesFolder = "Assets/Design/Fixtures";
        private const string StandardShelfPath =
            FixturesFolder + "/StandardShelf.asset";
        private const string HalfShelfPath =
            FixturesFolder + "/HalfShelf.asset";
        private const string CatalogPath =
            FixturesFolder + "/FixtureDefinitionCatalog.asset";
        private const string PlaceholderSpritePath =
            "Assets/Art/TilePlacementPylon/TilePlacementPylon.png";
        private const string FixtureArtFolder =
            "Assets/Art/Fixtures/StandardShelf";
        private const string HalfShelfArtFolder =
            "Assets/Art/Fixtures/HalfShelf";
        private const string StandardShelfIconPath =
            FixtureArtFolder
            + "/Fixture_2x1_StandardShelf01_Icon.png";
        private const string StandardShelfRisingLeftPath =
            FixtureArtFolder
            + "/Fixture_2x1_StandardShelf01_RisingLeft.png";
        private const string StandardShelfRisingRightPath =
            FixtureArtFolder
            + "/Fixture_2x1_StandardShelf01_RisingRight.png";
        private const string HalfShelfIconPath =
            HalfShelfArtFolder
            + "/Fixture_2x1_HalfShelf01_Icon.png";
        private const string HalfShelfRisingLeftPath =
            HalfShelfArtFolder
            + "/Fixture_2x1_HalfShelf01_RisingLeft.png";
        private const string HalfShelfRisingRightPath =
            HalfShelfArtFolder
            + "/Fixture_2x1_HalfShelf01_RisingRight.png";
        private const string HalfShelfBackRisingLeftPath =
            HalfShelfArtFolder
            + "/Fixture_2x1_HalfShelf01_Back_RisingLeft.png";
        private const string HalfShelfBackRisingRightPath =
            HalfShelfArtFolder
            + "/Fixture_2x1_HalfShelf01_Back_RisingRight.png";


        [MenuItem("Big Retail/Fixtures/Install Initial Shelf Placement")]
        private static void InstallInitialShelfPlacement()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(
                    "Exit Play Mode before installing Fixture Placement.");
                return;
            }

            SceneDependencies dependencies = FindDependencies();

            if (!dependencies.IsComplete)
            {
                Debug.LogError(
                    "Fixture Placement requires GridMapHost, FloorRuntimeHost, "
                    + "IsometricViewHost, GridCellTargetResolver, PlayerInput, "
                    + "ConstructionHistoryHost, ConstructionToolCoordinator, "
                    + "and ConstructionToolbarDocumentHost in the open scene.");
                return;
            }

            EnsureFolder();

            Sprite placeholder =
                AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);

            if (placeholder == null)
            {
                Debug.LogError(
                    $"Fixture Placement could not load its temporary shelf sprite at '{PlaceholderSpritePath}'.");
                return;
            }

            FixtureDefinitionAsset standardShelf =
                GetOrCreateStandardShelf(placeholder);
            FixtureDefinitionAsset halfShelf =
                GetOrCreateHalfShelf(placeholder);
            FixtureDefinitionAssetCatalog catalog =
                GetOrCreateCatalog(
                    standardShelf,
                    halfShelf);

            FixtureRuntimeHost runtimeHost =
                GetOrAddComponent<FixtureRuntimeHost>(
                    dependencies.MapHost.gameObject);

            FixtureViewSystem viewSystem =
                GetOrAddComponent<FixtureViewSystem>(
                    dependencies.MapHost.gameObject);

            FixtureDefinitionSelectionHost selectionHost =
                GetOrAddComponent<FixtureDefinitionSelectionHost>(
                    dependencies.ToolCoordinator.gameObject);

            FixturePlacementPreviewView previewView =
                GetOrAddComponent<FixturePlacementPreviewView>(
                    dependencies.ToolCoordinator.gameObject);

            FixtureConstructionToolController constructionTool =
                GetOrAddComponent<FixtureConstructionToolController>(
                    dependencies.ToolCoordinator.gameObject);

            FixtureDemolitionPreviewView demolitionPreview =
                GetOrAddComponent<FixtureDemolitionPreviewView>(
                    dependencies.ToolCoordinator.gameObject);

            FixtureDemolitionToolController demolitionTool =
                GetOrAddComponent<FixtureDemolitionToolController>(
                    dependencies.ToolCoordinator.gameObject);

            FixtureDefinitionPickerPresenter pickerPresenter =
                GetOrAddComponent<FixtureDefinitionPickerPresenter>(
                    dependencies.DocumentHost.gameObject);

            WireRuntimeHost(
                runtimeHost,
                dependencies.MapHost,
                dependencies.FloorRuntimeHost,
                catalog);

            WireViewSystem(
                viewSystem,
                runtimeHost,
                dependencies.ViewHost,
                dependencies.CellTargetResolver);

            WireSelectionHost(
                selectionHost,
                runtimeHost,
                standardShelf);

            WirePreview(
                previewView,
                runtimeHost,
                selectionHost,
                dependencies.CellTargetResolver,
                dependencies.ViewHost);

            WireConstructionTool(
                constructionTool,
                dependencies.PlayerInput,
                dependencies.CellTargetResolver,
                previewView,
                runtimeHost,
                dependencies.HistoryHost,
                selectionHost);

            WireDemolitionPreview(
                demolitionPreview,
                runtimeHost,
                dependencies.CellTargetResolver,
                dependencies.ViewHost);

            WireDemolitionTool(
                demolitionTool,
                dependencies.PlayerInput,
                dependencies.CellTargetResolver,
                demolitionPreview,
                runtimeHost,
                dependencies.HistoryHost);

            SetObjectReference(
                dependencies.ToolCoordinator,
                "fixtureConstructionTool",
                constructionTool);

            SetObjectReference(
                dependencies.ToolCoordinator,
                "fixtureDemolitionTool",
                demolitionTool);

            WirePickerPresenter(
                pickerPresenter,
                dependencies.DocumentHost,
                dependencies.ToolCoordinator,
                selectionHost);

            runtimeHost.enabled = true;
            viewSystem.enabled = true;
            selectionHost.enabled = true;
            previewView.enabled = true;
            constructionTool.enabled = true;
            demolitionPreview.enabled = true;
            demolitionTool.enabled = true;
            pickerPresenter.enabled = true;

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(
                dependencies.MapHost.gameObject.scene);
            Selection.activeObject = runtimeHost;

            Debug.Log(
                "Installed initial shelf placement and demolition. Save "
                + "Gameplay, then enter Play Mode and choose Fixtures. The "
                + "prepared Standard Shelf and Half Shelf directional art "
                + "is used when present; otherwise the safe pylon or front-"
                + "view fallback remains active.",
                runtimeHost);
        }


        private static SceneDependencies FindDependencies()
        {
            return new SceneDependencies(
                Object.FindAnyObjectByType<GridMapHost>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<FloorRuntimeHost>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<IsometricViewHost>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<GridCellTargetResolver>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<ConstructionHistoryHost>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<ConstructionToolCoordinator>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<ConstructionToolbarDocumentHost>(FindObjectsInactive.Exclude));
        }


        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Design"))
            {
                AssetDatabase.CreateFolder("Assets", "Design");
            }

            if (!AssetDatabase.IsValidFolder(FixturesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Design", "Fixtures");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Art/Fixtures"))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Fixtures");
            }

            if (!AssetDatabase.IsValidFolder(FixtureArtFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Art/Fixtures",
                    "StandardShelf");
            }

            if (!AssetDatabase.IsValidFolder(HalfShelfArtFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Art/Fixtures",
                    "HalfShelf");
            }
        }


        private static FixtureDefinitionAsset GetOrCreateStandardShelf(
            Sprite placeholder)
        {
            Sprite realIcon =
                LoadPreparedSprite(StandardShelfIconPath);
            Sprite realRisingLeft =
                LoadPreparedSprite(
                    StandardShelfRisingLeftPath);
            Sprite realRisingRight =
                LoadPreparedSprite(
                    StandardShelfRisingRightPath);

            bool hasCompleteDirectionalArt =
                realRisingLeft != null
                && realRisingRight != null;

            Sprite catalogIcon =
                realIcon != null
                    ? realIcon
                    : hasCompleteDirectionalArt
                        ? realRisingRight
                        : placeholder;

            Sprite north =
                hasCompleteDirectionalArt
                    ? realRisingRight
                    : placeholder;
            Sprite east =
                hasCompleteDirectionalArt
                    ? realRisingLeft
                    : placeholder;
            Sprite south =
                hasCompleteDirectionalArt
                    ? realRisingRight
                    : placeholder;
            Sprite west =
                hasCompleteDirectionalArt
                    ? realRisingLeft
                    : placeholder;

            FixtureDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<FixtureDefinitionAsset>(
                    StandardShelfPath);

            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<FixtureDefinitionAsset>();
                AssetDatabase.CreateAsset(definition, StandardShelfPath);
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("definitionId").stringValue =
                "STANDARD_SHELF";
            serialized.FindProperty("displayName").stringValue =
                "Standard Shelf";
            serialized.FindProperty("widthInCells").intValue = 2;
            serialized.FindProperty("depthInCells").intValue = 1;
            serialized.FindProperty("catalogIcon").objectReferenceValue =
                catalogIcon;
            serialized.FindProperty("northSprite").objectReferenceValue =
                north;
            serialized.FindProperty("eastSprite").objectReferenceValue =
                east;
            serialized.FindProperty("southSprite").objectReferenceValue =
                south;
            serialized.FindProperty("westSprite").objectReferenceValue =
                west;
            serialized.FindProperty("northSpriteAnchorCorner").intValue =
                (int)FixtureSpriteAnchorCorner.ViewerNearest;
            serialized.FindProperty("eastSpriteAnchorCorner").intValue =
                (int)FixtureSpriteAnchorCorner.ViewerNearest;
            serialized.FindProperty("southSpriteAnchorCorner").intValue =
                (int)FixtureSpriteAnchorCorner.ViewerNearest;
            serialized.FindProperty("westSpriteAnchorCorner").intValue =
                (int)FixtureSpriteAnchorCorner.ViewerNearest;
            serialized.FindProperty("repeatSpritePerOccupiedCell").boolValue =
                !hasCompleteDirectionalArt;
            FixtureAccessMode salesFloorAccess =
                FixtureAccessMode.CustomerBrowse
                | FixtureAccessMode.EmployeeStock;
            serialized.FindProperty("northAccess").intValue =
                (int)salesFloorAccess;
            serialized.FindProperty("eastAccess").intValue =
                (int)FixtureAccessMode.None;
            serialized.FindProperty("southAccess").intValue =
                (int)salesFloorAccess;
            serialized.FindProperty("westAccess").intValue =
                (int)FixtureAccessMode.None;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }


        private static FixtureDefinitionAsset GetOrCreateHalfShelf(
            Sprite placeholder)
        {
            Sprite realIcon =
                LoadPreparedSprite(HalfShelfIconPath);
            Sprite realRisingLeft =
                LoadPreparedSprite(
                    HalfShelfRisingLeftPath);
            Sprite realRisingRight =
                LoadPreparedSprite(
                    HalfShelfRisingRightPath);
            Sprite realBackRisingLeft =
                LoadPreparedSprite(
                    HalfShelfBackRisingLeftPath);
            Sprite realBackRisingRight =
                LoadPreparedSprite(
                    HalfShelfBackRisingRightPath);

            bool hasCompleteFrontArt =
                realRisingLeft != null
                && realRisingRight != null;

            Sprite catalogIcon =
                realIcon != null
                    ? realIcon
                    : hasCompleteFrontArt
                        ? realRisingRight
                        : placeholder;

            Sprite north =
                hasCompleteFrontArt
                    ? realRisingRight
                    : placeholder;
            Sprite east =
                realBackRisingLeft != null
                    ? realBackRisingLeft
                    : hasCompleteFrontArt
                        ? realRisingLeft
                        : placeholder;
            Sprite south =
                realBackRisingRight != null
                    ? realBackRisingRight
                    : north;
            Sprite west =
                hasCompleteFrontArt
                    ? realRisingLeft
                    : placeholder;

            FixtureDefinitionAsset definition =
                AssetDatabase.LoadAssetAtPath<FixtureDefinitionAsset>(
                    HalfShelfPath);

            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<FixtureDefinitionAsset>();
                AssetDatabase.CreateAsset(definition, HalfShelfPath);
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("definitionId").stringValue =
                "HALF_SHELF";
            serialized.FindProperty("displayName").stringValue =
                "Half Shelf";
            serialized.FindProperty("widthInCells").intValue = 2;
            serialized.FindProperty("depthInCells").intValue = 1;
            serialized.FindProperty("catalogIcon").objectReferenceValue =
                catalogIcon;
            serialized.FindProperty("northSprite").objectReferenceValue =
                north;
            serialized.FindProperty("eastSprite").objectReferenceValue =
                east;
            serialized.FindProperty("southSprite").objectReferenceValue =
                south;
            serialized.FindProperty("westSprite").objectReferenceValue =
                west;
            serialized.FindProperty("northSpriteAnchorCorner").intValue =
                (int)(hasCompleteFrontArt
                    ? FixtureSpriteAnchorCorner.ViewerBackLeft
                    : FixtureSpriteAnchorCorner.ViewerNearest);
            serialized.FindProperty("eastSpriteAnchorCorner").intValue =
                (int)FixtureSpriteAnchorCorner.ViewerNearest;
            serialized.FindProperty("southSpriteAnchorCorner").intValue =
                (int)FixtureSpriteAnchorCorner.ViewerNearest;
            serialized.FindProperty("westSpriteAnchorCorner").intValue =
                (int)(hasCompleteFrontArt
                    ? FixtureSpriteAnchorCorner.ViewerBackRight
                    : FixtureSpriteAnchorCorner.ViewerNearest);
            serialized.FindProperty("repeatSpritePerOccupiedCell").boolValue =
                !hasCompleteFrontArt;
            FixtureAccessMode salesFloorAccess =
                FixtureAccessMode.CustomerBrowse
                | FixtureAccessMode.EmployeeStock;
            serialized.FindProperty("northAccess").intValue =
                (int)FixtureAccessMode.None;
            serialized.FindProperty("eastAccess").intValue =
                (int)FixtureAccessMode.None;
            serialized.FindProperty("southAccess").intValue =
                (int)salesFloorAccess;
            serialized.FindProperty("westAccess").intValue =
                (int)FixtureAccessMode.None;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }


        private static Sprite LoadPreparedSprite(string assetPath)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                return null;
            }

            bool requiresReimport =
                importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled;

            if (requiresReimport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }


        private static FixtureDefinitionAssetCatalog GetOrCreateCatalog(
            FixtureDefinitionAsset standardShelf,
            FixtureDefinitionAsset halfShelf)
        {
            FixtureDefinitionAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<FixtureDefinitionAssetCatalog>(
                    CatalogPath);

            if (catalog == null)
            {
                catalog =
                    ScriptableObject.CreateInstance<FixtureDefinitionAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            serialized.FindProperty("defaultDefinition").objectReferenceValue =
                standardShelf;
            EnsureAdditionalDefinition(
                serialized.FindProperty("additionalDefinitions"),
                halfShelf);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }


        private static void EnsureAdditionalDefinition(
            SerializedProperty definitions,
            FixtureDefinitionAsset definition)
        {
            for (int index = 0;
                 index < definitions.arraySize;
                 index++)
            {
                if (definitions
                        .GetArrayElementAtIndex(index)
                        .objectReferenceValue
                    == definition)
                {
                    return;
                }
            }

            int newIndex = definitions.arraySize;
            definitions.InsertArrayElementAtIndex(newIndex);
            definitions
                .GetArrayElementAtIndex(newIndex)
                .objectReferenceValue = definition;
        }


        private static void WireRuntimeHost(
            FixtureRuntimeHost host,
            GridMapHost mapHost,
            FloorRuntimeHost floorHost,
            FixtureDefinitionAssetCatalog catalog)
        {
            SetObjectReference(host, "mapHost", mapHost);
            SetObjectReference(host, "floorRuntimeHost", floorHost);
            SetObjectReference(host, "definitionAssets", catalog);
        }


        private static void WireViewSystem(
            FixtureViewSystem viewSystem,
            FixtureRuntimeHost runtimeHost,
            IsometricViewHost viewHost,
            GridCellTargetResolver targetResolver)
        {
            SetObjectReference(viewSystem, "runtimeHost", runtimeHost);
            SetObjectReference(viewSystem, "viewHost", viewHost);
            SetObjectReference(
                viewSystem,
                "coordinateTilemap",
                targetResolver.CoordinateTilemap);
        }


        private static void WireSelectionHost(
            FixtureDefinitionSelectionHost selectionHost,
            FixtureRuntimeHost runtimeHost,
            FixtureDefinitionAsset startingDefinition)
        {
            SetObjectReference(selectionHost, "runtimeHost", runtimeHost);
            SetObjectReference(
                selectionHost,
                "startingDefinition",
                startingDefinition);
        }


        private static void WirePreview(
            FixturePlacementPreviewView preview,
            FixtureRuntimeHost runtimeHost,
            FixtureDefinitionSelectionHost selectionHost,
            GridCellTargetResolver targetResolver,
            IsometricViewHost viewHost)
        {
            SetObjectReference(preview, "runtimeHost", runtimeHost);
            SetObjectReference(preview, "definitionSelection", selectionHost);
            SetObjectReference(preview, "targetResolver", targetResolver);
            SetObjectReference(preview, "viewHost", viewHost);
        }


        private static void WireConstructionTool(
            FixtureConstructionToolController constructionTool,
            PlayerInput playerInput,
            GridCellTargetResolver targetResolver,
            FixturePlacementPreviewView preview,
            FixtureRuntimeHost runtimeHost,
            ConstructionHistoryHost historyHost,
            FixtureDefinitionSelectionHost selectionHost)
        {
            SetObjectReference(constructionTool, "playerInput", playerInput);
            SetObjectReference(constructionTool, "targetResolver", targetResolver);
            SetObjectReference(constructionTool, "previewView", preview);
            SetObjectReference(constructionTool, "runtimeHost", runtimeHost);
            SetObjectReference(constructionTool, "historyHost", historyHost);
            SetObjectReference(
                constructionTool,
                "definitionSelection",
                selectionHost);
        }


        private static void WireDemolitionPreview(
            FixtureDemolitionPreviewView preview,
            FixtureRuntimeHost runtimeHost,
            GridCellTargetResolver targetResolver,
            IsometricViewHost viewHost)
        {
            SetObjectReference(preview, "runtimeHost", runtimeHost);
            SetObjectReference(preview, "targetResolver", targetResolver);
            SetObjectReference(preview, "viewHost", viewHost);
        }


        private static void WireDemolitionTool(
            FixtureDemolitionToolController demolitionTool,
            PlayerInput playerInput,
            GridCellTargetResolver targetResolver,
            FixtureDemolitionPreviewView preview,
            FixtureRuntimeHost runtimeHost,
            ConstructionHistoryHost historyHost)
        {
            SetObjectReference(demolitionTool, "playerInput", playerInput);
            SetObjectReference(
                demolitionTool,
                "targetResolver",
                targetResolver);
            SetObjectReference(demolitionTool, "previewView", preview);
            SetObjectReference(demolitionTool, "runtimeHost", runtimeHost);
            SetObjectReference(demolitionTool, "historyHost", historyHost);
        }


        private static void WirePickerPresenter(
            FixtureDefinitionPickerPresenter presenter,
            ConstructionToolbarDocumentHost documentHost,
            ConstructionToolCoordinator toolCoordinator,
            FixtureDefinitionSelectionHost selectionHost)
        {
            SetObjectReference(presenter, "documentHost", documentHost);
            SetObjectReference(presenter, "toolCoordinator", toolCoordinator);
            SetObjectReference(
                presenter,
                "definitionSelectionHost",
                selectionHost);
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
            Undo.RecordObject(target, "Install Fixture Placement");
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);

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


        private readonly struct SceneDependencies
        {
            public SceneDependencies(
                GridMapHost mapHost,
                FloorRuntimeHost floorRuntimeHost,
                IsometricViewHost viewHost,
                GridCellTargetResolver cellTargetResolver,
                PlayerInput playerInput,
                ConstructionHistoryHost historyHost,
                ConstructionToolCoordinator toolCoordinator,
                ConstructionToolbarDocumentHost documentHost)
            {
                MapHost = mapHost;
                FloorRuntimeHost = floorRuntimeHost;
                ViewHost = viewHost;
                CellTargetResolver = cellTargetResolver;
                PlayerInput = playerInput;
                HistoryHost = historyHost;
                ToolCoordinator = toolCoordinator;
                DocumentHost = documentHost;
            }

            public GridMapHost MapHost { get; }
            public FloorRuntimeHost FloorRuntimeHost { get; }
            public IsometricViewHost ViewHost { get; }
            public GridCellTargetResolver CellTargetResolver { get; }
            public PlayerInput PlayerInput { get; }
            public ConstructionHistoryHost HistoryHost { get; }
            public ConstructionToolCoordinator ToolCoordinator { get; }
            public ConstructionToolbarDocumentHost DocumentHost { get; }

            public bool IsComplete =>
                MapHost != null
                && FloorRuntimeHost != null
                && ViewHost != null
                && CellTargetResolver != null
                && PlayerInput != null
                && HistoryHost != null
                && ToolCoordinator != null
                && DocumentHost != null;
        }
    }
}
