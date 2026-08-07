using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    public enum NpcHairLayerDepth
    {
        BehindHead = 0,
        Crown = 1,
        Fringe = 2
    }


    [Serializable]
    public sealed class NpcHairLayerPose
    {
        [SerializeField]
        private Vector3 localPosition;

        [SerializeField]
        private Vector3 localEulerAngles;

        [SerializeField]
        private Vector2 size = new Vector2(0.12f, 0.12f);

        [SerializeField]
        private bool visible = true;


        public Vector3 LocalPosition => localPosition;

        public Vector3 LocalEulerAngles => localEulerAngles;

        public Vector2 Size => size;

        public bool Visible => visible;


        public NpcHairLayerPose()
        {
        }


        public NpcHairLayerPose(
            Vector3 newLocalPosition,
            Vector3 newLocalEulerAngles,
            Vector2 newSize,
            bool newVisible = true)
        {
            localPosition = newLocalPosition;
            localEulerAngles = newLocalEulerAngles;
            size = newSize;
            visible = newVisible;
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                failureReason =
                    "Hair layer size must be greater than zero.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        public void Apply(
            SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            Transform layerTransform = renderer.transform;
            layerTransform.localPosition = localPosition;
            layerTransform.localEulerAngles = localEulerAngles;

            NpcAppearanceUtility.ApplySpriteSize(renderer, size);

            renderer.enabled = visible;
        }
    }


    /// <summary>
    /// One optional shape layered around the two canonical hair slots.
    /// It follows the existing Head bone and supplies one pose for each
    /// authored direction; west-facing variants still come from mirroring.
    /// </summary>
    [Serializable]
    public sealed class NpcHairDetailLayer
    {
        [SerializeField]
        private string displayName = "Hair Detail";

        [SerializeField]
        private NpcHairLayerDepth depth = NpcHairLayerDepth.Crown;

        [Range(0.35f, 1.35f)]
        [SerializeField]
        private float shadeMultiplier = 1f;

        [SerializeField]
        private Sprite southEastSprite;

        [SerializeField]
        private Sprite northEastSprite;

        [SerializeField]
        private NpcHairLayerPose southEastPose =
            new NpcHairLayerPose();

        [SerializeField]
        private NpcHairLayerPose northEastPose =
            new NpcHairLayerPose();


        public string DisplayName => displayName;

        public NpcHairLayerDepth Depth => depth;

        public float ShadeMultiplier => shadeMultiplier;


        public NpcHairDetailLayer()
        {
        }


        public NpcHairDetailLayer(
            string newDisplayName,
            NpcHairLayerDepth newDepth,
            float newShadeMultiplier,
            Sprite newSouthEastSprite,
            Sprite newNorthEastSprite,
            NpcHairLayerPose newSouthEastPose,
            NpcHairLayerPose newNorthEastPose)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? "Hair Detail"
                : newDisplayName;
            depth = newDepth;
            shadeMultiplier = Mathf.Clamp(
                newShadeMultiplier,
                0.35f,
                1.35f);
            southEastSprite = newSouthEastSprite;
            northEastSprite = newNorthEastSprite;
            southEastPose = newSouthEastPose
                ?? new NpcHairLayerPose();
            northEastPose = newNorthEastPose
                ?? new NpcHairLayerPose();
        }


        public Sprite GetSprite(
            NpcAuthoredDirection direction)
        {
            Sprite preferred =
                direction == NpcAuthoredDirection.SouthEast
                    ? southEastSprite
                    : northEastSprite;

            return preferred != null
                ? preferred
                : direction == NpcAuthoredDirection.SouthEast
                    ? northEastSprite
                    : southEastSprite;
        }


        public NpcHairLayerPose GetPose(
            NpcAuthoredDirection direction)
        {
            return direction == NpcAuthoredDirection.SouthEast
                ? southEastPose
                : northEastPose;
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (southEastSprite == null && northEastSprite == null)
            {
                failureReason =
                    $"{displayName} has no sprite.";
                return false;
            }

            if (southEastPose == null)
            {
                failureReason =
                    $"{displayName} has no South East pose.";
                return false;
            }

            if (!southEastPose.TryValidate(out failureReason))
            {
                failureReason =
                    $"{displayName} South East pose: {failureReason}";
                return false;
            }

            if (northEastPose == null)
            {
                failureReason =
                    $"{displayName} has no North East pose.";
                return false;
            }

            if (!northEastPose.TryValidate(out failureReason))
            {
                failureReason =
                    $"{displayName} North East pose: {failureReason}";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        public void Apply(
            SpriteRenderer renderer,
            NpcAuthoredDirection direction,
            Color baseHairColor,
            int sortingOrder)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sprite = GetSprite(direction);
            renderer.color = NpcAppearanceUtility.Shade(
                baseHairColor,
                shadeMultiplier);
            renderer.sortingOrder = sortingOrder;

            NpcHairLayerPose pose = GetPose(direction);

            if (pose != null)
            {
                pose.Apply(renderer);
            }
            else
            {
                renderer.enabled = false;
            }
        }
    }


    [CreateAssetMenu(
        fileName = "HairSet",
        menuName = "Big Retail/Characters/Hair Set")]
    public sealed class NpcHairSet : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Hair";

        [SerializeField]
        private Color hairColor = Color.black;

        [SerializeField]
        private NpcGenderCompatibility supportedGenders =
            NpcGenderCompatibility.Everyone;

        [SerializeField]
        private NpcOutfitPartStyle hairRear;

        [SerializeField]
        private NpcOutfitPartStyle hairFront;

        [SerializeField]
        private NpcAppearancePartShape hairRearShape;

        [SerializeField]
        private NpcAppearancePartShape hairFrontShape;

        [SerializeField]
        private List<NpcHairDetailLayer> detailLayers =
            new List<NpcHairDetailLayer>();


        public string DisplayName => displayName;

        public Color HairColor => hairColor;

        public NpcGenderCompatibility SupportedGenders =>
            supportedGenders;

        public IReadOnlyList<NpcHairDetailLayer> DetailLayers =>
            detailLayers;


        public void Configure(
            string newDisplayName,
            Color newHairColor,
            NpcOutfitPartStyle newHairRear,
            NpcOutfitPartStyle newHairFront,
            NpcAppearancePartShape newHairRearShape,
            NpcAppearancePartShape newHairFrontShape)
        {
            Configure(
                newDisplayName,
                newHairColor,
                NpcGenderCompatibility.Everyone,
                newHairRear,
                newHairFront,
                newHairRearShape,
                newHairFrontShape,
                null);
        }


        public void Configure(
            string newDisplayName,
            Color newHairColor,
            NpcGenderCompatibility newSupportedGenders,
            NpcOutfitPartStyle newHairRear,
            NpcOutfitPartStyle newHairFront,
            NpcAppearancePartShape newHairRearShape,
            NpcAppearancePartShape newHairFrontShape)
        {
            Configure(
                newDisplayName,
                newHairColor,
                newSupportedGenders,
                newHairRear,
                newHairFront,
                newHairRearShape,
                newHairFrontShape,
                null);
        }


        public void Configure(
            string newDisplayName,
            Color newHairColor,
            NpcGenderCompatibility newSupportedGenders,
            NpcOutfitPartStyle newHairRear,
            NpcOutfitPartStyle newHairFront,
            NpcAppearancePartShape newHairRearShape,
            NpcAppearancePartShape newHairFrontShape,
            IEnumerable<NpcHairDetailLayer> newDetailLayers)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            hairColor = newHairColor;
            supportedGenders = newSupportedGenders;
            hairRear = newHairRear;
            hairFront = newHairFront;
            hairRearShape = newHairRearShape;
            hairFrontShape = newHairFrontShape;
            detailLayers = newDetailLayers != null
                ? new List<NpcHairDetailLayer>(newDetailLayers)
                : new List<NpcHairDetailLayer>();
        }


        public bool Supports(
            NpcPersonGender gender)
        {
            return supportedGenders.Supports(gender);
        }


        public bool TryGetStyle(
            NpcRigPartId partId,
            out NpcOutfitPartStyle spriteStyle,
            out NpcAppearancePartShape shape)
        {
            switch (partId)
            {
                case NpcRigPartId.HairRear:
                    spriteStyle = hairRear;
                    shape = hairRearShape;
                    return spriteStyle != null || shape != null;

                case NpcRigPartId.HairFront:
                    spriteStyle = hairFront;
                    shape = hairFrontShape;
                    return spriteStyle != null || shape != null;

                default:
                    spriteStyle = null;
                    shape = null;
                    return false;
            }
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (supportedGenders == NpcGenderCompatibility.None)
            {
                failureReason =
                    "The hairstyle is not enabled for men or women.";
                return false;
            }

            if (hairRear == null || hairRearShape == null)
            {
                failureReason = "Rear hair style is incomplete.";
                return false;
            }

            if (hairFront == null || hairFrontShape == null)
            {
                failureReason = "Front hair style is incomplete.";
                return false;
            }

            if (hairRear.Id != NpcRigPartId.HairRear
                || hairRearShape.Id != NpcRigPartId.HairRear)
            {
                failureReason = "Rear hair uses the wrong part id.";
                return false;
            }

            if (hairFront.Id != NpcRigPartId.HairFront
                || hairFrontShape.Id != NpcRigPartId.HairFront)
            {
                failureReason = "Front hair uses the wrong part id.";
                return false;
            }

            if (detailLayers == null)
            {
                detailLayers = new List<NpcHairDetailLayer>();
            }

            for (int index = 0; index < detailLayers.Count; index++)
            {
                NpcHairDetailLayer layer = detailLayers[index];

                if (layer == null)
                {
                    failureReason =
                        $"Hair detail layer {index + 1} is empty.";
                    return false;
                }

                if (!layer.TryValidate(out failureReason))
                {
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
