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
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
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
        private const string StandardShelfMasksFolder =
            FixtureArtFolder + "/MerchandisingMasks";
        private const string StandardShelfRisingLeftMaskTopPath =
            StandardShelfMasksFolder
            + "/Fixture_2x1_StandardShelf01_RisingLeft_ShelfMask01_Top.png";
        private const string StandardShelfRisingLeftMaskMiddlePath =
            StandardShelfMasksFolder
            + "/Fixture_2x1_StandardShelf01_RisingLeft_ShelfMask02_Middle.png";
        private const string StandardShelfRisingLeftMaskBottomPath =
            StandardShelfMasksFolder
            + "/Fixture_2x1_StandardShelf01_RisingLeft_ShelfMask03_Bottom.png";
        private const string StandardShelfRisingRightMaskTopPath =
            StandardShelfMasksFolder
            + "/Fixture_2x1_StandardShelf01_RisingRight_ShelfMask01_Top.png";
        private const string StandardShelfRisingRightMaskMiddlePath =
            StandardShelfMasksFolder
            + "/Fixture_2x1_StandardShelf01_RisingRight_ShelfMask02_Middle.png";
        private const string StandardShelfRisingRightMaskBottomPath =
            StandardShelfMasksFolder
            + "/Fixture_2x1_StandardShelf01_RisingRight_ShelfMask03_Bottom.png";
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
        private const string HalfShelfMasksFolder =
            HalfShelfArtFolder + "/MerchandisingMasks";
        private const string HalfShelfRisingLeftMaskTopPath =
            HalfShelfMasksFolder
            + "/Fixture_2x1_HalfShelf01_RisingLeft_ShelfMask01_Top.png";
        private const string HalfShelfRisingLeftMaskMiddlePath =
            HalfShelfMasksFolder
            + "/Fixture_2x1_HalfShelf01_RisingLeft_ShelfMask02_Middle.png";
        private const string HalfShelfRisingLeftMaskBottomPath =
            HalfShelfMasksFolder
            + "/Fixture_2x1_HalfShelf01_RisingLeft_ShelfMask03_Bottom.png";
        private const string HalfShelfRisingRightMaskTopPath =
            HalfShelfMasksFolder
            + "/Fixture_2x1_HalfShelf01_RisingRight_ShelfMask01_Top.png";
        private const string HalfShelfRisingRightMaskMiddlePath =
            HalfShelfMasksFolder
            + "/Fixture_2x1_HalfShelf01_RisingRight_ShelfMask02_Middle.png";
        private const string HalfShelfRisingRightMaskBottomPath =
            HalfShelfMasksFolder
            + "/Fixture_2x1_HalfShelf01_RisingRight_ShelfMask03_Bottom.png";
        private const string MerchandiseFolder =
            "Assets/Design/Merchandise";
        private const string GrayboxCerealPath =
            MerchandiseFolder + "/GrayboxCereal.asset";
        private const string GrayboxSoupPath =
            MerchandiseFolder + "/GrayboxSoup.asset";
        private const string GrayboxColaPath =
            MerchandiseFolder + "/GrayboxCola.asset";
        private const string ProductCatalogPath =
            MerchandiseFolder + "/ProductCatalog.asset";


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

            ProductDefinitionAsset cereal =
                GetOrCreateProduct(
                    GrayboxCerealPath,
                    "CEREAL",
                    "Cereal");

            ProductDefinitionAsset soup =
                GetOrCreateProduct(
                    GrayboxSoupPath,
                    "SOUP",
                    "Soup");

            ProductDefinitionAsset cola =
                GetOrCreateProduct(
                    GrayboxColaPath,
                    "COLA",
                    "Cola");

            ProductCatalogAsset productCatalog =
                GetOrCreateProductCatalog(
                    cereal,
                    soup,
                    cola);

            FixtureRuntimeHost runtimeHost =
                GetOrAddComponent<FixtureRuntimeHost>(
                    dependencies.MapHost.gameObject);

            FixtureViewSystem viewSystem =
                GetOrAddComponent<FixtureViewSystem>(
                    dependencies.MapHost.gameObject);

            FixturePlanogramRuntimeHost planogramRuntimeHost =
                GetOrAddComponent<FixturePlanogramRuntimeHost>(
                    dependencies.MapHost.gameObject);

            FixtureMerchandisingOverlayViewSystem
                merchandisingOverlay =
                    GetOrAddComponent<
                        FixtureMerchandisingOverlayViewSystem>(
                            dependencies.MapHost.gameObject);

            FixtureMerchandisingHoverOutlineView
                merchandisingHoverOutline =
                    GetOrAddComponent<
                        FixtureMerchandisingHoverOutlineView>(
                            dependencies.MapHost.gameObject);

            FixtureDefinitionSelectionHost selectionHost =
                GetOrAddComponent<FixtureDefinitionSelectionHost>(
                    dependencies.ToolCoordinator.gameObject);

            FixtureMerchandisingSelectionHost
                merchandisingSelectionHost =
                    GetOrAddComponent<
                        FixtureMerchandisingSelectionHost>(
                            dependencies.ToolCoordinator.gameObject);

            FixtureMerchandisingInputController merchandisingInput =
                GetOrAddComponent<FixtureMerchandisingInputController>(
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

            FixtureMerchandisingInspectorPresenter
                merchandisingPresenter =
                    GetOrAddComponent<
                        FixtureMerchandisingInspectorPresenter>(
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

            WirePlanogramRuntimeHost(
                planogramRuntimeHost,
                runtimeHost,
                productCatalog);

            WireMerchandisingSelectionHost(
                merchandisingSelectionHost,
                runtimeHost);

            WireMerchandisingOverlay(
                merchandisingOverlay,
                runtimeHost,
                planogramRuntimeHost,
                viewSystem,
                merchandisingSelectionHost,
                dependencies.ViewHost,
                placeholder);

            WireMerchandisingHoverOutline(
                merchandisingHoverOutline,
                viewSystem);

            WireMerchandisingInput(
                merchandisingInput,
                dependencies.PlayerInput,
                dependencies.CellTargetResolver,
                dependencies.UiInputGate,
                dependencies.ToolCoordinator,
                runtimeHost,
                merchandisingSelectionHost,
                merchandisingOverlay,
                viewSystem,
                merchandisingHoverOutline);

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

            WireMerchandisingPresenter(
                merchandisingPresenter,
                dependencies.DocumentHost,
                runtimeHost,
                planogramRuntimeHost,
                merchandisingSelectionHost);

            runtimeHost.enabled = true;
            viewSystem.enabled = true;
            planogramRuntimeHost.enabled = true;
            merchandisingOverlay.enabled = true;
            merchandisingHoverOutline.enabled = true;
            selectionHost.enabled = true;
            merchandisingSelectionHost.enabled = true;
            merchandisingInput.enabled = true;
            previewView.enabled = true;
            constructionTool.enabled = true;
            demolitionPreview.enabled = true;
            demolitionTool.enabled = true;
            pickerPresenter.enabled = true;
            merchandisingPresenter.enabled = true;

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(
                dependencies.MapHost.gameObject.scene);
            Selection.activeObject = runtimeHost;

            Debug.Log(
                "Installed initial shelf placement, demolition, and the "
                + "fixture merchandising graybox. Save "
                + "Gameplay, then enter Play Mode and choose Fixtures. The "
                + "prepared Standard Shelf and Half Shelf directional art "
                + "is used when present; otherwise the safe pylon or front-"
                + "view fallback remains active. Choose the Merchandise "
                + "button, hover a shelf, "
                + "and click it to open its merchandising inspector.",
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
                Object.FindAnyObjectByType<ConstructionToolbarDocumentHost>(FindObjectsInactive.Exclude),
                Object.FindAnyObjectByType<ConstructionUiInputGate>(FindObjectsInactive.Exclude));
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

            if (!AssetDatabase.IsValidFolder(MerchandiseFolder))
            {
                AssetDatabase.CreateFolder("Assets/Design", "Merchandise");
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
            Sprite[] risingLeftShelfMasks =
            {
                LoadPreparedAlignedMaskSprite(
                    StandardShelfRisingLeftMaskTopPath,
                    StandardShelfRisingLeftPath),
                LoadPreparedAlignedMaskSprite(
                    StandardShelfRisingLeftMaskMiddlePath,
                    StandardShelfRisingLeftPath),
                LoadPreparedAlignedMaskSprite(
                    StandardShelfRisingLeftMaskBottomPath,
                    StandardShelfRisingLeftPath)
            };
            Sprite[] risingRightShelfMasks =
            {
                LoadPreparedAlignedMaskSprite(
                    StandardShelfRisingRightMaskTopPath,
                    StandardShelfRisingRightPath),
                LoadPreparedAlignedMaskSprite(
                    StandardShelfRisingRightMaskMiddlePath,
                    StandardShelfRisingRightPath),
                LoadPreparedAlignedMaskSprite(
                    StandardShelfRisingRightMaskBottomPath,
                    StandardShelfRisingRightPath)
            };

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
            ConfigureStandardShelfMerchandisingMasks(
                serialized,
                risingRightShelfMasks,
                risingLeftShelfMasks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }


        private static void ConfigureStandardShelfMerchandisingMasks(
            SerializedObject serializedDefinition,
            Sprite[] risingRightShelfMasks,
            Sprite[] risingLeftShelfMasks)
        {
            SerializedProperty maskSets =
                serializedDefinition.FindProperty(
                    "merchandisingMaskSets");

            if (maskSets == null)
            {
                Debug.LogError(
                    "Could not find the fixture merchandising-mask property.");
                return;
            }

            if (!AreAllSpritesPrepared(risingRightShelfMasks)
                || !AreAllSpritesPrepared(risingLeftShelfMasks))
            {
                maskSets.arraySize = 0;
                return;
            }

            // The double-sided fixture reuses its two directional canvases.
            // Which logical face owns the visible shelf masks is stable even
            // though camera rotation changes the presentation direction.
            maskSets.arraySize = 2;

            SerializedProperty northFace =
                maskSets.GetArrayElementAtIndex(0);
            northFace.FindPropertyRelative("localDisplaySide").intValue =
                (int)FixtureSide.North;
            SetSpriteArray(
                northFace.FindPropertyRelative("northShelfMasks"),
                System.Array.Empty<Sprite>());
            SetSpriteArray(
                northFace.FindPropertyRelative("eastShelfMasks"),
                risingLeftShelfMasks);
            SetSpriteArray(
                northFace.FindPropertyRelative("southShelfMasks"),
                risingRightShelfMasks);
            SetSpriteArray(
                northFace.FindPropertyRelative("westShelfMasks"),
                System.Array.Empty<Sprite>());

            SerializedProperty southFace =
                maskSets.GetArrayElementAtIndex(1);
            southFace.FindPropertyRelative("localDisplaySide").intValue =
                (int)FixtureSide.South;
            SetSpriteArray(
                southFace.FindPropertyRelative("northShelfMasks"),
                risingRightShelfMasks);
            SetSpriteArray(
                southFace.FindPropertyRelative("eastShelfMasks"),
                System.Array.Empty<Sprite>());
            SetSpriteArray(
                southFace.FindPropertyRelative("southShelfMasks"),
                System.Array.Empty<Sprite>());
            SetSpriteArray(
                southFace.FindPropertyRelative("westShelfMasks"),
                risingLeftShelfMasks);
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
            Sprite[] risingLeftShelfMasks =
            {
                LoadPreparedSprite(HalfShelfRisingLeftMaskTopPath),
                LoadPreparedSprite(HalfShelfRisingLeftMaskMiddlePath),
                LoadPreparedSprite(HalfShelfRisingLeftMaskBottomPath)
            };
            Sprite[] risingRightShelfMasks =
            {
                LoadPreparedSprite(HalfShelfRisingRightMaskTopPath),
                LoadPreparedSprite(HalfShelfRisingRightMaskMiddlePath),
                LoadPreparedSprite(HalfShelfRisingRightMaskBottomPath)
            };

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
            ConfigureHalfShelfMerchandisingMasks(
                serialized,
                risingRightShelfMasks,
                risingLeftShelfMasks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }


        private static void ConfigureHalfShelfMerchandisingMasks(
            SerializedObject serializedDefinition,
            Sprite[] northShelfMasks,
            Sprite[] westShelfMasks)
        {
            SerializedProperty maskSets =
                serializedDefinition.FindProperty(
                    "merchandisingMaskSets");

            if (maskSets == null)
            {
                Debug.LogError(
                    "Could not find the fixture merchandising-mask property.");
                return;
            }

            if (!AreAllSpritesPrepared(northShelfMasks)
                || !AreAllSpritesPrepared(westShelfMasks))
            {
                maskSets.arraySize = 0;
                return;
            }

            maskSets.arraySize = 1;
            SerializedProperty maskSet =
                maskSets.GetArrayElementAtIndex(0);

            maskSet.FindPropertyRelative("localDisplaySide").intValue =
                (int)FixtureSide.South;

            SetSpriteArray(
                maskSet.FindPropertyRelative("northShelfMasks"),
                northShelfMasks);
            SetSpriteArray(
                maskSet.FindPropertyRelative("eastShelfMasks"),
                System.Array.Empty<Sprite>());
            SetSpriteArray(
                maskSet.FindPropertyRelative("southShelfMasks"),
                System.Array.Empty<Sprite>());
            SetSpriteArray(
                maskSet.FindPropertyRelative("westShelfMasks"),
                westShelfMasks);
        }


        private static bool AreAllSpritesPrepared(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < sprites.Length; index++)
            {
                if (sprites[index] == null)
                {
                    return false;
                }
            }

            return true;
        }


        private static void SetSpriteArray(
            SerializedProperty property,
            Sprite[] sprites)
        {
            property.arraySize = sprites.Length;

            for (int index = 0; index < sprites.Length; index++)
            {
                property.GetArrayElementAtIndex(index)
                    .objectReferenceValue = sprites[index];
            }
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


        private static Sprite LoadPreparedAlignedMaskSprite(
            string assetPath,
            string fixtureSpritePath)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            TextureImporter fixtureImporter =
                AssetImporter.GetAtPath(fixtureSpritePath) as TextureImporter;

            if (importer == null || fixtureImporter == null)
            {
                return null;
            }

            TextureImporterSettings importerSettings =
                new TextureImporterSettings();
            TextureImporterSettings fixtureSettings =
                new TextureImporterSettings();

            importer.ReadTextureSettings(importerSettings);
            fixtureImporter.ReadTextureSettings(fixtureSettings);

            bool requiresReimport =
                importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled
                || !Mathf.Approximately(
                    importer.spritePixelsPerUnit,
                    fixtureImporter.spritePixelsPerUnit)
                || importerSettings.spriteAlignment
                    != fixtureSettings.spriteAlignment
                || Vector2.Distance(
                    importerSettings.spritePivot,
                    fixtureSettings.spritePivot) > 0.0001f;

            if (requiresReimport)
            {
                // Apply the fixture artwork's complete sprite settings first.
                // Setting individual importer properties before applying the
                // mask's stale settings would restore Multiple mode and its
                // old pixels-per-unit, leaving a cropped, misaligned mask.
                importer.SetTextureSettings(fixtureSettings);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit =
                    fixtureImporter.spritePixelsPerUnit;
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


        private static ProductDefinitionAsset GetOrCreateProduct(
            string assetPath,
            string productId,
            string displayName)
        {
            ProductDefinitionAsset product =
                AssetDatabase.LoadAssetAtPath<ProductDefinitionAsset>(
                    assetPath);

            if (product == null)
            {
                product =
                    ScriptableObject.CreateInstance<ProductDefinitionAsset>();
                AssetDatabase.CreateAsset(product, assetPath);
            }

            SerializedObject serialized = new SerializedObject(product);
            serialized.FindProperty("productId").stringValue = productId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("categoryId").stringValue = "GROCERY";
            serialized.FindProperty("stockUnit").enumValueIndex =
                (int)StockUnit.Each;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(product);
            return product;
        }


        private static ProductCatalogAsset GetOrCreateProductCatalog(
            params ProductDefinitionAsset[] products)
        {
            ProductCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<ProductCatalogAsset>(
                    ProductCatalogPath);

            if (catalog == null)
            {
                catalog =
                    ScriptableObject.CreateInstance<ProductCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, ProductCatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("products");
            entries.arraySize = products.Length;

            for (int index = 0;
                 index < products.Length;
                 index++)
            {
                entries
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue = products[index];
            }

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


        private static void WirePlanogramRuntimeHost(
            FixturePlanogramRuntimeHost planogramRuntimeHost,
            FixtureRuntimeHost fixtureRuntimeHost,
            ProductCatalogAsset productCatalog)
        {
            SetObjectReference(
                planogramRuntimeHost,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
            SetObjectReference(
                planogramRuntimeHost,
                "productCatalogAsset",
                productCatalog);
        }


        private static void WireMerchandisingSelectionHost(
            FixtureMerchandisingSelectionHost selectionHost,
            FixtureRuntimeHost fixtureRuntimeHost)
        {
            SetObjectReference(
                selectionHost,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
        }


        private static void WireMerchandisingOverlay(
            FixtureMerchandisingOverlayViewSystem overlay,
            FixtureRuntimeHost fixtureRuntimeHost,
            FixturePlanogramRuntimeHost planogramRuntimeHost,
            FixtureViewSystem fixtureViewSystem,
            FixtureMerchandisingSelectionHost selectionHost,
            IsometricViewHost viewHost,
            Sprite frontageMarkerSprite)
        {
            SetObjectReference(
                overlay,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
            SetObjectReference(
                overlay,
                "planogramRuntimeHost",
                planogramRuntimeHost);
            SetObjectReference(
                overlay,
                "fixtureViewSystem",
                fixtureViewSystem);
            SetObjectReference(
                overlay,
                "selectionHost",
                selectionHost);
            SetObjectReference(overlay, "viewHost", viewHost);
            SetObjectReference(
                overlay,
                "frontageMarkerSprite",
                frontageMarkerSprite);
        }


        private static void WireMerchandisingInput(
            FixtureMerchandisingInputController input,
            PlayerInput playerInput,
            GridCellTargetResolver targetResolver,
            ConstructionUiInputGate uiInputGate,
            ConstructionToolCoordinator toolCoordinator,
            FixtureRuntimeHost fixtureRuntimeHost,
            FixtureMerchandisingSelectionHost selectionHost,
            FixtureMerchandisingOverlayViewSystem overlay,
            FixtureViewSystem fixtureViewSystem,
            FixtureMerchandisingHoverOutlineView hoverOutline)
        {
            SetObjectReference(input, "playerInput", playerInput);
            SetObjectReference(input, "targetResolver", targetResolver);
            SetObjectReference(input, "uiInputGate", uiInputGate);
            SetObjectReference(input, "toolCoordinator", toolCoordinator);
            SetObjectReference(
                input,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
            SetObjectReference(input, "selectionHost", selectionHost);
            SetObjectReference(input, "overlayViewSystem", overlay);
            SetObjectReference(input, "fixtureViewSystem", fixtureViewSystem);
            SetObjectReference(input, "hoverOutlineView", hoverOutline);
        }


        private static void WireMerchandisingHoverOutline(
            FixtureMerchandisingHoverOutlineView hoverOutline,
            FixtureViewSystem fixtureViewSystem)
        {
            SetObjectReference(
                hoverOutline,
                "fixtureViewSystem",
                fixtureViewSystem);
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


        private static void WireMerchandisingPresenter(
            FixtureMerchandisingInspectorPresenter presenter,
            ConstructionToolbarDocumentHost documentHost,
            FixtureRuntimeHost fixtureRuntimeHost,
            FixturePlanogramRuntimeHost planogramRuntimeHost,
            FixtureMerchandisingSelectionHost selectionHost)
        {
            SetObjectReference(presenter, "documentHost", documentHost);
            SetObjectReference(
                presenter,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);
            SetObjectReference(
                presenter,
                "planogramRuntimeHost",
                planogramRuntimeHost);
            SetObjectReference(presenter, "selectionHost", selectionHost);
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
                ConstructionToolbarDocumentHost documentHost,
                ConstructionUiInputGate uiInputGate)
            {
                MapHost = mapHost;
                FloorRuntimeHost = floorRuntimeHost;
                ViewHost = viewHost;
                CellTargetResolver = cellTargetResolver;
                PlayerInput = playerInput;
                HistoryHost = historyHost;
                ToolCoordinator = toolCoordinator;
                DocumentHost = documentHost;
                UiInputGate = uiInputGate;
            }

            public GridMapHost MapHost { get; }
            public FloorRuntimeHost FloorRuntimeHost { get; }
            public IsometricViewHost ViewHost { get; }
            public GridCellTargetResolver CellTargetResolver { get; }
            public PlayerInput PlayerInput { get; }
            public ConstructionHistoryHost HistoryHost { get; }
            public ConstructionToolCoordinator ToolCoordinator { get; }
            public ConstructionToolbarDocumentHost DocumentHost { get; }
            public ConstructionUiInputGate UiInputGate { get; }

            public bool IsComplete =>
                MapHost != null
                && FloorRuntimeHost != null
                && ViewHost != null
                && CellTargetResolver != null
                && PlayerInput != null
                && HistoryHost != null
                && ToolCoordinator != null
                && DocumentHost != null
                && UiInputGate != null;
        }
    }
}
