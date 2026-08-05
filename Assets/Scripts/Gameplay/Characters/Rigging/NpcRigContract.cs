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
    /// Camera-left and camera-right always describe the authored image as seen
    /// by the viewer, never the character's anatomical left and right.
    /// </summary>
    public enum NpcRigBoneId
    {
        Root = 0,
        Pelvis = 1,
        SpineLower = 2,
        Chest = 3,
        Neck = 4,
        Head = 5,
        ShoulderSourceCameraLeft = 6,
        UpperArmSourceCameraLeft = 7,
        ForearmSourceCameraLeft = 8,
        HandSourceCameraLeft = 9,
        ShoulderSourceCameraRight = 10,
        UpperArmSourceCameraRight = 11,
        ForearmSourceCameraRight = 12,
        HandSourceCameraRight = 13,
        ThighSourceCameraLeft = 14,
        ShinSourceCameraLeft = 15,
        FootSourceCameraLeft = 16,
        ThighSourceCameraRight = 17,
        ShinSourceCameraRight = 18,
        FootSourceCameraRight = 19
    }

    /// <summary>
    /// Stable identifiers for the 18 visible cutout pieces.
    /// </summary>
    public enum NpcRigPartId
    {
        HairRear = 0,
        UpperArmSourceCameraLeft = 1,
        ForearmSourceCameraLeft = 2,
        HandSourceCameraLeft = 3,
        ThighSourceCameraLeft = 4,
        ShinSourceCameraLeft = 5,
        FootSourceCameraLeft = 6,
        Pelvis = 7,
        Torso = 8,
        Neck = 9,
        Head = 10,
        HairFront = 11,
        ThighSourceCameraRight = 12,
        ShinSourceCameraRight = 13,
        FootSourceCameraRight = 14,
        UpperArmSourceCameraRight = 15,
        ForearmSourceCameraRight = 16,
        HandSourceCameraRight = 17
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
                    NpcRigBoneId.ShoulderSourceCameraLeft,
                    NpcRigBoneId.Chest,
                    new Vector3(-0.13f, 0.02f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmSourceCameraLeft,
                    NpcRigBoneId.ShoulderSourceCameraLeft,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmSourceCameraLeft,
                    NpcRigBoneId.UpperArmSourceCameraLeft,
                    new Vector3(-0.05f, -0.25f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandSourceCameraLeft,
                    NpcRigBoneId.ForearmSourceCameraLeft,
                    new Vector3(-0.03f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShoulderSourceCameraRight,
                    NpcRigBoneId.Chest,
                    new Vector3(0.16f, 0f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.UpperArmSourceCameraRight,
                    NpcRigBoneId.ShoulderSourceCameraRight,
                    Vector3.zero),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ForearmSourceCameraRight,
                    NpcRigBoneId.UpperArmSourceCameraRight,
                    new Vector3(0.06f, -0.26f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.HandSourceCameraRight,
                    NpcRigBoneId.ForearmSourceCameraRight,
                    new Vector3(0.04f, -0.22f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighSourceCameraLeft,
                    NpcRigBoneId.Pelvis,
                    new Vector3(-0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinSourceCameraLeft,
                    NpcRigBoneId.ThighSourceCameraLeft,
                    new Vector3(-0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootSourceCameraLeft,
                    NpcRigBoneId.ShinSourceCameraLeft,
                    new Vector3(0.01f, -0.35f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ThighSourceCameraRight,
                    NpcRigBoneId.Pelvis,
                    new Vector3(0.10f, -0.04f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.ShinSourceCameraRight,
                    NpcRigBoneId.ThighSourceCameraRight,
                    new Vector3(0.02f, -0.36f, 0f)),

                new NpcRigBoneDefinition(
                    NpcRigBoneId.FootSourceCameraRight,
                    NpcRigBoneId.ShinSourceCameraRight,
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
                    NpcRigPartId.UpperArmSourceCameraLeft,
                    NpcRigBoneId.UpperArmSourceCameraLeft,
                    1,
                    new Vector2(-0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmSourceCameraLeft,
                    NpcRigBoneId.ForearmSourceCameraLeft,
                    2,
                    new Vector2(-0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandSourceCameraLeft,
                    NpcRigBoneId.HandSourceCameraLeft,
                    3,
                    new Vector2(0f, -0.07f),
                    new Vector2(0.12f, 0.16f)),

                DefinePart(
                    NpcRigPartId.ThighSourceCameraLeft,
                    NpcRigBoneId.ThighSourceCameraLeft,
                    4,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinSourceCameraLeft,
                    NpcRigBoneId.ShinSourceCameraLeft,
                    5,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootSourceCameraLeft,
                    NpcRigBoneId.FootSourceCameraLeft,
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
                    NpcRigPartId.ThighSourceCameraRight,
                    NpcRigBoneId.ThighSourceCameraRight,
                    12,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.18f, 0.40f)),

                DefinePart(
                    NpcRigPartId.ShinSourceCameraRight,
                    NpcRigBoneId.ShinSourceCameraRight,
                    13,
                    new Vector2(0f, -0.18f),
                    new Vector2(0.15f, 0.39f)),

                DefinePart(
                    NpcRigPartId.FootSourceCameraRight,
                    NpcRigBoneId.FootSourceCameraRight,
                    14,
                    new Vector2(0.04f, -0.04f),
                    new Vector2(0.23f, 0.12f)),

                DefinePart(
                    NpcRigPartId.UpperArmSourceCameraRight,
                    NpcRigBoneId.UpperArmSourceCameraRight,
                    15,
                    new Vector2(0.02f, -0.12f),
                    new Vector2(0.14f, 0.30f)),

                DefinePart(
                    NpcRigPartId.ForearmSourceCameraRight,
                    NpcRigBoneId.ForearmSourceCameraRight,
                    16,
                    new Vector2(0.01f, -0.11f),
                    new Vector2(0.12f, 0.27f)),

                DefinePart(
                    NpcRigPartId.HandSourceCameraRight,
                    NpcRigBoneId.HandSourceCameraRight,
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
        /// The single presentation rule for the current fixed camera view.
        /// Southeast presents the camera-left side in front; Southwest is
        /// the mirrored counterpart. North-facing values are the matching
        /// mirrored pair until their authored back view is introduced.
        /// </summary>
        public static NpcCameraSide GetForegroundCameraSide(
            NpcFacing facing)
        {
            switch (facing)
            {
                case NpcFacing.SouthEast:
                case NpcFacing.NorthWest:
                    return NpcCameraSide.CameraLeft;

                case NpcFacing.SouthWest:
                case NpcFacing.NorthEast:
                    return NpcCameraSide.CameraRight;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(facing),
                        facing,
                        "Unknown NPC facing.");
            }
        }

        /// <summary>
        /// Resolves one paired limb's side in the current camera view. The
        /// existing Far/Near identifiers describe source-canvas placement:
        /// Far is camera-left in an unmirrored authored view and Near is
        /// camera-right. Their displayed camera side reverses on a mirror.
        /// </summary>
        public static bool TryGetDisplayedCameraSide(
            NpcFacing facing,
            NpcRigPartId partId,
            out NpcCameraSide cameraSide)
        {
            bool isSourceCameraLeft;

            switch (partId)
            {
                case NpcRigPartId.UpperArmSourceCameraLeft:
                case NpcRigPartId.ForearmSourceCameraLeft:
                case NpcRigPartId.HandSourceCameraLeft:
                case NpcRigPartId.ThighSourceCameraLeft:
                case NpcRigPartId.ShinSourceCameraLeft:
                case NpcRigPartId.FootSourceCameraLeft:
                    isSourceCameraLeft = true;
                    break;

                case NpcRigPartId.UpperArmSourceCameraRight:
                case NpcRigPartId.ForearmSourceCameraRight:
                case NpcRigPartId.HandSourceCameraRight:
                case NpcRigPartId.ThighSourceCameraRight:
                case NpcRigPartId.ShinSourceCameraRight:
                case NpcRigPartId.FootSourceCameraRight:
                    isSourceCameraLeft = false;
                    break;

                default:
                    cameraSide = default;
                    return false;
            }

            bool displayedCameraLeft =
                IsMirrored(facing)
                    ? !isSourceCameraLeft
                    : isSourceCameraLeft;

            cameraSide = displayedCameraLeft
                ? NpcCameraSide.CameraLeft
                : NpcCameraSide.CameraRight;
            return true;
        }

        /// <summary>
        /// Resolves a renderer order from the visible camera side and facing,
        /// rather than from a permanently named Far/Near depth assumption.
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

            if (!TryGetDisplayedCameraSide(
                    facing,
                    partId,
                    out NpcCameraSide cameraSide))
            {
                return GetBaseSortingOrder(partId);
            }

            bool foreground = cameraSide
                == GetForegroundCameraSide(facing);

            switch (partId)
            {
                case NpcRigPartId.UpperArmSourceCameraLeft:
                case NpcRigPartId.UpperArmSourceCameraRight:
                    return foreground ? 15 : 1;

                case NpcRigPartId.ForearmSourceCameraLeft:
                case NpcRigPartId.ForearmSourceCameraRight:
                    return foreground ? 16 : 2;

                case NpcRigPartId.HandSourceCameraLeft:
                case NpcRigPartId.HandSourceCameraRight:
                    return foreground ? 17 : 3;

                case NpcRigPartId.ThighSourceCameraLeft:
                case NpcRigPartId.ThighSourceCameraRight:
                    return northFacing
                        ? (foreground ? 13 : 5)
                        : (foreground ? 12 : 4);

                case NpcRigPartId.ShinSourceCameraLeft:
                case NpcRigPartId.ShinSourceCameraRight:
                    return northFacing
                        ? (foreground ? 14 : 6)
                        : (foreground ? 13 : 5);

                case NpcRigPartId.FootSourceCameraLeft:
                case NpcRigPartId.FootSourceCameraRight:
                    return northFacing
                        ? (foreground ? 12 : 4)
                        : (foreground ? 14 : 6);

                default:
                    return GetBaseSortingOrder(partId);
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
