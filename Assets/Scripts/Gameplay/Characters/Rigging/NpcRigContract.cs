using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// The four directions an NPC can display in the isometric world.
    ///
    /// SouthEast and NorthEast are the canonical authored directions.
    /// SouthWest and NorthWest use their corresponding mirrored artwork.
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
    /// A stable horizontal side in the active Game-view camera frame.
    /// This never means the character's anatomical left or right.
    /// </summary>
    public enum NpcCameraSide
    {
        CameraLeft = 0,
        CameraRight = 1
    }

    /// <summary>
    /// Stable identifiers for the canonical 20-bone NPC skeleton. Limb bones
    /// are named for the visible segment they own, while their Transform
    /// origin is the segment's proximal joint: UpperArm = shoulder, Forearm =
    /// elbow, Hand = wrist, Thigh = hip, Shin = knee, and Foot = ankle.
    /// Near and Far are stable depth identities. Near is always the foreground
    /// chain and Far is always the background chain. Horizontal mirroring can
    /// move either chain to the opposite screen side without changing that
    /// identity.
    /// </summary>
    public enum NpcRigBoneId
    {
        Root = 0,
        Pelvis = 1,
        SpineLower = 2,
        Chest = 3,
        Neck = 4,
        Head = 5,
        ShoulderNear = 6,
        UpperArmNear = 7,
        ForearmNear = 8,
        HandNear = 9,
        ShoulderFar = 10,
        UpperArmFar = 11,
        ForearmFar = 12,
        HandFar = 13,
        ThighNear = 14,
        ShinNear = 15,
        FootNear = 16,
        ThighFar = 17,
        ShinFar = 18,
        FootFar = 19
    }

    /// <summary>
    /// Stable identifiers for the 18 visible cutout pieces.
    /// </summary>
    public enum NpcRigPartId
    {
        HairRear = 0,
        UpperArmNear = 1,
        ForearmNear = 2,
        HandNear = 3,
        ThighNear = 4,
        ShinNear = 5,
        FootNear = 6,
        Pelvis = 7,
        Torso = 8,
        Neck = 9,
        Head = 10,
        HairFront = 11,
        ThighFar = 12,
        ShinFar = 13,
        FootFar = 14,
        UpperArmFar = 15,
        ForearmFar = 16,
        HandFar = 17
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
                    NpcRigBoneId.ShoulderNear,
                    NpcRigBoneId.Chest,
                    new Vector3(-0.13f, 0.02f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmNear,
                    NpcRigBoneId.ShoulderNear,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmNear,
                    NpcRigBoneId.UpperArmNear,
                    new Vector3(-0.05f, -0.25f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandNear,
                    NpcRigBoneId.ForearmNear,
                    new Vector3(-0.03f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShoulderFar,
                    NpcRigBoneId.Chest,
                    new Vector3(0.16f, 0f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmFar,
                    NpcRigBoneId.ShoulderFar,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmFar,
                    NpcRigBoneId.UpperArmFar,
                    new Vector3(0.06f, -0.26f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandFar,
                    NpcRigBoneId.ForearmFar,
                    new Vector3(0.04f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighNear,
                    NpcRigBoneId.Pelvis,
                    new Vector3(-0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinNear,
                    NpcRigBoneId.ThighNear,
                    new Vector3(-0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootNear,
                    NpcRigBoneId.ShinNear,
                    new Vector3(0.01f, -0.35f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighFar,
                    NpcRigBoneId.Pelvis,
                    new Vector3(0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinFar,
                    NpcRigBoneId.ThighFar,
                    new Vector3(0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootFar,
                    NpcRigBoneId.ShinFar,
                    new Vector3(0.027f, -0.3599f, -0.0093f))
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
                    NpcRigPartId.UpperArmNear,
                    NpcRigBoneId.UpperArmNear,
                    1,
                    new Vector2(-0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmNear,
                    NpcRigBoneId.ForearmNear,
                    2,
                    new Vector2(-0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandNear,
                    NpcRigBoneId.HandNear,
                    3,
                    new Vector2(0f, -0.07f),
                    new Vector2(0.12f, 0.16f)),

                DefinePart(
                    NpcRigPartId.ThighNear,
                    NpcRigBoneId.ThighNear,
                    4,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinNear,
                    NpcRigBoneId.ShinNear,
                    5,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootNear,
                    NpcRigBoneId.FootNear,
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
                    NpcRigPartId.ThighFar,
                    NpcRigBoneId.ThighFar,
                    12,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinFar,
                    NpcRigBoneId.ShinFar,
                    13,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootFar,
                    NpcRigBoneId.FootFar,
                    14,
                    new Vector2(0.04f, -0.04f),
                    new Vector2(0.23f, 0.12f)),

                DefinePart(
                    NpcRigPartId.UpperArmFar,
                    NpcRigBoneId.UpperArmFar,
                    15,
                    new Vector2(0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmFar,
                    NpcRigBoneId.ForearmFar,
                    16,
                    new Vector2(0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandFar,
                    NpcRigBoneId.HandFar,
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


        public static bool TryGetBoneDefinition(
            NpcRigBoneId requestedId,
            out NpcRigBoneDefinition definition)
        {
            for (int index = 0; index < boneDefinitions.Length; index++)
            {
                if (boneDefinitions[index].Id == requestedId)
                {
                    definition = boneDefinitions[index];
                    return true;
                }
            }

            definition = default;
            return false;
        }


        public static bool TryGetPartDefinition(
            NpcRigPartId requestedId,
            out NpcRigPartDefinition definition)
        {
            for (int index = 0; index < partDefinitions.Length; index++)
            {
                if (partDefinitions[index].Id == requestedId)
                {
                    definition = partDefinitions[index];
                    return true;
                }
            }

            definition = default;
            return false;
        }


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
                case NpcFacing.SouthWest:
                case NpcFacing.NorthWest:
                    return true;

                case NpcFacing.SouthEast:
                case NpcFacing.NorthEast:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(facing),
                        facing,
                        "Unknown NPC facing.");
            }
        }

        /// <summary>
        /// Returns the screen side currently occupied by the stable Near
        /// (foreground) chain. This is presentation information only; it must
        /// never be used to redefine Near/Far identity.
        /// </summary>
        public static NpcCameraSide GetForegroundCameraSide(
            NpcFacing facing)
        {
            switch (facing)
            {
                case NpcFacing.SouthEast:
                case NpcFacing.NorthEast:
                    return NpcCameraSide.CameraLeft;

                case NpcFacing.SouthWest:
                case NpcFacing.NorthWest:
                    return NpcCameraSide.CameraRight;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(facing),
                        facing,
                        "Unknown NPC facing.");
            }
        }

        /// <summary>
        /// Resolves one depth-identified limb's temporary screen side. Near is
        /// camera-left in each unmirrored authored view and Far is camera-right;
        /// their displayed screen sides reverse when the visual is mirrored.
        /// Their Near/Far depth identities do not change.
        /// </summary>
        public static bool TryGetDisplayedCameraSide(
            NpcFacing facing,
            NpcRigPartId partId,
            out NpcCameraSide cameraSide)
        {
            bool isNear;

            switch (partId)
            {
                case NpcRigPartId.UpperArmNear:
                case NpcRigPartId.ForearmNear:
                case NpcRigPartId.HandNear:
                case NpcRigPartId.ThighNear:
                case NpcRigPartId.ShinNear:
                case NpcRigPartId.FootNear:
                    isNear = true;
                    break;

                case NpcRigPartId.UpperArmFar:
                case NpcRigPartId.ForearmFar:
                case NpcRigPartId.HandFar:
                case NpcRigPartId.ThighFar:
                case NpcRigPartId.ShinFar:
                case NpcRigPartId.FootFar:
                    isNear = false;
                    break;

                default:
                    cameraSide = default;
                    return false;
            }

            bool displayedCameraLeft =
                IsMirrored(facing)
                    ? !isNear
                    : isNear;

            cameraSide = displayedCameraLeft
                ? NpcCameraSide.CameraLeft
                : NpcCameraSide.CameraRight;
            return true;
        }

        /// <summary>
        /// Resolves renderer order from the stable Near/Far depth contract.
        /// Facing only changes direction-specific details such as north-facing
        /// hair and whether feet draw before or after their shins.
        /// </summary>
        public static int GetPresentationSortingOrder(
            NpcFacing facing,
            NpcRigPartId partId)
        {
            bool northFacing =
                NpcFacingUtility.GetAuthoredDirection(facing)
                == NpcAuthoredDirection.NorthEast;

            if (northFacing)
            {
                switch (partId)
                {
                    case NpcRigPartId.HairRear:
                        // North-facing characters show the back-hair mass
                        // over the head rather than the front fringe.
                        return 11;

                    case NpcRigPartId.HairFront:
                        return 0;
                }
            }

            bool near = IsNearPart(partId);

            if (!near && !IsFarPart(partId))
            {
                return GetBaseSortingOrder(partId);
            }

            switch (partId)
            {
                case NpcRigPartId.UpperArmNear:
                case NpcRigPartId.UpperArmFar:
                    return near ? 15 : 1;

                case NpcRigPartId.ForearmNear:
                case NpcRigPartId.ForearmFar:
                    return near ? 16 : 2;

                case NpcRigPartId.HandNear:
                case NpcRigPartId.HandFar:
                    return near ? 17 : 3;

                case NpcRigPartId.ThighNear:
                case NpcRigPartId.ThighFar:
                    return northFacing
                        ? (near ? 13 : 5)
                        : (near ? 12 : 4);

                case NpcRigPartId.ShinNear:
                case NpcRigPartId.ShinFar:
                    return northFacing
                        ? (near ? 14 : 6)
                        : (near ? 13 : 5);

                case NpcRigPartId.FootNear:
                case NpcRigPartId.FootFar:
                    return northFacing
                        ? (near ? 12 : 4)
                        : (near ? 14 : 6);

                default:
                    return GetBaseSortingOrder(partId);
            }
        }

        public static bool IsNearPart(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.UpperArmNear:
                case NpcRigPartId.ForearmNear:
                case NpcRigPartId.HandNear:
                case NpcRigPartId.ThighNear:
                case NpcRigPartId.ShinNear:
                case NpcRigPartId.FootNear:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsFarPart(
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

        private static int GetBaseSortingOrder(
            NpcRigPartId partId)
        {
            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (definition.Id == partId)
                {
                    return definition.SortingOrder;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(partId),
                partId,
                "Unknown NPC rig part.");
        }
    }
}
