using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Departments.Unity
{
    /// <summary>
    /// Unity-authored collection of department types available to the active
    /// planning system.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DepartmentDefinitionCatalog",
        menuName = "Big Retail/Departments/Department Definition Catalog")]
    public sealed class DepartmentDefinitionCatalogAsset :
        ScriptableObject
    {
        [SerializeField]
        private DepartmentDefinitionAsset[] definitions =
            Array.Empty<DepartmentDefinitionAsset>();


        public bool TryCreateCatalog(
            out DepartmentDefinitionCatalog catalog,
            out string error)
        {
            if (definitions == null)
            {
                catalog = null;
                error =
                    $"{name}: Department definition collection is missing.";

                return false;
            }

            List<DepartmentDefinition> domainDefinitions =
                new List<DepartmentDefinition>(definitions.Length);

            for (int index = 0;
                 index < definitions.Length;
                 index++)
            {
                DepartmentDefinitionAsset definitionAsset =
                    definitions[index];

                if (definitionAsset == null)
                {
                    catalog = null;
                    error =
                        $"{name}: Department definition entry {index} "
                        + "is missing.";

                    return false;
                }

                if (!definitionAsset.TryCreateDefinition(
                        out DepartmentDefinition definition,
                        out error))
                {
                    catalog = null;
                    return false;
                }

                domainDefinitions.Add(definition);
            }

            try
            {
                catalog =
                    new DepartmentDefinitionCatalog(
                        domainDefinitions);

                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                catalog = null;
                error =
                    $"{name}: {exception.Message}";

                return false;
            }
        }
    }
}
