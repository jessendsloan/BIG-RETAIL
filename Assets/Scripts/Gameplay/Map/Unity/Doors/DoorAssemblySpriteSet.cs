using System;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Generic, layered artwork for one four-panel door model: a static outer
    /// frame, one assembly-wide aperture mask, two fixed glass panels, and
    /// two independently movable doors. The artwork is intentionally
    /// unrelated to wall finishes.
    /// </summary>
    [Serializable]
    public sealed class DoorAssemblySpriteSet
    {
        [Header("Rising Left")]

        [SerializeField]
        private Sprite risingLeftFrame;

        [SerializeField]
        private Sprite risingLeftAperture;

        [SerializeField]
        private Sprite risingLeftLeftGlass;

        [SerializeField]
        private Sprite risingLeftLeftDoor;

        [SerializeField]
        private Sprite risingLeftRightDoor;

        [SerializeField]
        private Sprite risingLeftRightGlass;


        [Header("Rising Right")]

        [SerializeField]
        private Sprite risingRightFrame;

        [SerializeField]
        private Sprite risingRightAperture;

        [SerializeField]
        private Sprite risingRightLeftGlass;

        [SerializeField]
        private Sprite risingRightLeftDoor;

        [SerializeField]
        private Sprite risingRightRightDoor;

        [SerializeField]
        private Sprite risingRightRightGlass;


        public bool IsComplete =>
            risingLeftFrame != null
            && risingLeftAperture != null
            && risingLeftLeftGlass != null
            && risingLeftLeftDoor != null
            && risingLeftRightDoor != null
            && risingLeftRightGlass != null
            && risingRightFrame != null
            && risingRightAperture != null
            && risingRightLeftGlass != null
            && risingRightLeftDoor != null
            && risingRightRightDoor != null
            && risingRightRightGlass != null;


        public bool TryGetSprites(
            WallDisplaySlope displaySlope,
            out DoorAssemblySprites sprites)
        {
            if (!IsComplete)
            {
                sprites = default;
                return false;
            }

            sprites = displaySlope switch
            {
                WallDisplaySlope.RisingLeft =>
                    new DoorAssemblySprites(
                        risingLeftFrame,
                        risingLeftAperture,
                        risingLeftLeftGlass,
                        risingLeftLeftDoor,
                        risingLeftRightDoor,
                        risingLeftRightGlass),

                WallDisplaySlope.RisingRight =>
                    new DoorAssemblySprites(
                        risingRightFrame,
                        risingRightAperture,
                        risingRightLeftGlass,
                        risingRightLeftDoor,
                        risingRightRightDoor,
                        risingRightRightGlass),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(displaySlope),
                        displaySlope,
                        "Unsupported wall display slope.")
            };

            return true;
        }
    }


    /// <summary>
    /// One resolved directional set. The two center-door transforms can move
    /// independently while the outer glass and frame remain fixed.
    /// </summary>
    public readonly struct DoorAssemblySprites
    {
        public Sprite Frame { get; }

        public Sprite Aperture { get; }

        public Sprite LeftGlass { get; }

        public Sprite LeftDoor { get; }

        public Sprite RightDoor { get; }

        public Sprite RightGlass { get; }


        public DoorAssemblySprites(
            Sprite frame,
            Sprite aperture,
            Sprite leftGlass,
            Sprite leftDoor,
            Sprite rightDoor,
            Sprite rightGlass)
        {
            Frame =
                frame
                ?? throw new ArgumentNullException(
                    nameof(frame));

            Aperture =
                aperture
                ?? throw new ArgumentNullException(
                    nameof(aperture));

            LeftGlass =
                leftGlass
                ?? throw new ArgumentNullException(
                    nameof(leftGlass));

            LeftDoor =
                leftDoor
                ?? throw new ArgumentNullException(
                    nameof(leftDoor));

            RightDoor =
                rightDoor
                ?? throw new ArgumentNullException(
                    nameof(rightDoor));

            RightGlass =
                rightGlass
                ?? throw new ArgumentNullException(
                    nameof(rightGlass));
        }
    }
}
