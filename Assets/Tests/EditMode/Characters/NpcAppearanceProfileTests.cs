using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcAppearanceProfileTests
    {
        [TestCase(typeof(NpcBodySilhouette), "NpcBodySilhouette")]
        [TestCase(typeof(NpcSkinPalette), "NpcSkinPalette")]
        [TestCase(typeof(NpcOutfitSet), "NpcOutfitSet")]
        [TestCase(typeof(NpcHairSet), "NpcHairSet")]
        [TestCase(typeof(NpcAppearanceProfile), "NpcAppearanceProfile")]
        [TestCase(typeof(NpcPopulationDefinition), "NpcPopulationDefinition")]
        [TestCase(typeof(NpcAppearanceCatalog), "NpcAppearanceCatalog")]
        public void SavedAssetType_HasMatchingMonoScript(
            System.Type assetType,
            string expectedScriptName)
        {
            ScriptableObject asset =
                ScriptableObject.CreateInstance(assetType);

            try
            {
                MonoScript script =
                    MonoScript.FromScriptableObject(asset);

                Assert.That(
                    script,
                    Is.Not.Null,
                    $"{assetType.Name} has no Unity script asset.");

                Assert.That(
                    script.name,
                    Is.EqualTo(expectedScriptName));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }


        [Test]
        public void CompleteFourPartRecipe_Validates()
        {
            AppearanceFixture fixture = new AppearanceFixture();

            try
            {
                Assert.That(
                    fixture.Profile.TryValidate(
                        out string failureReason),
                    Is.True,
                    failureReason);
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void SkinAndOutfitRemainIndependent()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            GameObject headObject = new GameObject("Head");
            GameObject torsoObject = new GameObject("Torso");

            try
            {
                SpriteRenderer head =
                    headObject.AddComponent<SpriteRenderer>();

                SpriteRenderer torso =
                    torsoObject.AddComponent<SpriteRenderer>();

                head.sprite = fixture.Sprite;
                torso.sprite = fixture.Sprite;

                fixture.Profile.ApplyPart(
                    NpcRigPartId.Head,
                    head,
                    NpcAuthoredDirection.SouthEast);

                fixture.Profile.ApplyPart(
                    NpcRigPartId.Torso,
                    torso,
                    NpcAuthoredDirection.SouthEast);

                Assert.That(
                    head.color,
                    Is.EqualTo(fixture.SkinColor));

                Assert.That(
                    torso.color,
                    Is.EqualTo(fixture.ShirtColor));

                Assert.That(
                    head.transform.localScale.x,
                    Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(headObject);
                Object.DestroyImmediate(torsoObject);
                fixture.Dispose();
            }
        }


        [Test]
        public void MissingRecipeMember_IsRejected()
        {
            AppearanceFixture fixture = new AppearanceFixture();

            try
            {
                fixture.Profile.Configure(
                    "Incomplete",
                    fixture.Body,
                    fixture.Skin,
                    null,
                    fixture.Hair);

                Assert.That(
                    fixture.Profile.TryValidate(out string reason),
                    Is.False);

                StringAssert.Contains("outfit", reason.ToLowerInvariant());
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void SameTemplateAndSeed_ProduceSameSelection()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            Object[] variants = null;

            try
            {
                definition = CreateVariedDefinition(
                    fixture,
                    out variants);

                Assert.That(
                    NpcAppearanceGenerator.TryGenerate(
                        definition,
                        4815,
                        null,
                        null,
                        out NpcAppearanceSelection first,
                        out string firstReason),
                    Is.True,
                    firstReason);

                Assert.That(
                    NpcAppearanceGenerator.TryGenerate(
                        definition,
                        4815,
                        null,
                        null,
                        out NpcAppearanceSelection second,
                        out string secondReason),
                    Is.True,
                    secondReason);

                Assert.That(
                    second.BodySilhouette,
                    Is.SameAs(first.BodySilhouette));
                Assert.That(
                    second.SkinPalette,
                    Is.SameAs(first.SkinPalette));
                Assert.That(
                    second.OutfitSet,
                    Is.SameAs(first.OutfitSet));
                Assert.That(
                    second.HairSet,
                    Is.SameAs(first.HairSet));
            }
            finally
            {
                DestroyObjects(variants);
                Object.DestroyImmediate(definition);
                fixture.Dispose();
            }
        }


        [Test]
        public void OutfitLock_PreservesApprovedUniform()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            Object[] variants = null;

            try
            {
                definition = CreateVariedDefinition(
                    fixture,
                    out variants);

                NpcAppearanceSelection current =
                    fixture.Profile.CreateSelection();

                NpcAppearanceLocks locks =
                    new NpcAppearanceLocks();

                locks.Configure(
                    false,
                    false,
                    true,
                    false);

                Assert.That(
                    NpcAppearanceGenerator.TryGenerate(
                        definition,
                        99,
                        current,
                        locks,
                        out NpcAppearanceSelection generated,
                        out string reason),
                    Is.True,
                    reason);

                Assert.That(
                    generated.OutfitSet,
                    Is.SameAs(fixture.Outfit));
            }
            finally
            {
                DestroyObjects(variants);
                Object.DestroyImmediate(definition);
                fixture.Dispose();
            }
        }


        [Test]
        public void LockedOutfitOutsideTemplate_IsRejected()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            Object[] variants = null;
            NpcOutfitSet disallowedOutfit = null;

            try
            {
                definition = CreateVariedDefinition(
                    fixture,
                    out variants);

                disallowedOutfit =
                    Object.Instantiate(fixture.Outfit);
                disallowedOutfit.name = "Disallowed Outfit";

                NpcAppearanceSelection current =
                    new NpcAppearanceSelection(
                        fixture.Body,
                        fixture.Skin,
                        disallowedOutfit,
                        fixture.Hair);

                NpcAppearanceLocks locks =
                    new NpcAppearanceLocks();

                locks.Configure(
                    false,
                    false,
                    true,
                    false);

                Assert.That(
                    NpcAppearanceGenerator.TryGenerate(
                        definition,
                        99,
                        current,
                        locks,
                        out _,
                        out string reason),
                    Is.False);

                StringAssert.Contains(
                    "not allowed",
                    reason.ToLowerInvariant());
            }
            finally
            {
                Object.DestroyImmediate(disallowedOutfit);
                DestroyObjects(variants);
                Object.DestroyImmediate(definition);
                fixture.Dispose();
            }
        }


        [Test]
        public void EmployeeTemplate_GeneratesOnlyApprovedOutfits()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            Object[] variants = null;

            try
            {
                definition = CreateVariedDefinition(
                    fixture,
                    out variants);

                NpcOutfitSet alternate =
                    (NpcOutfitSet)variants[2];

                for (int seed = 0; seed < 64; seed++)
                {
                    Assert.That(
                        NpcAppearanceGenerator.TryGenerate(
                            definition,
                            seed,
                            null,
                            null,
                            out NpcAppearanceSelection generated,
                            out string reason),
                        Is.True,
                        reason);

                    Assert.That(
                        generated.OutfitSet == fixture.Outfit
                        || generated.OutfitSet == alternate,
                        Is.True);
                }
            }
            finally
            {
                DestroyObjects(variants);
                Object.DestroyImmediate(definition);
                fixture.Dispose();
            }
        }


        private static NpcPopulationDefinition CreateVariedDefinition(
            AppearanceFixture fixture,
            out Object[] variants)
        {
            NpcBodySilhouette alternateBody =
                Object.Instantiate(fixture.Body);
            NpcSkinPalette alternateSkin =
                Object.Instantiate(fixture.Skin);
            NpcOutfitSet alternateOutfit =
                Object.Instantiate(fixture.Outfit);
            NpcHairSet alternateHair =
                Object.Instantiate(fixture.Hair);

            alternateBody.name = "Alternate Body";
            alternateSkin.name = "Alternate Skin";
            alternateOutfit.name = "Alternate Outfit";
            alternateHair.name = "Alternate Hair";

            variants = new Object[]
            {
                alternateBody,
                alternateSkin,
                alternateOutfit,
                alternateHair
            };

            NpcPopulationDefinition definition =
                ScriptableObject.CreateInstance<NpcPopulationDefinition>();

            definition.Configure(
                "Store Employee",
                NpcCharacterRole.Employee,
                new[]
                {
                    new NpcWeightedBodyChoice(fixture.Body),
                    new NpcWeightedBodyChoice(alternateBody)
                },
                new[]
                {
                    new NpcWeightedSkinChoice(fixture.Skin),
                    new NpcWeightedSkinChoice(alternateSkin)
                },
                new[]
                {
                    new NpcWeightedOutfitChoice(fixture.Outfit),
                    new NpcWeightedOutfitChoice(alternateOutfit)
                },
                new[]
                {
                    new NpcWeightedHairChoice(fixture.Hair),
                    new NpcWeightedHairChoice(alternateHair)
                });

            return definition;
        }


        private static void DestroyObjects(
            Object[] objects)
        {
            if (objects == null)
            {
                return;
            }

            for (int index = 0; index < objects.Length; index++)
            {
                Object.DestroyImmediate(objects[index]);
            }
        }


        private sealed class AppearanceFixture
        {
            private readonly Texture2D texture;

            public readonly Color SkinColor =
                new Color(0.6f, 0.4f, 0.3f, 1f);

            public readonly Color ShirtColor =
                new Color(0.1f, 0.5f, 0.7f, 1f);

            public NpcBodySilhouette Body { get; }

            public NpcSkinPalette Skin { get; }

            public NpcOutfitSet Outfit { get; }

            public NpcHairSet Hair { get; }

            public NpcAppearanceProfile Profile { get; }

            public Sprite Sprite { get; }


            public AppearanceFixture()
            {
                texture = new Texture2D(1, 1);

                Sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);

                List<NpcAppearancePartShape> shapes =
                    new List<NpcAppearancePartShape>();

                List<NpcOutfitPartStyle> outfitParts =
                    new List<NpcOutfitPartStyle>();

                foreach (NpcRigPartDefinition definition
                         in NpcRigDefinition.PartDefinitions)
                {
                    shapes.Add(
                        new NpcAppearancePartShape(
                            definition.Id,
                            Vector3.zero,
                            Vector3.zero,
                            Vector2.one));

                    if (definition.Id == NpcRigPartId.HairRear
                        || definition.Id == NpcRigPartId.HairFront
                        || definition.Id == NpcRigPartId.Head
                        || definition.Id == NpcRigPartId.Neck)
                    {
                        continue;
                    }

                    NpcAppearanceColorRole role =
                        definition.Id == NpcRigPartId.Torso
                            ? NpcAppearanceColorRole.PrimaryFabric
                            : NpcAppearanceColorRole.Skin;

                    outfitParts.Add(
                        new NpcOutfitPartStyle(
                            definition.Id,
                            role,
                            Sprite,
                            Sprite));
                }

                Body = ScriptableObject
                    .CreateInstance<NpcBodySilhouette>();

                Body.Configure(
                    "Test Body",
                    NpcBodySilhouetteKind.Masculine,
                    shapes,
                    null);

                Skin = ScriptableObject
                    .CreateInstance<NpcSkinPalette>();

                Skin.Configure(
                    "Test Skin",
                    SkinColor);

                Outfit = ScriptableObject
                    .CreateInstance<NpcOutfitSet>();

                Outfit.Configure(
                    "Test Outfit",
                    ShirtColor,
                    Color.gray,
                    Color.black,
                    Color.white,
                    false,
                    outfitParts);

                Hair = ScriptableObject
                    .CreateInstance<NpcHairSet>();

                Hair.Configure(
                    "Test Hair",
                    Color.black,
                    new NpcOutfitPartStyle(
                        NpcRigPartId.HairRear,
                        NpcAppearanceColorRole.Preserve,
                        Sprite,
                        Sprite),
                    new NpcOutfitPartStyle(
                        NpcRigPartId.HairFront,
                        NpcAppearanceColorRole.Preserve,
                        Sprite,
                        Sprite),
                    new NpcAppearancePartShape(
                        NpcRigPartId.HairRear,
                        Vector3.zero,
                        Vector3.zero,
                        Vector2.one),
                    new NpcAppearancePartShape(
                        NpcRigPartId.HairFront,
                        Vector3.zero,
                        Vector3.zero,
                        Vector2.one));

                Profile = ScriptableObject
                    .CreateInstance<NpcAppearanceProfile>();

                Profile.Configure(
                    "Test Person",
                    Body,
                    Skin,
                    Outfit,
                    Hair);
            }


            public void Dispose()
            {
                Object.DestroyImmediate(Profile);
                Object.DestroyImmediate(Hair);
                Object.DestroyImmediate(Outfit);
                Object.DestroyImmediate(Skin);
                Object.DestroyImmediate(Body);
                Object.DestroyImmediate(Sprite);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
