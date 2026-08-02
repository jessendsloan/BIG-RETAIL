using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    public static class NpcAppearanceStarterFactory
    {
        private const string MenuPath =
            "Big Retail/Characters/Appearance Creator/Create Starter Library";

        private const string RootFolder =
            "Assets/Art/Characters/Appearance";

        private const string BodyFolder = RootFolder + "/Bodies";
        private const string SkinFolder = RootFolder + "/Skin Palettes";
        private const string OutfitFolder = RootFolder + "/Outfits";
        private const string HairFolder = RootFolder + "/Hair";
        private const string ProfileFolder = RootFolder + "/Profiles";

        private const string RowanPrefabPath =
            "Assets/Prefabs/Characters/Prototype/RoundedEmployeeRowan.prefab";

        private const string MinaPrefabPath =
            "Assets/Prefabs/Characters/Prototype/RoundedEmployeeMina.prefab";


        [MenuItem(MenuPath)]
        public static void CreateOrUpdateStarterLibrary()
        {
            EnsureFolder(BodyFolder);
            EnsureFolder(SkinFolder);
            EnsureFolder(OutfitFolder);
            EnsureFolder(HairFolder);
            EnsureFolder(ProfileFolder);

            GameObject rowanPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RowanPrefabPath);

            if (rowanPrefab == null)
            {
                throw new UnityException(
                    $"Rowan prefab was not found at '{RowanPrefabPath}'.");
            }

            NpcCutoutRig rowanRig =
                rowanPrefab.GetComponent<NpcCutoutRig>();

            if (rowanRig == null)
            {
                throw new UnityException(
                    "Rowan prefab has no NpcCutoutRig component.");
            }

            List<NpcAppearancePartShape> rowanShapes =
                CapturePartShapes(rowanRig);

            List<NpcAppearanceBonePlacement> rowanBones =
                CaptureWidthBones(rowanRig);

            NpcBodySilhouette masculine =
                LoadOrCreate<NpcBodySilhouette>(
                    BodyFolder + "/StandardMasculine.asset");

            masculine.Configure(
                "Standard Masculine",
                NpcBodySilhouetteKind.Masculine,
                rowanShapes,
                rowanBones);

            NpcBodySilhouette feminine =
                LoadOrCreate<NpcBodySilhouette>(
                    BodyFolder + "/StandardFeminine.asset");

            feminine.Configure(
                "Standard Feminine",
                NpcBodySilhouetteKind.Feminine,
                CreateFeminineShapes(rowanShapes),
                CreateFeminineBones(rowanBones));

            Color rowanSkin = GetPartColor(
                rowanRig,
                NpcRigPartId.Head,
                new Color(0.54f, 0.31f, 0.20f, 1f));

            NpcSkinPalette warmBrown = CreateSkinPalette(
                "Warm Brown",
                rowanSkin,
                "WarmBrown");

            NpcSkinPalette deepBrown = CreateSkinPalette(
                "Deep Brown",
                new Color(0.30f, 0.15f, 0.09f, 1f),
                "DeepBrown");

            NpcSkinPalette mediumTan = CreateSkinPalette(
                "Medium Tan",
                new Color(0.66f, 0.42f, 0.27f, 1f),
                "MediumTan");

            CreateSkinPalette(
                "Golden",
                new Color(0.76f, 0.53f, 0.31f, 1f),
                "Golden");

            CreateSkinPalette(
                "Light Warm",
                new Color(0.86f, 0.68f, 0.54f, 1f),
                "LightWarm");

            CreateSkinPalette(
                "Rosy Light",
                new Color(0.91f, 0.72f, 0.65f, 1f),
                "RosyLight");

            NpcOutfitSet rustPolo = CreateOutfit(
                rowanRig,
                "Rust Employee Polo",
                "RustEmployeePolo",
                GetPartColor(
                    rowanRig,
                    NpcRigPartId.Torso,
                    new Color(0.77f, 0.28f, 0.13f, 1f)),
                GetPartColor(
                    rowanRig,
                    NpcRigPartId.Pelvis,
                    new Color(0.26f, 0.20f, 0.16f, 1f)),
                GetPartColor(
                    rowanRig,
                    NpcRigPartId.FootSourceCameraRight,
                    new Color(0.10f, 0.07f, 0.05f, 1f)),
                new Color(0.92f, 0.84f, 0.59f, 1f),
                true,
                false);

            NpcOutfitSet tealShortSleeve = CreateOutfit(
                rowanRig,
                "Teal Short-Sleeve Employee Shirt",
                "TealShortSleeve",
                new Color(0.12f, 0.52f, 0.50f, 1f),
                new Color(0.16f, 0.20f, 0.29f, 1f),
                new Color(0.08f, 0.09f, 0.11f, 1f),
                new Color(0.95f, 0.82f, 0.46f, 1f),
                true,
                true);

            CreateOutfit(
                rowanRig,
                "Navy Jacket",
                "NavyJacket",
                new Color(0.12f, 0.23f, 0.42f, 1f),
                new Color(0.23f, 0.20f, 0.18f, 1f),
                new Color(0.07f, 0.07f, 0.08f, 1f),
                new Color(0.70f, 0.80f, 0.90f, 1f),
                false,
                false);

            NpcHairSet shortCrop = CreateHairSet(
                rowanRig,
                "Short Crop / Black",
                "ShortCropBlack",
                GetPartColor(
                    rowanRig,
                    NpcRigPartId.HairFront,
                    new Color(0.055f, 0.065f, 0.075f, 1f)),
                rowanShapes,
                1f,
                0f,
                1f,
                0f);

            NpcHairSet longAuburn = CreateHairSet(
                rowanRig,
                "Long Back / Auburn",
                "LongBackAuburn",
                new Color(0.31f, 0.11f, 0.055f, 1f),
                rowanShapes,
                1.05f,
                -0.09f,
                1.10f,
                0.015f);

            CreateHairSet(
                rowanRig,
                "High Top / Dark",
                "HighTopDark",
                new Color(0.045f, 0.038f, 0.035f, 1f),
                rowanShapes,
                0.95f,
                0.02f,
                1.55f,
                0.055f);

            CreateHairSet(
                rowanRig,
                "Close Crop / Silver",
                "CloseCropSilver",
                new Color(0.47f, 0.49f, 0.52f, 1f),
                rowanShapes,
                0.82f,
                0.025f,
                0.72f,
                -0.015f);

            NpcAppearanceProfile rowanProfile =
                LoadOrCreate<NpcAppearanceProfile>(
                    ProfileFolder + "/RowanAppearance.asset");

            rowanProfile.Configure(
                "Rowan",
                masculine,
                warmBrown,
                rustPolo,
                shortCrop);

            NpcAppearanceProfile minaProfile =
                LoadOrCreate<NpcAppearanceProfile>(
                    ProfileFolder + "/MinaAppearance.asset");

            minaProfile.Configure(
                "Mina",
                feminine,
                mediumTan,
                tealShortSleeve,
                longAuburn);

            MarkDirty(
                masculine,
                feminine,
                warmBrown,
                deepBrown,
                mediumTan,
                rustPolo,
                tealShortSleeve,
                shortCrop,
                longAuburn,
                rowanProfile,
                minaProfile);

            AssignProfileToPrefab(
                RowanPrefabPath,
                rowanProfile);

            CreateProfilePrefab(
                rowanPrefab,
                MinaPrefabPath,
                "Rounded Employee - Mina",
                minaProfile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = minaProfile;
            EditorGUIUtility.PingObject(minaProfile);

            Debug.Log(
                "Created the starter appearance library: two body " +
                "silhouettes, six skin palettes, three outfits, four " +
                "hair sets, Rowan and Mina profiles, and a Mina prefab.");
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
                        $"Rowan is missing part {definition.Id}.");
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
            bool exposeForearms)
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
            float frontYOffset)
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
                    rearHeightMultiplier,
                    rearYOffset);

            NpcAppearancePartShape frontShape =
                CreateHairShape(
                    frontBase,
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
                frontShape);

            EditorUtility.SetDirty(hair);
            return hair;
        }


        private static NpcAppearancePartShape CreateHairShape(
            NpcAppearancePartShape source,
            float heightMultiplier,
            float yOffset)
        {
            Vector2 size = source.Size;
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


        private static void CreateProfilePrefab(
            GameObject sourcePrefab,
            string destinationPath,
            string rootName,
            NpcAppearanceProfile profile)
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(sourcePrefab)
                as GameObject;

            if (instance == null)
            {
                throw new UnityException(
                    "Could not instantiate Rowan for a profile prefab.");
            }

            try
            {
                instance.name = rootName;

                NpcCutoutRig rig = instance.GetComponent<NpcCutoutRig>();
                rig.SetAppearanceProfile(profile);

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    destinationPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
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
