using System;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Directional artwork for a one-panel hinged door. The frame remains
    /// fixed while the door panel is transformed around its authored hinge.
    /// Both sprites for one slope share the same canvas and pivot.
    /// </summary>
    [Serializable]
    public sealed class HingedDoorSpriteSet
    {
        [Header("Rising Left")]

        [SerializeField]
        private Sprite risingLeftFrame;

        [SerializeField]
        private Sprite risingLeftDoor;


        [Header("Rising Right")]

        [SerializeField]
        private Sprite risingRightFrame;

        [SerializeField]
        private Sprite risingRightDoor;


        public bool IsComplete =>
            risingLeftFrame != null
            && risingLeftDoor != null
            && risingRightFrame != null
            && risingRightDoor != null;


        public bool TryGetSprites(
            WallDisplaySlope displaySlope,
            out HingedDoorSprites sprites)
        {
            if (!IsComplete)
            {
                sprites = default;
                return false;
            }

            sprites = displaySlope switch
            {
                WallDisplaySlope.RisingLeft =>
                    new HingedDoorSprites(
                        risingLeftFrame,
                        risingLeftDoor),

                WallDisplaySlope.RisingRight =>
                    new HingedDoorSprites(
                        risingRightFrame,
                        risingRightDoor),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(displaySlope),
                        displaySlope,
                        "Unsupported wall display slope.")
            };

            return true;
        }
    }


    public readonly struct HingedDoorSprites
    {
        public Sprite Frame { get; }

        public Sprite Door { get; }


        public HingedDoorSprites(
            Sprite frame,
            Sprite door)
        {
            Frame =
                frame
                ?? throw new ArgumentNullException(
                    nameof(frame));

            Door =
                door
                ?? throw new ArgumentNullException(
                    nameof(door));
        }
    }
}
