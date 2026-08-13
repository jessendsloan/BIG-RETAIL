using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Records one exact fixture-state mutation for construction history.
    /// </summary>
    public readonly struct FixtureEdit
    {
        public FixtureEditKind Kind { get; }

        public FixtureInstance Fixture { get; }

        public bool IsEmpty =>
            Fixture == null;


        private FixtureEdit(
            FixtureEditKind kind,
            FixtureInstance fixture)
        {
            Kind = kind;
            Fixture = fixture;
        }


        public static FixtureEdit AddFixture(
            FixtureInstance fixture)
        {
            return Create(
                FixtureEditKind.AddFixture,
                fixture);
        }

        public static FixtureEdit RemoveFixture(
            FixtureInstance fixture)
        {
            return Create(
                FixtureEditKind.RemoveFixture,
                fixture);
        }

        public FixtureEdit Inverse()
        {
            if (IsEmpty)
            {
                return default;
            }

            return Kind == FixtureEditKind.AddFixture
                ? RemoveFixture(Fixture)
                : AddFixture(Fixture);
        }


        private static FixtureEdit Create(
            FixtureEditKind kind,
            FixtureInstance fixture)
        {
            if (fixture == null)
            {
                throw new ArgumentNullException(
                    nameof(fixture));
            }

            return new FixtureEdit(
                kind,
                fixture);
        }


        public override string ToString()
        {
            return IsEmpty
                ? "Empty fixture edit."
                : $"{Kind}: {Fixture.Id}.";
        }
    }
}
