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
        private const string MenuPath =
            "Big Retail/Characters/Create Canonical NPC Rig Prototype";

        private const string PrefabFolder =
            "Assets/Prefabs/Characters/Prototype";

        private const string PrefabPath =
            PrefabFolder + "/CanonicalNpcRig.prefab";


        [MenuItem(MenuPath)]
        public static void CreateCanonicalRigPrefab()
        {
            EnsureAssetFolder(
                PrefabFolder);

            string uniquePrefabPath =
                AssetDatabase.GenerateUniqueAssetPath(
                    PrefabPath);

            GameObject rigRoot =
                BuildRigHierarchy();

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

            Debug.Log(
                $"Created canonical NPC rig prototype at " +
                $"'{uniquePrefabPath}'.");
        }


        private static GameObject BuildRigHierarchy()
        {
            GameObject characterRoot =
                new GameObject(
                    "Canonical NPC Rig");

            characterRoot.AddComponent<Animator>();
            characterRoot.AddComponent<SortingGroup>();

            NpcCutoutRig cutoutRig =
                characterRoot.AddComponent<NpcCutoutRig>();

            Transform mirrorRoot =
                CreateChild(
                    characterRoot.transform,
                    "Directional Visual");

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
                    placeholderSprite);

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
            Sprite placeholderSprite)
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
                    definition.LocalPosition;

                SpriteRenderer spriteRenderer =
                    slot.gameObject
                        .AddComponent<SpriteRenderer>();

                spriteRenderer.sprite =
                    placeholderSprite;
                spriteRenderer.sortingOrder =
                    definition.SortingOrder;
                spriteRenderer.color =
                    GetPlaceholderColor(
                        definition.Id);

                ApplyPlaceholderSize(
                    slot,
                    placeholderSprite,
                    definition.PlaceholderSize);

                bindings.Add(
                    new NpcRigPartBinding(
                        definition.Id,
                        spriteRenderer,
                        placeholderSprite));
            }

            return bindings;
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
    }
}
