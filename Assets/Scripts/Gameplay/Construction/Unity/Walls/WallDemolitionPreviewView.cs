using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays a demolition pylon at the grid vertex currently selected by
    /// the construction pointer.
    ///
    /// The hover marker identifies a possible demolition-run endpoint. Actual
    /// removable and empty wall status is evaluated once a run is planned.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(200)]
    public sealed class WallDemolitionPreviewView : MonoBehaviour
    {
        [Header("Target")]

        [SerializeField]
        private WallVertexTargetResolver targetResolver;


        [Header("Visual")]

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private Color targetColor =
            new Color(
                1f,
                0.5f,
                0.08f,
                0.95f);

        [Tooltip(
            "Optional world-space adjustment applied after the vertex "
            + "position has been calculated.")]
        [SerializeField]
        private Vector3 worldPositionOffset =
            Vector3.zero;


        public bool IsToolActive { get; private set; }

        public bool IsVisible =>
            spriteRenderer != null
            && spriteRenderer.enabled;

        public GridVertex CurrentVertex { get; private set; }


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            IsToolActive = false;
            SetVisible(false);
        }


        private void LateUpdate()
        {
            RefreshPreview();
        }


        public void SetToolActive(
            bool isActive)
        {
            IsToolActive = isActive;

            if (!IsToolActive)
            {
                SetVisible(false);
            }
        }


        private void RefreshPreview()
        {
            if (!IsToolActive
                || !targetResolver.HasTarget)
            {
                SetVisible(false);
                return;
            }

            WallVertexTarget target =
                targetResolver.CurrentTarget;

            GridVertexWorldPose worldPose =
                GridVertexWorldPose.Calculate(
                    target.Vertex,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            CurrentVertex = target.Vertex;

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            spriteRenderer.sortingOrder =
                WallRenderOrderResolver.ResolvePylon(
                    worldPose.DisplayDepth);

            spriteRenderer.color = targetColor;
            SetVisible(true);
        }


        private void SetVisible(
            bool isVisible)
        {
            spriteRenderer.enabled = isVisible;

            if (!isVisible)
            {
                CurrentVertex = default;
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView has no "
                    + "WallVertexTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (spriteRenderer == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView has no "
                    + "SpriteRenderer assigned.",
                    this);

                isValid = false;
            }
            else if (spriteRenderer.sprite == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView requires a pylon Sprite on "
                    + "its SpriteRenderer.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void Reset()
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }


        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }
        }
    }
}
