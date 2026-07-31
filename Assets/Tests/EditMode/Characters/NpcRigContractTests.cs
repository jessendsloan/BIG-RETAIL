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
                Is.True);

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.NorthEast),
                Is.True);

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.SouthWest),
                Is.False);

            Assert.That(
                NpcFacingUtility.IsMirrored(
                    NpcFacing.NorthWest),
                Is.False);
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
