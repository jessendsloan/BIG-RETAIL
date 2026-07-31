using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Produces an inspectable, art-independent NPC rig prefab from
    /// the canonical contract.
    /// </summary>
    public static class NpcRigLabGenerator
    {
        private const string CanonicalMenuPath =
            "Big Retail/Characters/Create Canonical NPC Rig Prototype";

        private const string CoworkerMenuPath =
            "Big Retail/Characters/Create Rounded Employee - Rowan";

        private const string PrefabFolder =
            "Assets/Prefabs/Characters/Prototype";

        private const string CanonicalPrefabPath =
            PrefabFolder + "/CanonicalNpcRig.prefab";

        private const string CoworkerPrefabPath =
            PrefabFolder + "/RoundedEmployeeRowan.prefab";


        [MenuItem(CanonicalMenuPath)]
        public static void CreateCanonicalRigPrefab()
        {
            CreateRigPrefab(
                CanonicalPrefabPath,
                null,
                false);
        }


        [MenuItem(CoworkerMenuPath)]
        public static void CreateRoundedEmployeeRowan()
        {
            CreateRigPrefab(
                CoworkerPrefabPath,
                CreateRowanProfile(),
                true);
        }


        private static void CreateRigPrefab(
            string preferredPrefabPath,
            RoundedEmployeeProfile profile,
            bool replaceExisting)
        {
            EnsureAssetFolder(
                PrefabFolder);

            string uniquePrefabPath =
                replaceExisting
                    ? preferredPrefabPath
                    : AssetDatabase.GenerateUniqueAssetPath(
                        preferredPrefabPath);

            GameObject rigRoot =
                BuildRigHierarchy(
                    profile);

            if (profile != null)
            {
                rigRoot.GetComponent<Animator>()
                    .runtimeAnimatorController =
                    NpcRigLabAnimationGenerator
                        .CreateOrUpdateRowanController();
            }

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    rigRoot,
                    uniquePrefabPath);

            Object.DestroyImmediate(
                rigRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(
                prefab);

            string rigLabel =
                profile != null
                    ? profile.RootName
                    : "canonical NPC rig prototype";

            Debug.Log(
                $"Created {rigLabel} at " +
                $"'{uniquePrefabPath}'.");
        }


        private static GameObject BuildRigHierarchy(
            RoundedEmployeeProfile profile)
        {
            GameObject characterRoot =
                new GameObject(
                    profile != null
                        ? profile.RootName
                        : "Canonical NPC Rig");

            characterRoot.AddComponent<Animator>();
            characterRoot.AddComponent<SortingGroup>();

            NpcCutoutRig cutoutRig =
                characterRoot.AddComponent<NpcCutoutRig>();

            Transform mirrorRoot =
                CreateChild(
                    characterRoot.transform,
                    "Directional Visual");

            if (profile != null)
            {
                mirrorRoot.localScale =
                    profile.VisualScale;
            }

            Dictionary<NpcRigBoneId, Transform> boneLookup =
                new Dictionary<NpcRigBoneId, Transform>();

            List<NpcRigBoneBinding> boneBindings =
                BuildBones(
                    mirrorRoot,
                    boneLookup);

            Sprite placeholderSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");

            List<NpcRigPartBinding> partBindings =
                BuildParts(
                    boneLookup,
                    placeholderSprite,
                    profile);

            if (profile != null)
            {
                AddProfileDetails(
                    boneLookup,
                    placeholderSprite,
                    profile);
            }

            cutoutRig.ConfigureGeneratedRig(
                mirrorRoot,
                boneBindings,
                partBindings);

            if (!cutoutRig.TryValidate(
                    out string failureReason))
            {
                Object.DestroyImmediate(
                    characterRoot);

                throw new UnityException(
                    $"Generated NPC rig is invalid: " +
                    $"{failureReason}");
            }

            return characterRoot;
        }

        private static List<NpcRigBoneBinding> BuildBones(
            Transform mirrorRoot,
            IDictionary<NpcRigBoneId, Transform> boneLookup)
        {
            List<NpcRigBoneBinding> bindings =
                new List<NpcRigBoneBinding>(
                    NpcRigDefinition.ExpectedBoneCount);

            foreach (NpcRigBoneDefinition definition
                     in NpcRigDefinition.BoneDefinitions)
            {
                Transform parent =
                    definition.HasParent
                        ? boneLookup[definition.ParentId]
                        : mirrorRoot;

                Transform bone =
                    CreateChild(
                        parent,
                        definition.Id.ToString());

                bone.localPosition =
                    definition.LocalPosition;

                boneLookup.Add(
                    definition.Id,
                    bone);

                bindings.Add(
                    new NpcRigBoneBinding(
                        definition.Id,
                        bone));
            }

            return bindings;
        }

        private static List<NpcRigPartBinding> BuildParts(
            IReadOnlyDictionary<NpcRigBoneId, Transform> boneLookup,
            Sprite placeholderSprite,
            RoundedEmployeeProfile profile)
        {
            List<NpcRigPartBinding> bindings =
                new List<NpcRigPartBinding>(
                    NpcRigDefinition.ExpectedPartCount);

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                Transform slot =
                    CreateChild(
                        boneLookup[definition.BoneId],
                        $"Slot - {definition.Id}");

                slot.localPosition =
                    profile != null
                        ? profile.GetPartPosition(
                            definition)
                        : definition.LocalPosition;

                if (profile != null)
                {
                    slot.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            profile.GetPartAngle(
                                definition.Id));
                }

                SpriteRenderer spriteRenderer =
                    slot.gameObject
                        .AddComponent<SpriteRenderer>();

                spriteRenderer.sprite =
                    placeholderSprite;
                spriteRenderer.sortingOrder =
                    profile != null
                        ? profile.GetSortingOrder(
                            definition)
                        : definition.SortingOrder;
                spriteRenderer.color =
                    profile != null
                        ? profile.GetPartColor(
                            definition.Id)
                        : GetPlaceholderColor(
                            definition.Id);

                ApplyPlaceholderSize(
                    slot,
                    placeholderSprite,
                    profile != null
                        ? profile.GetPartSize(
                            definition)
                        : definition.PlaceholderSize);

                bindings.Add(
                    new NpcRigPartBinding(
                        definition.Id,
                        spriteRenderer,
                        placeholderSprite));
            }

            return bindings;
        }

        private static void AddProfileDetails(
            IReadOnlyDictionary<NpcRigBoneId, Transform> boneLookup,
            Sprite placeholderSprite,
            RoundedEmployeeProfile profile)
        {
            Transform head =
                boneLookup[NpcRigBoneId.Head];

            CreateDetailSprite(
                head,
                "Face - Far Eye",
                placeholderSprite,
                new Vector2(-0.035f, 0.095f),
                new Vector2(0.026f, 0.038f),
                profile.FeatureColor,
                12);

            CreateDetailSprite(
                head,
                "Face - Near Eye",
                placeholderSprite,
                new Vector2(0.075f, 0.085f),
                new Vector2(0.032f, 0.043f),
                profile.FeatureColor,
                12);

            CreateDetailSprite(
                head,
                "Face - Smile",
                placeholderSprite,
                new Vector2(0.065f, 0.005f),
                new Vector2(0.070f, 0.014f),
                profile.FeatureColor,
                12,
                -7f);

            Transform chest =
                boneLookup[NpcRigBoneId.Chest];

            CreateDetailSprite(
                chest,
                "Uniform - Name Badge",
                placeholderSprite,
                new Vector2(0.13f, -0.08f),
                new Vector2(0.095f, 0.045f),
                profile.BadgeColor,
                18,
                -3f);
        }

        private static void CreateDetailSprite(
            Transform parent,
            string detailName,
            Sprite sprite,
            Vector2 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder,
            float angle = 0f)
        {
            Transform detail =
                CreateChild(
                    parent,
                    detailName);

            detail.localPosition =
                new Vector3(
                    localPosition.x,
                    localPosition.y,
                    0f);

            detail.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle);

            SpriteRenderer renderer =
                detail.gameObject
                    .AddComponent<SpriteRenderer>();

            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            ApplyPlaceholderSize(
                detail,
                sprite,
                size);
        }

        private static Transform CreateChild(
            Transform parent,
            string childName)
        {
            GameObject child =
                new GameObject(
                    childName);

            Transform childTransform =
                child.transform;

            childTransform.SetParent(
                parent,
                false);

            return childTransform;
        }

        private static void ApplyPlaceholderSize(
            Transform slot,
            Sprite placeholderSprite,
            Vector2 requestedSize)
        {
            if (placeholderSprite == null)
            {
                slot.localScale =
                    new Vector3(
                        requestedSize.x,
                        requestedSize.y,
                        1f);
                return;
            }

            Vector2 spriteSize =
                placeholderSprite.bounds.size;

            float safeWidth =
                Mathf.Max(
                    spriteSize.x,
                    0.0001f);

            float safeHeight =
                Mathf.Max(
                    spriteSize.y,
                    0.0001f);

            slot.localScale =
                new Vector3(
                    requestedSize.x / safeWidth,
                    requestedSize.y / safeHeight,
                    1f);
        }

        private static Color GetPlaceholderColor(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.HairRear:
                case NpcRigPartId.HairFront:
                    return new Color(
                        0.16f,
                        0.08f,
                        0.04f,
                        1f);

                case NpcRigPartId.Head:
                case NpcRigPartId.Neck:
                case NpcRigPartId.HandFar:
                case NpcRigPartId.HandNear:
                    return new Color(
                        0.85f,
                        0.58f,
                        0.42f,
                        1f);

                case NpcRigPartId.Torso:
                case NpcRigPartId.UpperArmFar:
                case NpcRigPartId.UpperArmNear:
                case NpcRigPartId.ForearmFar:
                case NpcRigPartId.ForearmNear:
                    return ShadeForDepth(
                        new Color(
                            0.12f,
                            0.45f,
                            0.70f,
                            1f),
                        IsFarPart(partId));

                case NpcRigPartId.Pelvis:
                case NpcRigPartId.ThighFar:
                case NpcRigPartId.ThighNear:
                case NpcRigPartId.ShinFar:
                case NpcRigPartId.ShinNear:
                    return ShadeForDepth(
                        new Color(
                            0.10f,
                            0.16f,
                            0.28f,
                            1f),
                        IsFarPart(partId));

                case NpcRigPartId.FootFar:
                case NpcRigPartId.FootNear:
                    return ShadeForDepth(
                        new Color(
                            0.08f,
                            0.08f,
                            0.09f,
                            1f),
                        IsFarPart(partId));

                default:
                    return Color.magenta;
            }
        }

        private static Color ShadeForDepth(
            Color color,
            bool isFarPart)
        {
            if (!isFarPart)
            {
                return color;
            }

            const float depthShade = 0.82f;

            return new Color(
                color.r * depthShade,
                color.g * depthShade,
                color.b * depthShade,
                color.a);
        }

        private static bool IsFarPart(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.UpperArmFar:
                case NpcRigPartId.ForearmFar:
                case NpcRigPartId.HandFar:
                case NpcRigPartId.ThighFar:
                case NpcRigPartId.ShinFar:
                case NpcRigPartId.FootFar:
                    return true;

                default:
                    return false;
            }
        }

        private static RoundedEmployeeProfile CreateRowanProfile()
        {
            Dictionary<NpcRigPartId, Vector2> partSizes =
                new Dictionary<NpcRigPartId, Vector2>
                {
                    {
                        NpcRigPartId.HairRear,
                        new Vector2(0.39f, 0.34f)
                    },
                    {
                        NpcRigPartId.UpperArmFar,
                        new Vector2(0.16f, 0.32f)
                    },
                    {
                        NpcRigPartId.ForearmFar,
                        new Vector2(0.14f, 0.30f)
                    },
                    {
                        NpcRigPartId.HandFar,
                        new Vector2(0.13f, 0.17f)
                    },
                    {
                        NpcRigPartId.ThighFar,
                        new Vector2(0.20f, 0.42f)
                    },
                    {
                        NpcRigPartId.ShinFar,
                        new Vector2(0.17f, 0.41f)
                    },
                    {
                        NpcRigPartId.FootFar,
                        new Vector2(0.24f, 0.15f)
                    },
                    {
                        NpcRigPartId.Pelvis,
                        new Vector2(0.43f, 0.29f)
                    },
                    {
                        NpcRigPartId.Torso,
                        new Vector2(0.54f, 0.58f)
                    },
                    {
                        NpcRigPartId.Neck,
                        new Vector2(0.14f, 0.22f)
                    },
                    {
                        NpcRigPartId.Head,
                        new Vector2(0.37f, 0.36f)
                    },
                    {
                        NpcRigPartId.HairFront,
                        new Vector2(0.37f, 0.17f)
                    },
                    {
                        NpcRigPartId.ThighNear,
                        new Vector2(0.20f, 0.42f)
                    },
                    {
                        NpcRigPartId.ShinNear,
                        new Vector2(0.17f, 0.41f)
                    },
                    {
                        NpcRigPartId.FootNear,
                        new Vector2(0.24f, 0.15f)
                    },
                    {
                        NpcRigPartId.UpperArmNear,
                        new Vector2(0.16f, 0.32f)
                    },
                    {
                        NpcRigPartId.ForearmNear,
                        new Vector2(0.14f, 0.30f)
                    },
                    {
                        NpcRigPartId.HandNear,
                        new Vector2(0.13f, 0.17f)
                    }
                };

            Dictionary<NpcRigPartId, Vector3> partPositions =
                new Dictionary<NpcRigPartId, Vector3>
                {
                    {
                        NpcRigPartId.HairRear,
                        new Vector3(0f, 0.10f, 0f)
                    },
                    {
                        NpcRigPartId.Head,
                        new Vector3(0.018f, 0.020f, 0f)
                    },
                    {
                        NpcRigPartId.HairFront,
                        new Vector3(0.020f, 0.170f, 0f)
                    },
                    {
                        NpcRigPartId.Torso,
                        new Vector3(0f, -0.120f, 0f)
                    },
                    {
                        NpcRigPartId.Neck,
                        new Vector3(0f, -0.070f, 0f)
                    },
                    {
                        NpcRigPartId.ThighFar,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.ShinFar,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.FootFar,
                        new Vector3(0.04f, -0.035f, 0f)
                    },
                    {
                        NpcRigPartId.ThighNear,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.ShinNear,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.FootNear,
                        new Vector3(0.04f, -0.035f, 0f)
                    }
                };

            Dictionary<NpcRigPartId, float> partAngles =
                new Dictionary<NpcRigPartId, float>
                {
                    {
                        NpcRigPartId.HairFront,
                        -7f
                    }
                };

            return new RoundedEmployeeProfile(
                "Rounded Employee - Rowan",
                new Vector3(1.08f, 0.94f, 1f),
                new Color(0.055f, 0.065f, 0.075f, 1f),
                new Color(0.54f, 0.31f, 0.20f, 1f),
                new Color(0.77f, 0.28f, 0.13f, 1f),
                new Color(0.26f, 0.20f, 0.16f, 1f),
                new Color(0.10f, 0.07f, 0.05f, 1f),
                new Color(0.09f, 0.055f, 0.04f, 1f),
                new Color(0.92f, 0.84f, 0.59f, 1f),
                partSizes,
                partPositions,
                partAngles);
        }

        private static void EnsureAssetFolder(
            string folderPath)
        {
            string[] pathParts =
                folderPath.Split('/');

            string currentPath =
                pathParts[0];

            for (int index = 1;
                 index < pathParts.Length;
                 index++)
            {
                string nextPath =
                    $"{currentPath}/{pathParts[index]}";

                if (!AssetDatabase.IsValidFolder(
                        nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        pathParts[index]);
                }

                currentPath = nextPath;
            }
        }

        private sealed class RoundedEmployeeProfile
        {
            private readonly Color hairColor;
            private readonly Color skinColor;
            private readonly Color shirtColor;
            private readonly Color pantsColor;
            private readonly Color shoeColor;

            private readonly IReadOnlyDictionary
                <NpcRigPartId, Vector2> partSizes;

            private readonly IReadOnlyDictionary
                <NpcRigPartId, Vector3> partPositions;

            private readonly IReadOnlyDictionary
                <NpcRigPartId, float> partAngles;


            public string RootName { get; }

            public Vector3 VisualScale { get; }

            public Color FeatureColor { get; }

            public Color BadgeColor { get; }


            public RoundedEmployeeProfile(
                string rootName,
                Vector3 visualScale,
                Color hairColor,
                Color skinColor,
                Color shirtColor,
                Color pantsColor,
                Color shoeColor,
                Color featureColor,
                Color badgeColor,
                IReadOnlyDictionary<NpcRigPartId, Vector2>
                    partSizes,
                IReadOnlyDictionary<NpcRigPartId, Vector3>
                    partPositions,
                IReadOnlyDictionary<NpcRigPartId, float>
                    partAngles)
            {
                RootName = rootName;
                VisualScale = visualScale;
                this.hairColor = hairColor;
                this.skinColor = skinColor;
                this.shirtColor = shirtColor;
                this.pantsColor = pantsColor;
                this.shoeColor = shoeColor;
                FeatureColor = featureColor;
                BadgeColor = badgeColor;
                this.partSizes = partSizes;
                this.partPositions = partPositions;
                this.partAngles = partAngles;
            }


            public Vector2 GetPartSize(
                NpcRigPartDefinition definition)
            {
                return partSizes.TryGetValue(
                    definition.Id,
                    out Vector2 size)
                        ? size
                        : definition.PlaceholderSize;
            }

            public Vector3 GetPartPosition(
                NpcRigPartDefinition definition)
            {
                return partPositions.TryGetValue(
                    definition.Id,
                    out Vector3 position)
                        ? position
                        : definition.LocalPosition;
            }

            public float GetPartAngle(
                NpcRigPartId partId)
            {
                return partAngles.TryGetValue(
                    partId,
                    out float angle)
                        ? angle
                        : 0f;
            }

            public int GetSortingOrder(
                NpcRigPartDefinition definition)
            {
                return definition.Id
                    == NpcRigPartId.HairFront
                        ? 13
                        : definition.SortingOrder;
            }

            public Color GetPartColor(
                NpcRigPartId partId)
            {
                Color color;

                switch (partId)
                {
                    case NpcRigPartId.HairRear:
                    case NpcRigPartId.HairFront:
                        color = hairColor;
                        break;

                    case NpcRigPartId.Head:
                    case NpcRigPartId.Neck:
                    case NpcRigPartId.HandFar:
                    case NpcRigPartId.HandNear:
                        color = skinColor;
                        break;

                    case NpcRigPartId.Torso:
                    case NpcRigPartId.UpperArmFar:
                    case NpcRigPartId.UpperArmNear:
                    case NpcRigPartId.ForearmFar:
                    case NpcRigPartId.ForearmNear:
                        color = shirtColor;
                        break;

                    case NpcRigPartId.Pelvis:
                    case NpcRigPartId.ThighFar:
                    case NpcRigPartId.ThighNear:
                    case NpcRigPartId.ShinFar:
                    case NpcRigPartId.ShinNear:
                        color = pantsColor;
                        break;

                    case NpcRigPartId.FootFar:
                    case NpcRigPartId.FootNear:
                        color = shoeColor;
                        break;

                    default:
                        return Color.magenta;
                }

                return ShadeForDepth(
                    color,
                    IsFarPart(partId));
            }
        }
    }
}
