using BigRetail.Departments.Unity;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Departments.Unity.Tests
{
    public sealed class DepartmentDefinitionAssetTests
    {
        [Test]
        public void DefinitionAsset_RejectsMissingIdentifier()
        {
            DepartmentDefinitionAsset asset =
                ScriptableObject.CreateInstance<DepartmentDefinitionAsset>();

            Assert.That(
                asset.TryCreateDefinition(
                    out _,
                    out string error),
                Is.False);
            Assert.That(error, Is.Not.Empty);

            Object.DestroyImmediate(asset);
        }
    }
}
