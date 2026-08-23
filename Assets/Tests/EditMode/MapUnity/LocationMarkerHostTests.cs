using BigRetail.Map.Unity;
using BigRetail.Map.Unity.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class LocationMarkerHostTests
    {
        [Test]
        public void StableMarkerIdResolvesAuthoredMarker()
        {
            GameObject root = null;

            try
            {
                LocationMarkerHost host =
                    CreateHost(out root);
                LocationMarkerAuthoring expected =
                    CreateMarker(
                        root.transform,
                        "Arrival",
                        "  bigretail.marker.test.arrival  ");

                Assert.That(
                    host.TryRebuildMarkerIndex(out string failure),
                    Is.True,
                    failure);
                Assert.That(
                    host.TryGetMarker(
                        "bigretail.marker.test.arrival",
                        out LocationMarkerAuthoring resolved),
                    Is.True);
                Assert.That(resolved, Is.SameAs(expected));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        [Test]
        public void DuplicateMarkerIdsAreRejected()
        {
            GameObject root = null;

            try
            {
                LocationMarkerHost host =
                    CreateHost(out root);

                CreateMarker(
                    root.transform,
                    "First",
                    "bigretail.marker.test.duplicate");
                CreateMarker(
                    root.transform,
                    "Second",
                    "bigretail.marker.test.duplicate");

                Assert.That(
                    host.TryRebuildMarkerIndex(out string failure),
                    Is.False);
                StringAssert.Contains(
                    "duplicated",
                    failure);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        [Test]
        public void BlankMarkerIdIsRejected()
        {
            GameObject root = null;

            try
            {
                LocationMarkerHost host =
                    CreateHost(out root);

                CreateMarker(
                    root.transform,
                    "MissingId",
                    "   ");

                Assert.That(
                    host.TryRebuildMarkerIndex(out string failure),
                    Is.False);
                StringAssert.Contains(
                    "no stable marker ID",
                    failure);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        private static LocationMarkerHost CreateHost(
            out GameObject root)
        {
            root = new GameObject("LocationMarkers");

            GameObject gridObject =
                new GameObject(
                    "Grid",
                    typeof(Grid));
            gridObject.transform.SetParent(
                root.transform,
                false);

            GameObject tilemapObject =
                new GameObject(
                    "CoordinateTilemap",
                    typeof(Tilemap),
                    typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(
                gridObject.transform,
                false);

            IsometricViewHost viewHost =
                root.AddComponent<IsometricViewHost>();
            LocationMarkerHost markerHost =
                root.AddComponent<LocationMarkerHost>();

            SerializedObject hostData =
                new SerializedObject(markerHost);

            hostData.FindProperty("viewHost")
                .objectReferenceValue = viewHost;
            hostData.FindProperty("coordinateTilemap")
                .objectReferenceValue =
                    tilemapObject.GetComponent<Tilemap>();
            hostData.ApplyModifiedPropertiesWithoutUndo();

            return markerHost;
        }


        private static LocationMarkerAuthoring CreateMarker(
            Transform parent,
            string objectName,
            string markerId)
        {
            GameObject markerObject =
                new GameObject(objectName);
            markerObject.transform.SetParent(
                parent,
                false);

            LocationMarkerAuthoring marker =
                markerObject.AddComponent<LocationMarkerAuthoring>();
            SerializedObject markerData =
                new SerializedObject(marker);

            markerData.FindProperty("markerId")
                .stringValue = markerId;
            markerData.ApplyModifiedPropertiesWithoutUndo();

            return marker;
        }
    }
}
