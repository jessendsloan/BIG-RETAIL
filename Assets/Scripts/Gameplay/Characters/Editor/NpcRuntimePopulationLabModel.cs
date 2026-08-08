using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;

namespace BigRetail.Characters.Editor
{
    internal sealed class NpcRuntimePopulationPlanEntry
    {
        public NpcRuntimePopulationPlanEntry(
            int index,
            int appearanceSeed,
            string persistentId)
        {
            Index = index;
            AppearanceSeed = appearanceSeed;
            PersistentId = persistentId ?? string.Empty;
        }


        public int Index { get; }

        public int AppearanceSeed { get; }

        public string PersistentId { get; }
    }


    internal sealed class NpcRuntimePopulationSnapshot
    {
        public NpcRuntimePopulationSnapshot(
            NpcRuntimePopulationPlanEntry planEntry,
            NpcAppearanceSelection appearance,
            string failureReason)
        {
            PlanEntry = planEntry;
            Appearance = appearance?.Copy();
            FailureReason = failureReason ?? string.Empty;
            RecipeKey = CreateRecipeKey(Appearance);
        }


        public NpcRuntimePopulationPlanEntry PlanEntry { get; }

        public NpcAppearanceSelection Appearance { get; }

        public string FailureReason { get; }

        public string RecipeKey { get; }

        public bool IsValid => Appearance != null
                               && string.IsNullOrWhiteSpace(FailureReason);


        private static string CreateRecipeKey(
            NpcAppearanceSelection selection)
        {
            if (selection == null)
            {
                return string.Empty;
            }

            return string.Join(
                ":",
                ((int)selection.Gender).ToString(),
                GetAssetKey(selection.BodySilhouette),
                GetAssetKey(selection.SkinPalette),
                GetAssetKey(selection.OutfitSet),
                GetAssetKey(selection.HairSet));
        }


        private static string GetAssetKey(
            UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "0";
            }

            string path = AssetDatabase.GetAssetPath(asset);

            return !string.IsNullOrWhiteSpace(path)
                ? path
                : $"{asset.GetType().FullName}:{asset.name}";
        }
    }


    internal sealed class NpcRuntimePopulationComparison
    {
        public int ComparedCount { get; set; }

        public int StableCount { get; set; }

        public int ChangedCount => ComparedCount - StableCount;

        public bool IsComplete => ComparedCount > 0
                                  && ComparedCount == StableCount;
    }


    /// <summary>
    /// Pure planning and comparison logic for the Runtime Population Lab.
    /// The Editor window uses these entries to initialize real Person prefab
    /// instances through NpcPersonIdentity.
    /// </summary>
    internal static class NpcRuntimePopulationLabModel
    {
        public const int MaximumPeople = 100;


        public static List<NpcRuntimePopulationPlanEntry> BuildPlan(
            NpcCharacterRole role,
            int baseSeed,
            int requestedCount)
        {
            int count = Math.Max(
                1,
                Math.Min(MaximumPeople, requestedCount));
            List<NpcRuntimePopulationPlanEntry> result =
                new List<NpcRuntimePopulationPlanEntry>(count);

            for (int index = 0; index < count; index++)
            {
                string persistentId = role == NpcCharacterRole.Employee
                    ? $"runtime-lab-employee-{index + 1:D3}"
                    : string.Empty;

                result.Add(
                    new NpcRuntimePopulationPlanEntry(
                        index,
                        unchecked(baseSeed + index),
                        persistentId));
            }

            return result;
        }


        public static int AdvanceCustomerSeedBlock(
            int baseSeed,
            int count)
        {
            int safeCount = Math.Max(
                1,
                Math.Min(MaximumPeople, count));

            return unchecked(baseSeed + safeCount);
        }


        public static NpcRuntimePopulationComparison Compare(
            IReadOnlyList<NpcRuntimePopulationSnapshot> previous,
            IReadOnlyList<NpcRuntimePopulationSnapshot> current)
        {
            NpcRuntimePopulationComparison result =
                new NpcRuntimePopulationComparison();

            if (previous == null || current == null)
            {
                return result;
            }

            int count = Math.Min(previous.Count, current.Count);

            for (int index = 0; index < count; index++)
            {
                NpcRuntimePopulationSnapshot before = previous[index];
                NpcRuntimePopulationSnapshot after = current[index];

                if (before == null || after == null)
                {
                    continue;
                }

                result.ComparedCount++;

                if (before.PlanEntry.AppearanceSeed
                    == after.PlanEntry.AppearanceSeed
                    && string.Equals(
                        before.PlanEntry.PersistentId,
                        after.PlanEntry.PersistentId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        before.RecipeKey,
                        after.RecipeKey,
                        StringComparison.Ordinal))
                {
                    result.StableCount++;
                }
            }

            return result;
        }
    }
}
