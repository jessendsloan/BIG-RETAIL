using System.Collections.Generic;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEngine;

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


        private DoorAssemblyView CreatePresentedView(
            Vector3[] panelPositions)
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
                panelPositions);

            return view;
        }


        private void ApplyPresentation(
            DoorAssemblyView view,
            Vector3[] panelPositions)
        {
            Sprite sprite =
                CreateSprite();

            view.ApplyPresentation(
                new DoorAssemblySprites(
                    sprite,
                    sprite,
                    sprite,
                    sprite,
                    sprite),
                panelPositions,
                CalculateCenter(panelPositions),
                sortingLayerId: 0,
                sortingOrder: 0,
                rendererPriority: 0,
                sharedMaterial: null,
                tint: Color.white);
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
