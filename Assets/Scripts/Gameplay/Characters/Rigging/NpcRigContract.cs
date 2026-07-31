using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// The four directions an NPC can display in the isometric world.
    ///
    /// The current canonical cutout artwork is horizontally handed
    /// opposite the world-facing labels, so East facings mirror and West
    /// facings use the unmirrored authored presentation.
    /// </summary>
    public enum NpcFacing
    {
        SouthEast = 0,
        SouthWest = 1,
        NorthEast = 2,
        NorthWest = 3
    }

    /// <summary>
    /// The two directions that require original artwork.
    /// </summary>
    public enum NpcAuthoredDirection
    {
        SouthEast = 0,
        NorthEast = 1
    }

    /// <summary>
    /// Stable identifiers for the canonical 20-bone NPC skeleton.
    /// </summary>
    public enum NpcRigBoneId
    {
        Root = 0,
        Pelvis = 1,
        SpineLower = 2,
        Chest = 3,
        Neck = 4,
        Head = 5,
        ShoulderFar = 6,
        UpperArmFar = 7,
        ForearmFar = 8,
        HandFar = 9,
        ShoulderNear = 10,
        UpperArmNear = 11,
        ForearmNear = 12,
        HandNear = 13,
        ThighFar = 14,
        ShinFar = 15,
        FootFar = 16,
        ThighNear = 17,
        ShinNear = 18,
        FootNear = 19
    }

    /// <summary>
    /// Stable identifiers for the 18 visible cutout pieces.
    /// </summary>
    public enum NpcRigPartId
    {
        HairRear = 0,
        UpperArmFar = 1,
        ForearmFar = 2,
        HandFar = 3,
        ThighFar = 4,
        ShinFar = 5,
        FootFar = 6,
        Pelvis = 7,
        Torso = 8,
        Neck = 9,
        Head = 10,
        HairFront = 11,
        ThighNear = 12,
        ShinNear = 13,
        FootNear = 14,
        UpperArmNear = 15,
        ForearmNear = 16,
        HandNear = 17
    }

    /// <summary>
    /// One bone in the canonical skeleton definition.
    /// </summary>
    public readonly struct NpcRigBoneDefinition
    {
        public NpcRigBoneId Id { get; }

        public bool HasParent { get; }

        public NpcRigBoneId ParentId { get; }

        public Vector3 LocalPosition { get; }


        public NpcRigBoneDefinition(
            NpcRigBoneId id,
            Vector3 localPosition)
        {
            Id = id;
            HasParent = false;
            ParentId = default;
            LocalPosition = localPosition;
        }

        public NpcRigBoneDefinition(
            NpcRigBoneId id,
            NpcRigBoneId parentId,
            Vector3 localPosition)
        {
            Id = id;
            HasParent = true;
            ParentId = parentId;
            LocalPosition = localPosition;
        }
    }

    /// <summary>
    /// One visible part and its initial placeholder presentation.
    /// </summary>
    public readonly struct NpcRigPartDefinition
    {
        public NpcRigPartId Id { get; }

        public NpcRigBoneId BoneId { get; }

        public int SortingOrder { get; }

        public Vector3 LocalPosition { get; }

        public Vector2 PlaceholderSize { get; }


        public NpcRigPartDefinition(
            NpcRigPartId id,
            NpcRigBoneId boneId,
            int sortingOrder,
            Vector3 localPosition,
            Vector2 placeholderSize)
        {
            Id = id;
            BoneId = boneId;
            SortingOrder = sortingOrder;
            LocalPosition = localPosition;
            PlaceholderSize = placeholderSize;
        }
    }

    /// <summary>
    /// The single source of truth for the first Big Retail NPC rig.
    ///
    /// Local positions and sizes only create a readable placeholder.
    /// Approved character artwork will determine final pivots,
    /// overlaps, and proportions.
    /// </summary>
    public static class NpcRigDefinition
    {
        public const int ExpectedBoneCount = 20;

        public const int ExpectedPartCount = 18;

        public const int ExpectedAuthoredSpriteCount =
            ExpectedPartCount * 2;


        private static readonly NpcRigBoneDefinition[]
            boneDefinitions =
            {
                new NpcRigBoneDefinition(
                    NpcRigBoneId.Root,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.Pelvis,
                    NpcRigBoneId.Root,
                    new Vector3(0f, 0.9f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.SpineLower,
                    NpcRigBoneId.Pelvis,
                    new Vector3(0f, 0.18f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.Chest,
                    NpcRigBoneId.SpineLower,
                    new Vector3(0f, 0.30f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.Neck,
                    NpcRigBoneId.Chest,
                    new Vector3(0f, 0.27f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.Head,
                    NpcRigBoneId.Neck,
                    new Vector3(0f, 0.18f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShoulderFar,
                    NpcRigBoneId.Chest,
                    new Vector3(-0.13f, 0.02f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmFar,
                    NpcRigBoneId.ShoulderFar,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmFar,
                    NpcRigBoneId.UpperArmFar,
                    new Vector3(-0.05f, -0.25f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandFar,
                    NpcRigBoneId.ForearmFar,
                    new Vector3(-0.03f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShoulderNear,
                    NpcRigBoneId.Chest,
                    new Vector3(0.16f, 0f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmNear,
                    NpcRigBoneId.ShoulderNear,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmNear,
                    NpcRigBoneId.UpperArmNear,
                    new Vector3(0.06f, -0.26f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandNear,
                    NpcRigBoneId.ForearmNear,
                    new Vector3(0.04f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighFar,
                    NpcRigBoneId.Pelvis,
                    new Vector3(-0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinFar,
                    NpcRigBoneId.ThighFar,
                    new Vector3(-0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootFar,
                    NpcRigBoneId.ShinFar,
                    new Vector3(0.01f, -0.35f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighNear,
                    NpcRigBoneId.Pelvis,
                    new Vector3(0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinNear,
                    NpcRigBoneId.ThighNear,
                    new Vector3(0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootNear,
                    NpcRigBoneId.ShinNear,
                    new Vector3(0.01f, -0.35f, 0f))
            };


        private static readonly NpcRigPartDefinition[]
            partDefinitions =
            {
                DefinePart(
                    NpcRigPartId.HairRear,
                    NpcRigBoneId.Head,
                    0,
                    new Vector2(0f, 0.13f),
                    new Vector2(0.38f, 0.42f)),

                DefinePart(
                    NpcRigPartId.UpperArmFar,
                    NpcRigBoneId.UpperArmFar,
                    1,
                    new Vector2(-0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmFar,
                    NpcRigBoneId.ForearmFar,
                    2,
                    new Vector2(-0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandFar,
                    NpcRigBoneId.HandFar,
                    3,
                    new Vector2(0f, -0.07f),
                    new Vector2(0.12f, 0.16f)),

                DefinePart(
                    NpcRigPartId.ThighFar,
                    NpcRigBoneId.ThighFar,
                    4,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinFar,
                    NpcRigBoneId.ShinFar,
                    5,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootFar,
                    NpcRigBoneId.FootFar,
                    6,
                    new Vector2(0.04f, -0.04f),
                    new Vector2(0.23f, 0.12f)),

                DefinePart(
                    NpcRigPartId.Pelvis,
                    NpcRigBoneId.Pelvis,
                    7,
                    new Vector2(0f, -0.04f),
                    new Vector2(0.39f, 0.28f)),

                DefinePart(
                    NpcRigPartId.Torso,
                    NpcRigBoneId.Chest,
                    8,
                    new Vector2(0f, -0.15f),
                    new Vector2(0.47f, 0.55f)),

                DefinePart(
                    NpcRigPartId.Neck,
                    NpcRigBoneId.Neck,
                    9,
                    new Vector2(0f, 0.07f),
                    new Vector2(0.13f, 0.18f)),

                DefinePart(
                    NpcRigPartId.Head,
                    NpcRigBoneId.Head,
                    10,
                    new Vector2(0f, 0.07f),
                    new Vector2(0.33f, 0.38f)),

                DefinePart(
                    NpcRigPartId.HairFront,
                    NpcRigBoneId.Head,
                    11,
                    new Vector2(0f, 0.13f),
                    new Vector2(0.36f, 0.32f)),

                DefinePart(
                    NpcRigPartId.ThighNear,
                    NpcRigBoneId.ThighNear,
                    12,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinNear,
                    NpcRigBoneId.ShinNear,
                    13,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootNear,
                    NpcRigBoneId.FootNear,
                    14,
                    new Vector2(0.04f, -0.04f),
                    new Vector2(0.23f, 0.12f)),

                DefinePart(
                    NpcRigPartId.UpperArmNear,
                    NpcRigBoneId.UpperArmNear,
                    15,
                    new Vector2(0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmNear,
                    NpcRigBoneId.ForearmNear,
                    16,
                    new Vector2(0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandNear,
                    NpcRigBoneId.HandNear,
                    17,
                    new Vector2(0f, -0.07f),
                    new Vector2(0.12f, 0.16f))
            };


        public static IReadOnlyList<NpcRigBoneDefinition>
            BoneDefinitions =>
            boneDefinitions;

        public static IReadOnlyList<NpcRigPartDefinition>
            PartDefinitions =>
            partDefinitions;


        private static NpcRigPartDefinition DefinePart(
            NpcRigPartId id,
            NpcRigBoneId boneId,
            int sortingOrder,
            Vector2 localPosition,
            Vector2 placeholderSize)
        {
            return new NpcRigPartDefinition(
                id,
                boneId,
                sortingOrder,
                new Vector3(
                    localPosition.x,
                    localPosition.y,
                    0f),
                placeholderSize);
        }
    }

    /// <summary>
    /// Converts four displayed facings into two authored directions
    /// plus a horizontal-mirroring decision.
    /// </summary>
    public static class NpcFacingUtility
    {
        public static NpcAuthoredDirection GetAuthoredDirection(
            NpcFacing facing)
        {
            switch (facing)
            {
                case NpcFacing.SouthEast:
                case NpcFacing.SouthWest:
                    return NpcAuthoredDirection.SouthEast;

                case NpcFacing.NorthEast:
                case NpcFacing.NorthWest:
                    return NpcAuthoredDirection.NorthEast;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(facing),
                        facing,
                        "Unknown NPC facing.");
            }
        }

        public static bool IsMirrored(
            NpcFacing facing)
        {
            switch (facing)
            {
                case NpcFacing.SouthEast:
                case NpcFacing.NorthEast:
                    return true;

                case NpcFacing.SouthWest:
                case NpcFacing.NorthWest:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(facing),
                        facing,
                        "Unknown NPC facing.");
            }
        }

        /// <summary>
        /// Returns the depth partner for a mirrored cutout limb. When the
        /// character flips horizontally, the visual near/far limb positions
        /// flip too, so their sorting order must exchange with this partner.
        /// Center body parts return themselves.
        /// </summary>
        public static NpcRigPartId GetMirroredDepthPart(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.UpperArmFar:
                    return NpcRigPartId.UpperArmNear;
                case NpcRigPartId.UpperArmNear:
                    return NpcRigPartId.UpperArmFar;
                case NpcRigPartId.ForearmFar:
                    return NpcRigPartId.ForearmNear;
                case NpcRigPartId.ForearmNear:
                    return NpcRigPartId.ForearmFar;
                case NpcRigPartId.HandFar:
                    return NpcRigPartId.HandNear;
                case NpcRigPartId.HandNear:
                    return NpcRigPartId.HandFar;
                case NpcRigPartId.ThighFar:
                    return NpcRigPartId.ThighNear;
                case NpcRigPartId.ThighNear:
                    return NpcRigPartId.ThighFar;
                case NpcRigPartId.ShinFar:
                    return NpcRigPartId.ShinNear;
                case NpcRigPartId.ShinNear:
                    return NpcRigPartId.ShinFar;
                case NpcRigPartId.FootFar:
                    return NpcRigPartId.FootNear;
                case NpcRigPartId.FootNear:
                    return NpcRigPartId.FootFar;
                default:
                    return partId;
            }
        }
    }
}
