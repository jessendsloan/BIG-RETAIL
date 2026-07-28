using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Exact structural and finish changes committed by one wall-tool stroke.
    /// </summary>
    public sealed class WallAppearanceStrokeEdit
    {
        private readonly CellEdge[] createdWalls;
        private readonly WallFaceFinishEdit[] finishEdits;


        public IReadOnlyList<CellEdge> CreatedWalls =>
            createdWalls;

        public IReadOnlyList<WallFaceFinishEdit> FinishEdits =>
            finishEdits;

        public int CreatedWallCount =>
            createdWalls.Length;

        public int FinishChangeCount =>
            finishEdits.Length;

        public int ChangeCount =>
            CreatedWallCount + FinishChangeCount;

        public bool IsEmpty =>
            ChangeCount == 0;


        public WallAppearanceStrokeEdit(
            IReadOnlyList<CellEdge> createdWalls,
            IReadOnlyList<WallFaceFinishEdit> finishEdits)
        {
            if (createdWalls == null)
            {
                throw new ArgumentNullException(
                    nameof(createdWalls));
            }

            if (finishEdits == null)
            {
                throw new ArgumentNullException(
                    nameof(finishEdits));
            }

            this.createdWalls =
                CopyUniqueWalls(createdWalls);

            this.finishEdits =
                CopyUniqueFinishEdits(finishEdits);
        }


        private static CellEdge[] CopyUniqueWalls(
            IReadOnlyList<CellEdge> walls)
        {
            CellEdge[] copy =
                new CellEdge[walls.Count];

            HashSet<CellEdge> uniqueWalls =
                new HashSet<CellEdge>();

            for (int index = 0;
                 index < walls.Count;
                 index++)
            {
                CellEdge wall =
                    walls[index];

                if (!uniqueWalls.Add(wall))
                {
                    throw new ArgumentException(
                        $"Wall appearance edit contains duplicate wall '{wall}'.",
                        nameof(walls));
                }

                copy[index] = wall;
            }

            return copy;
        }


        private static WallFaceFinishEdit[] CopyUniqueFinishEdits(
            IReadOnlyList<WallFaceFinishEdit> edits)
        {
            WallFaceFinishEdit[] copy =
                new WallFaceFinishEdit[edits.Count];

            HashSet<WallFaceKey> uniqueFaces =
                new HashSet<WallFaceKey>();

            for (int index = 0;
                 index < edits.Count;
                 index++)
            {
                WallFaceFinishEdit edit =
                    edits[index];

                if (!uniqueFaces.Add(edit.Face))
                {
                    throw new ArgumentException(
                        $"Wall appearance edit contains duplicate face '{edit.Face}'.",
                        nameof(edits));
                }

                copy[index] = edit;
            }

            return copy;
        }
    }
}
