using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;

namespace BigRetail.StoreLayouts.Unity
{
    internal static class StoreLayoutRuntimeConversions
    {
        public static GridPosition ToGridPosition(
            StoreCellData cell)
        {
            return new GridPosition(
                cell.X,
                cell.Y,
                cell.Level);
        }


        public static StoreCellData ToStoreCell(
            GridPosition cell)
        {
            return new StoreCellData(
                cell.X,
                cell.Y,
                cell.Level);
        }


        public static CellEdge ToCellEdge(
            StoreEdgeData edge)
        {
            CellEdgeDirection direction =
                edge.Direction == StoreEdgeDirection.NorthEast
                    ? CellEdgeDirection.NorthEast
                    : CellEdgeDirection.NorthWest;

            return new CellEdge(
                ToGridPosition(edge.AnchorCell),
                direction);
        }


        public static StoreEdgeData ToStoreEdge(
            CellEdge edge)
        {
            StoreEdgeDirection direction =
                edge.CanonicalDirection
                    == CellEdgeDirection.NorthEast
                    ? StoreEdgeDirection.NorthEast
                    : StoreEdgeDirection.NorthWest;

            return new StoreEdgeData(
                ToStoreCell(edge.AnchorCell),
                direction);
        }


        public static FixtureOrientation ToFixtureOrientation(
            StoreOrientation orientation)
        {
            return (FixtureOrientation)(int)orientation;
        }


        public static StoreOrientation ToStoreOrientation(
            FixtureOrientation orientation)
        {
            return (StoreOrientation)(int)orientation;
        }
    }
}
