using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Applies one player-facing wall-tool stroke.
    ///
    /// Missing legal walls are created, existing walls are preserved, invalid
    /// edges are skipped, and the requested finish is applied only to each
    /// supplied physical wall face. The opposite face remains unchanged.
    /// </summary>
    public sealed class WallAppearanceStrokeService
    {
        private readonly WallConstructionService wallConstruction;
        private readonly WallFinishService wallFinishes;
        private readonly WallFinishCatalog finishCatalog;


        public WallAppearanceStrokeService(
            WallConstructionService wallConstruction,
            WallFinishService wallFinishes,
            WallFinishCatalog finishCatalog)
        {
            this.wallConstruction =
                wallConstruction
                ?? throw new ArgumentNullException(
                    nameof(wallConstruction));

            this.wallFinishes =
                wallFinishes
                ?? throw new ArgumentNullException(
                    nameof(wallFinishes));

            this.finishCatalog =
                finishCatalog
                ?? throw new ArgumentNullException(
                    nameof(finishCatalog));
        }


        public WallAppearanceStrokeResult TryApply(
            IReadOnlyList<WallFaceKey> faces,
            WallFinishId finishId)
        {
            if (faces == null)
            {
                throw new ArgumentNullException(
                    nameof(faces));
            }

            if (faces.Count == 0)
            {
                return WallAppearanceStrokeResult.Rejected(
                    0,
                    WallAppearanceStrokeFailure.EmptyRequest);
            }

            if (!finishCatalog.Contains(finishId))
            {
                return WallAppearanceStrokeResult.Rejected(
                    faces.Count,
                    WallAppearanceStrokeFailure.UnknownFinish);
            }

            List<WallFaceKey> eligibleFaces =
                new List<WallFaceKey>(faces.Count);

            List<CellEdge> eligibleEdges =
                new List<CellEdge>(faces.Count);

            List<WallFinishId> previousFinishes =
                new List<WallFinishId>(faces.Count);

            HashSet<CellEdge> uniqueEdges =
                new HashSet<CellEdge>();

            int existingWallCount = 0;
            int skippedWallCount = 0;

            for (int index = 0;
                 index < faces.Count;
                 index++)
            {
                WallFaceKey face =
                    faces[index];

                if (!uniqueEdges.Add(face.Edge))
                {
                    return WallAppearanceStrokeResult.Rejected(
                        faces.Count,
                        WallAppearanceStrokeFailure.DuplicateEdge,
                        face.Edge);
                }

                if (wallConstruction.HasWall(face.Edge))
                {
                    eligibleFaces.Add(face);
                    eligibleEdges.Add(face.Edge);
                    previousFinishes.Add(
                        wallFinishes.GetEffectiveFinish(
                            face.Edge,
                            face.FacingCell));
                    existingWallCount++;
                    continue;
                }

                WallChangeResult placement =
                    wallConstruction.EvaluatePlacement(
                        face.Edge);

                if (!placement.Succeeded)
                {
                    skippedWallCount++;
                    continue;
                }

                eligibleFaces.Add(face);
                eligibleEdges.Add(face.Edge);
                previousFinishes.Add(
                    finishCatalog.DefaultFinishId);
            }

            if (eligibleEdges.Count == 0)
            {
                return WallAppearanceStrokeResult.Success(
                    faces.Count,
                    0,
                    0,
                    skippedWallCount,
                    0,
                    0,
                    new WallAppearanceStrokeEdit(
                        Array.Empty<CellEdge>(),
                        Array.Empty<WallFaceFinishEdit>()));
            }

            WallEnsureResult ensureResult =
                wallConstruction.TryEnsureWalls(
                    eligibleEdges);

            if (!ensureResult.Succeeded)
            {
                return WallAppearanceStrokeResult.Rejected(
                    faces.Count,
                    WallAppearanceStrokeFailure.WallEnsureRejected,
                    ensureResult.FailedEdge);
            }

            List<WallFaceFinishEdit> finishEdits =
                new List<WallFaceFinishEdit>();

            int unchangedFinishCount = 0;

            for (int index = 0;
                 index < eligibleFaces.Count;
                 index++)
            {
                WallFaceKey face =
                    eligibleFaces[index];

                WallFinishId previousFinish =
                    previousFinishes[index];

                WallFinishChangeResult finishResult =
                    wallFinishes.TrySetFinish(
                        face.Edge,
                        face.FacingCell,
                        finishId);

                if (!finishResult.Succeeded)
                {
                    bool rolledBack =
                        TryRollback(
                            ensureResult.Edit,
                            finishEdits);

                    return WallAppearanceStrokeResult.Rejected(
                        faces.Count,
                        rolledBack
                            ? WallAppearanceStrokeFailure
                                .FinishChangeRejected
                            : WallAppearanceStrokeFailure
                                .RollbackFailed,
                        face.Edge,
                        finishResult.Failure);
                }

                if (!finishResult.Changed)
                {
                    unchangedFinishCount++;
                    continue;
                }

                finishEdits.Add(
                    new WallFaceFinishEdit(
                        face,
                        previousFinish,
                        finishId));
            }

            WallAppearanceStrokeEdit edit =
                new WallAppearanceStrokeEdit(
                    ensureResult.Edit.Edges,
                    finishEdits);

            return WallAppearanceStrokeResult.Success(
                faces.Count,
                ensureResult.ChangedCount,
                existingWallCount,
                skippedWallCount,
                finishEdits.Count,
                unchangedFinishCount,
                edit);
        }


        private bool TryRollback(
            WallEdit createdWalls,
            IReadOnlyList<WallFaceFinishEdit> finishEdits)
        {
            bool succeeded = true;

            for (int index = finishEdits.Count - 1;
                 index >= 0;
                 index--)
            {
                WallFaceFinishEdit edit =
                    finishEdits[index];

                WallFinishChangeResult result =
                    wallFinishes.TrySetFinish(
                        edit.Face.Edge,
                        edit.Face.FacingCell,
                        edit.BeforeFinishId);

                succeeded &= result.Succeeded;
            }

            if (!createdWalls.IsEmpty)
            {
                WallBatchChangeResult removal =
                    wallConstruction.TryApplyEdit(
                        createdWalls.Inverse());

                succeeded &= removal.Succeeded;
            }

            return succeeded;
        }
    }
}
