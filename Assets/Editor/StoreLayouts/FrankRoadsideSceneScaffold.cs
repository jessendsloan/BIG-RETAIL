using System;
using System.Collections.Generic;
using BigRetail.CameraControl;
using BigRetail.Map.Construction;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

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

        private const int ExpectedConstructionWidth = 96;
        private const int ExpectedConstructionHeight = 32;
        private const float CameraBoundsPadding = 4f;

        private const string StoreFootprintCenterMarkerId =
            "bigretail.marker.frank.store_footprint_center";

        private const string RoadsideArrivalMarkerId =
            "bigretail.marker.frank.roadside_arrival";

        private const string RearServiceMarkerId =
            "bigretail.marker.frank.rear_service";


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


        [MenuItem(
            "Big Retail/Map Workshop/Finalize Authored Frank Roadside Map")]
        public static void FinalizeAuthoredMap()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    DestinationScenePath,
                    OpenSceneMode.Single);

            GridMapAuthoring mapAuthoring =
                FindRequiredInScene<GridMapAuthoring>(scene);
            GridMapHost mapHost =
                FindRequiredInScene<GridMapHost>(scene);
            IsometricViewHost viewHost =
                FindRequiredInScene<IsometricViewHost>(scene);
            CameraController cameraController =
                FindRequiredInScene<CameraController>(scene);

            Tilemap mapVisuals =
                FindRequiredTilemap(scene, "MapVIsuals");
            Tilemap mapAreaMask =
                FindRequiredTilemap(scene, "MapAreaMask");
            Tilemap constructionAreaMask =
                FindRequiredTilemap(scene, "ConstructionAreaMask");

            ValidateMapVisualsPath(
                mapVisuals.gameObject);

            AuthoredMapData authoredMap =
                ValidateAuthoredMap(
                    mapVisuals,
                    mapAreaMask,
                    constructionAreaMask);

            SetString(
                mapAuthoring,
                "mapId",
                FrankMapId);
            SetEnum(
                mapHost,
                "landPolicyKind",
                LocationLandPolicyKind.FixedFootprint);

            Bounds cameraWorldBounds =
                CalculateNorthWorldBounds(
                    mapVisuals,
                    authoredMap.MapBounds);

            cameraController.SetWorldBounds(
                cameraWorldBounds);
            cameraController.SetWorldCenter(
                cameraWorldBounds.center);

            EditorUtility.SetDirty(cameraController);
            EditorUtility.SetDirty(
                cameraController.transform);

            ConfigureLocationMarkers(
                scene,
                viewHost,
                mapVisuals,
                authoredMap);

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

            VerifyFinalizedScene();

            Debug.Log(
                "Frank Roadside authored map is finalized. Construction "
                + $"footprint: {ExpectedConstructionWidth} x "
                + $"{ExpectedConstructionHeight}. Camera bounds and "
                + "rotation-aware location markers are ready.");
        }


        public static void FinalizeForAutomation()
        {
            FinalizeAuthoredMap();
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


        private static void VerifyFinalizedScene()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    DestinationScenePath,
                    OpenSceneMode.Single);

            GridMapAuthoring mapAuthoring =
                FindRequiredInScene<GridMapAuthoring>(scene);
            GridMapHost mapHost =
                FindRequiredInScene<GridMapHost>(scene);
            CameraController cameraController =
                FindRequiredInScene<CameraController>(scene);
            LocationMarkerHost markerHost =
                FindRequiredInScene<LocationMarkerHost>(scene);

            Tilemap mapVisuals =
                FindRequiredTilemap(scene, "MapVIsuals");
            Tilemap mapAreaMask =
                FindRequiredTilemap(scene, "MapAreaMask");
            Tilemap constructionAreaMask =
                FindRequiredTilemap(scene, "ConstructionAreaMask");

            AuthoredMapData authoredMap =
                ValidateAuthoredMap(
                    mapVisuals,
                    mapAreaMask,
                    constructionAreaMask);

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
                    + "and fixed-footprint policy after finalization.");
            }

            Bounds expectedCameraBounds =
                CalculateNorthWorldBounds(
                    mapVisuals,
                    authoredMap.MapBounds);
            BoxCollider2D savedCameraBounds =
                FindRequiredGameObject(
                    scene,
                    "CameraBounds")
                .GetComponent<BoxCollider2D>();

            if (savedCameraBounds == null
                || !Approximately(
                    savedCameraBounds.bounds.center,
                    expectedCameraBounds.center)
                || !Approximately(
                    savedCameraBounds.bounds.size,
                    expectedCameraBounds.size))
            {
                throw new InvalidOperationException(
                    "Frank Roadside camera bounds did not retain the "
                    + "authored MapAreaMask framing after saving.");
            }

            if (!ApproximatelyInMapPlane(
                    cameraController.WorldCenter,
                    expectedCameraBounds.center))
            {
                throw new InvalidOperationException(
                    "Frank Roadside camera did not retain its centered "
                    + "starting view after saving.");
            }

            ValidateMarkerHost(
                markerHost,
                authoredMap);
        }


        private static AuthoredMapData ValidateAuthoredMap(
            Tilemap mapVisuals,
            Tilemap mapAreaMask,
            Tilemap constructionAreaMask)
        {
            if (mapVisuals.layoutGrid == null
                || mapVisuals.layoutGrid != mapAreaMask.layoutGrid
                || mapVisuals.layoutGrid
                    != constructionAreaMask.layoutGrid)
            {
                throw new InvalidOperationException(
                    "MapVIsuals, MapAreaMask, and ConstructionAreaMask "
                    + "must share the same Grid.");
            }

            HashSet<Vector3Int> visualCells =
                CollectOccupiedCells(mapVisuals);
            HashSet<Vector3Int> mapCells =
                CollectOccupiedCells(mapAreaMask);
            HashSet<Vector3Int> constructionCells =
                CollectOccupiedCells(constructionAreaMask);

            if (visualCells.Count == 0)
            {
                throw new InvalidOperationException(
                    "Frank Roadside MapVIsuals contains no authored art.");
            }

            BoundsInt mapBounds =
                CalculateOccupiedBounds(
                    "MapAreaMask",
                    mapCells);
            BoundsInt constructionBounds =
                CalculateOccupiedBounds(
                    "ConstructionAreaMask",
                    constructionCells);

            int expectedConstructionCells =
                ExpectedConstructionWidth
                * ExpectedConstructionHeight;

            if (constructionBounds.size.x
                    != ExpectedConstructionWidth
                || constructionBounds.size.y
                    != ExpectedConstructionHeight
                || constructionBounds.size.z != 1
                || constructionCells.Count
                    != expectedConstructionCells)
            {
                throw new InvalidOperationException(
                    "Frank Roadside ConstructionAreaMask must be one "
                    + $"complete {ExpectedConstructionWidth} x "
                    + $"{ExpectedConstructionHeight} footprint. Found "
                    + $"{constructionBounds.size.x} x "
                    + $"{constructionBounds.size.y} with "
                    + $"{constructionCells.Count} occupied cells.");
            }

            foreach (Vector3Int cell in constructionCells)
            {
                if (!mapCells.Contains(cell))
                {
                    throw new InvalidOperationException(
                        $"Construction cell {cell} is outside "
                        + "MapAreaMask.");
                }
            }

            return new AuthoredMapData(
                mapCells,
                constructionCells,
                mapBounds,
                constructionBounds);
        }


        private static HashSet<Vector3Int> CollectOccupiedCells(
            Tilemap tilemap)
        {
            HashSet<Vector3Int> cells =
                new HashSet<Vector3Int>();

            foreach (
                Vector3Int cell
                in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                if (cell.z != 0)
                {
                    throw new InvalidOperationException(
                        $"'{tilemap.name}' contains occupied cell {cell}; "
                        + "Frank Roadside authoring currently requires "
                        + "Unity cell Z 0.");
                }

                cells.Add(cell);
            }

            return cells;
        }


        private static BoundsInt CalculateOccupiedBounds(
            string layerName,
            HashSet<Vector3Int> cells)
        {
            if (cells.Count == 0)
            {
                throw new InvalidOperationException(
                    $"'{layerName}' contains no occupied cells.");
            }

            int minimumX = int.MaxValue;
            int minimumY = int.MaxValue;
            int maximumX = int.MinValue;
            int maximumY = int.MinValue;

            foreach (Vector3Int cell in cells)
            {
                minimumX = Mathf.Min(minimumX, cell.x);
                minimumY = Mathf.Min(minimumY, cell.y);
                maximumX = Mathf.Max(maximumX, cell.x);
                maximumY = Mathf.Max(maximumY, cell.y);
            }

            return new BoundsInt(
                minimumX,
                minimumY,
                0,
                (maximumX - minimumX) + 1,
                (maximumY - minimumY) + 1,
                1);
        }


        private static Bounds CalculateNorthWorldBounds(
            Tilemap coordinateTilemap,
            BoundsInt occupiedBounds)
        {
            int minimumX = occupiedBounds.xMin;
            int minimumY = occupiedBounds.yMin;
            int maximumX = occupiedBounds.xMax - 1;
            int maximumY = occupiedBounds.yMax - 1;

            Bounds bounds =
                new Bounds(
                    coordinateTilemap.GetCellCenterWorld(
                        new Vector3Int(
                            minimumX,
                            minimumY,
                            0)),
                    Vector3.zero);

            bounds.Encapsulate(
                coordinateTilemap.GetCellCenterWorld(
                    new Vector3Int(
                        maximumX,
                        minimumY,
                        0)));
            bounds.Encapsulate(
                coordinateTilemap.GetCellCenterWorld(
                    new Vector3Int(
                        minimumX,
                        maximumY,
                        0)));
            bounds.Encapsulate(
                coordinateTilemap.GetCellCenterWorld(
                    new Vector3Int(
                        maximumX,
                        maximumY,
                        0)));

            Grid layoutGrid = coordinateTilemap.layoutGrid;

            bounds.Expand(
                new Vector3(
                    Mathf.Abs(layoutGrid.cellSize.x),
                    Mathf.Abs(layoutGrid.cellSize.y),
                    0f));
            bounds.Expand(
                new Vector3(
                    CameraBoundsPadding * 2f,
                    CameraBoundsPadding * 2f,
                    0f));

            return bounds;
        }


        private static void ConfigureLocationMarkers(
            Scene scene,
            IsometricViewHost viewHost,
            Tilemap coordinateTilemap,
            AuthoredMapData authoredMap)
        {
            GameObject map =
                FindRequiredGameObject(
                    scene,
                    "Map");
            Transform markerRoot =
                FindOrCreateDirectChild(
                    map.transform,
                    "LocationMarkers");

            LocationMarkerHost markerHost =
                markerRoot.GetComponent<LocationMarkerHost>();

            if (markerHost == null)
            {
                markerHost =
                    markerRoot.gameObject
                        .AddComponent<LocationMarkerHost>();
            }

            SerializedObject hostData =
                new SerializedObject(markerHost);

            FindRequiredProperty(
                    hostData,
                    "viewHost")
                .objectReferenceValue = viewHost;
            FindRequiredProperty(
                    hostData,
                    "coordinateTilemap")
                .objectReferenceValue = coordinateTilemap;

            hostData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(markerHost);

            Transform storeMarkers =
                FindOrCreateDirectChild(
                    markerRoot,
                    "Store");
            Transform characterMarkers =
                FindOrCreateDirectChild(
                    markerRoot,
                    "Characters");
            Transform logisticsMarkers =
                FindOrCreateDirectChild(
                    markerRoot,
                    "Logistics");

            // The story group is intentionally empty until the prebuilt
            // store establishes exact cinematic staging positions.
            FindOrCreateDirectChild(
                markerRoot,
                "Story");

            Vector3Int footprintCenter =
                FindNearestCell(
                    authoredMap.ConstructionCells,
                    new Vector3Int(
                        authoredMap.ConstructionBounds.xMin
                            + ((ExpectedConstructionWidth - 1) / 2),
                        authoredMap.ConstructionBounds.yMin
                            + ((ExpectedConstructionHeight - 1) / 2),
                        0));

            Vector3Int roadsideArrival =
                FindNearestCell(
                    authoredMap.MapCells,
                    new Vector3Int(
                        footprintCenter.x,
                        authoredMap.ConstructionBounds.yMin - 6,
                        0));

            Vector3Int rearService =
                FindNearestCell(
                    authoredMap.ConstructionCells,
                    new Vector3Int(
                        authoredMap.ConstructionBounds.xMax - 4,
                        authoredMap.ConstructionBounds.yMax - 4,
                        0));

            ConfigureMarker(
                storeMarkers,
                "StoreFootprintCenter",
                StoreFootprintCenterMarkerId,
                footprintCenter,
                Vector3.zero,
                coordinateTilemap);
            ConfigureMarker(
                characterMarkers,
                "RoadsideArrival",
                RoadsideArrivalMarkerId,
                roadsideArrival,
                Vector3.zero,
                coordinateTilemap);
            ConfigureMarker(
                logisticsMarkers,
                "RearService",
                RearServiceMarkerId,
                rearService,
                Vector3.zero,
                coordinateTilemap);

            markerHost.RefreshWorldPositions();

            if (!markerHost.TryRebuildMarkerIndex(
                    out string validationFailure))
            {
                throw new InvalidOperationException(
                    validationFailure);
            }
        }


        private static void ValidateMarkerHost(
            LocationMarkerHost markerHost,
            AuthoredMapData authoredMap)
        {
            if (!markerHost.TryRebuildMarkerIndex(
                    out string validationFailure))
            {
                throw new InvalidOperationException(
                    validationFailure);
            }

            ValidateMarker(
                markerHost,
                StoreFootprintCenterMarkerId,
                authoredMap.ConstructionCells);
            ValidateMarker(
                markerHost,
                RoadsideArrivalMarkerId,
                authoredMap.MapCells);
            ValidateMarker(
                markerHost,
                RearServiceMarkerId,
                authoredMap.ConstructionCells);
        }


        private static void ValidateMarker(
            LocationMarkerHost markerHost,
            string markerId,
            HashSet<Vector3Int> allowedCells)
        {
            if (!markerHost.TryGetMarker(
                    markerId,
                    out LocationMarkerAuthoring marker))
            {
                throw new InvalidOperationException(
                    $"Frank Roadside is missing required location marker "
                    + $"'{markerId}'.");
            }

            if (!allowedCells.Contains(marker.LogicalCell))
            {
                throw new InvalidOperationException(
                    $"Location marker '{markerId}' uses logical cell "
                    + $"{marker.LogicalCell}, which is outside its "
                    + "required authored mask.");
            }
        }


        private static Transform FindOrCreateDirectChild(
            Transform parent,
            string objectName)
        {
            for (int index = 0;
                 index < parent.childCount;
                 index++)
            {
                Transform child = parent.GetChild(index);

                if (child.name == objectName)
                {
                    return child;
                }
            }

            GameObject created =
                new GameObject(objectName);

            created.transform.SetParent(
                parent,
                false);

            return created.transform;
        }


        private static void ConfigureMarker(
            Transform parent,
            string objectName,
            string markerId,
            Vector3Int logicalCell,
            Vector3 worldOffset,
            Tilemap coordinateTilemap)
        {
            Transform markerTransform =
                FindOrCreateDirectChild(
                    parent,
                    objectName);
            LocationMarkerAuthoring marker =
                markerTransform
                    .GetComponent<LocationMarkerAuthoring>();

            if (marker == null)
            {
                marker =
                    markerTransform.gameObject
                        .AddComponent<LocationMarkerAuthoring>();
            }

            SerializedObject markerData =
                new SerializedObject(marker);

            FindRequiredProperty(
                    markerData,
                    "markerId")
                .stringValue = markerId;
            FindRequiredProperty(
                    markerData,
                    "logicalCell")
                .vector3IntValue = logicalCell;
            FindRequiredProperty(
                    markerData,
                    "worldOffset")
                .vector3Value = worldOffset;

            markerData.ApplyModifiedPropertiesWithoutUndo();

            markerTransform.position =
                coordinateTilemap.GetCellCenterWorld(
                    logicalCell)
                + worldOffset;

            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(markerTransform);
        }


        private static Vector3Int FindNearestCell(
            HashSet<Vector3Int> cells,
            Vector3Int target)
        {
            bool hasBest = false;
            Vector3Int best = default;
            long bestDistance = long.MaxValue;

            foreach (Vector3Int cell in cells)
            {
                long deltaX = (long)cell.x - target.x;
                long deltaY = (long)cell.y - target.y;
                long distance =
                    (deltaX * deltaX)
                    + (deltaY * deltaY);

                if (!hasBest
                    || distance < bestDistance
                    || (distance == bestDistance
                        && CompareCells(cell, best) < 0))
                {
                    hasBest = true;
                    best = cell;
                    bestDistance = distance;
                }
            }

            if (!hasBest)
            {
                throw new InvalidOperationException(
                    "Cannot place a location marker because its authored "
                    + "mask contains no occupied cells.");
            }

            return best;
        }


        private static int CompareCells(
            Vector3Int left,
            Vector3Int right)
        {
            int xComparison = left.x.CompareTo(right.x);

            return xComparison != 0
                ? xComparison
                : left.y.CompareTo(right.y);
        }


        private static bool Approximately(
            Vector3 left,
            Vector3 right)
        {
            return Mathf.Abs(left.x - right.x) < 0.001f
                && Mathf.Abs(left.y - right.y) < 0.001f
                && Mathf.Abs(left.z - right.z) < 0.001f;
        }


        private static bool ApproximatelyInMapPlane(
            Vector3 left,
            Vector3 right)
        {
            return Mathf.Abs(left.x - right.x) < 0.001f
                && Mathf.Abs(left.y - right.y) < 0.001f;
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


        private static Tilemap FindRequiredTilemap(
            Scene scene,
            string objectName)
        {
            GameObject found =
                FindRequiredGameObject(
                    scene,
                    objectName);
            Tilemap tilemap =
                found.GetComponent<Tilemap>();

            if (tilemap == null)
            {
                throw new InvalidOperationException(
                    $"'{scene.path}' object '{objectName}' has no "
                    + "Tilemap component.");
            }

            return tilemap;
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


        private sealed class AuthoredMapData
        {
            public AuthoredMapData(
                HashSet<Vector3Int> mapCells,
                HashSet<Vector3Int> constructionCells,
                BoundsInt mapBounds,
                BoundsInt constructionBounds)
            {
                MapCells = mapCells;
                ConstructionCells = constructionCells;
                MapBounds = mapBounds;
                ConstructionBounds = constructionBounds;
            }


            public HashSet<Vector3Int> MapCells { get; }

            public HashSet<Vector3Int> ConstructionCells { get; }

            public BoundsInt MapBounds { get; }

            public BoundsInt ConstructionBounds { get; }
        }
    }
}
