using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Validates and applies finish changes to individual structural wall faces.
    ///
    /// The service stores only non-default overrides. Structural demolition
    /// automatically clears both face overrides through WallState notifications.
    /// </summary>
    public sealed class WallFinishService : IDisposable
    {
        private readonly WallState wallState;
        private readonly WallFinishCatalog finishCatalog;
        private readonly WallFinishState finishState;

        private bool isDisposed;


        public event Action<WallFaceKey, WallFinishId>
            EffectiveFinishChanged;


        public WallFinishService(
            WallState wallState,
            WallFinishCatalog finishCatalog,
            WallFinishState finishState)
        {
            this.wallState =
                wallState
                ?? throw new ArgumentNullException(
                    nameof(wallState));

            this.finishCatalog =
                finishCatalog
                ?? throw new ArgumentNullException(
                    nameof(finishCatalog));

            this.finishState =
                finishState
                ?? throw new ArgumentNullException(
                    nameof(finishState));

            wallState.WallRemoved +=
                HandleWallRemoved;
        }


        public WallFinishId GetEffectiveFinish(
            CellEdge edge,
            GridPosition facingCell)
        {
            ThrowIfDisposed();

            WallFinishChangeFailure targetFailure =
                EvaluateTarget(
                    edge,
                    facingCell);

            if (targetFailure ==
                WallFinishChangeFailure.WallNotFound)
            {
                throw new KeyNotFoundException(
                    $"Wall edge '{edge}' does not exist.");
            }

            if (targetFailure ==
                WallFinishChangeFailure.FacingCellNotOnEdge)
            {
                throw new ArgumentException(
                    $"Cell {facingCell} does not touch wall edge {edge}.",
                    nameof(facingCell));
            }

            WallFaceKey face =
                new WallFaceKey(
                    edge,
                    facingCell);

            return finishState.TryGetOverride(
                    face,
                    out WallFinishId finishId)
                ? finishId
                : finishCatalog.DefaultFinishId;
        }


        public WallFinishChangeResult TrySetFinish(
            CellEdge edge,
            GridPosition facingCell,
            WallFinishId finishId)
        {
            ThrowIfDisposed();

            WallFinishChangeFailure targetFailure =
                EvaluateTarget(
                    edge,
                    facingCell);

            if (targetFailure !=
                WallFinishChangeFailure.None)
            {
                return WallFinishChangeResult.Failed(
                    edge,
                    facingCell,
                    targetFailure);
            }

            if (!finishCatalog.Contains(finishId))
            {
                return WallFinishChangeResult.Failed(
                    edge,
                    facingCell,
                    WallFinishChangeFailure.UnknownFinish);
            }

            WallFaceKey face =
                new WallFaceKey(
                    edge,
                    facingCell);

            bool changed;

            if (finishId ==
                finishCatalog.DefaultFinishId)
            {
                changed =
                    finishState.ResetOverride(face);
            }
            else
            {
                changed =
                    finishState.SetOverride(
                        face,
                        finishId);
            }

            if (changed)
            {
                EffectiveFinishChanged?.Invoke(
                    face,
                    finishId);
            }

            return WallFinishChangeResult.Success(
                edge,
                facingCell,
                finishId,
                changed);
        }


        public WallFinishChangeResult TryResetFinish(
            CellEdge edge,
            GridPosition facingCell)
        {
            ThrowIfDisposed();

            WallFinishChangeFailure targetFailure =
                EvaluateTarget(
                    edge,
                    facingCell);

            if (targetFailure !=
                WallFinishChangeFailure.None)
            {
                return WallFinishChangeResult.Failed(
                    edge,
                    facingCell,
                    targetFailure);
            }

            WallFaceKey face =
                new WallFaceKey(
                    edge,
                    facingCell);

            bool changed =
                finishState.ResetOverride(face);

            if (changed)
            {
                EffectiveFinishChanged?.Invoke(
                    face,
                    finishCatalog.DefaultFinishId);
            }

            return WallFinishChangeResult.Success(
                edge,
                facingCell,
                finishCatalog.DefaultFinishId,
                changed);
        }


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            wallState.WallRemoved -=
                HandleWallRemoved;

            isDisposed = true;
        }


        private WallFinishChangeFailure EvaluateTarget(
            CellEdge edge,
            GridPosition facingCell)
        {
            if (!wallState.HasWall(edge))
            {
                return WallFinishChangeFailure.WallNotFound;
            }

            if (!edge.TouchesCell(facingCell))
            {
                return WallFinishChangeFailure.FacingCellNotOnEdge;
            }

            return WallFinishChangeFailure.None;
        }

        private void HandleWallRemoved(
            CellEdge edge)
        {
            finishState.ClearOverrides(edge);
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(WallFinishService));
            }
        }
    }
}
