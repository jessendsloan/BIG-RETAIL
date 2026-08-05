using System;
using System.Collections.Generic;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Engine-free geometry contract for one door model.
    ///
    /// Segment indices are ordered from the first supplied wall edge to the
    /// last. Passage indices identify the portions future navigation may
    /// cross; the remaining segments stay structural barriers.
    /// </summary>
    public sealed class DoorDefinition
    {
        private readonly int[] passageSegmentIndices;
        private readonly bool[] passageSegments;


        public DoorDefinitionId Id { get; }

        public int SegmentCount { get; }

        public int PassageSegmentCount =>
            passageSegmentIndices.Length;

        public IReadOnlyList<int> PassageSegmentIndices =>
            passageSegmentIndices;


        public DoorDefinition(
            DoorDefinitionId id,
            int segmentCount,
            IReadOnlyList<int> passageSegmentIndices)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A door definition requires a valid ID.",
                    nameof(id));
            }

            if (segmentCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segmentCount),
                    segmentCount,
                    "A door definition must occupy at least one segment.");
            }

            if (passageSegmentIndices == null)
            {
                throw new ArgumentNullException(
                    nameof(passageSegmentIndices));
            }

            if (passageSegmentIndices.Count == 0)
            {
                throw new ArgumentException(
                    "A door definition requires at least one passage segment.",
                    nameof(passageSegmentIndices));
            }

            this.passageSegmentIndices =
                new int[passageSegmentIndices.Count];

            passageSegments =
                new bool[segmentCount];

            for (int index = 0;
                 index < passageSegmentIndices.Count;
                 index++)
            {
                int segmentIndex =
                    passageSegmentIndices[index];

                if (segmentIndex < 0
                    || segmentIndex >= segmentCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(passageSegmentIndices),
                        segmentIndex,
                        "A passage index must identify an occupied segment.");
                }

                if (passageSegments[segmentIndex])
                {
                    throw new ArgumentException(
                        $"Passage segment {segmentIndex} is duplicated.",
                        nameof(passageSegmentIndices));
                }

                this.passageSegmentIndices[index] =
                    segmentIndex;

                passageSegments[segmentIndex] = true;
            }

            Id = id;
            SegmentCount = segmentCount;
        }


        public bool IsPassageSegment(
            int segmentIndex)
        {
            if (segmentIndex < 0
                || segmentIndex >= SegmentCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segmentIndex),
                    segmentIndex,
                    "The segment index is outside this door definition.");
            }

            return passageSegments[segmentIndex];
        }
    }
}
