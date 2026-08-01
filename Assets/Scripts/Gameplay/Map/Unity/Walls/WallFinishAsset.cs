using System;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Unity authoring asset for one wall-face finish.
    ///
    /// The finish is independent of whether a wall face currently borders
    /// an interior room or the outside world. Each finish supplies one sprite
    /// for each screen-space wall slope plus optional player-facing catalog art.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Walls/Wall Finish",
        fileName = "WallFinish")]
    public sealed class WallFinishAsset : ScriptableObject
    {
        [SerializeField]
        private string finishId;

        [SerializeField]
        private Sprite risingLeft;

        [SerializeField]
        private Sprite risingRight;

        [Tooltip(
            "Low/base wall shown in cutaway and walls-down views. "
            + "When empty, the matching full wall is used safely.")]
        [SerializeField]
        private Sprite lowRisingLeft;

        [Tooltip(
            "Low/base wall shown in cutaway and walls-down views. "
            + "When empty, the matching full wall is used safely.")]
        [SerializeField]
        private Sprite lowRisingRight;

        [Tooltip(
            "Optional icon displayed by player-facing wall-finish catalogs. "
            + "Wall rendering does not depend on this sprite.")]
        [SerializeField]
        private Sprite catalogIcon;


        public string FinishId =>
            finishId;

        public Sprite CatalogIcon =>
            catalogIcon;

        public WallFinishId Id
        {
            get
            {
                ValidateIdentifier();
                return new WallFinishId(finishId);
            }
        }


        public Sprite GetSprite(
            WallDisplaySlope displaySlope)
        {
            ValidateConfiguration();

            return GetFullSprite(
                displaySlope);
        }


        public Sprite GetSprite(
            WallDisplaySlope displaySlope,
            WallPresentationHeight presentationHeight)
        {
            ValidateConfiguration();

            switch (presentationHeight)
            {
                case WallPresentationHeight.Full:
                    return GetFullSprite(
                        displaySlope);

                case WallPresentationHeight.Low:
                    Sprite lowSprite =
                        GetLowSprite(
                            displaySlope);

                    return lowSprite != null
                        ? lowSprite
                        : GetFullSprite(
                            displaySlope);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(presentationHeight),
                        presentationHeight,
                        "Unsupported wall presentation height.");
            }
        }


        private Sprite GetFullSprite(
            WallDisplaySlope displaySlope)
        {
            switch (displaySlope)
            {
                case WallDisplaySlope.RisingLeft:
                    return risingLeft;

                case WallDisplaySlope.RisingRight:
                    return risingRight;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(displaySlope),
                        displaySlope,
                        "Unsupported wall display slope.");
            }
        }


        private Sprite GetLowSprite(
            WallDisplaySlope displaySlope)
        {
            switch (displaySlope)
            {
                case WallDisplaySlope.RisingLeft:
                    return lowRisingLeft;

                case WallDisplaySlope.RisingRight:
                    return lowRisingRight;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(displaySlope),
                        displaySlope,
                        "Unsupported wall display slope.");
            }
        }


        public void ValidateConfiguration()
        {
            ValidateIdentifier();

            if (risingLeft == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallFinishAsset)} '{name}' requires "
                    + "a RisingLeft sprite.");
            }

            if (risingRight == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallFinishAsset)} '{name}' requires "
                    + "a RisingRight sprite.");
            }
        }


        private void ValidateIdentifier()
        {
            if (string.IsNullOrWhiteSpace(finishId))
            {
                throw new InvalidOperationException(
                    $"{nameof(WallFinishAsset)} '{name}' requires "
                    + "a non-empty finish identifier.");
            }
        }


        private void OnValidate()
        {
            if (finishId != null)
            {
                finishId =
                    finishId.Trim();
            }
        }
    }
}
