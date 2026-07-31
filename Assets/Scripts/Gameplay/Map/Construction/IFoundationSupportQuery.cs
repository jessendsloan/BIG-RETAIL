using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Answers whether a logical map cell currently has structural support.
    ///
    /// Floors and walls depend on this small contract rather than depending
    /// directly on the Foundation subsystem.
    /// </summary>
    public interface IFoundationSupportQuery
    {
        bool HasFoundation(GridPosition cell);
    }


    /// <summary>
    /// Explicit compatibility query for isolated tests, migration, and
    /// scenario-loading paths that deliberately bypass Foundation support.
    /// Player construction must use the live Foundation subsystem instead.
    /// </summary>
    public sealed class UnrestrictedFoundationSupportQuery :
        IFoundationSupportQuery
    {
        public static UnrestrictedFoundationSupportQuery Instance
        {
            get;
        } = new UnrestrictedFoundationSupportQuery();


        private UnrestrictedFoundationSupportQuery()
        {
        }


        public bool HasFoundation(GridPosition cell)
        {
            return true;
        }
    }
}
