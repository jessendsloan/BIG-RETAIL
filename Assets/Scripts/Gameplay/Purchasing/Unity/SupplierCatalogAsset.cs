using System;
using System.Collections.Generic;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    [CreateAssetMenu(
        fileName = "SupplierCatalog",
        menuName = "Big Retail/Purchasing/Supplier Catalog")]
    public sealed class SupplierCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private SupplierDefinitionAsset[] suppliers =
            Array.Empty<SupplierDefinitionAsset>();


        public IReadOnlyList<SupplierDefinitionAsset> Suppliers =>
            suppliers;


        public bool TryCreateCatalog(
            out SupplierCatalog catalog,
            out string error)
        {
            if (suppliers == null)
            {
                catalog = null;
                error = $"{name}: Supplier asset collection is missing.";
                return false;
            }

            List<SupplierDefinition> definitions =
                new List<SupplierDefinition>(suppliers.Length);

            for (int index = 0; index < suppliers.Length; index++)
            {
                SupplierDefinitionAsset supplier = suppliers[index];

                if (supplier == null)
                {
                    catalog = null;
                    error = $"{name}: Supplier entry {index} is missing.";
                    return false;
                }

                if (!supplier.TryCreateDefinition(
                        out SupplierDefinition definition,
                        out error))
                {
                    catalog = null;
                    return false;
                }

                definitions.Add(definition);
            }

            try
            {
                catalog = new SupplierCatalog(definitions);
                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                catalog = null;
                error = $"{name}: {exception.Message}";
                return false;
            }
        }
    }
}
