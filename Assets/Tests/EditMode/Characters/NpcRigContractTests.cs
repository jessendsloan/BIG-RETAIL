using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    /// <summary>
    /// Locks the first NPC rig contract before final artwork and
    /// animation are introduced.
    /// </summary>
    public sealed class NpcRigContractTests
    {
        [Test]
        public void CanonicalRig_HasTwentyUniqueBones()
        {
            HashSet<NpcRigBoneId> boneIds =
                new HashSet<NpcRigBoneId>();

            foreach (NpcRigBoneDefinition definition
                     in NpcRigDefinition.BoneDefinitions)
            {
                Assert.That(
                    boneIds.Add(definition.Id),
                    Is.True,
                    $"Duplicate bone: {definition.Id}");
            }

            Assert.That(
                boneIds.Count,
                Is.EqualTo(
                    NpcRigDefinition.ExpectedBoneCount));
        }

        [Test]
        public void DepthTerminologyMigration_PreservesSerializedEnumValues()
        {
            Assert.That((int)NpcRigBoneId.ShoulderForeground, Is.EqualTo(6));
            Assert.That((int)NpcRigBoneId.HandForeground, Is.EqualTo(9));
            Assert.That((int)NpcRigBoneId.ShoulderBackground, Is.EqualTo(10));
            Assert.That((int)NpcRigBoneId.HandBackground, Is.EqualTo(13));
            Assert.That((int)NpcRigBoneId.ThighForeground, Is.EqualTo(14));
            Assert.That((int)NpcRigBoneId.FootForeground, Is.EqualTo(16));
            Assert.That((int)NpcRigBoneId.ThighBackground, Is.EqualTo(17));
            Assert.That((int)NpcRigBoneId.FootBackground, Is.EqualTo(19));

            Assert.That((int)NpcRigPartId.UpperArmForeground, Is.EqualTo(1));
            Assert.That((int)NpcRigPartId.FootForeground, Is.EqualTo(6));
            Assert.That((int)NpcRigPartId.ThighBackground, Is.EqualTo(12));
            Assert.That((int)NpcRigPartId.HandBackground, Is.EqualTo(17));
        }

        [Test]
        public void CanonicalRig_UsesForegroundBackgroundDepthTerminology()
        {
            foreach (NpcRigBoneId id in Enum.GetValues(typeof(NpcRigBoneId)))
            {
                Assert.That(id.ToString(), Does.Not.Contain("Near"));
                Assert.That(id.ToString(), Does.Not.Contain("Far"));
            }

            foreach (NpcRigPartId id in Enum.GetValues(typeof(NpcRigPartId)))
            {
                Assert.That(id.ToString(), Does.Not.Contain("Near"));
                Assert.That(id.ToString(), Does.Not.Contain("Far"));
            }
        }

        [Test]
        public void CanonicalRig_HasOneRootAndValidParentOrder()
        {
            HashSet<NpcRigBoneId> createdBones =
                new HashSet<NpcRigBoneId>();

            int rootCount = 0;

            foreach (NpcRigBoneDefinition definition
                     in NpcRigDefinition.BoneDefinitions)
            {
                if (!definition.HasParent)
                {
                    rootCount++;

                    Assert.That(
                        definition.Id,
                        Is.EqualTo(
                            NpcRigBoneId.Root));
                }
                else
                {
                    Assert.That(
                        createdBones.Contains(
                            definition.ParentId),
                        Is.True,
                        $"{definition.Id} appears before parent " +
                        $"{definition.ParentId}.");
                }

                createdBones.Add(
                    definition.Id);
            }

            Assert.That(
                rootCount,
                Is.EqualTo(1));
        }

        [Test]
        public void CanonicalRig_HasEighteenUniquePartsOnValidBones()
        {
            HashSet<NpcRigBoneId> boneIds =
                new HashSet<NpcRigBoneId>();

            foreach (NpcRigBoneDefinition definition
                     in NpcRigDefinition.BoneDefinitions)
            {
                boneIds.Add(
                    definition.Id);
            }

            HashSet<NpcRigPartId> partIds =
                new HashSet<NpcRigPartId>();

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                Assert.That(
                    partIds.Add(definition.Id),
                    Is.True,
                    $"Duplicate part: {definition.Id}");

                Assert.That(
                    boneIds.Contains(
                        definition.BoneId),
                    Is.True,
                    $"{definition.Id} targets missing bone " +
                    $"{definition.BoneId}.");
            }

            Assert.That(
                partIds.Count,
                Is.EqualTo(
                    NpcRigDefinition.ExpectedPartCount));
        }

        [Test]
        public void TwoAuthoredDirections_ProduceFourFacings()
        {
            AssertPresentation(
                NpcFacing.SouthEast,
                NpcAuthoredDirection.SouthEast,
                false,
                NpcCameraSide.CameraLeft);
            AssertPresentation(
                NpcFacing.SouthWest,
                NpcAuthoredDirection.SouthEast,
                true,
                NpcCameraSide.CameraRight);
            AssertPresentation(
                NpcFacing.NorthEast,
                NpcAuthoredDirection.NorthEast,
                false,
                NpcCameraSide.CameraRight);
            AssertPresentation(
                NpcFacing.NorthWest,
                NpcAuthoredDirection.NorthEast,
                true,
                NpcCameraSide.CameraLeft);

            Assert.That(
                NpcFacingUtility.GetAuthoredDirection(
                    NpcFacing.SouthEast),
                Is.EqualTo(
                    NpcAuthoredDirection.SouthEast));

            Assert.That(
                NpcFacingUtility.GetAuthoredDirection(
                    NpcFacing.SouthWest),
                Is.EqualTo(
                    NpcAuthoredDirection.SouthEast));

            Assert.That(
                NpcFacingUtility.GetAuthoredDirection(
                    NpcFacing.NorthEast),
                Is.EqualTo(
                    NpcAuthoredDirection.NorthEast));

            Assert.That(
                NpcFacingUtility.GetAuthoredDirection(
                    NpcFacing.NorthWest),
                Is.EqualTo(
                    NpcAuthoredDirection.NorthEast));

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.SouthEast),
                Is.False);

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.NorthEast),
                Is.False);

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.SouthWest),
                Is.True);

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.NorthWest),
                Is.True);

            Assert.That(
                NpcFacingUtility.UsesNorthFacingAnimation(
                    NpcFacing.SouthEast),
                Is.False);

            Assert.That(
                NpcFacingUtility.UsesNorthFacingAnimation(
                    NpcFacing.SouthWest),
                Is.False);

            Assert.That(
                NpcFacingUtility.UsesNorthFacingAnimation(
                    NpcFacing.NorthEast),
                Is.True);

            Assert.That(
                NpcFacingUtility.UsesNorthFacingAnimation(
                    NpcFacing.NorthWest),
                Is.True);
        }

        [Test]
        public void NorthAuthoredSource_ReflectsLimbAxesButPreservesFootHeading()
        {
            Vector3 canonicalPosition =
                new Vector3(0.37f, -0.61f, 0.08f);
            Vector3 canonicalEulerAngles =
                new Vector3(0f, 0f, 23f);

            foreach (NpcRigBoneId boneId in
                     (NpcRigBoneId[])Enum.GetValues(
                         typeof(NpcRigBoneId)))
            {
                Vector3 resolved =
                    NpcFacingUtility.ResolveAuthoredBonePosition(
                        NpcAuthoredDirection.NorthEast,
                        boneId,
                        canonicalPosition);

                Assert.That(
                    resolved.x,
                    Is.EqualTo(
                            NpcFacingUtility.IsDepthLimbBone(boneId)
                                ? -canonicalPosition.x
                                : canonicalPosition.x)
                        .Within(0.0001f),
                    $"Unexpected north/back X placement for bone {boneId}.");
                Assert.That(resolved.y, Is.EqualTo(canonicalPosition.y));
                Assert.That(resolved.z, Is.EqualTo(canonicalPosition.z));
            }

            foreach (NpcRigPartId partId in
                     (NpcRigPartId[])Enum.GetValues(
                         typeof(NpcRigPartId)))
            {
                bool depthLimb =
                    NpcFacingUtility.IsDepthLimbPart(partId);
                bool foot =
                    NpcFacingUtility.IsFootPart(partId);
                Vector3 resolvedPosition =
                    NpcFacingUtility.ResolveAuthoredPartPosition(
                        NpcAuthoredDirection.NorthEast,
                        partId,
                        canonicalPosition);
                Vector3 resolvedEulerAngles =
                    NpcFacingUtility.ResolveAuthoredPartEulerAngles(
                        NpcAuthoredDirection.NorthEast,
                        partId,
                        canonicalEulerAngles);
                Assert.That(
                    resolvedPosition.x,
                    Is.EqualTo(
                            depthLimb && !foot
                                ? -canonicalPosition.x
                                : canonicalPosition.x)
                        .Within(0.0001f),
                    $"Unexpected north/back X placement for part {partId}.");
                Assert.That(
                    Mathf.DeltaAngle(
                        0f,
                        resolvedEulerAngles.z),
                    Is.EqualTo(depthLimb && !foot ? -23f : 23f)
                        .Within(0.0001f),
                    $"Unexpected north/back artwork angle for part {partId}.");
            }

            Assert.That(
                NpcFacingUtility.ResolveAuthoredPartPosition(
                    NpcAuthoredDirection.NorthEast,
                    NpcRigPartId.FootForeground,
                    canonicalPosition),
                Is.EqualTo(canonicalPosition),
                "NorthEast foreground foot artwork must stay on its authored side of the ankle.");
            Assert.That(
                NpcFacingUtility.ResolveAuthoredPartPosition(
                    NpcAuthoredDirection.NorthEast,
                    NpcRigPartId.FootBackground,
                    canonicalPosition),
                Is.EqualTo(canonicalPosition),
                "NorthEast background foot artwork must stay on its authored side of the ankle.");

            Assert.That(
                NpcFacingUtility.ResolveAuthoredPartEulerAngles(
                    NpcAuthoredDirection.NorthEast,
                    NpcRigPartId.UpperArmForeground,
                    canonicalEulerAngles).z,
                Is.EqualTo(-23f).Within(0.0001f),
                "North/back arm segments must still reflect with their depth chain.");

            Assert.That(
                NpcFacingUtility.ResolveAuthoredPartEulerAngles(
                    NpcAuthoredDirection.NorthEast,
                    NpcRigPartId.FootForeground,
                    canonicalEulerAngles).z,
                Is.EqualTo(23f).Within(0.0001f),
                "NorthEast feet must preserve the authored eastward toe heading.");
            Assert.That(
                NpcFacingUtility.ResolveAuthoredPartEulerAngles(
                    NpcAuthoredDirection.NorthEast,
                    NpcRigPartId.FootBackground,
                    canonicalEulerAngles).z,
                Is.EqualTo(23f).Within(0.0001f),
                "Both NorthEast feet must preserve the same authored heading.");
        }

        [Test]
        public void SouthAuthoredSource_PreservesCanonicalBodyValues()
        {
            Vector3 canonicalPosition =
                new Vector3(0.37f, -0.61f, 0.08f);
            Vector3 canonicalEulerAngles =
                new Vector3(0f, 0f, 23f);

            foreach (NpcRigBoneId boneId in
                     (NpcRigBoneId[])Enum.GetValues(
                         typeof(NpcRigBoneId)))
            {
                Assert.That(
                    NpcFacingUtility.ResolveAuthoredBonePosition(
                        NpcAuthoredDirection.SouthEast,
                        boneId,
                        canonicalPosition),
                    Is.EqualTo(canonicalPosition));
            }

            foreach (NpcRigPartId partId in
                     (NpcRigPartId[])Enum.GetValues(
                         typeof(NpcRigPartId)))
            {
                Assert.That(
                    NpcFacingUtility.ResolveAuthoredPartPosition(
                        NpcAuthoredDirection.SouthEast,
                        partId,
                        canonicalPosition),
                    Is.EqualTo(canonicalPosition));
                Assert.That(
                    NpcFacingUtility.ResolveAuthoredPartEulerAngles(
                        NpcAuthoredDirection.SouthEast,
                        partId,
                        canonicalEulerAngles),
                    Is.EqualTo(canonicalEulerAngles));
            }
        }

        [Test]
        public void FacingRules_PreserveNearFarDepthWhenMirrored()
        {
            Assert.That(
                NpcFacingUtility.GetForegroundCameraSide(
                    NpcFacing.SouthEast),
                Is.EqualTo(NpcCameraSide.CameraLeft));

            Assert.That(
                NpcFacingUtility.GetForegroundCameraSide(
                    NpcFacing.SouthWest),
                Is.EqualTo(NpcCameraSide.CameraRight));

            Assert.That(
                NpcFacingUtility.GetForegroundCameraSide(
                    NpcFacing.NorthEast),
                Is.EqualTo(NpcCameraSide.CameraRight));

            Assert.That(
                NpcFacingUtility.GetForegroundCameraSide(
                    NpcFacing.NorthWest),
                Is.EqualTo(NpcCameraSide.CameraLeft));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthEast,
                    NpcRigPartId.UpperArmForeground),
                Is.EqualTo(15));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthEast,
                    NpcRigPartId.UpperArmBackground),
                Is.EqualTo(1));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthWest,
                    NpcRigPartId.UpperArmForeground),
                Is.EqualTo(15));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthWest,
                    NpcRigPartId.UpperArmBackground),
                Is.EqualTo(1));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.HairRear),
                Is.EqualTo(11));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.HairFront),
                Is.EqualTo(0));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthEast,
                    NpcRigPartId.FootForeground),
                Is.EqualTo(14));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.FootBackground),
                Is.EqualTo(4));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.ShinBackground),
                Is.EqualTo(6));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.FootForeground),
                Is.EqualTo(12));

            foreach (NpcFacing facing in
                     new[]
                     {
                         NpcFacing.SouthEast,
                         NpcFacing.SouthWest,
                         NpcFacing.NorthEast,
                         NpcFacing.NorthWest
                     })
            {
                NpcRigPartId[] nearParts =
                {
                    NpcRigPartId.UpperArmForeground,
                    NpcRigPartId.ForearmForeground,
                    NpcRigPartId.HandForeground,
                    NpcRigPartId.ThighForeground,
                    NpcRigPartId.ShinForeground,
                    NpcRigPartId.FootForeground
                };
                NpcRigPartId[] farParts =
                {
                    NpcRigPartId.UpperArmBackground,
                    NpcRigPartId.ForearmBackground,
                    NpcRigPartId.HandBackground,
                    NpcRigPartId.ThighBackground,
                    NpcRigPartId.ShinBackground,
                    NpcRigPartId.FootBackground
                };

                for (int index = 0; index < nearParts.Length; index++)
                {
                    Assert.That(
                        NpcFacingUtility.TryGetDisplayedCameraSide(
                            facing,
                            nearParts[index],
                            out NpcCameraSide nearSide),
                        Is.True);
                    Assert.That(
                        nearSide,
                        Is.EqualTo(
                            NpcFacingUtility.GetForegroundCameraSide(facing)),
                        $"{nearParts[index]} must occupy the foreground " +
                        $"screen side for {facing}.");

                    Assert.That(
                        NpcFacingUtility.TryGetDisplayedCameraSide(
                            facing,
                            farParts[index],
                            out NpcCameraSide farSide),
                        Is.True);
                    Assert.That(
                        farSide,
                        Is.Not.EqualTo(nearSide),
                        $"{farParts[index]} must occupy the background " +
                        $"screen side for {facing}.");

                    Assert.That(
                        NpcFacingUtility.GetPresentationSortingOrder(
                            facing,
                            nearParts[index]),
                        Is.GreaterThan(
                            NpcFacingUtility.GetPresentationSortingOrder(
                                facing,
                                farParts[index])),
                        $"{nearParts[index]} must draw over " +
                        $"{farParts[index]} for {facing}.");
                }
            }
        }

        [Test]
        public void FacingRules_ProduceConsistentVisibleLimbHeadingAtEveryCorner()
        {
            const float authoredAngle = 18f;

            AssertVisibleFootAngle(
                NpcFacing.SouthEast,
                authoredAngle);
            AssertVisibleFootAngle(
                NpcFacing.SouthWest,
                -authoredAngle);
            AssertVisibleFootAngle(
                NpcFacing.NorthEast,
                authoredAngle);
            AssertVisibleFootAngle(
                NpcFacing.NorthWest,
                -authoredAngle);

            Assert.That(
                NpcFacingUtility.RemapDirectionalFootAngle(
                    NpcAuthoredDirection.NorthEast,
                    NpcRigPartId.Torso,
                    authoredAngle),
                Is.EqualTo(authoredAngle));
        }

        [Test]
        public void ArtworkContract_RequiresThirtySixAuthoredSprites()
        {
            Assert.That(
                NpcRigDefinition.ExpectedAuthoredSpriteCount,
                Is.EqualTo(36));
        }

        private static void AssertVisibleFootAngle(
            NpcFacing facing,
            float expectedVisibleAngle)
        {
            const float authoredAngle = 18f;

            NpcRigPartId[] footParts =
            {
                NpcRigPartId.FootForeground,
                NpcRigPartId.FootBackground
            };

            foreach (NpcRigPartId footPart in footParts)
            {
                float sourceAngle =
                    NpcFacingUtility.RemapDirectionalFootAngle(
                        NpcFacingUtility.GetAuthoredDirection(facing),
                        footPart,
                        authoredAngle);
                float visibleAngle = NpcFacingUtility.IsMirrored(facing)
                    ? -sourceAngle
                    : sourceAngle;

                Assert.That(
                    Mathf.DeltaAngle(0f, visibleAngle),
                    Is.EqualTo(expectedVisibleAngle).Within(0.0001f),
                    $"Unexpected visible heading for {footPart} at {facing}.");
            }
        }


        private static void AssertPresentation(
            NpcFacing facing,
            NpcAuthoredDirection expectedDirection,
            bool expectedMirror,
            NpcCameraSide expectedForegroundSide)
        {
            NpcFacingPresentation presentation =
                NpcFacingUtility.GetPresentation(facing);

            Assert.That(
                presentation.AuthoredDirection,
                Is.EqualTo(expectedDirection));
            Assert.That(
                presentation.MirrorHorizontally,
                Is.EqualTo(expectedMirror));
            Assert.That(
                presentation.ForegroundCameraSide,
                Is.EqualTo(expectedForegroundSide));
            Assert.That(
                presentation.UsesNorthFacingAnimation,
                Is.EqualTo(
                    expectedDirection == NpcAuthoredDirection.NorthEast));
        }
    }
}
