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

                rigRoot.AddComponent<NpcPathFollower>()
                    .ConfigurePrototype(
                        1.2f,
                        0.02f,
                        1.2f);
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
                    boneLookup,
                    profile);

            Sprite placeholderSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");

            List<NpcRigPartBinding> partBindings =
                BuildParts(
                    boneLookup,
                    placeholderSprite,
                    profile);

            List<SpriteRenderer> northHiddenDetails =
                new List<SpriteRenderer>();

            if (profile != null)
            {
                northHiddenDetails.Add(
                    AddProfileDetails(
                        boneLookup,
                        placeholderSprite,
                        profile));
            }

            cutoutRig.ConfigureGeneratedRig(
                mirrorRoot,
                boneBindings,
                partBindings);

            if (profile != null)
            {
                cutoutRig.ConfigureAuthoredBonePoses(
                    CreateRowanSouthEastFootPose(),
                    CreateRowanNorthEastFootPose());
                cutoutRig.ConfigureNorthHiddenDetails(
                    northHiddenDetails);
            }

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
            IDictionary<NpcRigBoneId, Transform> boneLookup,
            RoundedEmployeeProfile profile)
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
                    profile != null
                        ? profile.GetBonePosition(definition)
                        : definition.LocalPosition;

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

        private static SpriteRenderer AddProfileDetails(
            IReadOnlyDictionary<NpcRigBoneId, Transform> boneLookup,
            Sprite placeholderSprite,
            RoundedEmployeeProfile profile)
        {
            // Rowan intentionally has no facial feature sprites. The head,
            // hair, and body silhouette are the complete visual language for
            // this procedural character. Keep the badge as the only optional
            // profile detail.

            Transform chest =
                boneLookup[NpcRigBoneId.Chest];

            return CreateDetailSprite(
                chest,
                "Uniform - Name Badge",
                placeholderSprite,
                new Vector2(0.13f, -0.08f),
                new Vector2(0.095f, 0.045f),
                profile.BadgeColor,
                11,
                -3f);
        }


        private static SpriteRenderer CreateDetailSprite(
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

            return renderer;
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
                case NpcRigPartId.HandSourceCameraLeft:
                case NpcRigPartId.HandSourceCameraRight:
                    return new Color(
                        0.85f,
                        0.58f,
                        0.42f,
                        1f);

                case NpcRigPartId.Torso:
                case NpcRigPartId.UpperArmSourceCameraLeft:
                case NpcRigPartId.UpperArmSourceCameraRight:
                case NpcRigPartId.ForearmSourceCameraLeft:
                case NpcRigPartId.ForearmSourceCameraRight:
                    return ShadeForDepth(
                        new Color(
                            0.12f,
                            0.45f,
                            0.70f,
                            1f),
                        IsFarPart(partId));

                case NpcRigPartId.Pelvis:
                case NpcRigPartId.ThighSourceCameraLeft:
                case NpcRigPartId.ThighSourceCameraRight:
                case NpcRigPartId.ShinSourceCameraLeft:
                case NpcRigPartId.ShinSourceCameraRight:
                    return ShadeForDepth(
                        new Color(
                            0.10f,
                            0.16f,
                            0.28f,
                            1f),
                        IsFarPart(partId));

                case NpcRigPartId.FootSourceCameraLeft:
                case NpcRigPartId.FootSourceCameraRight:
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
                case NpcRigPartId.UpperArmSourceCameraLeft:
                case NpcRigPartId.ForearmSourceCameraLeft:
                case NpcRigPartId.HandSourceCameraLeft:
                case NpcRigPartId.ThighSourceCameraLeft:
                case NpcRigPartId.ShinSourceCameraLeft:
                case NpcRigPartId.FootSourceCameraLeft:
                    return true;

                default:
                    return false;
            }
        }

        private static RoundedEmployeeProfile CreateRowanProfile()
        {
            Dictionary<NpcRigBoneId, Vector3> bonePositions =
                new Dictionary<NpcRigBoneId, Vector3>
                {
                    // Rowan's authored SouthEast bind pose. Root remains
                    // the floor anchor; these offsets shape the body above it.
                    {
                        NpcRigBoneId.Pelvis,
                        new Vector3(-0.065f, 0.808f, 0f)
                    },
                    {
                        NpcRigBoneId.ThighSourceCameraLeft,
                        new Vector3(-0.075f, -0.04f, 0f)
                    },
                    {
                        NpcRigBoneId.ThighSourceCameraRight,
                        new Vector3(0.075f, -0.04f, 0f)
                    }
                };

            Dictionary<NpcRigPartId, Vector2> partSizes =
                new Dictionary<NpcRigPartId, Vector2>
                {
                    {
                        NpcRigPartId.HairRear,
                        new Vector2(0.39f, 0.34f)
                    },
                    {
                        NpcRigPartId.UpperArmSourceCameraLeft,
                        new Vector2(0.16f, 0.32f)
                    },
                    {
                        NpcRigPartId.ForearmSourceCameraLeft,
                        new Vector2(0.14f, 0.30f)
                    },
                    {
                        NpcRigPartId.HandSourceCameraLeft,
                        new Vector2(0.13f, 0.17f)
                    },
                    {
                        NpcRigPartId.ThighSourceCameraLeft,
                        new Vector2(0.20f, 0.42f)
                    },
                    {
                        NpcRigPartId.ShinSourceCameraLeft,
                        new Vector2(0.17f, 0.41f)
                    },
                    {
                        NpcRigPartId.FootSourceCameraLeft,
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
                        NpcRigPartId.ThighSourceCameraRight,
                        new Vector2(0.20f, 0.42f)
                    },
                    {
                        NpcRigPartId.ShinSourceCameraRight,
                        new Vector2(0.17f, 0.41f)
                    },
                    {
                        NpcRigPartId.FootSourceCameraRight,
                        new Vector2(0.24f, 0.15f)
                    },
                    {
                        NpcRigPartId.UpperArmSourceCameraRight,
                        new Vector2(0.16f, 0.32f)
                    },
                    {
                        NpcRigPartId.ForearmSourceCameraRight,
                        new Vector2(0.14f, 0.30f)
                    },
                    {
                        NpcRigPartId.HandSourceCameraRight,
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
                        // Bring the head down into the neck in the bind pose.
                        // Keeping this on the generated profile makes the fix
                        // reproducible whenever Rowan is regenerated.
                        new Vector3(0.018f, -0.0383f, 0f)
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
                        NpcRigPartId.ThighSourceCameraLeft,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.ShinSourceCameraLeft,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.FootSourceCameraLeft,
                        new Vector3(0.04f, -0.035f, 0f)
                    },
                    {
                        NpcRigPartId.ThighSourceCameraRight,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.ShinSourceCameraRight,
                        new Vector3(0f, -0.170f, 0f)
                    },
                    {
                        NpcRigPartId.FootSourceCameraRight,
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
                bonePositions,
                partSizes,
                partPositions,
                partAngles);
        }


        private static List<NpcRigDirectionalBonePose>
            CreateRowanSouthEastFootPose()
        {
            return CreateRowanFootPose(
                new Vector3(10.738f, 21.991f, -24.766f));
        }


        private static List<NpcRigDirectionalBonePose>
            CreateRowanNorthEastFootPose()
        {
            Vector3 footEulerAngles =
                new Vector3(10.738f, 21.991f, 24.766f);

            return new List<NpcRigDirectionalBonePose>
            {
                new NpcRigDirectionalBonePose(
                    NpcRigBoneId.FootSourceCameraLeft,
                    new Vector3(-0.02f, -0.28f, 0f),
                    footEulerAngles,
                    new Vector3(1.1072f, 1f, 1f)),
                new NpcRigDirectionalBonePose(
                    NpcRigBoneId.FootSourceCameraRight,
                    new Vector3(-0.01f, -0.33f, -0.0093f),
                    footEulerAngles,
                    new Vector3(1.1072f, 1f, 1f))
            };
        }


        private static List<NpcRigDirectionalBonePose>
            CreateRowanFootPose(
                Vector3 footEulerAngles)
        {
            Vector3 footScale =
                new Vector3(1.1072f, 1f, 1f);

            return new List<NpcRigDirectionalBonePose>
            {
                new NpcRigDirectionalBonePose(
                    NpcRigBoneId.FootSourceCameraLeft,
                    new Vector3(0.01f, -0.35f, 0f),
                    footEulerAngles,
                    footScale),
                new NpcRigDirectionalBonePose(
                    NpcRigBoneId.FootSourceCameraRight,
                    new Vector3(0.027f, -0.3599f, -0.0093f),
                    footEulerAngles,
                    footScale)
            };
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
                <NpcRigBoneId, Vector3> bonePositions;

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
                IReadOnlyDictionary<NpcRigBoneId, Vector3>
                    bonePositions,
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
                this.bonePositions = bonePositions;
                this.partSizes = partSizes;
                this.partPositions = partPositions;
                this.partAngles = partAngles;
            }


            public Vector3 GetBonePosition(
                NpcRigBoneDefinition definition)
            {
                return bonePositions.TryGetValue(
                    definition.Id,
                    out Vector3 position)
                        ? position
                        : definition.LocalPosition;
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
                    case NpcRigPartId.HandSourceCameraLeft:
                    case NpcRigPartId.HandSourceCameraRight:
                        color = skinColor;
                        break;

                    case NpcRigPartId.Torso:
                    case NpcRigPartId.UpperArmSourceCameraLeft:
                    case NpcRigPartId.UpperArmSourceCameraRight:
                    case NpcRigPartId.ForearmSourceCameraLeft:
                    case NpcRigPartId.ForearmSourceCameraRight:
                        color = shirtColor;
                        break;

                    case NpcRigPartId.Pelvis:
                    case NpcRigPartId.ThighSourceCameraLeft:
                    case NpcRigPartId.ThighSourceCameraRight:
                    case NpcRigPartId.ShinSourceCameraLeft:
                    case NpcRigPartId.ShinSourceCameraRight:
                        color = pantsColor;
                        break;

                    case NpcRigPartId.FootSourceCameraLeft:
                    case NpcRigPartId.FootSourceCameraRight:
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
