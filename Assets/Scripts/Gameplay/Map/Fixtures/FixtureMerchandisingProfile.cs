using System;
using System.Collections.Generic;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Immutable planogram anatomy for one fixture definition.
    ///
    /// The first graybox uses three shelf runs and four invisible frontage
    /// units on every customer-browse side. Later fixture types can author a
    /// different profile without changing planogram state.
    /// </summary>
    public sealed class FixtureMerchandisingProfile
    {
        public const int StandardShelfRunCount = 3;
        public const int StandardFrontageUnitsPerRun = 4;

        public static FixtureMerchandisingProfile None { get; } =
            new FixtureMerchandisingProfile(
                Array.Empty<FixtureDisplayFaceDefinition>());

        private readonly FixtureDisplayFaceDefinition[] displayFaces;
        private readonly Dictionary<
            FixtureSide,
            FixtureDisplayFaceDefinition> displayFacesBySide;


        public int DisplayFaceCount =>
            displayFaces.Length;

        public bool HasDisplayFaces =>
            displayFaces.Length > 0;


        public FixtureMerchandisingProfile(
            IEnumerable<FixtureDisplayFaceDefinition> displayFaces)
        {
            if (displayFaces == null)
            {
                throw new ArgumentNullException(nameof(displayFaces));
            }

            List<FixtureDisplayFaceDefinition> collectedFaces =
                new List<FixtureDisplayFaceDefinition>();

            displayFacesBySide =
                new Dictionary<
                    FixtureSide,
                    FixtureDisplayFaceDefinition>();

            foreach (FixtureDisplayFaceDefinition displayFace in displayFaces)
            {
                if (displayFace == null)
                {
                    throw new ArgumentException(
                        "A merchandising profile cannot contain a null display face.",
                        nameof(displayFaces));
                }

                if (displayFacesBySide.ContainsKey(displayFace.LocalSide))
                {
                    throw new ArgumentException(
                        $"The display side '{displayFace.LocalSide}' is duplicated.",
                        nameof(displayFaces));
                }

                displayFacesBySide.Add(
                    displayFace.LocalSide,
                    displayFace);

                collectedFaces.Add(displayFace);
            }

            this.displayFaces = collectedFaces.ToArray();
        }


        public FixtureDisplayFaceDefinition GetDisplayFace(
            int index)
        {
            return displayFaces[index];
        }

        public bool TryGetDisplayFace(
            FixtureSide localSide,
            out FixtureDisplayFaceDefinition displayFace)
        {
            return displayFacesBySide.TryGetValue(
                localSide,
                out displayFace);
        }

        public static FixtureMerchandisingProfile
            CreateForCustomerBrowseSides(
                FixtureAccessProfile accessProfile)
        {
            if (accessProfile == null)
            {
                throw new ArgumentNullException(nameof(accessProfile));
            }

            List<FixtureDisplayFaceDefinition> faces =
                new List<FixtureDisplayFaceDefinition>();

            for (FixtureSide side = FixtureSide.North;
                 side <= FixtureSide.West;
                 side++)
            {
                if (!accessProfile
                    .GetMode(side)
                    .Includes(FixtureAccessMode.CustomerBrowse))
                {
                    continue;
                }

                faces.Add(
                    new FixtureDisplayFaceDefinition(
                        side,
                        StandardShelfRunCount,
                        StandardFrontageUnitsPerRun));
            }

            return faces.Count == 0
                ? None
                : new FixtureMerchandisingProfile(faces);
        }
    }
}
