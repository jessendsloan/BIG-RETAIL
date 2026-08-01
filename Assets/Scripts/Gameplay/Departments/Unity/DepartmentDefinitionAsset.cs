using System;
using UnityEngine;

namespace BigRetail.Departments.Unity
{
    /// <summary>
    /// Unity authoring data for one player-designatable department type.
    /// The asset describes a department; it never stores a player's painted
    /// department area.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DepartmentDefinition",
        menuName = "Big Retail/Departments/Department Definition")]
    public sealed class DepartmentDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId;

        [SerializeField]
        private string displayName;

        [Min(1)]
        [SerializeField]
        private int minimumCellCount = 1;


        public string DisplayName =>
            displayName;


        public bool TryCreateDefinition(
            out DepartmentDefinition definition,
            out string error)
        {
            try
            {
                definition =
                    new DepartmentDefinition(
                        new DepartmentDefinitionId(definitionId),
                        minimumCellCount);

                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                definition = null;
                error =
                    $"{name}: {exception.Message}";

                return false;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            definitionId =
                string.IsNullOrWhiteSpace(definitionId)
                    ? string.Empty
                    : definitionId.Trim().ToUpperInvariant();

            displayName =
                displayName == null
                    ? string.Empty
                    : displayName.Trim();

            minimumCellCount =
                Mathf.Max(1, minimumCellCount);
        }
#endif
    }
}
