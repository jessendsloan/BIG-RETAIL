using System;
using System.Collections.Generic;

namespace BigRetail.Departments
{
    /// <summary>
    /// Immutable lookup for the department definitions available in one map
    /// or campaign configuration.
    /// </summary>
    public sealed class DepartmentDefinitionCatalog
    {
        private readonly Dictionary<DepartmentDefinitionId,
            DepartmentDefinition> definitions;


        public DepartmentDefinitionCatalog(
            IEnumerable<DepartmentDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions));
            }

            this.definitions =
                new Dictionary<DepartmentDefinitionId,
                    DepartmentDefinition>();

            foreach (DepartmentDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A department catalog cannot contain null definitions.",
                        nameof(definitions));
                }

                if (!this.definitions.TryAdd(
                        definition.Id,
                        definition))
                {
                    throw new ArgumentException(
                        "A department catalog cannot contain duplicate IDs.",
                        nameof(definitions));
                }
            }

            if (this.definitions.Count == 0)
            {
                throw new ArgumentException(
                    "A department catalog requires at least one definition.",
                    nameof(definitions));
            }
        }


        public int Count =>
            definitions.Count;


        public bool TryGetDefinition(
            DepartmentDefinitionId id,
            out DepartmentDefinition definition)
        {
            return definitions.TryGetValue(
                id,
                out definition);
        }
    }
}
