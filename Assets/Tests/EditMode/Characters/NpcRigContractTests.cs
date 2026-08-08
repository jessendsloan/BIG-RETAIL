using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using NUnit.Framework;

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
        public void NearFarMigration_PreservesSerializedEnumValues()
        {
            Assert.That((int)NpcRigBoneId.ShoulderNear, Is.EqualTo(6));
            Assert.That((int)NpcRigBoneId.HandNear, Is.EqualTo(9));
            Assert.That((int)NpcRigBoneId.ShoulderFar, Is.EqualTo(10));
            Assert.That((int)NpcRigBoneId.HandFar, Is.EqualTo(13));
            Assert.That((int)NpcRigBoneId.ThighNear, Is.EqualTo(14));
            Assert.That((int)NpcRigBoneId.FootNear, Is.EqualTo(16));
            Assert.That((int)NpcRigBoneId.ThighFar, Is.EqualTo(17));
            Assert.That((int)NpcRigBoneId.FootFar, Is.EqualTo(19));

            Assert.That((int)NpcRigPartId.UpperArmNear, Is.EqualTo(1));
            Assert.That((int)NpcRigPartId.FootNear, Is.EqualTo(6));
            Assert.That((int)NpcRigPartId.ThighFar, Is.EqualTo(12));
            Assert.That((int)NpcRigPartId.HandFar, Is.EqualTo(17));
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
                Is.EqualTo(NpcCameraSide.CameraLeft));

            Assert.That(
                NpcFacingUtility.GetForegroundCameraSide(
                    NpcFacing.NorthWest),
                Is.EqualTo(NpcCameraSide.CameraRight));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthEast,
                    NpcRigPartId.UpperArmNear),
                Is.EqualTo(15));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthEast,
                    NpcRigPartId.UpperArmFar),
                Is.EqualTo(1));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthWest,
                    NpcRigPartId.UpperArmNear),
                Is.EqualTo(15));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.SouthWest,
                    NpcRigPartId.UpperArmFar),
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
                    NpcRigPartId.FootNear),
                Is.EqualTo(14));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.FootFar),
                Is.EqualTo(4));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.ShinFar),
                Is.EqualTo(6));

            Assert.That(
                NpcFacingUtility.GetPresentationSortingOrder(
                    NpcFacing.NorthEast,
                    NpcRigPartId.FootNear),
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
                Assert.That(
                    NpcFacingUtility.GetPresentationSortingOrder(
                        facing,
                        NpcRigPartId.UpperArmNear),
                    Is.GreaterThan(
                        NpcFacingUtility.GetPresentationSortingOrder(
                            facing,
                            NpcRigPartId.UpperArmFar)),
                    $"Near arm must remain foreground for {facing}.");

                Assert.That(
                    NpcFacingUtility.GetPresentationSortingOrder(
                        facing,
                        NpcRigPartId.ThighNear),
                    Is.GreaterThan(
                        NpcFacingUtility.GetPresentationSortingOrder(
                            facing,
                            NpcRigPartId.ThighFar)),
                    $"Near leg must remain foreground for {facing}.");
            }
        }

        [Test]
        public void ArtworkContract_RequiresThirtySixAuthoredSprites()
        {
            Assert.That(
                NpcRigDefinition.ExpectedAuthoredSpriteCount,
                Is.EqualTo(36));
        }
    }
}
