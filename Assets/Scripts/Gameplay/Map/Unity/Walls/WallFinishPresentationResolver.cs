using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Resolves the authored presentation asset and directional sprite for one
    /// effective structural wall-face finish.
    /// </summary>
    public sealed class WallFinishPresentationResolver
    {
        private readonly WallFinishService finishService;
        private readonly WallFinishAssetCatalog assetCatalog;


        public WallFinishPresentationResolver(
            WallFinishService finishService,
            WallFinishAssetCatalog assetCatalog)
        {
            this.finishService =
                finishService
                ?? throw new ArgumentNullException(
                    nameof(finishService));

            this.assetCatalog =
                assetCatalog
                ?? throw new ArgumentNullException(
                    nameof(assetCatalog));

            assetCatalog.ValidateConfiguration();
        }


        public WallFinishAsset ResolveAsset(
            CellEdge edge,
            GridPosition viewerFacingCell)
        {
            WallFinishId finishId =
                finishService.GetEffectiveFinish(
                    edge,
                    viewerFacingCell);

            return assetCatalog.GetAsset(
                finishId);
        }


        public Sprite ResolveSprite(
            CellEdge edge,
            GridPosition viewerFacingCell,
            WallDisplaySlope displaySlope)
        {
            return ResolveAsset(
                    edge,
                    viewerFacingCell)
                .GetSprite(
                    displaySlope);
        }
    }
}
