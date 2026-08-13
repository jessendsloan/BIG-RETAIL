using BigRetail.Characters.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcAnimationWorkbenchTests
    {
        private const string BonePath = "Root/Pelvis";
        private const string RotationProperty = "localEulerAnglesRaw.z";

        [Test]
        public void CopyClipContents_CopiesCurvesAndClipSettings()
        {
            AnimationClip source = new AnimationClip
            {
                frameRate = 12f,
                wrapMode = WrapMode.Loop
            };
            AnimationClip destination = new AnimationClip();

            try
            {
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                    BonePath,
                    typeof(Transform),
                    RotationProperty);
                AnimationUtility.SetEditorCurve(
                    source,
                    binding,
                    AnimationCurve.Linear(0f, 0f, 1f, 30f));

                NpcAnimationWorkbenchClipUtility.CopyClipContents(source, destination);

                AnimationCurve copiedCurve = AnimationUtility.GetEditorCurve(destination, binding);
                Assert.That(copiedCurve, Is.Not.Null);
                Assert.That(copiedCurve.length, Is.EqualTo(2));
                Assert.That(copiedCurve.keys[1].value, Is.EqualTo(30f).Within(0.001f));
                Assert.That(destination.frameRate, Is.EqualTo(12f));
                Assert.That(destination.wrapMode, Is.EqualTo(WrapMode.Loop));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(destination);
            }
        }

        [Test]
        public void SetOrReplaceRotationKey_ReplacesKeyAtTheSameFrame()
        {
            AnimationClip clip = new AnimationClip
            {
                frameRate = 12f
            };

            try
            {
                NpcAnimationWorkbenchClipUtility.SetOrReplaceRotationKey(
                    clip,
                    BonePath,
                    0.5f,
                    10f);
                NpcAnimationWorkbenchClipUtility.SetOrReplaceRotationKey(
                    clip,
                    BonePath,
                    0.5f,
                    25f);

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                    BonePath,
                    typeof(Transform),
                    RotationProperty);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                Assert.That(curve, Is.Not.Null);
                Assert.That(curve.length, Is.EqualTo(1));
                Assert.That(curve.keys[0].time, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(curve.keys[0].value, Is.EqualTo(25f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }
    }
}
