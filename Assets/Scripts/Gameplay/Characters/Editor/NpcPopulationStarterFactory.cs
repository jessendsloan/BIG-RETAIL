using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    public static class NpcPopulationStarterFactory
    {
        private const string MenuPath =
            "Big Retail/Population/Setup/Repair Starter Content";

        private const string MasculineStylePackMenuPath =
            "Big Retail/Population/Setup/Add Masculine Style Pack";

        private const string RootFolder =
            "Assets/Art/Characters/Appearance";

        private const string BodyFolder = RootFolder + "/Bodies";
        private const string SkinFolder = RootFolder + "/Skin Palettes";
        private const string OutfitFolder = RootFolder + "/Outfits";
        private const string HairFolder = RootFolder + "/Hair";
        private const string DefaultFolder = RootFolder + "/Defaults";
        private const string PopulationDefinitionFolder =
            RootFolder + "/Population Definitions";
        private const string CatalogFolder = RootFolder + "/Catalog";

        private const string BasePersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";


        [MenuItem(MenuPath)]
        public static void CreateOrUpdateStarterCatalog()
        {
            EnsureFolder(BodyFolder);
            EnsureFolder(SkinFolder);
            EnsureFolder(OutfitFolder);
            EnsureFolder(HairFolder);
            EnsureFolder(DefaultFolder);
            EnsureFolder(PopulationDefinitionFolder);
            EnsureFolder(CatalogFolder);

            GameObject basePersonPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BasePersonPrefabPath);

            if (basePersonPrefab == null)
            {
                throw new UnityException(
                    "The shared base person prefab was not found at " +
                    $"'{BasePersonPrefabPath}'.");
            }

            NpcCutoutRig baseRig =
                basePersonPrefab.GetComponent<NpcCutoutRig>();

            if (baseRig == null)
            {
                throw new UnityException(
                    "The shared base person prefab has no " +
                    "NpcCutoutRig component.");
            }

            List<NpcAppearancePartShape> baselineShapes =
                CapturePartShapes(baseRig);

            List<NpcAppearanceBonePlacement> baselineBones =
                CaptureWidthBones(baseRig);

            NpcBodySilhouette masculine =
                LoadOrCreate<NpcBodySilhouette>(
                    BodyFolder + "/StandardMasculine.asset");

            masculine.Configure(
                "Standard Masculine",
                NpcBodySilhouetteKind.Masculine,
                baselineShapes,
                baselineBones);

            NpcBodySilhouette feminine =
                LoadOrCreate<NpcBodySilhouette>(
                    BodyFolder + "/StandardFeminine.asset");

            feminine.Configure(
                "Standard Feminine",
                NpcBodySilhouetteKind.Feminine,
                CreateFeminineShapes(baselineShapes),
                CreateFeminineBones(baselineBones));

            Color baselineSkin = GetPartColor(
                baseRig,
                NpcRigPartId.Head,
                new Color(0.54f, 0.31f, 0.20f, 1f));

            NpcSkinPalette warmBrown = CreateSkinPalette(
                "Warm Brown",
                baselineSkin,
                "WarmBrown");

            NpcSkinPalette deepBrown = CreateSkinPalette(
                "Deep Brown",
                new Color(0.30f, 0.15f, 0.09f, 1f),
                "DeepBrown");

            NpcSkinPalette mediumTan = CreateSkinPalette(
                "Medium Tan",
                new Color(0.66f, 0.42f, 0.27f, 1f),
                "MediumTan");

            NpcSkinPalette golden = CreateSkinPalette(
                "Golden",
                new Color(0.76f, 0.53f, 0.31f, 1f),
                "Golden");

            NpcSkinPalette lightWarm = CreateSkinPalette(
                "Light Warm",
                new Color(0.86f, 0.68f, 0.54f, 1f),
                "LightWarm");

            NpcSkinPalette rosyLight = CreateSkinPalette(
                "Rosy Light",
                new Color(0.91f, 0.72f, 0.65f, 1f),
                "RosyLight");

            NpcOutfitSet rustPolo = CreateOutfit(
                baseRig,
                "Rust Employee Polo",
                "RustEmployeePolo",
                GetPartColor(
                    baseRig,
                    NpcRigPartId.Torso,
                    new Color(0.77f, 0.28f, 0.13f, 1f)),
                GetPartColor(
                    baseRig,
                    NpcRigPartId.Pelvis,
                    new Color(0.26f, 0.20f, 0.16f, 1f)),
                GetPartColor(
                    baseRig,
                    NpcRigPartId.FootSourceCameraRight,
                    new Color(0.10f, 0.07f, 0.05f, 1f)),
                new Color(0.92f, 0.84f, 0.59f, 1f),
                true,
                false);

            NpcOutfitSet tealShortSleeve = CreateOutfit(
                baseRig,
                "Teal Short-Sleeve Employee Shirt",
                "TealShortSleeve",
                new Color(0.12f, 0.52f, 0.50f, 1f),
                new Color(0.16f, 0.20f, 0.29f, 1f),
                new Color(0.08f, 0.09f, 0.11f, 1f),
                new Color(0.95f, 0.82f, 0.46f, 1f),
                true,
                true);

            NpcOutfitSet navyJacket = CreateOutfit(
                baseRig,
                "Navy Jacket",
                "NavyJacket",
                new Color(0.12f, 0.23f, 0.42f, 1f),
                new Color(0.23f, 0.20f, 0.18f, 1f),
                new Color(0.07f, 0.07f, 0.08f, 1f),
                new Color(0.70f, 0.80f, 0.90f, 1f),
                false,
                false);

            NpcOutfitSet casualShortSleeve = CreateOutfit(
                baseRig,
                "Casual Green Short-Sleeve Shirt",
                "CasualGreenShortSleeve",
                new Color(0.23f, 0.48f, 0.28f, 1f),
                new Color(0.22f, 0.19f, 0.25f, 1f),
                new Color(0.10f, 0.08f, 0.08f, 1f),
                new Color(0.72f, 0.76f, 0.62f, 1f),
                false,
                true);

            NpcHairSet shortCrop = CreateHairSet(
                baseRig,
                "Short Crop / Black",
                "ShortCropBlack",
                GetPartColor(
                    baseRig,
                    NpcRigPartId.HairFront,
                    new Color(0.055f, 0.065f, 0.075f, 1f)),
                baselineShapes,
                1f,
                0f,
                1f,
                0f);

            NpcHairSet longAuburn = CreateHairSet(
                baseRig,
                "Long Back / Auburn",
                "LongBackAuburn",
                new Color(0.31f, 0.11f, 0.055f, 1f),
                baselineShapes,
                1.05f,
                -0.09f,
                1.10f,
                0.015f);

            NpcHairSet highTop = CreateHairSet(
                baseRig,
                "High Top / Dark",
                "HighTopDark",
                new Color(0.045f, 0.038f, 0.035f, 1f),
                baselineShapes,
                0.95f,
                0.02f,
                1.55f,
                0.055f);

            NpcHairSet closeCropSilver = CreateHairSet(
                baseRig,
                "Close Crop / Silver",
                "CloseCropSilver",
                new Color(0.47f, 0.49f, 0.52f, 1f),
                baselineShapes,
                0.82f,
                0.025f,
                0.72f,
                -0.015f);

            NpcAppearanceProfile defaultAppearance =
                LoadOrCreate<NpcAppearanceProfile>(
                    DefaultFolder + "/DefaultAppearance.asset");

            defaultAppearance.Configure(
                "Default Appearance",
                masculine,
                warmBrown,
                rustPolo,
                shortCrop);

            NpcPopulationDefinition customerDefinition =
                LoadOrCreate<NpcPopulationDefinition>(
                    PopulationDefinitionFolder + "/Customer.asset");

            customerDefinition.Configure(
                "Customer",
                NpcCharacterRole.Customer,
                CreateBodyChoices(masculine, feminine),
                CreateSkinChoices(
                    warmBrown,
                    deepBrown,
                    mediumTan,
                    golden,
                    lightWarm,
                    rosyLight),
                CreateOutfitChoices(
                    navyJacket,
                    casualShortSleeve),
                CreateHairChoices(
                    shortCrop,
                    longAuburn,
                    highTop,
                    closeCropSilver));

            NpcPopulationDefinition employeeDefinition =
                LoadOrCreate<NpcPopulationDefinition>(
                    PopulationDefinitionFolder + "/StoreEmployee.asset");

            employeeDefinition.Configure(
                "Store Employee",
                NpcCharacterRole.Employee,
                CreateBodyChoices(masculine, feminine),
                CreateSkinChoices(
                    warmBrown,
                    deepBrown,
                    mediumTan,
                    golden,
                    lightWarm,
                    rosyLight),
                CreateOutfitChoices(
                    rustPolo,
                    tealShortSleeve),
                CreateHairChoices(
                    shortCrop,
                    longAuburn,
                    highTop,
                    closeCropSilver));

            NpcAppearanceCatalog catalog =
                LoadOrCreate<NpcAppearanceCatalog>(
                    CatalogFolder + "/PersonAppearanceCatalog.asset");

            catalog.Configure(
                "Person Appearance Catalog",
                new[] { customerDefinition, employeeDefinition },
                new[] { masculine, feminine },
                new[]
                {
                    warmBrown,
                    deepBrown,
                    mediumTan,
                    golden,
                    lightWarm,
                    rosyLight
                },
                new[]
                {
                    rustPolo,
                    tealShortSleeve,
                    navyJacket,
                    casualShortSleeve
                },
                new[]
                {
                    shortCrop,
                    longAuburn,
                    highTop,
                    closeCropSilver
                });

            MarkDirty(
                masculine,
                feminine,
                warmBrown,
                deepBrown,
                mediumTan,
                golden,
                lightWarm,
                rosyLight,
                rustPolo,
                tealShortSleeve,
                navyJacket,
                casualShortSleeve,
                shortCrop,
                longAuburn,
                highTop,
                closeCropSilver,
                defaultAppearance,
                customerDefinition,
                employeeDefinition,
                catalog);

            AssignProfileToPrefab(
                BasePersonPrefabPath,
                defaultAppearance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Repair restores the complete supported starter library. The
            // pack command is idempotent, so this never duplicates choices.
            AddMasculineStylePack();

            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);

            Debug.Log(
                "Population Definitions starter content is ready: Customer " +
                "and Store Employee definitions, four body silhouettes, " +
                "six skin palettes, seven outfits, seven hair sets, and " +
                "one neutral default appearance.");
        }


        [MenuItem(MasculineStylePackMenuPath)]
        public static void AddMasculineStylePack()
        {
            EnsureFolder(BodyFolder);
            EnsureFolder(OutfitFolder);
            EnsureFolder(HairFolder);

            GameObject basePersonPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BasePersonPrefabPath);

            NpcCutoutRig baseRig =
                basePersonPrefab != null
                    ? basePersonPrefab.GetComponent<NpcCutoutRig>()
                    : null;

            if (baseRig == null)
            {
                throw new UnityException(
                    "The shared Person prefab is missing or has no " +
                    "NpcCutoutRig. Repair the base character before " +
                    "installing the style pack.");
            }

            NpcPopulationDefinition customer =
                AssetDatabase.LoadAssetAtPath<NpcPopulationDefinition>(
                    PopulationDefinitionFolder + "/Customer.asset");

            NpcPopulationDefinition employee =
                AssetDatabase.LoadAssetAtPath<NpcPopulationDefinition>(
                    PopulationDefinitionFolder + "/StoreEmployee.asset");

            NpcAppearanceCatalog catalog =
                AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(
                    CatalogFolder + "/PersonAppearanceCatalog.asset");

            if (customer == null || employee == null || catalog == null)
            {
                throw new UnityException(
                    "The starter population library is incomplete. Run " +
                    "Repair Starter Content once, then add this pack.");
            }

            List<NpcAppearancePartShape> baselineShapes =
                CapturePartShapes(baseRig);

            List<NpcAppearanceBonePlacement> baselineBones =
                CaptureWidthBones(baseRig);

            NpcBodySilhouette leanMasculine =
                CreateMasculineBodyVariant(
                    "Lean Masculine",
                    "LeanMasculine",
                    baselineShapes,
                    baselineBones,
                    0.88f,
                    0.90f,
                    0.88f,
                    0.92f,
                    0.88f,
                    0.92f);

            NpcBodySilhouette broadMasculine =
                CreateMasculineBodyVariant(
                    "Broad Masculine",
                    "BroadMasculine",
                    baselineShapes,
                    baselineBones,
                    1.13f,
                    1.04f,
                    1.08f,
                    1.03f,
                    1.12f,
                    1.04f);

            NpcOutfitSet burgundyCrewneck = CreateOutfit(
                baseRig,
                "Burgundy Crewneck / Dark Denim",
                "BurgundyCrewneckDarkDenim",
                new Color(0.43f, 0.10f, 0.15f, 1f),
                new Color(0.10f, 0.16f, 0.24f, 1f),
                new Color(0.08f, 0.065f, 0.055f, 1f),
                new Color(0.72f, 0.58f, 0.42f, 1f),
                false,
                false,
                NpcGenderCompatibility.Men);

            NpcOutfitSet mustardOvershirt = CreateOutfit(
                baseRig,
                "Mustard Overshirt / Navy Chinos",
                "MustardOvershirtNavyChinos",
                new Color(0.68f, 0.43f, 0.10f, 1f),
                new Color(0.11f, 0.17f, 0.26f, 1f),
                new Color(0.16f, 0.10f, 0.06f, 1f),
                new Color(0.91f, 0.78f, 0.42f, 1f),
                false,
                true,
                NpcGenderCompatibility.Men);

            NpcOutfitSet slateEmployeeShirt = CreateOutfit(
                baseRig,
                "Slate Employee Shirt",
                "SlateEmployeeShirt",
                new Color(0.22f, 0.31f, 0.38f, 1f),
                new Color(0.13f, 0.15f, 0.18f, 1f),
                new Color(0.055f, 0.06f, 0.07f, 1f),
                new Color(0.93f, 0.77f, 0.36f, 1f),
                true,
                true,
                NpcGenderCompatibility.Men);

            NpcHairSet sidePart = CreateHairSet(
                baseRig,
                "Tidy Side Part / Chestnut",
                "TidySidePartChestnut",
                new Color(0.22f, 0.095f, 0.045f, 1f),
                baselineShapes,
                0.78f,
                0.015f,
                0.88f,
                0.025f,
                0.92f,
                1.12f,
                NpcGenderCompatibility.Men,
                CreateSidePartLayers(baseRig));

            NpcHairSet buzzCut = CreateHairSet(
                baseRig,
                "Buzz Cut / Dark Brown",
                "BuzzCutDarkBrown",
                new Color(0.095f, 0.065f, 0.05f, 1f),
                baselineShapes,
                0.56f,
                0.04f,
                0.48f,
                0.055f,
                0.80f,
                0.82f,
                NpcGenderCompatibility.Men,
                CreateBuzzCutLayers(baseRig));

            NpcHairSet tousledCrop = CreateHairSet(
                baseRig,
                "Tousled Crop / Sandy Brown",
                "TousledCropSandyBrown",
                new Color(0.43f, 0.29f, 0.16f, 1f),
                baselineShapes,
                0.82f,
                0.015f,
                1.18f,
                0.045f,
                0.94f,
                1.08f,
                NpcGenderCompatibility.Men,
                CreateTousledCropLayers(baseRig));

            NpcPopulationAppearancePool customerMen =
                ExpandAppearancePool(
                    customer.MenAppearance,
                    new[] { leanMasculine, broadMasculine },
                    new[] { burgundyCrewneck, mustardOvershirt },
                    new[] { sidePart, buzzCut, tousledCrop });

            customer.Configure(
                customer.DisplayName,
                customer.Role,
                customerMen,
                customer.WomenAppearance,
                customer.MenWeight,
                customer.WomenWeight);

            NpcPopulationAppearancePool employeeMen =
                ExpandAppearancePool(
                    employee.MenAppearance,
                    new[] { leanMasculine, broadMasculine },
                    new[] { slateEmployeeShirt },
                    new[] { sidePart, buzzCut, tousledCrop });

            employee.Configure(
                employee.DisplayName,
                employee.Role,
                employeeMen,
                employee.WomenAppearance,
                employee.MenWeight,
                employee.WomenWeight);

            catalog.RegisterAssetsFrom(customer);
            catalog.RegisterAssetsFrom(employee);

            MarkDirty(
                leanMasculine,
                broadMasculine,
                burgundyCrewneck,
                mustardOvershirt,
                slateEmployeeShirt,
                sidePart,
                buzzCut,
                tousledCrop,
                customer,
                employee,
                catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);

            Debug.Log(
                "Masculine style pack installed: two body types, three " +
                "hairstyles, two customer outfits, and one employee " +
                "uniform were added to the Men appearance pools. Women " +
                "appearance pools were preserved unchanged.");
        }


        private static NpcWeightedBodyChoice[] CreateBodyChoices(
            params NpcBodySilhouette[] assets)
        {
            NpcWeightedBodyChoice[] choices =
                new NpcWeightedBodyChoice[assets.Length];

            for (int index = 0; index < assets.Length; index++)
            {
                choices[index] = new NpcWeightedBodyChoice(assets[index]);
            }

            return choices;
        }


        private static NpcWeightedSkinChoice[] CreateSkinChoices(
            params NpcSkinPalette[] assets)
        {
            NpcWeightedSkinChoice[] choices =
                new NpcWeightedSkinChoice[assets.Length];

            for (int index = 0; index < assets.Length; index++)
            {
                choices[index] = new NpcWeightedSkinChoice(assets[index]);
            }

            return choices;
        }


        private static NpcWeightedOutfitChoice[] CreateOutfitChoices(
            params NpcOutfitSet[] assets)
        {
            NpcWeightedOutfitChoice[] choices =
                new NpcWeightedOutfitChoice[assets.Length];

            for (int index = 0; index < assets.Length; index++)
            {
                choices[index] = new NpcWeightedOutfitChoice(assets[index]);
            }

            return choices;
        }


        private static NpcWeightedHairChoice[] CreateHairChoices(
            params NpcHairSet[] assets)
        {
            NpcWeightedHairChoice[] choices =
                new NpcWeightedHairChoice[assets.Length];

            for (int index = 0; index < assets.Length; index++)
            {
                choices[index] = new NpcWeightedHairChoice(assets[index]);
            }

            return choices;
        }


        private static NpcPopulationAppearancePool ExpandAppearancePool(
            NpcPopulationAppearancePool source,
            IReadOnlyList<NpcBodySilhouette> addedBodies,
            IReadOnlyList<NpcOutfitSet> addedOutfits,
            IReadOnlyList<NpcHairSet> addedHair)
        {
            source ??= new NpcPopulationAppearancePool();

            NpcPopulationAppearancePool expanded =
                new NpcPopulationAppearancePool();

            expanded.Configure(
                AppendBodyChoices(source.Bodies, addedBodies),
                source.Skins,
                AppendOutfitChoices(source.Outfits, addedOutfits),
                AppendHairChoices(source.Hair, addedHair));

            return expanded;
        }


        private static List<NpcWeightedBodyChoice> AppendBodyChoices(
            IReadOnlyList<NpcWeightedBodyChoice> source,
            IReadOnlyList<NpcBodySilhouette> additions)
        {
            List<NpcWeightedBodyChoice> choices =
                new List<NpcWeightedBodyChoice>();

            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                {
                    choices.Add(source[index]);
                }
            }

            if (additions == null)
            {
                return choices;
            }

            for (int index = 0; index < additions.Count; index++)
            {
                NpcBodySilhouette asset = additions[index];

                if (asset != null && !ContainsBody(choices, asset))
                {
                    choices.Add(new NpcWeightedBodyChoice(asset));
                }
            }

            return choices;
        }


        private static List<NpcWeightedOutfitChoice> AppendOutfitChoices(
            IReadOnlyList<NpcWeightedOutfitChoice> source,
            IReadOnlyList<NpcOutfitSet> additions)
        {
            List<NpcWeightedOutfitChoice> choices =
                new List<NpcWeightedOutfitChoice>();

            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                {
                    choices.Add(source[index]);
                }
            }

            if (additions == null)
            {
                return choices;
            }

            for (int index = 0; index < additions.Count; index++)
            {
                NpcOutfitSet asset = additions[index];

                if (asset != null && !ContainsOutfit(choices, asset))
                {
                    choices.Add(new NpcWeightedOutfitChoice(asset));
                }
            }

            return choices;
        }


        private static List<NpcWeightedHairChoice> AppendHairChoices(
            IReadOnlyList<NpcWeightedHairChoice> source,
            IReadOnlyList<NpcHairSet> additions)
        {
            List<NpcWeightedHairChoice> choices =
                new List<NpcWeightedHairChoice>();

            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                {
                    choices.Add(source[index]);
                }
            }

            if (additions == null)
            {
                return choices;
            }

            for (int index = 0; index < additions.Count; index++)
            {
                NpcHairSet asset = additions[index];

                if (asset != null && !ContainsHair(choices, asset))
                {
                    choices.Add(new NpcWeightedHairChoice(asset));
                }
            }

            return choices;
        }


        private static bool ContainsBody(
            IReadOnlyList<NpcWeightedBodyChoice> choices,
            NpcBodySilhouette asset)
        {
            for (int index = 0; index < choices.Count; index++)
            {
                if (choices[index]?.Asset == asset)
                {
                    return true;
                }
            }

            return false;
        }


        private static bool ContainsOutfit(
            IReadOnlyList<NpcWeightedOutfitChoice> choices,
            NpcOutfitSet asset)
        {
            for (int index = 0; index < choices.Count; index++)
            {
                if (choices[index]?.Asset == asset)
                {
                    return true;
                }
            }

            return false;
        }


        private static bool ContainsHair(
            IReadOnlyList<NpcWeightedHairChoice> choices,
            NpcHairSet asset)
        {
            for (int index = 0; index < choices.Count; index++)
            {
                if (choices[index]?.Asset == asset)
                {
                    return true;
                }
            }

            return false;
        }


        private static List<NpcAppearancePartShape> CapturePartShapes(
            NpcCutoutRig rig)
        {
            List<NpcAppearancePartShape> shapes =
                new List<NpcAppearancePartShape>(
                    NpcRigDefinition.ExpectedPartCount);

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (!rig.TryGetPartRenderer(
                        definition.Id,
                        out SpriteRenderer renderer))
                {
                    throw new UnityException(
                        "The shared base rig is missing part " +
                        $"{definition.Id}.");
                }

                Transform partTransform = renderer.transform;
                Vector2 size = renderer.sprite != null
                    ? Vector2.Scale(
                        renderer.sprite.bounds.size,
                        new Vector2(
                            Mathf.Abs(partTransform.localScale.x),
                            Mathf.Abs(partTransform.localScale.y)))
                    : new Vector2(
                        Mathf.Abs(partTransform.localScale.x),
                        Mathf.Abs(partTransform.localScale.y));

                shapes.Add(
                    new NpcAppearancePartShape(
                        definition.Id,
                        partTransform.localPosition,
                        partTransform.localEulerAngles,
                        size,
                        renderer.enabled));
            }

            return shapes;
        }


        private static List<NpcAppearanceBonePlacement> CaptureWidthBones(
            NpcCutoutRig rig)
        {
            NpcRigBoneId[] widthBones =
            {
                NpcRigBoneId.ShoulderSourceCameraLeft,
                NpcRigBoneId.ShoulderSourceCameraRight,
                NpcRigBoneId.ThighSourceCameraLeft,
                NpcRigBoneId.ThighSourceCameraRight
            };

            List<NpcAppearanceBonePlacement> placements =
                new List<NpcAppearanceBonePlacement>();

            for (int index = 0; index < widthBones.Length; index++)
            {
                if (rig.TryGetBone(
                        widthBones[index],
                        out Transform bone))
                {
                    placements.Add(
                        new NpcAppearanceBonePlacement(
                            widthBones[index],
                            bone.localPosition));
                }
            }

            return placements;
        }


        private static List<NpcAppearancePartShape> CreateFeminineShapes(
            IReadOnlyList<NpcAppearancePartShape> source)
        {
            List<NpcAppearancePartShape> shapes =
                new List<NpcAppearancePartShape>(source.Count);

            for (int index = 0; index < source.Count; index++)
            {
                NpcAppearancePartShape shape = source[index];
                Vector2 size = shape.Size;

                switch (shape.Id)
                {
                    case NpcRigPartId.Torso:
                        size.x *= 0.84f;
                        break;

                    case NpcRigPartId.Pelvis:
                        size.x *= 0.90f;
                        break;

                    case NpcRigPartId.UpperArmSourceCameraLeft:
                    case NpcRigPartId.UpperArmSourceCameraRight:
                    case NpcRigPartId.ForearmSourceCameraLeft:
                    case NpcRigPartId.ForearmSourceCameraRight:
                    case NpcRigPartId.HandSourceCameraLeft:
                    case NpcRigPartId.HandSourceCameraRight:
                        size.x *= 0.88f;
                        break;

                    case NpcRigPartId.ThighSourceCameraLeft:
                    case NpcRigPartId.ThighSourceCameraRight:
                    case NpcRigPartId.ShinSourceCameraLeft:
                    case NpcRigPartId.ShinSourceCameraRight:
                        size.x *= 0.92f;
                        break;
                }

                shapes.Add(shape.WithSize(size));
            }

            return shapes;
        }


        private static List<NpcAppearanceBonePlacement>
            CreateFeminineBones(
                IReadOnlyList<NpcAppearanceBonePlacement> source)
        {
            List<NpcAppearanceBonePlacement> placements =
                new List<NpcAppearanceBonePlacement>(source.Count);

            for (int index = 0; index < source.Count; index++)
            {
                NpcAppearanceBonePlacement placement = source[index];
                Vector3 position = placement.LocalPosition;

                switch (placement.Id)
                {
                    case NpcRigBoneId.ShoulderSourceCameraLeft:
                    case NpcRigBoneId.ShoulderSourceCameraRight:
                        position.x *= 0.80f;
                        break;

                    case NpcRigBoneId.ThighSourceCameraLeft:
                    case NpcRigBoneId.ThighSourceCameraRight:
                        position.x *= 0.88f;
                        break;
                }

                placements.Add(
                    new NpcAppearanceBonePlacement(
                        placement.Id,
                        position));
            }

            return placements;
        }


        private static NpcBodySilhouette CreateMasculineBodyVariant(
            string displayName,
            string fileName,
            IReadOnlyList<NpcAppearancePartShape> sourceShapes,
            IReadOnlyList<NpcAppearanceBonePlacement> sourceBones,
            float torsoWidth,
            float pelvisWidth,
            float armWidth,
            float legWidth,
            float shoulderSpacing,
            float thighSpacing)
        {
            NpcBodySilhouette body =
                LoadOrCreate<NpcBodySilhouette>(
                    BodyFolder + "/" + fileName + ".asset");

            body.Configure(
                displayName,
                NpcBodySilhouetteKind.Masculine,
                CreateMasculineShapes(
                    sourceShapes,
                    torsoWidth,
                    pelvisWidth,
                    armWidth,
                    legWidth),
                CreateMasculineBones(
                    sourceBones,
                    shoulderSpacing,
                    thighSpacing));

            EditorUtility.SetDirty(body);
            return body;
        }


        private static List<NpcAppearancePartShape>
            CreateMasculineShapes(
                IReadOnlyList<NpcAppearancePartShape> source,
                float torsoWidth,
                float pelvisWidth,
                float armWidth,
                float legWidth)
        {
            List<NpcAppearancePartShape> shapes =
                new List<NpcAppearancePartShape>(source.Count);

            for (int index = 0; index < source.Count; index++)
            {
                NpcAppearancePartShape shape = source[index];
                Vector2 size = shape.Size;

                switch (shape.Id)
                {
                    case NpcRigPartId.Torso:
                        size.x *= torsoWidth;
                        break;

                    case NpcRigPartId.Pelvis:
                        size.x *= pelvisWidth;
                        break;

                    case NpcRigPartId.UpperArmSourceCameraLeft:
                    case NpcRigPartId.UpperArmSourceCameraRight:
                    case NpcRigPartId.ForearmSourceCameraLeft:
                    case NpcRigPartId.ForearmSourceCameraRight:
                    case NpcRigPartId.HandSourceCameraLeft:
                    case NpcRigPartId.HandSourceCameraRight:
                        size.x *= armWidth;
                        break;

                    case NpcRigPartId.ThighSourceCameraLeft:
                    case NpcRigPartId.ThighSourceCameraRight:
                    case NpcRigPartId.ShinSourceCameraLeft:
                    case NpcRigPartId.ShinSourceCameraRight:
                        size.x *= legWidth;
                        break;
                }

                shapes.Add(shape.WithSize(size));
            }

            return shapes;
        }


        private static List<NpcAppearanceBonePlacement>
            CreateMasculineBones(
                IReadOnlyList<NpcAppearanceBonePlacement> source,
                float shoulderSpacing,
                float thighSpacing)
        {
            List<NpcAppearanceBonePlacement> placements =
                new List<NpcAppearanceBonePlacement>(source.Count);

            for (int index = 0; index < source.Count; index++)
            {
                NpcAppearanceBonePlacement placement = source[index];
                Vector3 position = placement.LocalPosition;

                switch (placement.Id)
                {
                    case NpcRigBoneId.ShoulderSourceCameraLeft:
                    case NpcRigBoneId.ShoulderSourceCameraRight:
                        position.x *= shoulderSpacing;
                        break;

                    case NpcRigBoneId.ThighSourceCameraLeft:
                    case NpcRigBoneId.ThighSourceCameraRight:
                        position.x *= thighSpacing;
                        break;
                }

                placements.Add(
                    new NpcAppearanceBonePlacement(
                        placement.Id,
                        position));
            }

            return placements;
        }


        private static NpcSkinPalette CreateSkinPalette(
            string displayName,
            Color color,
            string fileName)
        {
            NpcSkinPalette palette =
                LoadOrCreate<NpcSkinPalette>(
                    SkinFolder + "/" + fileName + ".asset");

            palette.Configure(displayName, color);
            EditorUtility.SetDirty(palette);
            return palette;
        }


        private static NpcOutfitSet CreateOutfit(
            NpcCutoutRig rig,
            string displayName,
            string fileName,
            Color primary,
            Color secondary,
            Color footwear,
            Color accent,
            bool showBadge,
            bool exposeForearms,
            NpcGenderCompatibility supportedGenders =
                NpcGenderCompatibility.Everyone)
        {
            List<NpcOutfitPartStyle> styles =
                new List<NpcOutfitPartStyle>();

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                NpcRigPartId id = definition.Id;

                if (id == NpcRigPartId.HairRear
                    || id == NpcRigPartId.HairFront
                    || id == NpcRigPartId.Head
                    || id == NpcRigPartId.Neck)
                {
                    continue;
                }

                NpcAppearanceColorRole role = GetOutfitRole(
                    id,
                    exposeForearms);

                Sprite sprite = GetPartSprite(rig, id);

                styles.Add(
                    new NpcOutfitPartStyle(
                        id,
                        role,
                        sprite,
                        sprite));
            }

            NpcOutfitSet outfit =
                LoadOrCreate<NpcOutfitSet>(
                    OutfitFolder + "/" + fileName + ".asset");

            outfit.Configure(
                displayName,
                primary,
                secondary,
                footwear,
                accent,
                showBadge,
                supportedGenders,
                styles);

            EditorUtility.SetDirty(outfit);
            return outfit;
        }


        private static NpcAppearanceColorRole GetOutfitRole(
            NpcRigPartId id,
            bool exposeForearms)
        {
            switch (id)
            {
                case NpcRigPartId.HandSourceCameraLeft:
                case NpcRigPartId.HandSourceCameraRight:
                    return NpcAppearanceColorRole.Skin;

                case NpcRigPartId.ForearmSourceCameraLeft:
                case NpcRigPartId.ForearmSourceCameraRight:
                    return exposeForearms
                        ? NpcAppearanceColorRole.Skin
                        : NpcAppearanceColorRole.PrimaryFabric;

                case NpcRigPartId.Torso:
                case NpcRigPartId.UpperArmSourceCameraLeft:
                case NpcRigPartId.UpperArmSourceCameraRight:
                    return NpcAppearanceColorRole.PrimaryFabric;

                case NpcRigPartId.Pelvis:
                case NpcRigPartId.ThighSourceCameraLeft:
                case NpcRigPartId.ThighSourceCameraRight:
                case NpcRigPartId.ShinSourceCameraLeft:
                case NpcRigPartId.ShinSourceCameraRight:
                    return NpcAppearanceColorRole.SecondaryFabric;

                case NpcRigPartId.FootSourceCameraLeft:
                case NpcRigPartId.FootSourceCameraRight:
                    return NpcAppearanceColorRole.Footwear;

                default:
                    return NpcAppearanceColorRole.Preserve;
            }
        }


        private static NpcHairSet CreateHairSet(
            NpcCutoutRig rig,
            string displayName,
            string fileName,
            Color color,
            IReadOnlyList<NpcAppearancePartShape> bodyShapes,
            float rearHeightMultiplier,
            float rearYOffset,
            float frontHeightMultiplier,
            float frontYOffset,
            float rearWidthMultiplier = 1f,
            float frontWidthMultiplier = 1f,
            NpcGenderCompatibility supportedGenders =
                NpcGenderCompatibility.Everyone,
            IEnumerable<NpcHairDetailLayer> detailLayers = null)
        {
            NpcAppearancePartShape rearBase = FindShape(
                bodyShapes,
                NpcRigPartId.HairRear);

            NpcAppearancePartShape frontBase = FindShape(
                bodyShapes,
                NpcRigPartId.HairFront);

            NpcAppearancePartShape rearShape =
                CreateHairShape(
                    rearBase,
                    rearWidthMultiplier,
                    rearHeightMultiplier,
                    rearYOffset);

            NpcAppearancePartShape frontShape =
                CreateHairShape(
                    frontBase,
                    frontWidthMultiplier,
                    frontHeightMultiplier,
                    frontYOffset);

            Sprite rearSprite = GetPartSprite(
                rig,
                NpcRigPartId.HairRear);

            Sprite frontSprite = GetPartSprite(
                rig,
                NpcRigPartId.HairFront);

            NpcHairSet hair =
                LoadOrCreate<NpcHairSet>(
                    HairFolder + "/" + fileName + ".asset");

            hair.Configure(
                displayName,
                color,
                supportedGenders,
                new NpcOutfitPartStyle(
                    NpcRigPartId.HairRear,
                    NpcAppearanceColorRole.Preserve,
                    rearSprite,
                    rearSprite),
                new NpcOutfitPartStyle(
                    NpcRigPartId.HairFront,
                    NpcAppearanceColorRole.Preserve,
                    frontSprite,
                    frontSprite),
                rearShape,
                frontShape,
                detailLayers);

            EditorUtility.SetDirty(hair);
            return hair;
        }


        private static IEnumerable<NpcHairDetailLayer>
            CreateSidePartLayers(
                NpcCutoutRig rig)
        {
            Sprite sprite = GetPartSprite(
                rig,
                NpcRigPartId.HairFront);

            return new[]
            {
                CreateHairLayer(
                    "Side Sweep",
                    NpcHairLayerDepth.Crown,
                    1.08f,
                    sprite,
                    new Vector2(0.055f, 0.245f),
                    new Vector2(0.25f, 0.10f),
                    -10f,
                    new Vector2(-0.055f, 0.245f),
                    new Vector2(0.25f, 0.10f),
                    10f),
                CreateHairLayer(
                    "Side Lock",
                    NpcHairLayerDepth.Fringe,
                    0.90f,
                    sprite,
                    new Vector2(0.18f, 0.105f),
                    new Vector2(0.075f, 0.16f),
                    -8f,
                    new Vector2(-0.18f, 0.105f),
                    new Vector2(0.075f, 0.16f),
                    8f,
                    true,
                    false),
                CreateHairLayer(
                    "Part Line",
                    NpcHairLayerDepth.Fringe,
                    0.55f,
                    sprite,
                    new Vector2(-0.045f, 0.258f),
                    new Vector2(0.14f, 0.022f),
                    -7f,
                    new Vector2(0.045f, 0.258f),
                    new Vector2(0.14f, 0.022f),
                    7f,
                    true,
                    false)
            };
        }


        private static IEnumerable<NpcHairDetailLayer>
            CreateBuzzCutLayers(
                NpcCutoutRig rig)
        {
            Sprite sprite = GetPartSprite(
                rig,
                NpcRigPartId.HairRear);

            return new[]
            {
                CreateHairLayer(
                    "Camera Left Taper",
                    NpcHairLayerDepth.BehindHead,
                    0.72f,
                    sprite,
                    new Vector2(-0.185f, 0.085f),
                    new Vector2(0.055f, 0.15f),
                    0f,
                    new Vector2(-0.185f, 0.085f),
                    new Vector2(0.055f, 0.15f),
                    0f),
                CreateHairLayer(
                    "Camera Right Taper",
                    NpcHairLayerDepth.BehindHead,
                    0.82f,
                    sprite,
                    new Vector2(0.185f, 0.085f),
                    new Vector2(0.055f, 0.15f),
                    0f,
                    new Vector2(0.185f, 0.085f),
                    new Vector2(0.055f, 0.15f),
                    0f)
            };
        }


        private static IEnumerable<NpcHairDetailLayer>
            CreateTousledCropLayers(
                NpcCutoutRig rig)
        {
            Sprite sprite = GetPartSprite(
                rig,
                NpcRigPartId.HairFront);

            return new[]
            {
                CreateHairLayer(
                    "Left Crown Tuft",
                    NpcHairLayerDepth.Crown,
                    0.94f,
                    sprite,
                    new Vector2(-0.12f, 0.255f),
                    new Vector2(0.13f, 0.075f),
                    24f,
                    new Vector2(-0.12f, 0.255f),
                    new Vector2(0.13f, 0.075f),
                    24f),
                CreateHairLayer(
                    "Center Crown Tuft",
                    NpcHairLayerDepth.Crown,
                    1.08f,
                    sprite,
                    new Vector2(0f, 0.275f),
                    new Vector2(0.14f, 0.085f),
                    2f,
                    new Vector2(0f, 0.275f),
                    new Vector2(0.14f, 0.085f),
                    -2f),
                CreateHairLayer(
                    "Right Crown Tuft",
                    NpcHairLayerDepth.Crown,
                    0.88f,
                    sprite,
                    new Vector2(0.12f, 0.25f),
                    new Vector2(0.13f, 0.075f),
                    -24f,
                    new Vector2(0.12f, 0.25f),
                    new Vector2(0.13f, 0.075f),
                    -24f),
                CreateHairLayer(
                    "Loose Fringe",
                    NpcHairLayerDepth.Fringe,
                    0.98f,
                    sprite,
                    new Vector2(0.14f, 0.16f),
                    new Vector2(0.075f, 0.14f),
                    -17f,
                    new Vector2(-0.14f, 0.16f),
                    new Vector2(0.075f, 0.14f),
                    17f,
                    true,
                    false)
            };
        }


        private static NpcHairDetailLayer CreateHairLayer(
            string displayName,
            NpcHairLayerDepth depth,
            float shadeMultiplier,
            Sprite sprite,
            Vector2 southEastPosition,
            Vector2 southEastSize,
            float southEastAngle,
            Vector2 northEastPosition,
            Vector2 northEastSize,
            float northEastAngle,
            bool southEastVisible = true,
            bool northEastVisible = true)
        {
            return new NpcHairDetailLayer(
                displayName,
                depth,
                shadeMultiplier,
                sprite,
                sprite,
                new NpcHairLayerPose(
                    new Vector3(
                        southEastPosition.x,
                        southEastPosition.y,
                        0f),
                    new Vector3(0f, 0f, southEastAngle),
                    southEastSize,
                    southEastVisible),
                new NpcHairLayerPose(
                    new Vector3(
                        northEastPosition.x,
                        northEastPosition.y,
                        0f),
                    new Vector3(0f, 0f, northEastAngle),
                    northEastSize,
                    northEastVisible));
        }


        private static NpcAppearancePartShape CreateHairShape(
            NpcAppearancePartShape source,
            float widthMultiplier,
            float heightMultiplier,
            float yOffset)
        {
            Vector2 size = source.Size;
            size.x *= widthMultiplier;
            size.y *= heightMultiplier;

            Vector3 position = source.LocalPosition;
            position.y += yOffset;

            return new NpcAppearancePartShape(
                source.Id,
                position,
                source.LocalEulerAngles,
                size,
                source.Visible);
        }


        private static NpcAppearancePartShape FindShape(
            IReadOnlyList<NpcAppearancePartShape> shapes,
            NpcRigPartId id)
        {
            for (int index = 0; index < shapes.Count; index++)
            {
                if (shapes[index].Id == id)
                {
                    return shapes[index];
                }
            }

            throw new UnityException(
                $"Captured silhouette is missing {id}.");
        }


        private static Sprite GetPartSprite(
            NpcCutoutRig rig,
            NpcRigPartId id)
        {
            return rig.TryGetPartRenderer(
                       id,
                       out SpriteRenderer renderer)
                ? renderer.sprite
                : null;
        }


        private static Color GetPartColor(
            NpcCutoutRig rig,
            NpcRigPartId id,
            Color fallback)
        {
            return rig.TryGetPartRenderer(
                       id,
                       out SpriteRenderer renderer)
                ? renderer.color
                : fallback;
        }


        private static void AssignProfileToPrefab(
            string prefabPath,
            NpcAppearanceProfile profile)
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                NpcCutoutRig rig = root.GetComponent<NpcCutoutRig>();

                if (rig == null)
                {
                    throw new UnityException(
                        $"Prefab '{prefabPath}' has no NpcCutoutRig.");
                }

                rig.SetAppearanceProfile(profile);
                EditorUtility.SetDirty(rig);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }


        private static T LoadOrCreate<T>(
            string assetPath)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }


        private static void MarkDirty(
            params Object[] assets)
        {
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] != null)
                {
                    EditorUtility.SetDirty(assets[index]);
                }
            }
        }


        private static void EnsureFolder(
            string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
