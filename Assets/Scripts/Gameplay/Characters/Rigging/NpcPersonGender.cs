using System;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// First-class simulation identity used by population generation,
    /// persistence, names, and future person-facing gameplay.
    /// </summary>
    public enum NpcPersonGender
    {
        Man = 0,
        Woman = 1
    }


    [Flags]
    public enum NpcGenderCompatibility
    {
        None = 0,
        Men = 1 << 0,
        Women = 1 << 1,
        Everyone = Men | Women
    }


    public static class NpcGenderCompatibilityExtensions
    {
        public static bool Supports(
            this NpcGenderCompatibility compatibility,
            NpcPersonGender gender)
        {
            NpcGenderCompatibility required =
                gender == NpcPersonGender.Woman
                    ? NpcGenderCompatibility.Women
                    : NpcGenderCompatibility.Men;

            return (compatibility & required) != 0;
        }
    }
}
