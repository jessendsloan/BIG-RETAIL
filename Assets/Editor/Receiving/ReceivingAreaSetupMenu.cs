using System;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.Input;
using BigRetail.Construction.Unity.Receiving;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.View;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace BigRetail.Editor.Receiving
{
    /// <summary>
    /// Installs the first concrete operational area into Gameplay without
    /// creating a generic room framework.
    /// </summary>
    public static class ReceivingAreaSetupMenu
    {
        private const string GameplayScenePath =
            "Assets/Scenes/Gameplay.unity";
        private const string MarkerTilePath =
            "Assets/Art/Semantic/SemanticCellMarkerTile.asset";


        [MenuItem("Big Retail/Operations/Integrate Receiving Area Into Gameplay")]
        public static void IntegrateReceivingAreaIntoGameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);

            GridMapHost mapHost = FindRequired<GridMapHost>();
            FloorRuntimeHost floorRuntimeHost =
                FindRequired<FloorRuntimeHost>();
            FixtureRuntimeHost fixtureRuntimeHost =
                FindRequired<FixtureRuntimeHost>();
            IsometricViewHost viewHost =
                FindRequired<IsometricViewHost>();
            GridCellTargetResolver cellTargetResolver =
                FindRequired<GridCellTargetResolver>();
            ConstructionPointerController pointerController =
                FindRequired<ConstructionPointerController>();
            PlayerInput playerInput = FindRequired<PlayerInput>();
            ConstructionToolCoordinator toolCoordinator =
                FindRequired<ConstructionToolCoordinator>();
            ConstructionToolbarPresenter toolbarPresenter =
                FindRequired<ConstructionToolbarPresenter>();
            PurchasingRuntimeHost purchasingRuntimeHost =
                FindRequired<PurchasingRuntimeHost>();
            InboundDeliveryViewSystem deliveryViewSystem =
                FindRequired<InboundDeliveryViewSystem>();
            TileBase markerTile =
                AssetDatabase.LoadAssetAtPath<TileBase>(MarkerTilePath);

            if (markerTile == null)
            {
                throw new InvalidOperationException(
                    $"Receiving Area marker Tile is missing at "
                    + $"'{MarkerTilePath}'.");
            }

            GameObject gridObject = FindSceneGameObject(scene, "Grid")
                ?? throw new InvalidOperationException(
                    "Gameplay has no Grid object for the Receiving overlay.");
            GameObject runtimeObject =
                FindSceneGameObject(scene, "ReceivingAreaRuntime");

            if (runtimeObject == null)
            {
                runtimeObject = new GameObject("ReceivingAreaRuntime");
                runtimeObject.transform.SetParent(
                    mapHost.transform.parent,
                    false);
            }

            GameObject overlayObject =
                FindSceneGameObject(scene, "ReceivingAreaOverlay");

            if (overlayObject == null)
            {
                overlayObject = new GameObject("ReceivingAreaOverlay");
            }

            overlayObject.transform.SetParent(gridObject.transform, false);
            Tilemap overlayTilemap =
                GetOrAddComponent<Tilemap>(overlayObject);
            TilemapRenderer overlayRenderer =
                GetOrAddComponent<TilemapRenderer>(overlayObject);
            overlayRenderer.mode = TilemapRenderer.Mode.Individual;
            overlayRenderer.sortingLayerName = "Default";
            overlayRenderer.sortingOrder = 25;
            EditorUtility.SetDirty(overlayRenderer);

            ReceivingAreaRuntimeHost receivingRuntimeHost =
                GetOrAddComponent<ReceivingAreaRuntimeHost>(runtimeObject);
            SetObjectReference(receivingRuntimeHost, "mapHost", mapHost);
            SetObjectReference(
                receivingRuntimeHost,
                "floorRuntimeHost",
                floorRuntimeHost);
            SetObjectReference(
                receivingRuntimeHost,
                "fixtureRuntimeHost",
                fixtureRuntimeHost);

            ReceivingAreaViewSystem receivingViewSystem =
                GetOrAddComponent<ReceivingAreaViewSystem>(runtimeObject);
            SetObjectReference(
                receivingViewSystem,
                "runtimeHost",
                receivingRuntimeHost);
            SetObjectReference(receivingViewSystem, "viewHost", viewHost);
            SetObjectReference(
                receivingViewSystem,
                "overlayTilemap",
                overlayTilemap);
            SetObjectReference(
                receivingViewSystem,
                "overlayRenderer",
                overlayRenderer);
            SetObjectReference(
                receivingViewSystem,
                "markerTile",
                markerTile);

            GameObject toolObject =
                FindSceneGameObject(scene, "ReceivingAreaTool");

            if (toolObject == null)
            {
                toolObject = new GameObject("ReceivingAreaTool");
                toolObject.transform.SetParent(
                    toolCoordinator.transform.parent,
                    false);
            }

            ReceivingAreaToolController receivingAreaTool =
                GetOrAddComponent<ReceivingAreaToolController>(toolObject);
            SetObjectReference(receivingAreaTool, "playerInput", playerInput);
            SetObjectReference(
                receivingAreaTool,
                "pointerController",
                pointerController);
            SetObjectReference(
                receivingAreaTool,
                "cellTargetResolver",
                cellTargetResolver);
            SetObjectReference(
                receivingAreaTool,
                "runtimeHost",
                receivingRuntimeHost);
            SetObjectReference(
                receivingAreaTool,
                "viewSystem",
                receivingViewSystem);

            SetObjectReference(
                toolCoordinator,
                "receivingAreaTool",
                receivingAreaTool);
            SetObjectReference(
                toolbarPresenter,
                "receivingAreaRuntimeHost",
                receivingRuntimeHost);
            SetObjectReference(
                toolbarPresenter,
                "purchasingRuntimeHost",
                purchasingRuntimeHost);
            FixtureMerchandisingInspectorPresenter merchandisingPresenter =
                FindRequired<FixtureMerchandisingInspectorPresenter>();
            SetObjectReference(
                merchandisingPresenter,
                "receivingAreaRuntimeHost",
                receivingRuntimeHost);
            SetObjectReference(
                purchasingRuntimeHost,
                "receivingAreaRuntimeHost",
                receivingRuntimeHost);
            SetObjectReference(
                deliveryViewSystem,
                "receivingAreaRuntimeHost",
                receivingRuntimeHost);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Integrated the player-designated Receiving Area, capacity "
                + "overlay, construction input, and supplier-pallet target "
                + "into Gameplay.",
                runtimeObject);
        }

        public static void IntegrateForAutomation()
        {
            IntegrateReceivingAreaIntoGameplay();
        }


        private static T FindRequired<T>()
            where T : UnityEngine.Object
        {
            T found = UnityEngine.Object.FindAnyObjectByType<T>(
                FindObjectsInactive.Include);

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"Gameplay is missing required component "
                    + $"'{typeof(T).Name}'.");
            }

            return found;
        }

        private static GameObject FindSceneGameObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0; index < roots.Length; index++)
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

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serializedObject =
                new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException(
                    $"'{target.GetType().Name}' has no serialized property "
                    + $"named '{propertyName}'.");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
