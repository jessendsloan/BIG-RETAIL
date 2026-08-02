using System;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// Produces repeatable appearance selections without using Unity's
    /// global random state. Each category has its own random stream, so
    /// locking one category does not reshuffle the other three.
    /// </summary>
    public static class NpcAppearanceGenerator
    {
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

            if (!TryUseLockedChoice(
                    locks.Body,
                    current.BodySilhouette,
                    definition.Allows,
                    "body",
                    out NpcBodySilhouette lockedBody,
                    out failureReason)
                || !TryUseLockedChoice(
                    locks.Skin,
                    current.SkinPalette,
                    definition.Allows,
                    "skin",
                    out NpcSkinPalette lockedSkin,
                    out failureReason)
                || !TryUseLockedChoice(
                    locks.Outfit,
                    current.OutfitSet,
                    definition.Allows,
                    "outfit",
                    out NpcOutfitSet lockedOutfit,
                    out failureReason)
                || !TryUseLockedChoice(
                    locks.Hair,
                    current.HairSet,
                    definition.Allows,
                    "hair",
                    out NpcHairSet lockedHair,
                    out failureReason))
            {
                return false;
            }

            NpcBodySilhouette body = locks.Body
                ? lockedBody
                : PickBody(definition, seed);

            NpcSkinPalette skin = locks.Skin
                ? lockedSkin
                : PickSkin(definition, seed);

            NpcOutfitSet outfit = locks.Outfit
                ? lockedOutfit
                : PickOutfit(definition, seed);

            NpcHairSet hair = locks.Hair
                ? lockedHair
                : PickHair(definition, seed);

            selection = new NpcAppearanceSelection(
                body,
                skin,
                outfit,
                hair);

            return selection.TryValidate(out failureReason);
        }


        private static NpcBodySilhouette PickBody(
            NpcPopulationDefinition definition,
            int seed)
        {
            int selected = PickWeightedIndex(
                definition.Bodies.Count,
                index => definition.Bodies[index].Weight,
                seed,
                BodySalt);

            return definition.Bodies[selected].Asset;
        }


        private static NpcSkinPalette PickSkin(
            NpcPopulationDefinition definition,
            int seed)
        {
            int selected = PickWeightedIndex(
                definition.Skins.Count,
                index => definition.Skins[index].Weight,
                seed,
                SkinSalt);

            return definition.Skins[selected].Asset;
        }


        private static NpcOutfitSet PickOutfit(
            NpcPopulationDefinition definition,
            int seed)
        {
            int selected = PickWeightedIndex(
                definition.Outfits.Count,
                index => definition.Outfits[index].Weight,
                seed,
                OutfitSalt);

            return definition.Outfits[selected].Asset;
        }


        private static NpcHairSet PickHair(
            NpcPopulationDefinition definition,
            int seed)
        {
            int selected = PickWeightedIndex(
                definition.Hair.Count,
                index => definition.Hair[index].Weight,
                seed,
                HairSalt);

            return definition.Hair[selected].Asset;
        }


        private static int PickWeightedIndex(
            int count,
            Func<int, int> getWeight,
            int seed,
            uint salt)
        {
            int totalWeight = 0;

            for (int index = 0; index < count; index++)
            {
                totalWeight += Math.Max(1, getWeight(index));
            }

            DeterministicRandom random =
                new DeterministicRandom(
                    unchecked((uint)seed) ^ salt);

            int roll = random.Next(totalWeight);

            for (int index = 0; index < count; index++)
            {
                roll -= Math.Max(1, getWeight(index));

                if (roll < 0)
                {
                    return index;
                }
            }

            return count - 1;
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
