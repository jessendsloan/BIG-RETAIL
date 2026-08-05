using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Protects construction that currently depends on Foundation cells.
    /// Validation receives the complete removal batch so two sides of the
    /// same supported wall cannot both be removed accidentally.
    /// </summary>
    public interface IFoundationRemovalValidator
    {
        FoundationRemovalValidation ValidateRemoval(
            IReadOnlyList<GridPosition> cells);
    }


    /// <summary>
    /// Explicit compatibility validator for isolated tests, migration, and
    /// scenario-loading paths that have no dependent construction.
    /// </summary>
    public sealed class UnrestrictedFoundationRemovalValidator :
        IFoundationRemovalValidator
    {
        public static UnrestrictedFoundationRemovalValidator Instance
        {
            get;
        } = new UnrestrictedFoundationRemovalValidator();


        private UnrestrictedFoundationRemovalValidator()
        {
        }


        public FoundationRemovalValidation ValidateRemoval(
            IReadOnlyList<GridPosition> cells)
        {
            return FoundationRemovalValidation.Allowed();
        }
    }
}
