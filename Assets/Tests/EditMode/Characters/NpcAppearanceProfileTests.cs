using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcAppearanceProfileTests
    {
        [Test]
        public void AppearanceCreator_RepeatedSavesPreserveAssetName()
        {
            const string testAssetPath =
                "Assets/Tests/EditMode/Characters/" +
                "AppearanceCreatorNameRegression.asset";

            AssetDatabase.DeleteAsset(testAssetPath);

            NpcHairSet savedAsset =
                ScriptableObject.CreateInstance<NpcHairSet>();
            ScriptableObject workingCopy = null;

            try
            {
                savedAsset.name = "Original Internal Name";
                AssetDatabase.CreateAsset(
                    savedAsset,
                    testAssetPath);

                System.Type windowType = System.Type.GetType(
                    "BigRetail.Characters.Editor." +
                    "NpcAppearanceCreatorWindow, " +
                    "BigRetail.Characters.Editor");

                Assert.That(windowType, Is.Not.Null);

                System.Reflection.MethodInfo saveMethod =
                    windowType.GetMethod(
                        "CopyWorkingAssetToSelectedAsset",
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.NonPublic);

                Assert.That(saveMethod, Is.Not.Null);

                workingCopy = Object.Instantiate(savedAsset);

                for (int saveIndex = 0; saveIndex < 3; saveIndex++)
                {
                    workingCopy.name =
                        savedAsset.name + " Working Copy";

                    saveMethod.Invoke(
                        null,
                        new object[]
                        {
                            workingCopy,
                            savedAsset
                        });

                    Assert.That(
                        savedAsset.name,
                        Is.EqualTo(
                            "AppearanceCreatorNameRegression"));
                }
            }
            finally
            {
                if (workingCopy != null)
                {
                    Object.DestroyImmediate(workingCopy);
                }

                AssetDatabase.DeleteAsset(testAssetPath);
            }
        }


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
        public void CompleteAppearanceRecipe_Validates()
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
        public void OutfitPart_AppliesOptionalGarmentMaterial()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            GameObject torsoObject = new GameObject("Textured Torso");
            NpcOutfitSet texturedOutfit = null;
            NpcAppearanceProfile texturedProfile = null;

            try
            {
                Material garmentMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Art/Characters/Appearance/Materials/" +
                    "CharacterGarmentTextureLit.mat");

                Assert.That(garmentMaterial, Is.Not.Null);
                Assert.That(
                    garmentMaterial.shader.name,
                    Is.EqualTo(
                        "Big Retail/Characters/Textured Garment Lit"));

                texturedOutfit = ScriptableObject
                    .CreateInstance<NpcOutfitSet>();
                texturedOutfit.Configure(
                    "Textured Outfit",
                    fixture.ShirtColor,
                    Color.gray,
                    Color.black,
                    Color.white,
                    false,
                    new[]
                    {
                        new NpcOutfitPartStyle(
                            NpcRigPartId.Torso,
                            NpcAppearanceColorRole.PrimaryFabric,
                            fixture.Sprite,
                            fixture.Sprite,
                            garmentMaterial)
                    });

                texturedProfile = ScriptableObject
                    .CreateInstance<NpcAppearanceProfile>();
                texturedProfile.Configure(
                    "Textured Person",
                    fixture.Body,
                    fixture.Skin,
                    texturedOutfit,
                    fixture.Hair);

                SpriteRenderer renderer =
                    torsoObject.AddComponent<SpriteRenderer>();
                renderer.sprite = fixture.Sprite;

                texturedProfile.ApplyPart(
                    NpcRigPartId.Torso,
                    renderer,
                    NpcAuthoredDirection.SouthEast);

                Assert.That(
                    renderer.sharedMaterial,
                    Is.SameAs(garmentMaterial));
            }
            finally
            {
                Object.DestroyImmediate(texturedProfile);
                Object.DestroyImmediate(texturedOutfit);
                Object.DestroyImmediate(torsoObject);
                fixture.Dispose();
            }
        }


        [Test]
        public void PartBinding_RestoresPrefabMaterialBeforeNextAppearance()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            GameObject torsoObject = new GameObject("Reusable Torso");

            try
            {
                Material fallbackMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/Art/Characters/Appearance/Materials/" +
                        "CharacterChestPlain.mat");
                Material garmentMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/Art/Characters/Appearance/Materials/" +
                        "CharacterGarmentTextureLit.mat");

                Assert.That(fallbackMaterial, Is.Not.Null);
                Assert.That(garmentMaterial, Is.Not.Null);
                Assert.That(
                    fallbackMaterial.shader.name,
                    Is.EqualTo(
                        "Big Retail/Characters/Plain Chest Lit"));

                SpriteRenderer renderer =
                    torsoObject.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = fallbackMaterial;

                NpcRigPartBinding binding = new NpcRigPartBinding(
                    NpcRigPartId.Torso,
                    renderer,
                    fixture.Sprite);

                binding.Apply(NpcAuthoredDirection.SouthEast);
                renderer.sharedMaterial = garmentMaterial;
                binding.Apply(NpcAuthoredDirection.SouthEast);

                Assert.That(
                    renderer.sharedMaterial,
                    Is.SameAs(fallbackMaterial));
            }
            finally
            {
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
        public void HairDetailLayer_AppliesDirectionalPoseAndShade()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            GameObject layerObject = new GameObject("Hair Detail");

            try
            {
                SpriteRenderer renderer =
                    layerObject.AddComponent<SpriteRenderer>();

                Color hairColor =
                    new Color(0.4f, 0.2f, 0.1f, 1f);

                NpcHairDetailLayer layer =
                    new NpcHairDetailLayer(
                        "Sweep",
                        NpcHairLayerDepth.Crown,
                        0.5f,
                        fixture.Sprite,
                        fixture.Sprite,
                        new NpcHairLayerPose(
                            new Vector3(0.12f, 0.25f, 0f),
                            new Vector3(0f, 0f, -18f),
                            new Vector2(0.3f, 0.08f)),
                        new NpcHairLayerPose(
                            new Vector3(-0.12f, 0.25f, 0f),
                            new Vector3(0f, 0f, 18f),
                            new Vector2(0.3f, 0.08f)));

                layer.Apply(
                    renderer,
                    NpcAuthoredDirection.SouthEast,
                    hairColor,
                    12);

                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sprite, Is.SameAs(fixture.Sprite));
                Assert.That(renderer.sortingOrder, Is.EqualTo(12));
                Assert.That(
                    renderer.transform.localPosition,
                    Is.EqualTo(new Vector3(0.12f, 0.25f, 0f)));
                Assert.That(
                    renderer.transform.localEulerAngles.z,
                    Is.EqualTo(342f).Within(0.01f));
                Assert.That(
                    renderer.transform.localScale.x,
                    Is.EqualTo(0.3f).Within(0.001f));
                Assert.That(
                    renderer.color.r,
                    Is.EqualTo(0.2f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(layerObject);
                fixture.Dispose();
            }
        }


        [Test]
        public void LayeredHairSet_ValidatesWithoutChangingCoreRigContract()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcHairSet layeredHair =
                ScriptableObject.CreateInstance<NpcHairSet>();

            try
            {
                NpcAppearancePartShape rearShape =
                    new NpcAppearancePartShape(
                        NpcRigPartId.HairRear,
                        Vector3.zero,
                        Vector3.zero,
                        Vector2.one);
                NpcAppearancePartShape frontShape =
                    new NpcAppearancePartShape(
                        NpcRigPartId.HairFront,
                        Vector3.zero,
                        Vector3.zero,
                        Vector2.one);

                layeredHair.Configure(
                    "Layered Hair",
                    Color.black,
                    NpcGenderCompatibility.Everyone,
                    new NpcOutfitPartStyle(
                        NpcRigPartId.HairRear,
                        NpcAppearanceColorRole.Preserve,
                        fixture.Sprite,
                        fixture.Sprite),
                    new NpcOutfitPartStyle(
                        NpcRigPartId.HairFront,
                        NpcAppearanceColorRole.Preserve,
                        fixture.Sprite,
                        fixture.Sprite),
                    rearShape,
                    frontShape,
                    new[]
                    {
                        new NpcHairDetailLayer(
                            "Tuft",
                            NpcHairLayerDepth.Crown,
                            1f,
                            fixture.Sprite,
                            fixture.Sprite,
                            new NpcHairLayerPose(
                                Vector3.zero,
                                Vector3.zero,
                                new Vector2(0.1f, 0.1f)),
                            new NpcHairLayerPose(
                                Vector3.zero,
                                Vector3.zero,
                                new Vector2(0.1f, 0.1f)))
                    });

                Assert.That(
                    layeredHair.TryValidate(out string reason),
                    Is.True,
                    reason);
                Assert.That(layeredHair.DetailLayers, Has.Count.EqualTo(1));
                Assert.That(
                    NpcRigDefinition.ExpectedPartCount,
                    Is.EqualTo(18));
            }
            finally
            {
                Object.DestroyImmediate(layeredHair);
                fixture.Dispose();
            }
        }


        [Test]
        public void CatalogSupportsMultipleDefinitionsInOneBehaviorFamily()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition employee = null;
            NpcPopulationDefinition manager = null;
            NpcAppearanceCatalog catalog = null;

            try
            {
                employee = CreateDefinition(
                    fixture,
                    "Store Employee",
                    NpcCharacterRole.Employee);

                manager = CreateDefinition(
                    fixture,
                    "Manager",
                    NpcCharacterRole.Employee);

                catalog = ScriptableObject
                    .CreateInstance<NpcAppearanceCatalog>();

                catalog.Configure(
                    "Test Catalog",
                    new[] { employee, manager },
                    new[] { fixture.Body },
                    new[] { fixture.Skin },
                    new[] { fixture.Outfit },
                    new[] { fixture.Hair });

                IReadOnlyList<NpcPopulationDefinition> definitions =
                    catalog.GetDefinitions(NpcCharacterRole.Employee);

                Assert.That(definitions.Count, Is.EqualTo(2));
                Assert.That(definitions[0], Is.SameAs(employee));
                Assert.That(definitions[1], Is.SameAs(manager));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(employee);
                fixture.Dispose();
            }
        }


        [Test]
        public void RegisterAssetsFromDefinitionBuildsCatalogLibrary()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            NpcAppearanceCatalog catalog = null;

            try
            {
                definition = CreateDefinition(
                    fixture,
                    "Customer",
                    NpcCharacterRole.Customer);

                catalog = ScriptableObject
                    .CreateInstance<NpcAppearanceCatalog>();

                catalog.Configure(
                    "Test Catalog",
                    new[] { definition },
                    System.Array.Empty<NpcBodySilhouette>(),
                    System.Array.Empty<NpcSkinPalette>(),
                    System.Array.Empty<NpcOutfitSet>(),
                    System.Array.Empty<NpcHairSet>());

                Assert.That(
                    catalog.RegisterAssetsFrom(definition),
                    Is.True);
                Assert.That(catalog.Bodies, Has.Count.EqualTo(1));
                Assert.That(catalog.Skins, Has.Count.EqualTo(1));
                Assert.That(catalog.Outfits, Has.Count.EqualTo(1));
                Assert.That(catalog.Hair, Has.Count.EqualTo(1));
                Assert.That(
                    catalog.TryValidate(out string reason),
                    Is.True,
                    reason);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(definition);
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
        public void WomanOnlyPopulation_GeneratesCompatibleAppearance()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcBodySilhouette feminineBody = null;
            NpcPopulationDefinition definition = null;

            try
            {
                feminineBody = ScriptableObject
                    .CreateInstance<NpcBodySilhouette>();

                feminineBody.Configure(
                    "Test Feminine Body",
                    NpcBodySilhouetteKind.Feminine,
                    fixture.Body.PartShapes,
                    null);

                definition = ScriptableObject
                    .CreateInstance<NpcPopulationDefinition>();

                definition.Configure(
                    "Women Customers",
                    NpcCharacterRole.Customer,
                    new[]
                    {
                        new NpcWeightedBodyChoice(fixture.Body),
                        new NpcWeightedBodyChoice(feminineBody)
                    },
                    new[] { new NpcWeightedSkinChoice(fixture.Skin) },
                    new[] { new NpcWeightedOutfitChoice(fixture.Outfit) },
                    new[] { new NpcWeightedHairChoice(fixture.Hair) });

                definition.SetGenderWeights(0, 1);

                Assert.That(
                    NpcAppearanceGenerator.TryGenerate(
                        definition,
                        1234,
                        null,
                        null,
                        out NpcAppearanceSelection generated,
                        out string reason),
                    Is.True,
                    reason);

                Assert.That(
                    generated.Gender,
                    Is.EqualTo(NpcPersonGender.Woman));
                Assert.That(
                    generated.BodySilhouette,
                    Is.SameAs(feminineBody));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(feminineBody);
                fixture.Dispose();
            }
        }


        [Test]
        public void LegacySharedPool_SplitsIntoMenAndWomenPools()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcBodySilhouette feminineBody = null;
            NpcPopulationDefinition definition = null;

            try
            {
                feminineBody = ScriptableObject
                    .CreateInstance<NpcBodySilhouette>();

                feminineBody.Configure(
                    "Test Feminine Body",
                    NpcBodySilhouetteKind.Feminine,
                    fixture.Body.PartShapes,
                    null);

                definition = ScriptableObject
                    .CreateInstance<NpcPopulationDefinition>();

                definition.Configure(
                    "Customers",
                    NpcCharacterRole.Customer,
                    new[]
                    {
                        new NpcWeightedBodyChoice(fixture.Body),
                        new NpcWeightedBodyChoice(feminineBody)
                    },
                    new[] { new NpcWeightedSkinChoice(fixture.Skin) },
                    new[] { new NpcWeightedOutfitChoice(fixture.Outfit) },
                    new[] { new NpcWeightedHairChoice(fixture.Hair) });

                Assert.That(
                    definition.MenAppearance.Bodies,
                    Has.Count.EqualTo(1));
                Assert.That(
                    definition.MenAppearance.Bodies[0].Asset,
                    Is.SameAs(fixture.Body));
                Assert.That(
                    definition.WomenAppearance.Bodies,
                    Has.Count.EqualTo(1));
                Assert.That(
                    definition.WomenAppearance.Bodies[0].Asset,
                    Is.SameAs(feminineBody));
                Assert.That(
                    definition.MenAppearance.Skins,
                    Has.Count.EqualTo(1));
                Assert.That(
                    definition.WomenAppearance.Skins,
                    Has.Count.EqualTo(1));
                Assert.That(
                    definition.MenAppearance.Outfits,
                    Has.Count.EqualTo(1));
                Assert.That(
                    definition.WomenAppearance.Outfits,
                    Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(feminineBody);
                fixture.Dispose();
            }
        }


        [Test]
        public void GeneratorUsesOnlyTheSelectedGenderPool()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcBodySilhouette feminineBody = null;
            NpcSkinPalette womenSkin = null;
            NpcPopulationDefinition definition = null;

            try
            {
                feminineBody = ScriptableObject
                    .CreateInstance<NpcBodySilhouette>();

                feminineBody.Configure(
                    "Test Feminine Body",
                    NpcBodySilhouetteKind.Feminine,
                    fixture.Body.PartShapes,
                    null);

                womenSkin = Object.Instantiate(fixture.Skin);
                womenSkin.name = "Women Pool Skin";

                NpcPopulationAppearancePool men =
                    CreateAppearancePool(
                        fixture.Body,
                        fixture.Skin,
                        fixture.Outfit,
                        fixture.Hair);

                NpcPopulationAppearancePool women =
                    CreateAppearancePool(
                        feminineBody,
                        womenSkin,
                        fixture.Outfit,
                        fixture.Hair);

                definition = ScriptableObject
                    .CreateInstance<NpcPopulationDefinition>();

                definition.Configure(
                    "Customers",
                    NpcCharacterRole.Customer,
                    men,
                    women,
                    0,
                    1);

                Assert.That(
                    NpcAppearanceGenerator.TryGenerate(
                        definition,
                        703,
                        null,
                        null,
                        out NpcAppearanceSelection generated,
                        out string reason),
                    Is.True,
                    reason);

                Assert.That(
                    generated.Gender,
                    Is.EqualTo(NpcPersonGender.Woman));
                Assert.That(
                    generated.BodySilhouette,
                    Is.SameAs(feminineBody));
                Assert.That(
                    generated.SkinPalette,
                    Is.SameAs(womenSkin));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(womenSkin);
                Object.DestroyImmediate(feminineBody);
                fixture.Dispose();
            }
        }


        [Test]
        public void SelectionRejectsBodyFromDifferentGender()
        {
            AppearanceFixture fixture = new AppearanceFixture();

            try
            {
                NpcAppearanceSelection selection =
                    new NpcAppearanceSelection(
                        NpcPersonGender.Woman,
                        fixture.Body,
                        fixture.Skin,
                        fixture.Outfit,
                        fixture.Hair);

                Assert.That(
                    selection.TryValidate(out string reason),
                    Is.False);
                StringAssert.Contains(
                    "body",
                    reason.ToLowerInvariant());
            }
            finally
            {
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


        [Test]
        public void PersonIdentity_SamePopulationAndSeedProducesSameAppearance()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            GameObject firstPerson = new GameObject("First Person");
            GameObject secondPerson = new GameObject("Second Person");

            try
            {
                definition = CreateDefinition(
                    fixture,
                    "Customers",
                    NpcCharacterRole.Customer);

                NpcPersonIdentity firstIdentity =
                    firstPerson.AddComponent<NpcPersonIdentity>();
                NpcPersonIdentity secondIdentity =
                    secondPerson.AddComponent<NpcPersonIdentity>();

                Assert.That(
                    firstIdentity.TryInitialize(
                        definition,
                        4817,
                        string.Empty,
                        out string firstFailure),
                    Is.True,
                    firstFailure);

                Assert.That(
                    secondIdentity.TryInitialize(
                        definition,
                        4817,
                        string.Empty,
                        out string secondFailure),
                    Is.True,
                    secondFailure);

                NpcAppearanceSelection first =
                    firstIdentity.CurrentAppearance;
                NpcAppearanceSelection second =
                    secondIdentity.CurrentAppearance;

                Assert.That(second.Gender, Is.EqualTo(first.Gender));
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
                Object.DestroyImmediate(firstPerson);
                Object.DestroyImmediate(secondPerson);
                Object.DestroyImmediate(definition);
                fixture.Dispose();
            }
        }


        [Test]
        public void PersonIdentity_FailedReinitializationPreservesExistingPerson()
        {
            AppearanceFixture fixture = new AppearanceFixture();
            NpcPopulationDefinition definition = null;
            GameObject person = new GameObject("Persistent Employee");

            try
            {
                definition = CreateDefinition(
                    fixture,
                    "Employees",
                    NpcCharacterRole.Employee);

                NpcPersonIdentity identity =
                    person.AddComponent<NpcPersonIdentity>();

                Assert.That(
                    identity.TryInitialize(
                        definition,
                        912,
                        "employee-42",
                        out string initialFailure),
                    Is.True,
                    initialFailure);

                NpcAppearanceSelection original =
                    identity.CurrentAppearance;

                Assert.That(
                    identity.TryInitialize(
                        null,
                        999,
                        "replacement",
                        out string failureReason),
                    Is.False);

                Assert.That(failureReason, Is.Not.Empty);
                Assert.That(identity.AppearanceSeed, Is.EqualTo(912));
                Assert.That(
                    identity.PersistentId,
                    Is.EqualTo("employee-42"));
                Assert.That(
                    identity.PopulationDefinition,
                    Is.SameAs(definition));
                Assert.That(
                    identity.CurrentAppearance.BodySilhouette,
                    Is.SameAs(original.BodySilhouette));
            }
            finally
            {
                Object.DestroyImmediate(person);
                Object.DestroyImmediate(definition);
                fixture.Dispose();
            }
        }


        [Test]
        public void PersonPrefab_HasDormantRuntimeIdentityBridge()
        {
            const string PersonPrefabPath =
                "Assets/Prefabs/Characters/Core/Person.prefab";

            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(PersonPrefabPath);

            try
            {
                NpcPersonIdentity identity =
                    prefabRoot.GetComponent<NpcPersonIdentity>();

                Assert.That(identity, Is.Not.Null);
                Assert.That(identity.InitializeOnAwake, Is.False);
                Assert.That(identity.PopulationDefinition, Is.Null);
                Assert.That(
                    prefabRoot.GetComponent<NpcCutoutRig>(),
                    Is.Not.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
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


        private static NpcPopulationAppearancePool CreateAppearancePool(
            NpcBodySilhouette body,
            NpcSkinPalette skin,
            NpcOutfitSet outfit,
            NpcHairSet hair)
        {
            NpcPopulationAppearancePool pool =
                new NpcPopulationAppearancePool();

            pool.Configure(
                new[] { new NpcWeightedBodyChoice(body) },
                new[] { new NpcWeightedSkinChoice(skin) },
                new[] { new NpcWeightedOutfitChoice(outfit) },
                new[] { new NpcWeightedHairChoice(hair) });

            return pool;
        }


        private static NpcPopulationDefinition CreateDefinition(
            AppearanceFixture fixture,
            string displayName,
            NpcCharacterRole role)
        {
            NpcPopulationDefinition definition =
                ScriptableObject.CreateInstance<NpcPopulationDefinition>();

            definition.Configure(
                displayName,
                role,
                new[] { new NpcWeightedBodyChoice(fixture.Body) },
                new[] { new NpcWeightedSkinChoice(fixture.Skin) },
                new[] { new NpcWeightedOutfitChoice(fixture.Outfit) },
                new[] { new NpcWeightedHairChoice(fixture.Hair) });

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
