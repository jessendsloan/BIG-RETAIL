using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    internal enum NpcPopulationAuditGenderFilter
    {
        PopulationMix = 0,
        MenOnly = 1,
        WomenOnly = 2
    }


    internal sealed class NpcPopulationAuditSample
    {
        public NpcPopulationAuditSample(
            int seed,
            NpcAppearanceSelection selection,
            string failureReason)
        {
            Seed = seed;
            Selection = selection;
            FailureReason = failureReason ?? string.Empty;
        }


        public int Seed { get; }

        public NpcAppearanceSelection Selection { get; }

        public string FailureReason { get; }

        public bool IsValid => Selection != null
                               && string.IsNullOrWhiteSpace(FailureReason);
    }


    internal sealed class NpcPopulationAuditFrequency
    {
        public NpcPopulationAuditFrequency(
            string displayName,
            int count)
        {
            DisplayName = displayName;
            Count = count;
        }


        public string DisplayName { get; }

        public int Count { get; }
    }


    internal sealed class NpcPopulationAuditCategory
    {
        public NpcPopulationAuditCategory(
            string label,
            int allowedCount,
            List<NpcPopulationAuditFrequency> frequencies)
        {
            Label = label;
            AllowedCount = allowedCount;
            Frequencies = frequencies
                          ?? new List<NpcPopulationAuditFrequency>();
        }


        public string Label { get; }

        public int AllowedCount { get; }

        public IReadOnlyList<NpcPopulationAuditFrequency> Frequencies
        {
            get;
        }

        public int ObservedCount => Frequencies.Count;
    }


    /// <summary>
    /// Read-only analysis of a deterministic sample generated from one
    /// population definition. This exists only in the Editor assembly and
    /// never creates or modifies project assets.
    /// </summary>
    internal sealed class NpcPopulationAuditReport
    {
        private NpcPopulationAuditReport()
        {
        }


        public int RequestedCount { get; private set; }

        public int ValidCount { get; private set; }

        public int InvalidCount => RequestedCount - ValidCount;

        public int MenCount { get; private set; }

        public int WomenCount { get; private set; }

        public int DuplicateRecipeCount { get; private set; }

        public IReadOnlyList<NpcPopulationAuditCategory> Categories =>
            categories;

        public IReadOnlyList<string> Warnings => warnings;

        private readonly List<NpcPopulationAuditCategory> categories =
            new List<NpcPopulationAuditCategory>();

        private readonly List<string> warnings = new List<string>();


        public static NpcPopulationAuditReport Create(
            NpcPopulationDefinition definition,
            NpcPopulationAuditGenderFilter genderFilter,
            IReadOnlyList<NpcPopulationAuditSample> samples)
        {
            NpcPopulationAuditReport report =
                new NpcPopulationAuditReport
                {
                    RequestedCount = samples?.Count ?? 0
                };

            if (definition == null)
            {
                report.warnings.Add(
                    "Choose a Population Definition to run an audit.");
                return report;
            }

            if (!definition.TryValidate(out string definitionFailure))
            {
                report.warnings.Add(definitionFailure);
            }

            Dictionary<NpcBodySilhouette, int> bodies =
                new Dictionary<NpcBodySilhouette, int>();
            Dictionary<NpcSkinPalette, int> skins =
                new Dictionary<NpcSkinPalette, int>();
            Dictionary<NpcOutfitSet, int> outfits =
                new Dictionary<NpcOutfitSet, int>();
            Dictionary<NpcHairSet, int> hair =
                new Dictionary<NpcHairSet, int>();
            Dictionary<string, int> recipes =
                new Dictionary<string, int>();
            HashSet<string> generationFailures = new HashSet<string>();

            if (samples != null)
            {
                for (int index = 0; index < samples.Count; index++)
                {
                    NpcPopulationAuditSample sample = samples[index];

                    if (sample == null || !sample.IsValid)
                    {
                        if (sample != null
                            && !string.IsNullOrWhiteSpace(
                                sample.FailureReason))
                        {
                            generationFailures.Add(sample.FailureReason);
                        }

                        continue;
                    }

                    NpcAppearanceSelection selection = sample.Selection;
                    report.ValidCount++;

                    if (selection.Gender == NpcPersonGender.Woman)
                    {
                        report.WomenCount++;
                    }
                    else
                    {
                        report.MenCount++;
                    }

                    Increment(bodies, selection.BodySilhouette);
                    Increment(skins, selection.SkinPalette);
                    Increment(outfits, selection.OutfitSet);
                    Increment(hair, selection.HairSet);

                    string recipeKey = CreateRecipeKey(selection);
                    Increment(recipes, recipeKey);
                }
            }

            foreach (KeyValuePair<string, int> recipe in recipes)
            {
                if (recipe.Value > 1)
                {
                    report.DuplicateRecipeCount += recipe.Value - 1;
                }
            }

            foreach (string failure in generationFailures)
            {
                report.warnings.Add(failure);
            }

            HashSet<NpcBodySilhouette> allowedBodies =
                CollectAllowed(
                    definition,
                    genderFilter,
                    pool => pool.Bodies,
                    choice => choice?.Asset);
            HashSet<NpcSkinPalette> allowedSkins =
                CollectAllowed(
                    definition,
                    genderFilter,
                    pool => pool.Skins,
                    choice => choice?.Asset);
            HashSet<NpcOutfitSet> allowedOutfits =
                CollectAllowed(
                    definition,
                    genderFilter,
                    pool => pool.Outfits,
                    choice => choice?.Asset);
            HashSet<NpcHairSet> allowedHair =
                CollectAllowed(
                    definition,
                    genderFilter,
                    pool => pool.Hair,
                    choice => choice?.Asset);

            report.categories.Add(
                CreateCategory(
                    "Bodies",
                    allowedBodies.Count,
                    bodies,
                    asset => asset.DisplayName));
            report.categories.Add(
                CreateCategory(
                    "Skin Palettes",
                    allowedSkins.Count,
                    skins,
                    asset => asset.DisplayName));
            report.categories.Add(
                CreateCategory(
                    "Outfits",
                    allowedOutfits.Count,
                    outfits,
                    asset => asset.DisplayName));
            report.categories.Add(
                CreateCategory(
                    "Hair Sets",
                    allowedHair.Count,
                    hair,
                    asset => asset.DisplayName));

            AddCoverageWarnings(report);

            if (report.ValidCount >= 4
                && report.DuplicateRecipeCount
                > report.ValidCount / 3)
            {
                report.warnings.Add(
                    $"{report.DuplicateRecipeCount} of " +
                    $"{report.ValidCount} generated people repeat an exact " +
                    "Body + Skin + Outfit + Hair recipe. Add options or " +
                    "adjust weights if the lineup feels repetitive.");
            }

            return report;
        }


        private static void AddCoverageWarnings(
            NpcPopulationAuditReport report)
        {
            for (int index = 0; index < report.categories.Count; index++)
            {
                NpcPopulationAuditCategory category =
                    report.categories[index];

                if (category.AllowedCount > 1
                    && category.ObservedCount <= 1
                    && report.ValidCount >= 4)
                {
                    report.warnings.Add(
                        $"Only {category.ObservedCount} of " +
                        $"{category.AllowedCount} allowed {category.Label} " +
                        "appeared in this sample.");
                }
                else if (category.AllowedCount > 0
                         && report.ValidCount
                         >= category.AllowedCount * 2
                         && category.ObservedCount
                         < category.AllowedCount)
                {
                    report.warnings.Add(
                        $"This sample showed {category.ObservedCount} of " +
                        $"{category.AllowedCount} allowed " +
                        $"{category.Label}. Try another seed block to " +
                        "distinguish weighting from a configuration issue.");
                }
            }
        }


        private static NpcPopulationAuditCategory CreateCategory<T>(
            string label,
            int allowedCount,
            Dictionary<T, int> counts,
            Func<T, string> getDisplayName)
            where T : UnityEngine.Object
        {
            List<NpcPopulationAuditFrequency> frequencies =
                new List<NpcPopulationAuditFrequency>();

            foreach (KeyValuePair<T, int> entry in counts)
            {
                string displayName = entry.Key != null
                    ? getDisplayName(entry.Key)
                    : "Missing Asset";

                if (string.IsNullOrWhiteSpace(displayName)
                    && entry.Key != null)
                {
                    displayName = entry.Key.name;
                }

                frequencies.Add(
                    new NpcPopulationAuditFrequency(
                        displayName,
                        entry.Value));
            }

            frequencies.Sort(
                (left, right) =>
                {
                    int countComparison = right.Count.CompareTo(left.Count);

                    return countComparison != 0
                        ? countComparison
                        : string.Compare(
                            left.DisplayName,
                            right.DisplayName,
                            StringComparison.OrdinalIgnoreCase);
                });

            return new NpcPopulationAuditCategory(
                label,
                allowedCount,
                frequencies);
        }


        private static HashSet<TAsset> CollectAllowed<TChoice, TAsset>(
            NpcPopulationDefinition definition,
            NpcPopulationAuditGenderFilter genderFilter,
            Func<NpcPopulationAppearancePool, IReadOnlyList<TChoice>>
                getChoices,
            Func<TChoice, TAsset> getAsset)
            where TAsset : UnityEngine.Object
        {
            HashSet<TAsset> result = new HashSet<TAsset>();

            if (definition == null)
            {
                return result;
            }

            if (genderFilter != NpcPopulationAuditGenderFilter.WomenOnly
                && definition.Allows(NpcPersonGender.Man))
            {
                AddAllowed(
                    result,
                    getChoices(definition.MenAppearance),
                    getAsset);
            }

            if (genderFilter != NpcPopulationAuditGenderFilter.MenOnly
                && definition.Allows(NpcPersonGender.Woman))
            {
                AddAllowed(
                    result,
                    getChoices(definition.WomenAppearance),
                    getAsset);
            }

            return result;
        }


        private static void AddAllowed<TChoice, TAsset>(
            HashSet<TAsset> result,
            IReadOnlyList<TChoice> choices,
            Func<TChoice, TAsset> getAsset)
            where TAsset : UnityEngine.Object
        {
            if (choices == null)
            {
                return;
            }

            for (int index = 0; index < choices.Count; index++)
            {
                TAsset asset = getAsset(choices[index]);

                if (asset != null)
                {
                    result.Add(asset);
                }
            }
        }


        private static string CreateRecipeKey(
            NpcAppearanceSelection selection)
        {
            return string.Join(
                ":",
                ((int)selection.Gender).ToString(),
                GetInstanceId(selection.BodySilhouette),
                GetInstanceId(selection.SkinPalette),
                GetInstanceId(selection.OutfitSet),
                GetInstanceId(selection.HairSet));
        }


        private static string GetInstanceId(
            UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "0";
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);

            return !string.IsNullOrWhiteSpace(assetPath)
                ? assetPath
                : $"{asset.GetType().FullName}:{asset.name}";
        }


        private static void Increment<T>(
            Dictionary<T, int> counts,
            T key)
            where T : class
        {
            if (key == null)
            {
                return;
            }

            counts.TryGetValue(key, out int current);
            counts[key] = current + 1;
        }
    }


    internal static class NpcPopulationAuditSampler
    {
        public static List<NpcPopulationAuditSample> Generate(
            NpcPopulationDefinition definition,
            int baseSeed,
            int count,
            NpcPopulationAuditGenderFilter genderFilter)
        {
            List<NpcPopulationAuditSample> result =
                new List<NpcPopulationAuditSample>();
            int safeCount = Mathf.Clamp(count, 1, 64);

            for (int index = 0; index < safeCount; index++)
            {
                int seed = unchecked(baseSeed + index);
                NpcAppearanceSelection current = null;
                NpcAppearanceLocks locks = new NpcAppearanceLocks();

                if (genderFilter
                    != NpcPopulationAuditGenderFilter.PopulationMix)
                {
                    NpcPersonGender gender =
                        genderFilter
                        == NpcPopulationAuditGenderFilter.WomenOnly
                            ? NpcPersonGender.Woman
                            : NpcPersonGender.Man;

                    current = new NpcAppearanceSelection(
                        gender,
                        null,
                        null,
                        null,
                        null);
                    locks.Configure(
                        true,
                        false,
                        false,
                        false,
                        false);
                }

                bool generated = NpcAppearanceGenerator.TryGenerate(
                    definition,
                    seed,
                    current,
                    locks,
                    out NpcAppearanceSelection selection,
                    out string failureReason);

                result.Add(
                    new NpcPopulationAuditSample(
                        seed,
                        generated ? selection : null,
                        generated ? string.Empty : failureReason));
            }

            return result;
        }
    }
}
