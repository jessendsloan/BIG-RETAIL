using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Foundations;
using BigRetail.Construction.Unity.Sidewalks;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Foundations;
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
    /// the normal Map Workshop construction model and synchronizes the
    /// painted roadside walk into the logical navigation surface.
    /// </summary>
    public static class FrankRoadsidePropertyExpansion
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string LayoutPath =
            "Assets/Design/StoreLayouts/FrankStoreLayoutV1.asset";

        private const string RoadsideSurfaceTilePath =
            "Assets/Art/GroundTileArt/Brick/"
            + "groundtile_brick_2_0.asset";

        private const string TrailerAccessSurfaceTilePath =
            "Assets/Art/GroundTileArt/Minerals/"
            + "groundtile_gravel_1_0.asset";

        private const int PropertyMinimumX = -67;
        private const int PropertyMaximumX = 28;
        private const int PropertyMinimumY = 13;
        private const int PropertyMaximumY = 59;

        private const string RoadsideArrivalMarkerId =
            "bigretail.marker.frank.roadside_arrival";

        private const int FirstDraftTrailerPathStartX = -8;
        private const int TrailerPathStartX = -7;
        private const int TrailerPathStartY = 47;
        private const int PreviousTrailerPathEndX = 19;
        private const int PreviousTrailerPathEndY = 54;
        private const int PreviousRenderedPathEndX = 8;
        private const int PreviousRenderedPathEndY = 51;
        private const int TrailerPathEndX = 12;
        private const int TrailerPathEndY = 52;

        private static readonly Vector3Int RoadsideSidewalkSeed =
            new Vector3Int(12, 27, 0);

        private static readonly Vector3Int TrailerAccessPathSeed =
            new Vector3Int(12, 51, 0);


        public static void SynchronizeRoadsideWalkabilityForAutomation()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "Frank's roadside walkability sync requires Edit Mode.");
            }

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            GridMapHost mapHost =
                FindRequiredComponent<GridMapHost>(scene);
            mapHost.Initialize();

            if (!mapHost.IsInitialized
                || string.IsNullOrWhiteSpace(mapHost.MapFingerprint))
            {
                throw new InvalidOperationException(
                    "Frank's map could not produce its geometry "
                    + "fingerprint for the walkability sync.");
            }

            RestorePlayerAuthoredSidewalks(
                scene,
                mapHost.MapFingerprint);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Frank Roadside's painted upper sidewalk and gravel "
                + "trailer path are now part of the navigation surface.");
        }


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
            ConfigureRoadsideMarker(scene);
            ConfigureRoadsideSurfaceTile(scene);
            RestorePlayerAuthoredSidewalks(
                scene,
                mapHost.MapFingerprint);

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
                + "property to Map Workshop construction and leaves "
                + "the trailer landing ready for an authored walkway.");
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


        private static void ConfigureRoadsideMarker(Scene scene)
        {
            LocationMarkerHost markerHost =
                FindRequiredComponent<LocationMarkerHost>(scene);
            LocationMarkerAuthoring[] markers =
                FindComponents<LocationMarkerAuthoring>(scene);
            LocationMarkerAuthoring roadsideMarker = null;

            for (int index = 0; index < markers.Length; index++)
            {
                if (string.Equals(
                        markers[index].MarkerId,
                        RoadsideArrivalMarkerId,
                        StringComparison.Ordinal))
                {
                    roadsideMarker = markers[index];
                    break;
                }
            }

            if (roadsideMarker == null)
            {
                throw new InvalidOperationException(
                    "Frank Roadside is missing its stable arrival marker.");
            }

            SerializedObject serialized =
                new SerializedObject(roadsideMarker);
            serialized.FindProperty("logicalCell").vector3IntValue =
                new Vector3Int(
                    TrailerPathEndX,
                    TrailerPathEndY,
                    0);
            serialized.FindProperty("worldOffset").vector3Value =
                Vector3.zero;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(roadsideMarker);

            if (!markerHost.TryRebuildMarkerIndex(
                    out string validationFailure))
            {
                throw new InvalidOperationException(validationFailure);
            }

            markerHost.RefreshWorldPositions();
        }


        private static void ConfigureRoadsideSurfaceTile(Scene scene)
        {
            TileBase surfaceTile =
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    RoadsideSurfaceTilePath);

            if (surfaceTile == null)
            {
                throw new InvalidOperationException(
                    $"Frank's roadside surface tile is missing at "
                    + $"'{RoadsideSurfaceTilePath}'.");
            }

            SetTileReference(
                FindRequiredComponent<
                    FoundationApronTilemapViewSystem>(scene),
                "apronTile",
                surfaceTile);
            SetTileReference(
                FindRequiredComponent<FoundationAreaPreviewView>(scene),
                "previewApronTile",
                surfaceTile);
            SetTileReference(
                FindRequiredComponent<SidewalkTilemapViewSystem>(scene),
                "sidewalkTile",
                surfaceTile);
            SetTileReference(
                FindRequiredComponent<SidewalkAreaPreviewView>(scene),
                "previewTile",
                surfaceTile);
            SetTileReference(
                FindRequiredComponent<
                    SidewalkDemolitionAreaPreviewView>(scene),
                "previewTile",
                surfaceTile);
        }


        private static void SetTileReference(
            Component component,
            string propertyName,
            TileBase tile)
        {
            SerializedObject serialized =
                new SerializedObject(component);
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{component.GetType().Name} is missing serialized "
                    + $"tile property '{propertyName}'.");
            }

            property.objectReferenceValue = tile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }


        private static void RestorePlayerAuthoredSidewalks(
            Scene scene,
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

            // Remove the short storefront spur created by the first draft.
            for (int y = 22; y <= 27; y++)
            {
                for (int x = -20; x <= -17; x++)
                {
                    sidewalks.Remove(new StoreCellData(x, y, 0));
                }
            }

            // Remove both earlier diagonal drafts before authoring the
            // shorter route that ends at the trailer's rendered steps.
            RemoveRoundedPath(
                sidewalks,
                FirstDraftTrailerPathStartX,
                TrailerPathStartY,
                PreviousTrailerPathEndX,
                PreviousTrailerPathEndY,
                1);
            RemoveRoundedPath(
                sidewalks,
                TrailerPathStartX,
                TrailerPathStartY,
                PreviousTrailerPathEndX,
                PreviousTrailerPathEndY,
                -1);
            RemoveRoundedPath(
                sidewalks,
                TrailerPathStartX,
                TrailerPathStartY,
                PreviousRenderedPathEndX,
                PreviousRenderedPathEndY,
                -1);
            RemoveRoundedPath(
                sidewalks,
                TrailerPathStartX,
                TrailerPathStartY,
                TrailerPathEndX,
                TrailerPathEndY,
                -1);

            // Restore the untouched end of the original street-sidewalk
            // network where the experimental routes overlapped it.
            for (int x = -10; x <= -7; x++)
            {
                sidewalks.Add(new StoreCellData(x, 46, 0));
            }
            sidewalks.Add(new StoreCellData(-9, 47, 0));
            sidewalks.Add(new StoreCellData(-8, 47, 0));

            // Keep the trailer landing itself available even though the
            // authored gravel stops immediately below the rendered steps.
            sidewalks.Add(
                new StoreCellData(
                    TrailerPathEndX,
                    TrailerPathEndY,
                    0));

            Tilemap mapVisuals =
                FindRequiredTilemap(scene, "MapVIsuals");
            Tilemap constructionAreaMask =
                FindRequiredTilemap(scene, "ConstructionAreaMask");
            TileBase roadsideSurface =
                LoadRequiredTile(RoadsideSurfaceTilePath);
            TileBase trailerAccessSurface =
                LoadRequiredTile(TrailerAccessSurfaceTilePath);

            // MapVIsuals extends beyond Frank's owned lot so the roadside
            // art can continue offscreen. Logical sidewalks must stop at the
            // authored construction boundary or layout restoration will
            // correctly reject those cells.
            sidewalks.RemoveWhere(
                cell =>
                    !constructionAreaMask.HasTile(
                        new Vector3Int(
                            cell.X,
                            cell.Y,
                            cell.Level)));

            AddConnectedAuthoredSurface(
                mapVisuals,
                constructionAreaMask,
                roadsideSurface,
                RoadsideSidewalkSeed,
                sidewalks);
            AddConnectedAuthoredSurface(
                mapVisuals,
                constructionAreaMask,
                trailerAccessSurface,
                TrailerAccessPathSeed,
                sidewalks);

            layout.Sidewalks.Clear();
            layout.Sidewalks.AddRange(sidewalks);
            layoutAsset.ReplaceData(layout);
            EditorUtility.SetDirty(layoutAsset);
            AssetDatabase.SaveAssetIfDirty(layoutAsset);
        }


        private static TileBase LoadRequiredTile(string assetPath)
        {
            TileBase tile =
                AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);

            if (tile == null)
            {
                throw new InvalidOperationException(
                    $"Frank's authored surface tile is missing at "
                    + $"'{assetPath}'.");
            }

            return tile;
        }


        private static void AddConnectedAuthoredSurface(
            Tilemap source,
            Tilemap constructionAreaMask,
            TileBase surfaceTile,
            Vector3Int seed,
            ISet<StoreCellData> sidewalks)
        {
            if (source.GetTile(seed) != surfaceTile)
            {
                throw new InvalidOperationException(
                    $"Frank's authored walking surface at {seed} no "
                    + "longer uses the expected tile. Move the seed or "
                    + "restore the painted route before synchronizing.");
            }

            Queue<Vector3Int> frontier =
                new Queue<Vector3Int>();
            HashSet<Vector3Int> visited =
                new HashSet<Vector3Int>();
            frontier.Enqueue(seed);

            while (frontier.Count > 0)
            {
                Vector3Int cell = frontier.Dequeue();

                if (!visited.Add(cell)
                    || !constructionAreaMask.HasTile(cell)
                    || source.GetTile(cell) != surfaceTile)
                {
                    continue;
                }

                sidewalks.Add(
                    new StoreCellData(cell.x, cell.y, 0));
                frontier.Enqueue(cell + Vector3Int.right);
                frontier.Enqueue(cell + Vector3Int.left);
                frontier.Enqueue(cell + Vector3Int.up);
                frontier.Enqueue(cell + Vector3Int.down);
            }
        }


        private static void RemoveRoundedPath(
            HashSet<StoreCellData> sidewalks,
            int startX,
            int startY,
            int endX,
            int endY,
            int secondLaneOffsetY)
        {
            int horizontalDistance = endX - startX;
            int verticalDistance = endY - startY;

            for (int x = startX; x <= endX; x++)
            {
                int progress = x - startX;
                int y = startY
                    + ((progress * verticalDistance
                        + horizontalDistance / 2)
                       / horizontalDistance);

                sidewalks.Remove(new StoreCellData(x, y, 0));
                sidewalks.Remove(
                    new StoreCellData(
                        x,
                        y + secondLaneOffsetY,
                        0));
            }
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
