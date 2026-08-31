using BigRetail.Map.Construction;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Navigation;
using BigRetail.Map.Unity.Sidewalks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FrankRoadsideSceneTests
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string MapId =
            "bigretail.map.frank_roadside";

        private const string RoadsideSurfaceTilePath =
            "Assets/Art/GroundTileArt/Brick/"
            + "groundtile_brick_2_0.asset";


        [Test]
        public void FrankRoadside_ComposesFixedFootprintAndStableMarkers()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene =
                    EditorSceneManager.OpenScene(
                        ScenePath,
                        OpenSceneMode.Single);
                GridMapHost mapHost =
                    FindSceneComponent<GridMapHost>(scene);
                LocationMarkerHost markerHost =
                    FindSceneComponent<LocationMarkerHost>(scene);
                GridNavigationSurfaceHost navigationHost =
                    FindSceneComponent<GridNavigationSurfaceHost>(scene);
                Tilemap foundationViews =
                    FindSceneComponent<Tilemap>(scene, "FoundationViews");

                Assert.That(mapHost, Is.Not.Null);
                Assert.That(markerHost, Is.Not.Null);
                Assert.That(navigationHost, Is.Not.Null);
                Assert.That(foundationViews, Is.Not.Null);
                Assert.That(foundationViews.GetUsedTilesCount(), Is.Zero);
                AssertNoComponentTypeOnObject(
                    scene,
                    "EquipmentCatalogWorkspaceUI",
                    "BigRetail.Purchasing.Unity.UI."
                    + "PurchasingWorkspaceDocumentHost");

                mapHost.Initialize();

                Assert.That(mapHost.IsInitialized, Is.True);
                Assert.That(mapHost.MapDefinition, Is.Not.Null);
                Assert.That(mapHost.MapDefinition.MapId, Is.EqualTo(MapId));
                Assert.That(
                    mapHost.MapDefinition.ValidCellCount,
                    Is.EqualTo(5502));
                Assert.That(
                    mapHost.ConstructionArea.EligibleCellCount,
                    Is.EqualTo(96 * 47));
                Assert.That(
                    mapHost.ConstructionArea.IsEligible(
                        new BigRetail.Map.Domain.GridPosition(
                            -67,
                            13,
                            0)),
                    Is.True);
                Assert.That(
                    mapHost.ConstructionArea.IsEligible(
                        new BigRetail.Map.Domain.GridPosition(
                            28,
                            59,
                            0)),
                    Is.True);
                Assert.That(
                    mapHost.ConstructionArea.IsEligible(
                        new BigRetail.Map.Domain.GridPosition(
                            -68,
                            13,
                            0)),
                    Is.False);
                Assert.That(
                    mapHost.LandPolicy.Kind,
                    Is.EqualTo(LocationLandPolicyKind.FixedFootprint));
                Assert.That(mapHost.LandRegions, Is.Null);
                Assert.That(mapHost.LandRegionOwnership, Is.Null);
                Assert.That(mapHost.LandRegionPurchases, Is.Null);
                Assert.That(mapHost.MapFingerprint, Is.Not.Empty);

                Assert.That(
                    markerHost.TryRebuildMarkerIndex(
                        out string markerFailure),
                    Is.True,
                    markerFailure);

                AssertMarker(
                    markerHost,
                    "bigretail.marker.frank.store_footprint_center");
                AssertMarker(
                    markerHost,
                    "bigretail.marker.frank.roadside_arrival",
                    new Vector3Int(12, 52, 0));
                AssertMarker(
                    markerHost,
                    "bigretail.marker.frank.rear_service");

                AssertRoadsideSurfaceTiles(scene);

                AssertHasComponentType(
                    scene,
                    "BigRetail.Purchasing.Unity."
                    + "FixtureEquipmentRuntimeHost");
                AssertHasComponentType(
                    scene,
                    "BigRetail.Purchasing.Unity."
                    + "FixtureEquipmentPlanViewSystem");
                AssertHasComponentType(
                    scene,
                    "BigRetail.Purchasing.Unity."
                    + "FixtureEquipmentDeliveryViewSystem");
                AssertHasComponentType(
                    scene,
                    "BigRetail.Purchasing.Unity.UI."
                    + "EquipmentCatalogWorkspacePresenter");

                AssertNoMissingScripts(scene);
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }


        private static void AssertRoadsideSurfaceTiles(Scene scene)
        {
            TileBase expectedTile =
                AssetDatabase.LoadAssetAtPath<TileBase>(
                    RoadsideSurfaceTilePath);

            Assert.That(expectedTile, Is.Not.Null);
            AssertSerializedTile(
                FindSceneComponent<
                    FoundationApronTilemapViewSystem>(scene),
                "apronTile",
                expectedTile);
            AssertSerializedTile(
                FindSceneComponent<SidewalkTilemapViewSystem>(scene),
                "sidewalkTile",
                expectedTile);
            AssertSerializedTile(
                FindSceneMonoBehaviour(
                    scene,
                    "BigRetail.Construction.Unity.Foundations."
                    + "FoundationAreaPreviewView"),
                "previewApronTile",
                expectedTile);
            AssertSerializedTile(
                FindSceneMonoBehaviour(
                    scene,
                    "BigRetail.Construction.Unity.Sidewalks."
                    + "SidewalkAreaPreviewView"),
                "previewTile",
                expectedTile);
            AssertSerializedTile(
                FindSceneMonoBehaviour(
                    scene,
                    "BigRetail.Construction.Unity.Sidewalks."
                    + "SidewalkDemolitionAreaPreviewView"),
                "previewTile",
                expectedTile);
        }


        private static void AssertSerializedTile(
            Component component,
            string propertyName,
            TileBase expectedTile)
        {
            Assert.That(component, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            Assert.That(property, Is.Not.Null);
            Assert.That(
                property.objectReferenceValue,
                Is.SameAs(expectedTile));
        }


        private static MonoBehaviour FindSceneMonoBehaviour(
            Scene scene,
            string componentTypeName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                MonoBehaviour[] components =
                    roots[rootIndex]
                        .GetComponentsInChildren<MonoBehaviour>(true);

                for (int index = 0;
                     index < components.Length;
                     index++)
                {
                    MonoBehaviour component = components[index];

                    if (component != null
                        && component.GetType().FullName
                            == componentTypeName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }


        private static void AssertMarker(
            LocationMarkerHost markerHost,
            string markerId,
            Vector3Int? expectedCell = null)
        {
            Assert.That(
                markerHost.TryGetMarker(
                    markerId,
                    out LocationMarkerAuthoring marker),
                Is.True,
                $"Missing location marker '{markerId}'.");
            Assert.That(marker, Is.Not.Null);

            if (expectedCell.HasValue)
            {
                Assert.That(
                    marker.LogicalCell,
                    Is.EqualTo(expectedCell.Value));
            }
        }


        private static void AssertNoMissingScripts(
            Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex]
                        .GetComponentsInChildren<Transform>(true);

                for (int index = 0;
                     index < transforms.Length;
                     index++)
                {
                    GameObject gameObject =
                        transforms[index].gameObject;

                    Assert.That(
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                gameObject),
                        Is.Zero,
                        $"'{gameObject.name}' contains a missing script.");
                }
            }
        }


        private static void AssertHasComponentType(
            Scene scene,
            string componentTypeName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                MonoBehaviour[] components =
                    roots[rootIndex]
                        .GetComponentsInChildren<MonoBehaviour>(true);

                for (int index = 0;
                     index < components.Length;
                     index++)
                {
                    MonoBehaviour component = components[index];

                    if (component != null
                        && component.GetType().FullName
                            == componentTypeName)
                    {
                        return;
                    }
                }
            }

            Assert.Fail(
                $"Frank Roadside is missing required component "
                + $"'{componentTypeName}'.");
        }


        private static void AssertNoComponentTypeOnObject(
            Scene scene,
            string objectName,
            string componentTypeName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                MonoBehaviour[] components =
                    roots[rootIndex]
                        .GetComponentsInChildren<MonoBehaviour>(true);

                for (int index = 0;
                     index < components.Length;
                     index++)
                {
                    MonoBehaviour component = components[index];

                    Assert.That(
                        component == null
                        || component.gameObject.name != objectName
                        || component.GetType().FullName
                            != componentTypeName,
                        $"'{objectName}' must not contain "
                        + $"'{componentTypeName}'.");
                }
            }
        }


        private static T FindSceneComponent<T>(
            Scene scene,
            string objectName = null)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                T[] components =
                    roots[index].GetComponentsInChildren<T>(true);

                for (int componentIndex = 0;
                     componentIndex < components.Length;
                     componentIndex++)
                {
                    T component = components[componentIndex];

                    if (string.IsNullOrEmpty(objectName)
                        || component.gameObject.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }
    }
}
