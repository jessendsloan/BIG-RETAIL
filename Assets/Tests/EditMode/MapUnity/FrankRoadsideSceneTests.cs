using BigRetail.Map.Construction;
using BigRetail.Map.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FrankRoadsideSceneTests
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string MapId =
            "bigretail.map.frank_roadside";


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

                Assert.That(mapHost, Is.Not.Null);
                Assert.That(markerHost, Is.Not.Null);

                mapHost.Initialize();

                Assert.That(mapHost.IsInitialized, Is.True);
                Assert.That(mapHost.MapDefinition, Is.Not.Null);
                Assert.That(mapHost.MapDefinition.MapId, Is.EqualTo(MapId));
                Assert.That(
                    mapHost.MapDefinition.ValidCellCount,
                    Is.EqualTo(5502));
                Assert.That(
                    mapHost.ConstructionArea.EligibleCellCount,
                    Is.EqualTo(96 * 32));
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
                    "bigretail.marker.frank.roadside_arrival");
                AssertMarker(
                    markerHost,
                    "bigretail.marker.frank.rear_service");

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


        private static void AssertMarker(
            LocationMarkerHost markerHost,
            string markerId)
        {
            Assert.That(
                markerHost.TryGetMarker(
                    markerId,
                    out LocationMarkerAuthoring marker),
                Is.True,
                $"Missing location marker '{markerId}'.");
            Assert.That(marker, Is.Not.Null);
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


        private static T FindSceneComponent<T>(
            Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                T component =
                    roots[index].GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
