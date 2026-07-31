using System;
using BigRetail.Map.Floors;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Floors
{
    /// <summary>
    /// Unity authoring asset for one player-selectable Floor finish.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Floors/Floor Finish",
        fileName = "FloorFinish")]
    public sealed class FloorFinishAsset : ScriptableObject
    {
        [SerializeField]
        private string finishId;

        [SerializeField]
        private TileBase floorTile;

        [Tooltip(
            "Optional icon displayed by player-facing Floor-finish catalogs. "
            + "Floor rendering does not depend on this sprite.")]
        [SerializeField]
        private Sprite catalogIcon;


        public string FinishId =>
            finishId;

        public TileBase FloorTile =>
            floorTile;

        public Sprite CatalogIcon =>
            catalogIcon;

        public FloorFinishId Id
        {
            get
            {
                ValidateIdentifier();
                return new FloorFinishId(finishId);
            }
        }


        public void ValidateConfiguration()
        {
            ValidateIdentifier();

            if (floorTile == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(FloorFinishAsset)} '{name}' requires "
                    + "a Floor Tile.");
            }
        }


        private void ValidateIdentifier()
        {
            if (string.IsNullOrWhiteSpace(finishId))
            {
                throw new InvalidOperationException(
                    $"{nameof(FloorFinishAsset)} '{name}' requires "
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
