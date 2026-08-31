using BigRetail.Map.Domain;
using BigRetail.Map.Unity.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class IsometricDepthSortingGroupTests
    {
        [Test]
        public void ApplyDisplayCell_PlacesCompleteGroupInCellOccupantBand()
        {
            GameObject actor = new GameObject("Dynamic actor");

            try
            {
                IsometricDepthSortingGroup depthSorting =
                    actor.AddComponent<IsometricDepthSortingGroup>();
                SortingGroup sortingGroup =
                    actor.GetComponent<SortingGroup>();
                GridPosition displayCell =
                    new GridPosition(4, 7);

                int result =
                    depthSorting.ApplyDisplayCell(displayCell);

                Assert.That(
                    result,
                    Is.EqualTo(
                        IsometricRenderOrderResolver.ResolveCell(
                            displayCell)));
                Assert.That(
                    sortingGroup.sortingOrder,
                    Is.EqualTo(result));
                Assert.That(depthSorting.HasAppliedDepth, Is.True);
                Assert.That(
                    depthSorting.CurrentDisplayCell,
                    Is.EqualTo(displayCell));
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }


        [Test]
        public void ApplyDisplayCell_UpdatesOrderWhenActorChangesDepth()
        {
            GameObject actor = new GameObject("Moving actor");

            try
            {
                IsometricDepthSortingGroup depthSorting =
                    actor.AddComponent<IsometricDepthSortingGroup>();

                int closerOrder = depthSorting.ApplyDisplayCell(
                    new GridPosition(4, 7));
                int fartherOrder = depthSorting.ApplyDisplayCell(
                    new GridPosition(4, 8));

                Assert.That(closerOrder, Is.GreaterThan(fartherOrder));
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }
    }
}
