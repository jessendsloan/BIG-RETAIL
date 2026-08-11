using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Draws a lightweight silhouette around the fixture currently targeted
    /// by the merchandise tool. The target identity survives camera-driven
    /// fixture-view reconstruction.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(155)]
    public sealed class FixtureMerchandisingHoverOutlineView : MonoBehaviour
    {
        private static readonly Vector2[] OutlineDirections =
        {
            new Vector2(-1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(0f, 1f),
            new Vector2(-0.7071f, -0.7071f),
            new Vector2(-0.7071f, 0.7071f),
            new Vector2(0.7071f, -0.7071f),
            new Vector2(0.7071f, 0.7071f)
        };

        [SerializeField]
        private FixtureViewSystem fixtureViewSystem;

        [SerializeField]
        private Color outlineColor =
            new Color(1f, 0.72f, 0.16f, 0.96f);

        [SerializeField]
        [Min(0.001f)]
        private float outlineThickness = 0.032f;


        private readonly List<SpriteRenderer> outlineRenderers =
            new List<SpriteRenderer>();

        private bool hasTarget;


        public bool IsVisible { get; private set; }

        public FixtureInstanceId TargetFixtureId { get; private set; }


        private void OnEnable()
        {
            if (fixtureViewSystem == null)
            {
                fixtureViewSystem = GetComponent<FixtureViewSystem>();
            }

            if (fixtureViewSystem == null)
            {
                Debug.LogError(
                    "FixtureMerchandisingHoverOutlineView requires a fixture view system.",
                    this);
                enabled = false;
                return;
            }

            fixtureViewSystem.FixtureViewShown += HandleFixtureViewShown;
            fixtureViewSystem.FixtureViewHidden += HandleFixtureViewHidden;
        }

        private void OnDisable()
        {
            if (fixtureViewSystem != null)
            {
                fixtureViewSystem.FixtureViewShown -= HandleFixtureViewShown;
                fixtureViewSystem.FixtureViewHidden -= HandleFixtureViewHidden;
            }

            Hide();
        }


        public void ShowFixture(FixtureInstanceId fixtureId)
        {
            if (hasTarget
                && TargetFixtureId == fixtureId
                && IsVisible)
            {
                return;
            }

            hasTarget = true;
            TargetFixtureId = fixtureId;
            RebuildOutline();
        }

        public void Hide()
        {
            hasTarget = false;
            TargetFixtureId = default;
            HideUnused(0);
            IsVisible = false;
        }


        private void RebuildOutline()
        {
            if (!hasTarget
                || !fixtureViewSystem.TryGetRenderers(
                    TargetFixtureId,
                    out IReadOnlyList<SpriteRenderer> sourceRenderers))
            {
                HideUnused(0);
                IsVisible = false;
                return;
            }

            int requiredCount =
                sourceRenderers.Count * OutlineDirections.Length;

            EnsureCapacity(requiredCount);
            int outlineIndex = 0;

            for (int sourceIndex = 0;
                 sourceIndex < sourceRenderers.Count;
                 sourceIndex++)
            {
                SpriteRenderer source = sourceRenderers[sourceIndex];

                if (source == null || source.sprite == null)
                {
                    continue;
                }

                for (int directionIndex = 0;
                     directionIndex < OutlineDirections.Length;
                     directionIndex++)
                {
                    ConfigureOutlineRenderer(
                        outlineRenderers[outlineIndex],
                        source,
                        OutlineDirections[directionIndex]);

                    outlineIndex++;
                }
            }

            HideUnused(outlineIndex);
            IsVisible = outlineIndex > 0;
        }

        private void EnsureCapacity(int requiredCount)
        {
            while (outlineRenderers.Count < requiredCount)
            {
                GameObject outlineObject =
                    new GameObject("Fixture Merchandise Hover Outline");

                outlineObject.transform.SetParent(
                    transform,
                    worldPositionStays: true);

                outlineRenderers.Add(
                    outlineObject.AddComponent<SpriteRenderer>());
            }
        }

        private void ConfigureOutlineRenderer(
            SpriteRenderer outline,
            SpriteRenderer source,
            Vector2 direction)
        {
            outline.sprite = source.sprite;
            outline.color = outlineColor;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder - 1;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
            outline.spriteSortPoint = source.spriteSortPoint;

            Vector3 offset =
                new Vector3(
                    direction.x * outlineThickness,
                    direction.y * outlineThickness,
                    0f);

            outline.transform.SetPositionAndRotation(
                source.transform.position + offset,
                source.transform.rotation);

            outline.transform.localScale =
                source.transform.lossyScale;

            outline.gameObject.SetActive(true);
        }

        private void HideUnused(int usedCount)
        {
            for (int index = usedCount;
                 index < outlineRenderers.Count;
                 index++)
            {
                outlineRenderers[index].gameObject.SetActive(false);
            }
        }

        private void HandleFixtureViewShown(
            FixtureInstance fixture,
            SpriteRenderer primaryRenderer)
        {
            if (hasTarget && fixture.Id == TargetFixtureId)
            {
                RebuildOutline();
            }
        }

        private void HandleFixtureViewHidden(FixtureInstanceId fixtureId)
        {
            if (!hasTarget || fixtureId != TargetFixtureId)
            {
                return;
            }

            HideUnused(0);
            IsVisible = false;
        }
    }
}
