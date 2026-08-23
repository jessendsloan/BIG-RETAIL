using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Produces a stable versioned fingerprint from canonical map and
    /// construction-mask coordinates. The value changes when authored
    /// geometry changes, regardless of enumeration order.
    /// </summary>
    public static class MapGeometryFingerprint
    {
        private const string FormatVersion =
            "bigretail.map-geometry.v1";


        public static string Compute(
            GridMapDefinition mapDefinition,
            ConstructionAreaDefinition constructionArea)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(mapDefinition));
            }

            if (constructionArea == null)
            {
                throw new ArgumentNullException(
                    nameof(constructionArea));
            }

            List<GridPosition> mapCells =
                new List<GridPosition>(
                    mapDefinition.EnumerateValidCells());

            List<GridPosition> constructionCells =
                new List<GridPosition>(
                    constructionArea.EnumerateEligibleCells());

            mapCells.Sort(CompareCells);
            constructionCells.Sort(CompareCells);

            StringBuilder source =
                new StringBuilder(
                    (mapCells.Count + constructionCells.Count) * 16);

            source.Append(FormatVersion);
            source.Append('\n');
            AppendCells(source, 'M', mapCells);
            AppendCells(source, 'C', constructionCells);

            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash =
                    algorithm.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            source.ToString()));

                StringBuilder value =
                    new StringBuilder(hash.Length * 2);

                for (int index = 0;
                     index < hash.Length;
                     index++)
                {
                    value.Append(
                        hash[index].ToString(
                            "x2",
                            CultureInfo.InvariantCulture));
                }

                return $"v1:{value}";
            }
        }


        private static void AppendCells(
            StringBuilder source,
            char kind,
            IReadOnlyList<GridPosition> cells)
        {
            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell = cells[index];

                source.Append(kind);
                source.Append(':');
                source.Append(cell.Level);
                source.Append(':');
                source.Append(cell.Y);
                source.Append(':');
                source.Append(cell.X);
                source.Append('\n');
            }
        }


        private static int CompareCells(
            GridPosition left,
            GridPosition right)
        {
            int level = left.Level.CompareTo(right.Level);

            if (level != 0)
            {
                return level;
            }

            int y = left.Y.CompareTo(right.Y);

            return y != 0
                ? y
                : left.X.CompareTo(right.X);
        }
    }
}
