using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// The locked 96x96 Big Retail property divided into nine 32x32
    /// Land Regions.
    /// </summary>
    public sealed class LandRegionCatalog
    {
        public const int RegionColumns = 3;

        public const int RegionRows = 3;

        public const int RegionCount = RegionColumns * RegionRows;

        public const int PropertySideLength =
            RegionColumns * LandRegionDefinition.SideLength;

        public const int PropertyCellCount =
            PropertySideLength * PropertySideLength;

        public static readonly LandRegionId FrontCornerRegionId =
            new LandRegionId(0, 0);

        private readonly Dictionary<LandRegionId, LandRegionDefinition>
            definitions;

        public GridPosition PropertyMinimumCell { get; }

        private LandRegionCatalog(
            GridPosition propertyMinimumCell,
            Dictionary<LandRegionId, LandRegionDefinition> definitions)
        {
            PropertyMinimumCell = propertyMinimumCell;
            this.definitions = definitions;
        }

        /// <summary>
        /// Derives the nine locked region boundaries from the authored
        /// physical construction mask and validates that it is exactly the
        /// expected 96x96 property.
        /// </summary>
        public static LandRegionCatalog CreateFor(
            ConstructionAreaDefinition constructionArea)
        {
            if (constructionArea == null)
            {
                throw new ArgumentNullException(nameof(constructionArea));
            }

            if (constructionArea.EligibleCellCount != PropertyCellCount)
            {
                throw new ArgumentException(
                    $"The Land Region layout requires exactly "
                    + $"{PropertyCellCount} physically eligible cells, but "
                    + $"the authored construction area contains "
                    + $"{constructionArea.EligibleCellCount}.",
                    nameof(constructionArea));
            }

            bool hasCell = false;
            int minimumX = int.MaxValue;
            int minimumY = int.MaxValue;
            int maximumX = int.MinValue;
            int maximumY = int.MinValue;
            int level = 0;

            foreach (GridPosition cell in
                     constructionArea.EnumerateEligibleCells())
            {
                if (!hasCell)
                {
                    level = cell.Level;
                    hasCell = true;
                }
                else if (cell.Level != level)
                {
                    throw new ArgumentException(
                        "The Land Region layout must occupy one logical level.",
                        nameof(constructionArea));
                }

                minimumX = Math.Min(minimumX, cell.X);
                minimumY = Math.Min(minimumY, cell.Y);
                maximumX = Math.Max(maximumX, cell.X);
                maximumY = Math.Max(maximumY, cell.Y);
            }

            if (!hasCell
                || maximumX - minimumX + 1 != PropertySideLength
                || maximumY - minimumY + 1 != PropertySideLength)
            {
                throw new ArgumentException(
                    $"The authored construction area must be a complete "
                    + $"{PropertySideLength}x{PropertySideLength} square.",
                    nameof(constructionArea));
            }

            GridPosition propertyMinimum =
                new GridPosition(minimumX, minimumY, level);

            Dictionary<LandRegionId, LandRegionDefinition> definitions =
                new Dictionary<LandRegionId, LandRegionDefinition>(RegionCount);

            for (int row = 0; row < RegionRows; row++)
            {
                for (int column = 0; column < RegionColumns; column++)
                {
                    LandRegionId id = new LandRegionId(column, row);
                    GridPosition regionMinimum = propertyMinimum.Offset(
                        column * LandRegionDefinition.SideLength,
                        row * LandRegionDefinition.SideLength);
                    LandRegionDefinition definition =
                        new LandRegionDefinition(id, regionMinimum);

                    foreach (GridPosition cell in definition.EnumerateCells())
                    {
                        if (!constructionArea.IsEligible(cell))
                        {
                            throw new ArgumentException(
                                $"The authored construction area is missing "
                                + $"{cell} from {id}.",
                                nameof(constructionArea));
                        }
                    }

                    definitions.Add(id, definition);
                }
            }

            return new LandRegionCatalog(propertyMinimum, definitions);
        }

        public bool Contains(LandRegionId id)
        {
            return definitions.ContainsKey(id);
        }

        public LandRegionDefinition GetDefinition(LandRegionId id)
        {
            if (definitions.TryGetValue(id, out LandRegionDefinition definition))
            {
                return definition;
            }

            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "The Land Region does not belong to this property.");
        }

        public bool TryGetRegion(
            GridPosition cell,
            out LandRegionDefinition definition)
        {
            int relativeX = cell.X - PropertyMinimumCell.X;
            int relativeY = cell.Y - PropertyMinimumCell.Y;

            if (cell.Level != PropertyMinimumCell.Level
                || relativeX < 0
                || relativeY < 0
                || relativeX >= PropertySideLength
                || relativeY >= PropertySideLength)
            {
                definition = null;
                return false;
            }

            LandRegionId id = new LandRegionId(
                relativeX / LandRegionDefinition.SideLength,
                relativeY / LandRegionDefinition.SideLength);

            definition = definitions[id];
            return true;
        }

        public IEnumerable<LandRegionDefinition> EnumerateDefinitions()
        {
            for (int row = 0; row < RegionRows; row++)
            {
                for (int column = 0; column < RegionColumns; column++)
                {
                    yield return definitions[new LandRegionId(column, row)];
                }
            }
        }
    }
}
