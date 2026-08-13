using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcRigArtKitTests
    {
        [Test]
        public void NormalizedArtKit_HasCanonicalStructure()
        {
            NpcRigArtKit artKit =
                ScriptableObject.CreateInstance<NpcRigArtKit>();

            try
            {
                artKit.NormalizeCanonicalLayout();

                Assert.That(
                    artKit.PartCount,
                    Is.EqualTo(
                        NpcRigDefinition.ExpectedPartCount));

                Assert.That(
                    artKit.TryValidateStructure(
                        out string failureReason),
                    Is.True,
                    failureReason);

                foreach (NpcRigPartDefinition definition
                         in NpcRigDefinition.PartDefinitions)
                {
                    Assert.That(
                        artKit.TrySetSprite(
                            definition.Id,
                            NpcAuthoredDirection.SouthEast,
                            null),
                        Is.True,
                        $"Missing art slot: {definition.Id}");
                }
            }
            finally
            {
                Object.DestroyImmediate(
                    artKit);
            }
        }

        [Test]
        public void EmptyArtKit_ReportsEverySouthEastPartMissing()
        {
            NpcRigArtKit artKit =
                ScriptableObject.CreateInstance<NpcRigArtKit>();

            try
            {
                artKit.NormalizeCanonicalLayout();

                List<NpcRigPartId> missingParts =
                    new List<NpcRigPartId>();

                artKit.GetMissingParts(
                    NpcAuthoredDirection.SouthEast,
                    missingParts);

                Assert.That(
                    artKit.CountAssignedSprites(
                        NpcAuthoredDirection.SouthEast),
                    Is.Zero);

                Assert.That(
                    missingParts.Count,
                    Is.EqualTo(
                        NpcRigDefinition.ExpectedPartCount));

                Assert.That(
                    artKit.TryValidateDirection(
                        NpcAuthoredDirection.SouthEast,
                        out _),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(
                    artKit);
            }
        }

        [Test]
        public void UniqueSprites_CompleteOneAuthoredDirection()
        {
            NpcRigArtKit artKit =
                ScriptableObject.CreateInstance<NpcRigArtKit>();

            Texture2D texture =
                new Texture2D(
                    NpcRigDefinition.ExpectedPartCount,
                    1);

            List<Sprite> sprites =
                new List<Sprite>();

            try
            {
                artKit.NormalizeCanonicalLayout();

                int index = 0;

                foreach (NpcRigPartDefinition definition
                         in NpcRigDefinition.PartDefinitions)
                {
                    Sprite sprite =
                        Sprite.Create(
                            texture,
                            new Rect(
                                index,
                                0f,
                                1f,
                                1f),
                            new Vector2(
                                0.5f,
                                0.5f),
                            1f);

                    sprite.name =
                        definition.Id.ToString();

                    sprites.Add(
                        sprite);

                    Assert.That(
                        artKit.TrySetSprite(
                            definition.Id,
                            NpcAuthoredDirection.SouthEast,
                            sprite),
                        Is.True);

                    index++;
                }

                Assert.That(
                    artKit.CountAssignedSprites(
                        NpcAuthoredDirection.SouthEast),
                    Is.EqualTo(
                        NpcRigDefinition.ExpectedPartCount));

                Assert.That(
                    artKit.TryValidateDirection(
                        NpcAuthoredDirection.SouthEast,
                        out string failureReason),
                    Is.True,
                    failureReason);

                Assert.That(
                    artKit.TryValidateDirection(
                        NpcAuthoredDirection.NorthEast,
                        out _),
                    Is.False);
            }
            finally
            {
                for (int index = 0;
                     index < sprites.Count;
                     index++)
                {
                    Object.DestroyImmediate(
                        sprites[index]);
                }

                Object.DestroyImmediate(
                    texture);
                Object.DestroyImmediate(
                    artKit);
            }
        }

        [Test]
        public void DuplicateSpriteAssignment_IsRejected()
        {
            NpcRigArtKit artKit =
                ScriptableObject.CreateInstance<NpcRigArtKit>();

            Texture2D texture =
                new Texture2D(
                    1,
                    1);

            Sprite sharedSprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        1f,
                        1f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    1f);

            try
            {
                artKit.NormalizeCanonicalLayout();

                foreach (NpcRigPartDefinition definition
                         in NpcRigDefinition.PartDefinitions)
                {
                    artKit.TrySetSprite(
                        definition.Id,
                        NpcAuthoredDirection.SouthEast,
                        sharedSprite);
                }

                Assert.That(
                    artKit.TryValidateDirection(
                        NpcAuthoredDirection.SouthEast,
                        out string failureReason),
                    Is.False);

                StringAssert.Contains(
                    "more than one part",
                    failureReason);
            }
            finally
            {
                Object.DestroyImmediate(
                    sharedSprite);
                Object.DestroyImmediate(
                    texture);
                Object.DestroyImmediate(
                    artKit);
            }
        }
    }
}
