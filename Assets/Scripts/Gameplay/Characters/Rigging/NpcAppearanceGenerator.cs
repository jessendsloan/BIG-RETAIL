using System;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// Produces repeatable appearance selections without using Unity's
    /// global random state. Gender is selected first; body, outfit, and hair
    /// are then filtered to compatible assets. Each category has its own
    /// random stream, so locking one category does not reshuffle the others.
    /// </summary>
    public static class NpcAppearanceGenerator
    {
        private const uint GenderSalt = 0x9E3779B9u;
        private const uint BodySalt = 0xA511E9B3u;
        private const uint SkinSalt = 0x63D83595u;
        private const uint OutfitSalt = 0xC2B2AE35u;
        private const uint HairSalt = 0x27D4EB2Fu;


        public static bool TryGenerate(
            NpcPopulationDefinition definition,
            int seed,
            NpcAppearanceSelection current,
            NpcAppearanceLocks locks,
            out NpcAppearanceSelection selection,
            out string failureReason)
        {
            selection = null;

            if (definition == null)
            {
                failureReason = "No population definition is selected.";
                return false;
            }

            if (!definition.TryValidate(out failureReason))
            {
                return false;
            }

            current ??= new NpcAppearanceSelection();
            locks ??= new NpcAppearanceLocks();

            NpcPersonGender gender = locks.Gender
                ? current.Gender
                : PickGender(definition, seed);

            if (locks.Gender && !definition.Allows(gender))
            {
                failureReason =
                    $"{gender} is not enabled for this population.";
                return false;
            }

            NpcPopulationAppearancePool pool =
                definition.GetAppearancePool(gender);

            if (!TryUseLockedChoice(
                    locks.Body,
                    current.BodySilhouette,
                    candidate => definition.Allows(gender, candidate),
                    "body",
                    out NpcBodySilhouette lockedBody,
                    out failureReason)
                || !TryUseLockedChoice(
                    locks.Skin,
                    current.SkinPalette,
                    candidate => definition.Allows(gender, candidate),
                    "skin",
                    out NpcSkinPalette lockedSkin,
                    out failureReason)
                || !TryUseLockedChoice(
                    locks.Outfit,
                    current.OutfitSet,
                    candidate => definition.Allows(gender, candidate),
                    "outfit",
                    out NpcOutfitSet lockedOutfit,
                    out failureReason)
                || !TryUseLockedChoice(
                    locks.Hair,
                    current.HairSet,
                    candidate => definition.Allows(gender, candidate),
                    "hair",
                    out NpcHairSet lockedHair,
                    out failureReason))
            {
                return false;
            }

            if (locks.Body && !lockedBody.Supports(gender))
            {
                failureReason =
                    $"{lockedBody.DisplayName} is not compatible with " +
                    $"{gender}.";
                return false;
            }

            if (locks.Outfit && !lockedOutfit.Supports(gender))
            {
                failureReason =
                    $"{lockedOutfit.DisplayName} is not compatible with " +
                    $"{gender}.";
                return false;
            }

            if (locks.Hair && !lockedHair.Supports(gender))
            {
                failureReason =
                    $"{lockedHair.DisplayName} is not compatible with " +
                    $"{gender}.";
                return false;
            }

            NpcBodySilhouette body = locks.Body
                ? lockedBody
                : PickBody(pool, gender, seed);

            NpcSkinPalette skin = locks.Skin
                ? lockedSkin
                : PickSkin(pool, seed);

            NpcOutfitSet outfit = locks.Outfit
                ? lockedOutfit
                : PickOutfit(pool, gender, seed);

            NpcHairSet hair = locks.Hair
                ? lockedHair
                : PickHair(pool, gender, seed);

            selection = new NpcAppearanceSelection(
                gender,
                body,
                skin,
                outfit,
                hair);

            return selection.TryValidate(out failureReason);
        }


        private static NpcPersonGender PickGender(
            NpcPopulationDefinition definition,
            int seed)
        {
            int menWeight = Math.Max(0, definition.MenWeight);
            int womenWeight = Math.Max(0, definition.WomenWeight);
            int totalWeight = menWeight + womenWeight;

            DeterministicRandom random =
                new DeterministicRandom(
                    unchecked((uint)seed) ^ GenderSalt);

            return random.Next(totalWeight) < menWeight
                ? NpcPersonGender.Man
                : NpcPersonGender.Woman;
        }


        private static NpcBodySilhouette PickBody(
            NpcPopulationAppearancePool pool,
            NpcPersonGender gender,
            int seed)
        {
            int selected = PickWeightedIndex(
                pool.Bodies.Count,
                index => pool.Bodies[index].Weight,
                index => pool.Bodies[index].Asset
                    .Supports(gender),
                seed,
                BodySalt);

            return pool.Bodies[selected].Asset;
        }


        private static NpcSkinPalette PickSkin(
            NpcPopulationAppearancePool pool,
            int seed)
        {
            int selected = PickWeightedIndex(
                pool.Skins.Count,
                index => pool.Skins[index].Weight,
                index => true,
                seed,
                SkinSalt);

            return pool.Skins[selected].Asset;
        }


        private static NpcOutfitSet PickOutfit(
            NpcPopulationAppearancePool pool,
            NpcPersonGender gender,
            int seed)
        {
            int selected = PickWeightedIndex(
                pool.Outfits.Count,
                index => pool.Outfits[index].Weight,
                index => pool.Outfits[index].Asset
                    .Supports(gender),
                seed,
                OutfitSalt);

            return pool.Outfits[selected].Asset;
        }


        private static NpcHairSet PickHair(
            NpcPopulationAppearancePool pool,
            NpcPersonGender gender,
            int seed)
        {
            int selected = PickWeightedIndex(
                pool.Hair.Count,
                index => pool.Hair[index].Weight,
                index => pool.Hair[index].Asset
                    .Supports(gender),
                seed,
                HairSalt);

            return pool.Hair[selected].Asset;
        }


        private static int PickWeightedIndex(
            int count,
            Func<int, int> getWeight,
            Func<int, bool> isCompatible,
            int seed,
            uint salt)
        {
            int totalWeight = 0;

            for (int index = 0; index < count; index++)
            {
                if (isCompatible(index))
                {
                    totalWeight += Math.Max(1, getWeight(index));
                }
            }

            DeterministicRandom random =
                new DeterministicRandom(
                    unchecked((uint)seed) ^ salt);

            int roll = random.Next(totalWeight);

            for (int index = 0; index < count; index++)
            {
                if (!isCompatible(index))
                {
                    continue;
                }

                roll -= Math.Max(1, getWeight(index));

                if (roll < 0)
                {
                    return index;
                }
            }

            return FindLastCompatibleIndex(count, isCompatible);
        }


        private static int FindLastCompatibleIndex(
            int count,
            Func<int, bool> isCompatible)
        {
            for (int index = count - 1; index >= 0; index--)
            {
                if (isCompatible(index))
                {
                    return index;
                }
            }

            throw new InvalidOperationException(
                "A validated population has no compatible appearance " +
                "choice.");
        }


        private static bool TryUseLockedChoice<T>(
            bool isLocked,
            T current,
            Func<T, bool> isAllowed,
            string label,
            out T lockedChoice,
            out string failureReason)
            where T : UnityEngine.Object
        {
            lockedChoice = current;

            if (!isLocked)
            {
                failureReason = string.Empty;
                return true;
            }

            if (current == null)
            {
                failureReason =
                    $"The {label} lock is on, but no {label} is selected.";
                return false;
            }

            if (!isAllowed(current))
            {
                failureReason =
                    $"{current.name} is not allowed by this definition. " +
                    $"Unlock {label} or choose an allowed option.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        private struct DeterministicRandom
        {
            private uint state;


            public DeterministicRandom(
                uint seed)
            {
                state = seed == 0u ? 0x6D2B79F5u : seed;
            }


            public int Next(
                int maximum)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;

                return (int)(state % (uint)maximum);
            }
        }
    }
}
