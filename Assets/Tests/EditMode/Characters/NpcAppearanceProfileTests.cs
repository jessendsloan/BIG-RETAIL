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
