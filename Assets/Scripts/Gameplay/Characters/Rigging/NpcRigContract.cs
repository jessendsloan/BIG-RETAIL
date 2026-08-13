using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// The four directions an NPC can display in the isometric world.
    ///
    /// SouthEast and NorthEast identify the two stored source sets: the
    /// south/front view and the north/back view. Each east-facing source is
    /// authored directly; its west-facing partner mirrors the complete visual.
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
    /// The complete presentation contract for one displayed facing. Keeping
    /// these decisions together prevents artwork, skeleton, layering, and
    /// animation code from inventing separate direction conventions.
    /// </summary>
    public readonly struct NpcFacingPresentation
    {
        public NpcAuthoredDirection AuthoredDirection { get; }

        public bool MirrorHorizontally { get; }

        public NpcCameraSide ForegroundCameraSide { get; }

        public bool UsesNorthFacingAnimation =>
            AuthoredDirection == NpcAuthoredDirection.NorthEast;


        public NpcFacingPresentation(
            NpcAuthoredDirection authoredDirection,
            bool mirrorHorizontally,
            NpcCameraSide foregroundCameraSide)
        {
            AuthoredDirection = authoredDirection;
            MirrorHorizontally = mirrorHorizontally;
            ForegroundCameraSide = foregroundCameraSide;
        }
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
        ShoulderForeground = 6,
        UpperArmForeground = 7,
        ForearmForeground = 8,
        HandForeground = 9,
        ShoulderBackground = 10,
        UpperArmBackground = 11,
        ForearmBackground = 12,
        HandBackground = 13,
        ThighForeground = 14,
        ShinForeground = 15,
        FootForeground = 16,
        ThighBackground = 17,
        ShinBackground = 18,
        FootBackground = 19
    }

    /// <summary>
    /// Stable identifiers for the 18 visible cutout pieces.
    /// </summary>
    public enum NpcRigPartId
    {
        HairRear = 0,
        UpperArmForeground = 1,
        ForearmForeground = 2,
        HandForeground = 3,
        ThighForeground = 4,
        ShinForeground = 5,
        FootForeground = 6,
        Pelvis = 7,
        Torso = 8,
        Neck = 9,
        Head = 10,
        HairFront = 11,
        ThighBackground = 12,
        ShinBackground = 13,
        FootBackground = 14,
        UpperArmBackground = 15,
        ForearmBackground = 16,
        HandBackground = 17
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
                    NpcRigBoneId.ShoulderForeground,
                    NpcRigBoneId.Chest,
                    new Vector3(-0.13f, 0.02f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmForeground,
                    NpcRigBoneId.ShoulderForeground,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmForeground,
                    NpcRigBoneId.UpperArmForeground,
                    new Vector3(-0.05f, -0.25f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandForeground,
                    NpcRigBoneId.ForearmForeground,
                    new Vector3(-0.03f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShoulderBackground,
                    NpcRigBoneId.Chest,
                    new Vector3(0.16f, 0f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmBackground,
                    NpcRigBoneId.ShoulderBackground,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmBackground,
                    NpcRigBoneId.UpperArmBackground,
                    new Vector3(0.06f, -0.26f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandBackground,
                    NpcRigBoneId.ForearmBackground,
                    new Vector3(0.04f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighForeground,
                    NpcRigBoneId.Pelvis,
                    new Vector3(-0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinForeground,
                    NpcRigBoneId.ThighForeground,
                    new Vector3(-0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootForeground,
                    NpcRigBoneId.ShinForeground,
                    new Vector3(0.01f, -0.35f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighBackground,
                    NpcRigBoneId.Pelvis,
                    new Vector3(0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinBackground,
                    NpcRigBoneId.ThighBackground,
                    new Vector3(0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootBackground,
                    NpcRigBoneId.ShinBackground,
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
                    NpcRigPartId.UpperArmForeground,
                    NpcRigBoneId.UpperArmForeground,
                    1,
                    new Vector2(-0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmForeground,
                    NpcRigBoneId.ForearmForeground,
                    2,
                    new Vector2(-0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandForeground,
                    NpcRigBoneId.HandForeground,
                    3,
                    new Vector2(0f, -0.07f),
                    new Vector2(0.12f, 0.16f)),

                DefinePart(
                    NpcRigPartId.ThighForeground,
                    NpcRigBoneId.ThighForeground,
                    4,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinForeground,
                    NpcRigBoneId.ShinForeground,
                    5,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootForeground,
                    NpcRigBoneId.FootForeground,
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
                    NpcRigPartId.ThighBackground,
                    NpcRigBoneId.ThighBackground,
                    12,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinBackground,
                    NpcRigBoneId.ShinBackground,
                    13,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootBackground,
                    NpcRigBoneId.FootBackground,
                    14,
                    new Vector2(0.04f, -0.04f),
                    new Vector2(0.23f, 0.12f)),

                DefinePart(
                    NpcRigPartId.UpperArmBackground,
                    NpcRigBoneId.UpperArmBackground,
                    15,
                    new Vector2(0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmBackground,
                    NpcRigBoneId.ForearmBackground,
                    16,
                    new Vector2(0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandBackground,
                    NpcRigBoneId.HandBackground,
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
        /// <summary>
        /// Single source of truth for all four displayed directions.
        /// South/North choose the authored body and animation view. East/West
        /// choose whether that complete view is mirrored. Near always remains
        /// the foreground limb chain.
        /// </summary>
        public static NpcFacingPresentation GetPresentation(
            NpcFacing facing)
        {
            switch (facing)
            {
                case NpcFacing.SouthEast:
                    return new NpcFacingPresentation(
                        NpcAuthoredDirection.SouthEast,
                        false,
                        NpcCameraSide.CameraLeft);

                case NpcFacing.SouthWest:
                    return new NpcFacingPresentation(
                        NpcAuthoredDirection.SouthEast,
                        true,
                        NpcCameraSide.CameraRight);

                case NpcFacing.NorthEast:
                    return new NpcFacingPresentation(
                        NpcAuthoredDirection.NorthEast,
                        false,
                        NpcCameraSide.CameraRight);

                case NpcFacing.NorthWest:
                    return new NpcFacingPresentation(
                        NpcAuthoredDirection.NorthEast,
                        true,
                        NpcCameraSide.CameraLeft);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(facing),
                        facing,
                        "Unknown NPC facing.");
            }
        }

        public static NpcAuthoredDirection GetAuthoredDirection(
            NpcFacing facing)
        {
            return GetPresentation(facing).AuthoredDirection;
        }

        /// <summary>
        /// Returns whether the completed character is horizontally mirrored
        /// for display. Authored sources face east, so only west facings mirror.
        /// </summary>
        public static bool IsDisplayMirrored(
            NpcFacing facing)
        {
            return GetPresentation(facing).MirrorHorizontally;
        }

        /// <summary>
        /// Returns the screen side currently occupied by the stable Near
        /// (foreground) chain. This is presentation information only; it must
        /// never be used to redefine Near/Far identity.
        /// </summary>
        public static NpcCameraSide GetForegroundCameraSide(
            NpcFacing facing)
        {
            return GetPresentation(facing).ForegroundCameraSide;
        }

        /// <summary>
        /// Resolves one depth-identified limb's displayed screen side. Near
        /// always means foreground and occupies the facing-dependent foreground
        /// side; Far occupies the opposite side. Their depth identities never
        /// change when the character turns or mirrors.
        /// </summary>
        public static bool TryGetDisplayedCameraSide(
            NpcFacing facing,
            NpcRigPartId partId,
            out NpcCameraSide cameraSide)
        {
            bool isNear;

            switch (partId)
            {
                case NpcRigPartId.UpperArmForeground:
                case NpcRigPartId.ForearmForeground:
                case NpcRigPartId.HandForeground:
                case NpcRigPartId.ThighForeground:
                case NpcRigPartId.ShinForeground:
                case NpcRigPartId.FootForeground:
                    isNear = true;
                    break;

                case NpcRigPartId.UpperArmBackground:
                case NpcRigPartId.ForearmBackground:
                case NpcRigPartId.HandBackground:
                case NpcRigPartId.ThighBackground:
                case NpcRigPartId.ShinBackground:
                case NpcRigPartId.FootBackground:
                    isNear = false;
                    break;

                default:
                    cameraSide = default;
                    return false;
            }

            NpcCameraSide foregroundSide =
                GetForegroundCameraSide(facing);

            cameraSide = isNear
                ? foregroundSide
                : GetOppositeCameraSide(foregroundSide);
            return true;
        }

        /// <summary>
        /// Converts a canonical south/front limb-bone position into the
        /// independently authored north/back source frame. Turning from the
        /// front source to the back source swaps the screen side of every
        /// foreground/background limb chain, while core bones remain fixed.
        /// </summary>
        public static Vector3 ResolveAuthoredBonePosition(
            NpcAuthoredDirection direction,
            NpcRigBoneId boneId,
            Vector3 canonicalPosition)
        {
            return direction == NpcAuthoredDirection.NorthEast
                   && IsDepthLimbBone(boneId)
                ? ReflectHorizontal(canonicalPosition)
                : canonicalPosition;
        }

        /// <summary>
        /// Artwork counterpart of ResolveAuthoredBonePosition. Arm, thigh, and
        /// shin artwork follows the north/back source-view reflection. Feet keep
        /// their authored placement beside the ankle so NorthEast owns one
        /// complete east-pointing foot pose; NorthWest mirrors that result.
        /// </summary>
        public static Vector3 ResolveAuthoredPartPosition(
            NpcAuthoredDirection direction,
            NpcRigPartId partId,
            Vector3 canonicalPosition)
        {
            return direction == NpcAuthoredDirection.NorthEast
                   && IsDepthLimbPart(partId)
                   && !IsFootPart(partId)
                ? ReflectHorizontal(canonicalPosition)
                : canonicalPosition;
        }

        /// <summary>
        /// Converts canonical south/front artwork rotation into the north/back
        /// source frame. Limb segments reflect with their depth chains, while
        /// feet preserve their canonical toe heading. NorthEast is the authored
        /// north/back source and NorthWest is produced by mirroring that complete
        /// source once, which supplies the opposite foot heading.
        /// </summary>
        public static Vector3 ResolveAuthoredPartEulerAngles(
            NpcAuthoredDirection direction,
            NpcRigPartId partId,
            Vector3 canonicalEulerAngles)
        {
            if (direction == NpcAuthoredDirection.NorthEast
                && IsDepthLimbPart(partId)
                && !IsFootPart(partId))
            {
                canonicalEulerAngles.z =
                    Mathf.DeltaAngle(0f, -canonicalEulerAngles.z);
            }

            return canonicalEulerAngles;
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
                case NpcRigPartId.UpperArmForeground:
                case NpcRigPartId.UpperArmBackground:
                    return near ? 15 : 1;

                case NpcRigPartId.ForearmForeground:
                case NpcRigPartId.ForearmBackground:
                    return near ? 16 : 2;

                case NpcRigPartId.HandForeground:
                case NpcRigPartId.HandBackground:
                    return near ? 17 : 3;

                case NpcRigPartId.ThighForeground:
                case NpcRigPartId.ThighBackground:
                    return northFacing
                        ? (near ? 13 : 5)
                        : (near ? 12 : 4);

                case NpcRigPartId.ShinForeground:
                case NpcRigPartId.ShinBackground:
                    return northFacing
                        ? (near ? 14 : 6)
                        : (near ? 13 : 5);

                case NpcRigPartId.FootForeground:
                case NpcRigPartId.FootBackground:
                    return northFacing
                        ? (near ? 12 : 4)
                        : (near ? 14 : 6);

                default:
                    return GetBaseSortingOrder(partId);
            }
        }

        /// <summary>
        /// Compatibility alias for callers that predate the explicit display
        /// mirror name. It must not be used to infer depth or source direction.
        /// </summary>
        public static bool IsMirrored(
            NpcFacing facing)
        {
            return IsDisplayMirrored(facing);
        }

        /// <summary>
        /// Compatibility hook retained for existing authoring tools. The
        /// NorthEast source preserves the authored toe heading; west presentation
        /// mirrors the complete character once to create the opposite heading.
        /// </summary>
        public static float RemapDirectionalFootAngle(
            NpcAuthoredDirection direction,
            NpcRigPartId partId,
            float authoredAngle)
        {
            return ResolveAuthoredPartEulerAngles(
                    direction,
                    partId,
                    new Vector3(0f, 0f, authoredAngle))
                .z;
        }

        /// <summary>
        /// Bone-angle compatibility hook. Directional pose assets own their
        /// local angles; the display mirror handles east/west presentation.
        /// </summary>
        public static float RemapDirectionalFootAngle(
            NpcFacing facing,
            NpcRigBoneId boneId,
            float authoredAngle)
        {
            return authoredAngle;
        }

        /// <summary>
        /// Selects the independently authored north/back animation view.
        /// East/west presentation remains the responsibility of the visual
        /// root mirror; this choice only distinguishes front from back motion.
        /// </summary>
        public static bool UsesNorthFacingAnimation(
            NpcFacing facing)
        {
            return GetPresentation(facing).UsesNorthFacingAnimation;
        }

        public static bool IsNearPart(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.UpperArmForeground:
                case NpcRigPartId.ForearmForeground:
                case NpcRigPartId.HandForeground:
                case NpcRigPartId.ThighForeground:
                case NpcRigPartId.ShinForeground:
                case NpcRigPartId.FootForeground:
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
                case NpcRigPartId.UpperArmBackground:
                case NpcRigPartId.ForearmBackground:
                case NpcRigPartId.HandBackground:
                case NpcRigPartId.ThighBackground:
                case NpcRigPartId.ShinBackground:
                case NpcRigPartId.FootBackground:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsDepthLimbPart(
            NpcRigPartId partId)
        {
            return IsNearPart(partId) || IsFarPart(partId);
        }

        public static bool IsFootPart(
            NpcRigPartId partId)
        {
            return partId == NpcRigPartId.FootForeground
                   || partId == NpcRigPartId.FootBackground;
        }

        public static bool IsDepthLimbBone(
            NpcRigBoneId boneId)
        {
            switch (boneId)
            {
                case NpcRigBoneId.ShoulderForeground:
                case NpcRigBoneId.UpperArmForeground:
                case NpcRigBoneId.ForearmForeground:
                case NpcRigBoneId.HandForeground:
                case NpcRigBoneId.ShoulderBackground:
                case NpcRigBoneId.UpperArmBackground:
                case NpcRigBoneId.ForearmBackground:
                case NpcRigBoneId.HandBackground:
                case NpcRigBoneId.ThighForeground:
                case NpcRigBoneId.ShinForeground:
                case NpcRigBoneId.FootForeground:
                case NpcRigBoneId.ThighBackground:
                case NpcRigBoneId.ShinBackground:
                case NpcRigBoneId.FootBackground:
                    return true;

                default:
                    return false;
            }
        }

        private static NpcCameraSide GetOppositeCameraSide(
            NpcCameraSide cameraSide)
        {
            return cameraSide == NpcCameraSide.CameraLeft
                ? NpcCameraSide.CameraRight
                : NpcCameraSide.CameraLeft;
        }

        private static Vector3 ReflectHorizontal(
            Vector3 position)
        {
            position.x = -position.x;
            return position;
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
