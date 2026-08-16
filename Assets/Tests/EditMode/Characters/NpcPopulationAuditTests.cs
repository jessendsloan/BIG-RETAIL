using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcPopulationAuditTests
    {
        [Test]
        public void AuditSampler_RepeatsSeedBlockAndReportsCoverage()
        {
            NpcPopulationDefinition definition = FindValidDefinition();
            Type samplerType = FindEditorType(
                "NpcPopulationAuditSampler");
            Type filterType = FindEditorType(
                "NpcPopulationAuditGenderFilter");
            Type reportType = FindEditorType(
                "NpcPopulationAuditReport");
            object populationMix = Enum.ToObject(filterType, 0);
            MethodInfo generate = samplerType.GetMethod(
                "Generate",
                BindingFlags.Static | BindingFlags.Public);
            MethodInfo createReport = reportType.GetMethod(
                "Create",
                BindingFlags.Static | BindingFlags.Public);

            Assert.That(generate, Is.Not.Null);
            Assert.That(createReport, Is.Not.Null);

            IList first = (IList)generate.Invoke(
                null,
                new[]
                {
                    definition,
                    (object)1200,
                    12,
                    populationMix
                });
            IList second = (IList)generate.Invoke(
                null,
                new[]
                {
                    definition,
                    (object)1200,
                    12,
                    populationMix
                });

            Assert.That(first, Has.Count.EqualTo(12));
            Assert.That(second, Has.Count.EqualTo(12));

            for (int index = 0; index < first.Count; index++)
            {
                object firstSample = first[index];
                object secondSample = second[index];
                Type sampleType = firstSample.GetType();
                bool isValid = (bool)sampleType
                    .GetProperty("IsValid")
                    .GetValue(firstSample);
                int firstSeed = (int)sampleType
                    .GetProperty("Seed")
                    .GetValue(firstSample);
                int secondSeed = (int)sampleType
                    .GetProperty("Seed")
                    .GetValue(secondSample);
                NpcAppearanceSelection firstSelection =
                    (NpcAppearanceSelection)sampleType
                        .GetProperty("Selection")
                        .GetValue(firstSample);
                NpcAppearanceSelection secondSelection =
                    (NpcAppearanceSelection)sampleType
                        .GetProperty("Selection")
                        .GetValue(secondSample);

                Assert.That(isValid, Is.True);
                Assert.That(firstSeed, Is.EqualTo(1200 + index));
                Assert.That(secondSeed, Is.EqualTo(firstSeed));
                Assert.That(
                    secondSelection.Gender,
                    Is.EqualTo(firstSelection.Gender));
                Assert.That(
                    secondSelection.BodySilhouette,
                    Is.SameAs(firstSelection.BodySilhouette));
                Assert.That(
                    secondSelection.SkinPalette,
                    Is.SameAs(firstSelection.SkinPalette));
                Assert.That(
                    secondSelection.OutfitSet,
                    Is.SameAs(firstSelection.OutfitSet));
                Assert.That(
                    secondSelection.HairSet,
                    Is.SameAs(firstSelection.HairSet));
            }

            object report = createReport.Invoke(
                null,
                new object[]
                {
                    definition,
                    populationMix,
                    first
                });
            int validCount = (int)reportType
                .GetProperty("ValidCount")
                .GetValue(report);
            ICollection categories = (ICollection)reportType
                .GetProperty("Categories")
                .GetValue(report);

            Assert.That(validCount, Is.EqualTo(12));
            Assert.That(categories, Has.Count.EqualTo(4));
        }


        private static Type FindEditorType(string shortName)
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


        private static NpcPopulationDefinition FindValidDefinition()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:NpcPopulationDefinition");

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                NpcPopulationDefinition definition =
                    AssetDatabase.LoadAssetAtPath<NpcPopulationDefinition>(
                        path);

                if (definition != null
                    && definition.TryValidate(out _))
                {
                    return definition;
                }
            }

            Assert.Fail(
                "No valid Population Definition exists for the audit test.");
            return null;
        }
    }
}
