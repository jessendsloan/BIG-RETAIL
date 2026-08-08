using System;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Directional artwork for an always-open doorway. Its frame and aperture
    /// share one assembly-wide canvas so the opening is cut continuously
    /// across every supporting wall panel.
    /// </summary>
    [Serializable]
    public sealed class DoorwaySpriteSet
    {
        [Header("Rising Left")]

        [SerializeField]
        private Sprite risingLeftFrame;

        [SerializeField]
        private Sprite risingLeftAperture;


        [Header("Rising Right")]

        [SerializeField]
        private Sprite risingRightFrame;

        [SerializeField]
        private Sprite risingRightAperture;


        public bool IsComplete =>
            risingLeftFrame != null
            && risingLeftAperture != null
            && risingRightFrame != null
            && risingRightAperture != null;


        public bool TryGetSprites(
            WallDisplaySlope displaySlope,
            out DoorwaySprites sprites)
        {
            if (!IsComplete)
            {
                sprites = default;
                return false;
            }

            sprites = displaySlope switch
            {
                WallDisplaySlope.RisingLeft =>
                    new DoorwaySprites(
                        risingLeftFrame,
                        risingLeftAperture),

                WallDisplaySlope.RisingRight =>
                    new DoorwaySprites(
                        risingRightFrame,
                        risingRightAperture),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(displaySlope),
                        displaySlope,
                        "Unsupported wall display slope.")
            };

            return true;
        }
    }


    public readonly struct DoorwaySprites
    {
        public Sprite Frame { get; }

        public Sprite Aperture { get; }


        public DoorwaySprites(
            Sprite frame,
            Sprite aperture)
        {
            Frame =
                frame
                ?? throw new ArgumentNullException(
                    nameof(frame));

            Aperture =
                aperture
                ?? throw new ArgumentNullException(
                    nameof(aperture));
        }
    }
}
