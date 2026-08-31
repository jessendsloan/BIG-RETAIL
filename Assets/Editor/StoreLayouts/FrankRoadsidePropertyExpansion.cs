using System;
using System.Collections.Generic;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Navigation;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Unity;
using BigRetail.StoreLayouts;
using BigRetail.StoreLayouts.Unity;
using BigRetail.Work.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace BigRetail.Editor.StoreLayouts
{
    /// <summary>
    /// One-time authored migration that brings Frank's upper property into
    /// the normal Map Workshop construction model and connects the trailer
    /// report marker to the existing sidewalk network.
    /// </summary>
    public static class FrankRoadsidePropertyExpansion
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string LayoutPath =
            "Assets/Design/StoreLayouts/FrankStoreLayoutV1.asset";

        private const int PropertyMinimumX = -67;
        private const int PropertyMaximumX = 28;
        private const int PropertyMinimumY = 13;
        private const int PropertyMaximumY = 59;


        public static void ApplyForAutomation()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Frank's property expansion requires Edit Mode.");
            }

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            Tilemap mapAreaMask =
                FindRequiredTilemap(scene, "MapAreaMask");
            Tilemap constructionAreaMask =
                FindRequiredTilemap(scene, "ConstructionAreaMask");
            TileBase constructionTile =
                constructionAreaMask.GetTile(
                    new Vector3Int(PropertyMinimumX, 28, 0));

            if (constructionTile == null)
            {
                throw new InvalidOperationException(
                    "Frank's existing ConstructionAreaMask has no tile "
                    + "available for the property expansion.");
            }

            for (int y = PropertyMinimumY;
                 y <= PropertyMaximumY;
                 y++)
            {
                for (int x = PropertyMinimumX;
                     x <= PropertyMaximumX;
                     x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);

                    if (!mapAreaMask.HasTile(cell))
                    {
                        throw new InvalidOperationException(
                            $"Owned property cell {cell} falls outside "
                            + "Frank's MapAreaMask.");
                    }

                    constructionAreaMask.SetTile(
                        cell,
                        constructionTile);
                }
            }

            constructionAreaMask.CompressBounds();
            GridNavigationSurfaceHost navigationHost =
                ConfigureNavigationHost(scene);
            GridMapHost mapHost =
                FindRequiredComponent<GridMapHost>(scene);
            mapHost.Initialize();

            if (!mapHost.IsInitialized
                || string.IsNullOrWhiteSpace(mapHost.MapFingerprint))
            {
                throw new InvalidOperationException(
                    "Frank's expanded map could not produce its geometry "
                    + "fingerprint.");
            }

            ConfigureFounderWork(scene, navigationHost);
            ExpandOpeningLayoutSidewalks(mapHost.MapFingerprint);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save Frank's expanded property.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Frank Roadside now exposes the full 96 x 47 owned "
                + "property to Map Workshop construction and connects "
                + "the trailer marker to the normal sidewalk network.");
        }


        private static GridNavigationSurfaceHost
            ConfigureNavigationHost(Scene scene)
        {
            GridMapHost mapHost =
                FindRequiredComponent<GridMapHost>(scene);
            SidewalkRuntimeHost sidewalkHost =
                FindRequiredComponent<SidewalkRuntimeHost>(scene);
            FixtureRuntimeHost fixtureHost =
                FindRequiredComponent<FixtureRuntimeHost>(scene);
            GridNavigationSurfaceHost navigationHost =
                mapHost.GetComponent<GridNavigationSurfaceHost>();

            if (navigationHost == null)
            {
                navigationHost =
                    mapHost.gameObject.AddComponent<
                        GridNavigationSurfaceHost>();
            }

            SerializedObject serialized =
                new SerializedObject(navigationHost);
            serialized.FindProperty("mapHost").objectReferenceValue =
                mapHost;
            serialized.FindProperty("sidewalkRuntimeHost")
                .objectReferenceValue = sidewalkHost;
            serialized.FindProperty("fixtureRuntimeHost")
                .objectReferenceValue = fixtureHost;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(navigationHost);
            return navigationHost;
        }


        private static void ConfigureFounderWork(
            Scene scene,
            GridNavigationSurfaceHost navigationHost)
        {
            FounderStockTaskController founderController =
                FindRequiredComponent<FounderStockTaskController>(scene);
            PurchasingRuntimeHost purchasingHost =
                FindRequiredComponent<PurchasingRuntimeHost>(scene);
            ReceivingAreaRuntimeHost receivingHost =
                FindRequiredComponent<ReceivingAreaRuntimeHost>(scene);
            SerializedObject serialized =
                new SerializedObject(founderController);

            serialized.FindProperty("navigationSurfaceHost")
                .objectReferenceValue = navigationHost;
            serialized.FindProperty("purchasingRuntimeHost")
                .objectReferenceValue = purchasingHost;
            serialized.FindProperty("receivingAreaRuntimeHost")
                .objectReferenceValue = receivingHost;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(founderController);
        }


        private static void ExpandOpeningLayoutSidewalks(
            string mapFingerprint)
        {
            StoreLayoutAsset layoutAsset =
                AssetDatabase.LoadAssetAtPath<StoreLayoutAsset>(
                    LayoutPath);

            if (layoutAsset == null)
            {
                throw new InvalidOperationException(
                    $"Frank's opening layout is missing at '{LayoutPath}'.");
            }

            StoreLayoutData layout = layoutAsset.CreateRuntimeCopy();
            layout.MapFingerprint = mapFingerprint;
            HashSet<StoreCellData> sidewalks =
                new HashSet<StoreCellData>(layout.Sidewalks);

            for (int y = 22; y <= 27; y++)
            {
                for (int x = -20; x <= -17; x++)
                {
                    sidewalks.Add(new StoreCellData(x, y, 0));
                }
            }

            layout.Sidewalks.Clear();
            layout.Sidewalks.AddRange(sidewalks);
            layoutAsset.ReplaceData(layout);
            EditorUtility.SetDirty(layoutAsset);
            AssetDatabase.SaveAssetIfDirty(layoutAsset);
        }


        private static Tilemap FindRequiredTilemap(
            Scene scene,
            string objectName)
        {
            Tilemap[] tilemaps =
                FindComponents<Tilemap>(scene);

            for (int index = 0; index < tilemaps.Length; index++)
            {
                if (tilemaps[index].gameObject.name == objectName)
                {
                    return tilemaps[index];
                }
            }

            throw new InvalidOperationException(
                $"Frank Roadside is missing Tilemap '{objectName}'.");
        }


        private static T FindRequiredComponent<T>(Scene scene)
            where T : Component
        {
            T[] components = FindComponents<T>(scene);

            if (components.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Frank Roadside is missing {typeof(T).Name}.");
            }

            return components[0];
        }


        private static T[] FindComponents<T>(Scene scene)
            where T : Component
        {
            List<T> components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0; index < roots.Length; index++)
            {
                components.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }
    }
}
