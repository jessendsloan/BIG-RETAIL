using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class DoorAssemblyViewTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();


        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1;
                 index >= 0;
                 index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }


        [Test]
        public void SetOpenProgressImmediately_MovesOnlyCenterPanelsOutward()
        {
            DoorAssemblyView view =
                CreatePresentedView(
                    CreatePanelPositions(0f));

            Transform leftGlass =
                view.transform.Find(
                    "Left Fixed Glass");

            Transform rightGlass =
                view.transform.Find(
                    "Right Fixed Glass");

            Transform frame =
                view.transform.Find(
                    "Static Door Frame");

            Vector3 leftGlassPosition =
                leftGlass.localPosition;

            Vector3 rightGlassPosition =
                rightGlass.localPosition;

            Vector3 framePosition =
                frame.localPosition;

            view.SetOpenProgressImmediately(
                1f);

            AssertPosition(
                view.LeftDoorTransform.localPosition,
                leftGlassPosition);

            AssertPosition(
                view.RightDoorTransform.localPosition,
                rightGlassPosition);

            AssertPosition(
                leftGlass.localPosition,
                leftGlassPosition);

            AssertPosition(
                rightGlass.localPosition,
                rightGlassPosition);

            AssertPosition(
                frame.localPosition,
                framePosition);
        }


        [Test]
        public void ApplyPresentation_WhilePartiallyOpen_PreservesEasedMotion()
        {
            DoorAssemblyView view =
                CreatePresentedView(
                    CreatePanelPositions(0f));

            view.SetOpenProgressImmediately(
                0.25f);

            Vector3[] rotatedPositions =
                CreatePanelPositions(10f);

            ApplyPresentation(
                view,
                rotatedPositions);

            Vector3 worldPosition =
                CalculateCenter(
                    rotatedPositions);

            Assert.That(
                view.OpenProgress,
                Is.EqualTo(0.25f));

            float easedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    0.25f);

            AssertPosition(
                view.LeftDoorTransform.localPosition,
                Vector3.Lerp(
                    rotatedPositions[1] - worldPosition,
                    rotatedPositions[0] - worldPosition,
                    easedProgress));

            AssertPosition(
                view.RightDoorTransform.localPosition,
                Vector3.Lerp(
                    rotatedPositions[2] - worldPosition,
                    rotatedPositions[3] - worldPosition,
                    easedProgress));
        }


        [TestCase(
            WallDisplaySlope.RisingLeft,
            DoorViewerSide.Outside,
            20,
            22,
            23,
            21)]
        [TestCase(
            WallDisplaySlope.RisingRight,
            DoorViewerSide.Outside,
            21,
            23,
            22,
            20)]
        [TestCase(
            WallDisplaySlope.RisingLeft,
            DoorViewerSide.Inside,
            22,
            20,
            21,
            23)]
        [TestCase(
            WallDisplaySlope.RisingRight,
            DoorViewerSide.Inside,
            23,
            21,
            20,
            22)]
        public void ApplyPresentation_OrdersSlidingPanelsForViewerSide(
            WallDisplaySlope displaySlope,
            DoorViewerSide viewerSide,
            int expectedLeftGlassPriority,
            int expectedLeftDoorPriority,
            int expectedRightDoorPriority,
            int expectedRightGlassPriority)
        {
            DoorAssemblyView view =
                CreatePresentedView(
                    CreatePanelPositions(0f),
                    displaySlope,
                    viewerSide,
                    rendererPriority: 20);

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Left Fixed Glass"),
                Is.EqualTo(expectedLeftGlassPriority));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Left Fixed Glass"),
                Is.EqualTo(
                    expectedLeftGlassPriority - 20));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Left Sliding Door"),
                Is.EqualTo(expectedLeftDoorPriority));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Left Sliding Door"),
                Is.EqualTo(
                    expectedLeftDoorPriority - 20));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Right Sliding Door"),
                Is.EqualTo(expectedRightDoorPriority));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Right Sliding Door"),
                Is.EqualTo(
                    expectedRightDoorPriority - 20));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Right Fixed Glass"),
                Is.EqualTo(expectedRightGlassPriority));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Right Fixed Glass"),
                Is.EqualTo(
                    expectedRightGlassPriority - 20));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Static Door Frame"),
                Is.EqualTo(24));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Static Door Frame"),
                Is.EqualTo(4));

            Assert.That(
                view.GetComponent<SortingGroup>(),
                Is.Not.Null);
        }


        [Test]
        public void ApplyPresentation_AfterRotation_ReappliesSlidingLayering()
        {
            DoorAssemblyView view =
                CreatePresentedView(
                    CreatePanelPositions(0f),
                    WallDisplaySlope.RisingLeft,
                    DoorViewerSide.Outside,
                    rendererPriority: 20);

            ApplyPresentation(
                view,
                CreatePanelPositions(10f),
                WallDisplaySlope.RisingRight,
                DoorViewerSide.Inside,
                rendererPriority: 20);

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Left Fixed Glass"),
                Is.EqualTo(23));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Left Fixed Glass"),
                Is.EqualTo(3));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Left Sliding Door"),
                Is.EqualTo(21));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Left Sliding Door"),
                Is.EqualTo(1));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Right Sliding Door"),
                Is.EqualTo(20));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Right Sliding Door"),
                Is.EqualTo(0));

            Assert.That(
                GetRendererPriority(
                    view.transform,
                    "Right Fixed Glass"),
                Is.EqualTo(22));

            Assert.That(
                GetRendererSortingOrder(
                    view.transform,
                    "Right Fixed Glass"),
                Is.EqualTo(2));
        }


        [Test]
        public void ViewerSideResolver_ChangesAcrossOppositeCameraViews()
        {
            IsometricMapFootprint footprint =
                new IsometricMapFootprint(
                    minimumX: 0,
                    minimumY: 0,
                    maximumX: 3,
                    maximumY: 3,
                    logicalLevel: 0);

            IsometricViewProjection northProjection =
                new IsometricViewProjection(
                    footprint,
                    IsometricViewOrientation.North);

            IsometricViewProjection southProjection =
                northProjection.WithOrientation(
                    IsometricViewOrientation.South);

            CellEdge supportingEdge =
                new CellEdge(
                    new GridPosition(1, 1, 0),
                    CellEdgeDirection.NorthEast);

            GridPosition insideCell =
                northProjection.GetViewerFacingCell(
                    supportingEdge);

            FoundationState foundationState =
                new FoundationState(
                    new[]
                    {
                        insideCell
                    });

            Assert.That(
                DoorViewerSideResolver.Resolve(
                    supportingEdge,
                    northProjection,
                    foundationState),
                Is.EqualTo(DoorViewerSide.Inside));

            Assert.That(
                DoorViewerSideResolver.Resolve(
                    supportingEdge,
                    southProjection,
                    foundationState),
                Is.EqualTo(DoorViewerSide.Outside));
        }


        [Test]
        public void OpenAndClose_UpdateTheRequestedTarget()
        {
            DoorAssemblyView view =
                CreatePresentedView(
                    CreatePanelPositions(0f));

            view.Open();

            Assert.That(
                view.TargetOpenProgress,
                Is.EqualTo(1f));

            Assert.That(
                view.IsAnimating,
                Is.True);

            view.SetOpenProgressImmediately(
                1f);

            view.Close();

            Assert.That(
                view.TargetOpenProgress,
                Is.EqualTo(0f));

            Assert.That(
                view.IsAnimating,
                Is.True);
        }


        [Test]
        public void HingedDoor_OpenSwitchesToPerpendicularPresentation()
        {
            GameObject gameObject =
                new GameObject("Hinged Door View Test");

            createdObjects.Add(
                gameObject);

            DoorAssemblyView view =
                gameObject.AddComponent<DoorAssemblyView>();

            view.Initialize(
                new DoorAssemblyId("hinged-door-view-test"));

            Sprite closedSprite =
                CreateSprite();

            Sprite openSprite =
                CreateSprite();

            Vector3 closedWorldPosition =
                new Vector3(4f, 3f, 0f);

            Vector3 openWorldPosition =
                new Vector3(2f, 2f, 0f);

            view.ApplyHingedPresentation(
                new HingedDoorSprites(
                    closedSprite,
                    closedSprite),
                openSprite,
                closedWorldPosition,
                openWorldPosition,
                sortingLayerId: 0,
                closedSortingOrder: 10,
                openSortingOrder: 20,
                closedRendererPriority: 30,
                openRendererPriority: 40,
                sharedMaterial: null,
                tint: Color.white);

            Transform door =
                view.HingedDoorTransform;

            SpriteRenderer doorRenderer =
                door.GetComponent<SpriteRenderer>();

            Transform frame =
                view.transform.Find(
                    "Static Hinged Door Frame");

            Vector3 framePosition =
                frame.localPosition;

            view.Open();

            Assert.That(
                doorRenderer.sprite,
                Is.SameAs(openSprite));

            AssertPosition(
                door.localPosition,
                openWorldPosition
                - closedWorldPosition);

            Assert.That(door.localScale, Is.EqualTo(Vector3.one));
            Assert.That(doorRenderer.sortingOrder, Is.EqualTo(20));
            Assert.That(doorRenderer.rendererPriority, Is.EqualTo(40));
            Assert.That(view.IsAnimating, Is.False);

            AssertPosition(
                frame.localPosition,
                framePosition);

            Assert.That(
                frame.localScale,
                Is.EqualTo(Vector3.one));

            view.Close();

            Assert.That(
                doorRenderer.sprite,
                Is.SameAs(closedSprite));

            AssertPosition(
                door.localPosition,
                Vector3.zero);

            Assert.That(doorRenderer.sortingOrder, Is.EqualTo(10));
            Assert.That(doorRenderer.rendererPriority, Is.EqualTo(30));
        }


        [Test]
        public void StaticDoorway_RemainsPermanentlyOpen()
        {
            GameObject gameObject =
                new GameObject("Static Doorway View Test");

            createdObjects.Add(
                gameObject);

            DoorAssemblyView view =
                gameObject.AddComponent<DoorAssemblyView>();

            view.Initialize(
                new DoorAssemblyId("static-doorway-view-test"));

            Sprite frameSprite =
                CreateSprite();

            Vector3 worldPosition =
                new Vector3(4f, 3f, 0f);

            view.ApplyDoorwayPresentation(
                frameSprite,
                worldPosition,
                sortingLayerId: 0,
                sortingOrder: 10,
                rendererPriority: 20,
                sharedMaterial: null,
                tint: Color.white);

            Transform frame =
                view.DoorwayFrameTransform;

            Assert.That(frame, Is.Not.Null);
            AssertPosition(view.transform.position, worldPosition);
            Assert.That(view.OpenProgress, Is.EqualTo(1f));
            Assert.That(view.TargetOpenProgress, Is.EqualTo(1f));
            Assert.That(view.IsAnimating, Is.False);

            view.Close();

            Assert.That(view.OpenProgress, Is.EqualTo(1f));
            Assert.That(view.TargetOpenProgress, Is.EqualTo(1f));
            Assert.That(view.IsAnimating, Is.False);
            Assert.That(frame.gameObject.activeSelf, Is.True);
        }


        [TestCase(
            CellEdgeDirection.NorthEast,
            CellEdgeDirection.NorthWest)]
        [TestCase(
            CellEdgeDirection.NorthWest,
            CellEdgeDirection.NorthEast)]
        public void HingedDoorSwingResolver_UsesStableLogicalHinge(
            CellEdgeDirection closedDirection,
            CellEdgeDirection expectedOpenDirection)
        {
            CellEdge closedEdge =
                new CellEdge(
                    new GridPosition(3, 3),
                    closedDirection);

            CellEdge openEdge =
                HingedDoorSwingResolver.ResolveOpenLogicalEdge(
                    closedEdge);

            Assert.That(
                openEdge.CanonicalDirection,
                Is.EqualTo(expectedOpenDirection));

            Assert.That(
                openEdge.FirstVertex,
                Is.EqualTo(closedEdge.SecondVertex));

            Assert.That(
                openEdge.SecondVertex,
                Is.Not.EqualTo(closedEdge.SecondVertex));
        }


        [TestCase(
            IsometricViewOrientation.North,
            4,
            3,
            CellEdgeDirection.NorthWest)]
        [TestCase(
            IsometricViewOrientation.East,
            3,
            5,
            CellEdgeDirection.NorthEast)]
        [TestCase(
            IsometricViewOrientation.South,
            5,
            5,
            CellEdgeDirection.NorthWest)]
        [TestCase(
            IsometricViewOrientation.West,
            5,
            4,
            CellEdgeDirection.NorthEast)]
        public void HingedDoorSwing_ProjectsWithCameraRotation(
            IsometricViewOrientation orientation,
            int expectedAnchorX,
            int expectedAnchorY,
            CellEdgeDirection expectedDirection)
        {
            CellEdge closedLogicalEdge =
                new CellEdge(
                    new GridPosition(3, 3),
                    CellEdgeDirection.NorthEast);

            CellEdge openLogicalEdge =
                HingedDoorSwingResolver.ResolveOpenLogicalEdge(
                    closedLogicalEdge);

            IsometricViewProjection projection =
                new IsometricViewProjection(
                    new IsometricMapFootprint(
                        minimumX: 0,
                        minimumY: 0,
                        maximumX: 9,
                        maximumY: 9,
                        logicalLevel: 0),
                    orientation);

            CellEdge openDisplayEdge =
                projection.ToDisplayEdge(
                    openLogicalEdge);

            Assert.That(
                openDisplayEdge.AnchorCell,
                Is.EqualTo(
                    new GridPosition(
                        expectedAnchorX,
                        expectedAnchorY)));

            Assert.That(
                openDisplayEdge.CanonicalDirection,
                Is.EqualTo(expectedDirection));
        }


        [TestCase(WallPresentationHeight.Full)]
        [TestCase(WallPresentationHeight.Low)]
        public void WallViewSystem_KeepsLayeredDoorAtAnyWallHeight(
            WallPresentationHeight wallHeight)
        {
            DoorDefinitionAssetCatalog assetCatalog =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    DoorDefinitionAssetCatalog>(
                    "Assets/Design/Doors/DoorDefinitionCatalog.asset");

            GameObject wallPrefab =
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Walls/Wall Segment View.prefab");

            Assert.That(
                assetCatalog,
                Is.Not.Null);

            Assert.That(
                wallPrefab,
                Is.Not.Null);

            DoorAssemblyId assemblyId =
                new DoorAssemblyId(
                    "wall-visibility-door");

            CellEdge[] edges =
                CreateDoorEdges();

            DoorAssemblyState assemblyState =
                new DoorAssemblyState();

            WallState wallState =
                new WallState(
                    edges);

            using (
                DoorConstructionService constructionService =
                    new DoorConstructionService(
                        assetCatalog.CreateDomainCatalog(),
                        assemblyState,
                        wallState))
            {
                DoorAssemblyChangeResult placement =
                    constructionService.TryPlaceAssembly(
                        assemblyId,
                        assetCatalog.DefaultDefinition.Id,
                        edges);

                Assert.That(
                    placement.Succeeded,
                    Is.True);
            }

            Assert.That(
                assemblyState.TryGetAssembly(
                    assemblyId,
                    out DoorAssembly assembly),
                Is.True);

            GameObject systemObject =
                new GameObject(
                    "Wall View System Test");

            systemObject.SetActive(
                false);

            createdObjects.Add(
                systemObject);

            WallViewSystem system =
                systemObject.AddComponent<WallViewSystem>();

            SetPrivateField(
                system,
                "wallViewParent",
                system.transform);

            SetPrivateField(
                system,
                "subscribedDoorAssemblyState",
                assemblyState);

            SetPrivateField(
                system,
                "doorResolver",
                new DoorPresentationResolver(
                    assemblyState,
                    assetCatalog));

            Dictionary<CellEdge, WallSegmentView> wallViews =
                GetPrivateField<
                    Dictionary<CellEdge, WallSegmentView>>(
                    system,
                    "wallViews");

            for (int index = 0;
                 index < edges.Length;
                 index++)
            {
                wallViews.Add(
                    edges[index],
                    CreateSupportingWallView(
                        wallPrefab,
                        edges[index],
                        index,
                        wallHeight));
            }

            InvokePrivate(
                system,
                "SynchronizeDoorAssemblyView",
                assembly);

            Dictionary<DoorAssemblyId, DoorAssemblyView> doorViews =
                GetPrivateField<
                    Dictionary<DoorAssemblyId, DoorAssemblyView>>(
                    system,
                    "doorAssemblyViews");

            Assert.That(
                doorViews.ContainsKey(
                    assemblyId),
                Is.True);

            Assert.That(
                doorViews[assemblyId].transform.childCount,
                Is.EqualTo(5));

            Vector3 expectedApertureWorldPosition =
                doorViews[assemblyId].transform.position;

            for (int index = 0;
                 index < edges.Length;
                 index++)
            {
                WallSegmentView wallView =
                    wallViews[edges[index]];

                Assert.That(
                    GetPrivateField<bool>(
                        wallView,
                        "hasApertureAssemblyWorldPosition"),
                    Is.True);

                AssertPosition(
                    GetPrivateField<Vector3>(
                        wallView,
                        "apertureAssemblyWorldPosition"),
                    expectedApertureWorldPosition);
            }
        }


        private DoorAssemblyView CreatePresentedView(
            Vector3[] panelPositions,
            WallDisplaySlope displaySlope =
                WallDisplaySlope.RisingLeft,
            DoorViewerSide viewerSide =
                DoorViewerSide.Outside,
            int rendererPriority = 0)
        {
            GameObject gameObject =
                new GameObject("Door Assembly View Test");

            createdObjects.Add(
                gameObject);

            DoorAssemblyView view =
                gameObject.AddComponent<DoorAssemblyView>();

            view.Initialize(
                new DoorAssemblyId("door-view-test"));

            ApplyPresentation(
                view,
                panelPositions,
                displaySlope,
                viewerSide,
                rendererPriority);

            return view;
        }


        private void ApplyPresentation(
            DoorAssemblyView view,
            Vector3[] panelPositions,
            WallDisplaySlope displaySlope =
                WallDisplaySlope.RisingLeft,
            DoorViewerSide viewerSide =
                DoorViewerSide.Outside,
            int rendererPriority = 0)
        {
            Sprite sprite =
                CreateSprite();

            view.ApplyPresentation(
                new DoorAssemblySprites(
                    sprite,
                    sprite,
                    sprite,
                    sprite,
                    sprite,
                    sprite),
                displaySlope,
                viewerSide,
                panelPositions,
                CalculateCenter(panelPositions),
                sortingLayerId: 0,
                sortingOrder: 0,
                rendererPriority: rendererPriority,
                sharedMaterial: null,
                tint: Color.white);
        }


        private static int GetRendererPriority(
            Transform parent,
            string childName)
        {
            Transform child =
                parent.Find(
                    childName);

            Assert.That(
                child,
                Is.Not.Null);

            return child
                .GetComponent<SpriteRenderer>()
                .rendererPriority;
        }


        private static int GetRendererSortingOrder(
            Transform parent,
            string childName)
        {
            Transform child =
                parent.Find(
                    childName);

            Assert.That(
                child,
                Is.Not.Null);

            return child
                .GetComponent<SpriteRenderer>()
                .sortingOrder;
        }


        private Sprite CreateSprite()
        {
            Texture2D texture =
                new Texture2D(1, 1);

            texture.SetPixel(
                0,
                0,
                Color.white);

            texture.Apply();

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));

            createdObjects.Add(texture);
            createdObjects.Add(sprite);

            return sprite;
        }


        private WallSegmentView CreateSupportingWallView(
            GameObject wallPrefab,
            CellEdge edge,
            int index,
            WallPresentationHeight wallHeight)
        {
            GameObject wallObject =
                UnityEngine.Object.Instantiate(
                    wallPrefab);

            wallObject.SetActive(
                false);

            createdObjects.Add(
                wallObject);

            WallSegmentView view =
                wallObject.AddComponent<WallSegmentView>();

            SpriteRenderer renderer =
                wallObject.GetComponent<SpriteRenderer>();

            SortingGroup sortingGroup =
                wallObject.GetComponent<SortingGroup>();

            Assert.That(
                view,
                Is.Not.Null);

            Assert.That(
                renderer,
                Is.Not.Null);

            Assert.That(
                sortingGroup,
                Is.Not.Null);

            renderer.sortingOrder =
                index;

            renderer.rendererPriority =
                index;

            sortingGroup.sortingOrder =
                index;

            wallObject.transform.position =
                new Vector3(
                    index,
                    -index,
                    0f);

            SetAutoProperty(
                view,
                "Edge",
                edge);

            SetAutoProperty(
                view,
                "CurrentDisplaySlope",
                WallDisplaySlope.RisingLeft);

            SetAutoProperty(
                view,
                "CurrentHeight",
                wallHeight);

            return view;
        }


        private static CellEdge[] CreateDoorEdges()
        {
            CellEdge[] edges =
                new CellEdge[DoorAssemblyView.RequiredPanelCount];

            for (int index = 0;
                 index < edges.Length;
                 index++)
            {
                edges[index] =
                    new CellEdge(
                        new GridPosition(2, 2 + index),
                        CellEdgeDirection.NorthEast);
            }

            return edges;
        }


        private static T GetPrivateField<T>(
            object target,
            string fieldName)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing private field '{fieldName}'.");

            return (T)field.GetValue(
                target);
        }


        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing private field '{fieldName}'.");

            field.SetValue(
                target,
                value);
        }


        private static void SetAutoProperty(
            object target,
            string propertyName,
            object value)
        {
            SetPrivateField(
                target,
                $"<{propertyName}>k__BackingField",
                value);
        }


        private static void InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"Missing private method '{methodName}'.");

            method.Invoke(
                target,
                arguments);
        }


        private static Vector3[] CreatePanelPositions(
            float offset)
        {
            return new[]
            {
                new Vector3(offset - 3f, 3f, 0f),
                new Vector3(offset - 1f, 1f, 0f),
                new Vector3(offset + 1f, -1f, 0f),
                new Vector3(offset + 3f, -3f, 0f)
            };
        }


        private static Vector3 CalculateCenter(
            Vector3[] positions)
        {
            Vector3 center =
                Vector3.zero;

            for (int index = 0;
                 index < positions.Length;
                 index++)
            {
                center +=
                    positions[index];
            }

            return center
                / positions.Length;
        }


        private static void AssertPosition(
            Vector3 actual,
            Vector3 expected)
        {
            Assert.That(
                Vector3.Distance(
                    actual,
                    expected),
                Is.LessThan(0.0001f));
        }
    }
}
