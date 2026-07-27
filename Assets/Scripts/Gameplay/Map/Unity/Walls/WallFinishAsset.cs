using System;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Unity authoring asset for one wall-face finish.
    ///
    /// The finish is independent of whether a wall face currently borders
    /// an interior room or the outside world. Each finish supplies one sprite
    /// for each screen-space wall slope.
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


        public string FinishId =>
            finishId;


        public Sprite GetSprite(
            WallDisplaySlope displaySlope)
        {
            ValidateConfiguration();

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


        public void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(finishId))
            {
                throw new InvalidOperationException(
                    $"{nameof(WallFinishAsset)} '{name}' requires "
                    + "a non-empty finish identifier.");
            }

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
