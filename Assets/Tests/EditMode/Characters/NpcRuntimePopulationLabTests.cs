using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcRuntimePopulationLabTests
    {
        [Test]
        public void BuildPlan_EmployeesKeepStableIdsAndSeeds()
        {
            Type modelType = FindEditorType(
                "NpcRuntimePopulationLabModel");
            MethodInfo buildPlan = modelType.GetMethod(
                "BuildPlan",
                BindingFlags.Static | BindingFlags.Public);

            Assert.That(buildPlan, Is.Not.Null);

            IList first = (IList)buildPlan.Invoke(
                null,
                new object[]
                {
                    NpcCharacterRole.Employee,
                    7000,
                    12
                });
            IList second = (IList)buildPlan.Invoke(
                null,
                new object[]
                {
                    NpcCharacterRole.Employee,
                    7000,
                    12
                });

            Assert.That(first, Has.Count.EqualTo(12));
            Assert.That(second, Has.Count.EqualTo(12));

            for (int index = 0; index < first.Count; index++)
            {
                Type entryType = first[index].GetType();
                int firstSeed = (int)entryType
                    .GetProperty("AppearanceSeed")
                    .GetValue(first[index]);
                int secondSeed = (int)entryType
                    .GetProperty("AppearanceSeed")
                    .GetValue(second[index]);
                string firstId = (string)entryType
                    .GetProperty("PersistentId")
                    .GetValue(first[index]);
                string secondId = (string)entryType
                    .GetProperty("PersistentId")
                    .GetValue(second[index]);

                Assert.That(firstSeed, Is.EqualTo(7000 + index));
                Assert.That(secondSeed, Is.EqualTo(firstSeed));
                Assert.That(firstId, Is.Not.Empty);
                Assert.That(secondId, Is.EqualTo(firstId));
            }
        }


        [Test]
        public void BuildPlan_CustomersAreTransientAndCapAtOneHundred()
        {
            Type modelType = FindEditorType(
                "NpcRuntimePopulationLabModel");
            MethodInfo buildPlan = modelType.GetMethod(
                "BuildPlan",
                BindingFlags.Static | BindingFlags.Public);

            IList plan = (IList)buildPlan.Invoke(
                null,
                new object[]
                {
                    NpcCharacterRole.Customer,
                    9000,
                    500
                });

            Assert.That(plan, Has.Count.EqualTo(100));

            for (int index = 0; index < plan.Count; index++)
            {
                Type entryType = plan[index].GetType();
                string persistentId = (string)entryType
                    .GetProperty("PersistentId")
                    .GetValue(plan[index]);

                Assert.That(persistentId, Is.Empty);
            }
        }


        [Test]
        public void PersonPrefab_ContainsRuntimePopulationComponents()
        {
            UnityEngine.GameObject prefab =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
                    "Assets/Prefabs/Characters/Core/Person.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<NpcPersonIdentity>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<NpcPathFollower>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<NpcCutoutRig>(true),
                Is.Not.Null);
        }


        private static Type FindEditorType(
            string shortName)
        {
            Type type = Type.GetType(
                $"BigRetail.Characters.Editor.{shortName}, " +
                "BigRetail.Characters.Editor");

            Assert.That(
                type,
                Is.Not.Null,
                $"Could not find Editor type {shortName}.");
            return type;
        }
    }
}
