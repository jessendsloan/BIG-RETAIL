using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Validates and applies finish changes to constructed Floor cells.
    ///
    /// Structural Floor removal automatically clears its finish override.
    /// </summary>
    public sealed class FloorFinishService : IDisposable
    {
        private readonly FloorState floorState;
        private readonly FloorFinishCatalog finishCatalog;
        private readonly FloorFinishState finishState;

        private bool isDisposed;


        public event Action<GridPosition, FloorFinishId>
            EffectiveFinishChanged;


        public FloorFinishService(
            FloorState floorState,
            FloorFinishCatalog finishCatalog,
            FloorFinishState finishState)
        {
            this.floorState =
                floorState
                ?? throw new ArgumentNullException(
                    nameof(floorState));

            this.finishCatalog =
                finishCatalog
                ?? throw new ArgumentNullException(
                    nameof(finishCatalog));

            this.finishState =
                finishState
                ?? throw new ArgumentNullException(
                    nameof(finishState));

            floorState.FloorRemoved +=
                HandleFloorRemoved;
        }


        public FloorFinishId GetEffectiveFinish(
            GridPosition cell)
        {
            ThrowIfDisposed();

            if (!floorState.HasFloor(cell))
            {
                throw new KeyNotFoundException(
                    $"Floor cell '{cell}' does not exist.");
            }

            return finishState.TryGetOverride(
                    cell,
                    out FloorFinishId finishId)
                ? finishId
                : finishCatalog.DefaultFinishId;
        }

        public FloorFinishChangeResult TrySetFinish(
            GridPosition cell,
            FloorFinishId finishId)
        {
            ThrowIfDisposed();

            if (!floorState.HasFloor(cell))
            {
                return FloorFinishChangeResult.Failed(
                    cell,
                    FloorFinishChangeFailure.FloorNotFound);
            }

            if (!finishCatalog.Contains(finishId))
            {
                return FloorFinishChangeResult.Failed(
                    cell,
                    FloorFinishChangeFailure.UnknownFinish);
            }

            bool changed =
                finishId == finishCatalog.DefaultFinishId
                    ? finishState.ResetOverride(cell)
                    : finishState.SetOverride(
                        cell,
                        finishId);

            if (changed)
            {
                EffectiveFinishChanged?.Invoke(
                    cell,
                    finishId);
            }

            return FloorFinishChangeResult.Success(
                cell,
                finishId,
                changed);
        }

        public FloorFinishChangeResult TryResetFinish(
            GridPosition cell)
        {
            return TrySetFinish(
                cell,
                finishCatalog.DefaultFinishId);
        }


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            floorState.FloorRemoved -=
                HandleFloorRemoved;

            isDisposed = true;
        }


        private void HandleFloorRemoved(
            GridPosition cell)
        {
            finishState.ResetOverride(cell);
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(FloorFinishService));
            }
        }
    }
}
