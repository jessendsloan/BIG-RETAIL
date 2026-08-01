using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Departments.Unity.UI
{
    /// <summary>
    /// Owns the player's currently selected department type. It deliberately
    /// contains no paint or planning rules; a future planning tool consumes
    /// this selection when it creates a DepartmentPlan.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DepartmentDefinitionSelectionHost : MonoBehaviour
    {
        [SerializeField]
        private DepartmentDefinitionCatalogAsset definitionCatalog;


        public DepartmentDefinitionAsset SelectedDefinition
        {
            get;
            private set;
        }

        public event Action<DepartmentDefinitionAsset>
            SelectedDefinitionChanged;


        public IEnumerable<DepartmentDefinitionAsset>
            EnumerateAvailableDefinitions()
        {
            if (definitionCatalog == null
                || definitionCatalog.Definitions == null)
            {
                yield break;
            }

            for (int index = 0;
                 index < definitionCatalog.Definitions.Count;
                 index++)
            {
                DepartmentDefinitionAsset definition =
                    definitionCatalog.Definitions[index];

                if (definition != null)
                {
                    yield return definition;
                }
            }
        }


        public void SelectDefinition(
            DepartmentDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!IsAvailableDefinition(definition))
            {
                throw new ArgumentException(
                    $"Department definition '{definition.name}' is not "
                    + "registered in this selection host's catalog.",
                    nameof(definition));
            }

            if (SelectedDefinition == definition)
            {
                return;
            }

            SelectedDefinition = definition;
            SelectedDefinitionChanged?.Invoke(definition);
        }


        private bool IsAvailableDefinition(
            DepartmentDefinitionAsset candidate)
        {
            if (definitionCatalog == null
                || definitionCatalog.Definitions == null)
            {
                return false;
            }

            for (int index = 0;
                 index < definitionCatalog.Definitions.Count;
                 index++)
            {
                if (definitionCatalog.Definitions[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
