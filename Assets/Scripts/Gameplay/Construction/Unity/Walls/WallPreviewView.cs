using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays the currently selected wall edge without modifying
    /// model-owned wall state.
    ///
    /// Green means a new wall can be created.
    /// Blue means a wall already exists and satisfies the request.
    /// Red means a brand-new wall cannot be created on this edge.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(200)]
    public sealed class WallPreviewView : MonoBehaviour
    {
        [Header("Target")]

        [SerializeField]
        private WallTargetResolver targetResolver;

        [SerializeField]
        private GridMapHost mapHost;


        [Header("Visual")]

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField, Min(0.001f)]
        private float previewThickness = 0.1f;

        [SerializeField]
        private Color validColor =
            new Color(
                0.2f,
                1f,
                0.3f,
                0.85f);

        [SerializeField]
        private Color existingColor =
            new Color(
                0.15f,
                0.65f,
                1f,
                0.9f);

        [SerializeField]
        private Color invalidColor =
            new Color(
                1f,
                0.2f,
                0.2f,
                0.85f);

        [Tooltip(
            "Optional world-space adjustment applied after the edge " +
            "position has been calculated.")]
        [SerializeField]
        private Vector3 worldPositionOffset =
            Vector3.zero;


        public bool IsToolActive { get; private set; }

        public bool IsVisible =>
            spriteRenderer != null
            && spriteRenderer.enabled;

        /// <summary>
        /// True when confirming this edge is harmless:
        /// either a new wall can be created or one already exists.
        /// </summary>
        public bool IsPlacementValid { get; private set; }

        public WallChangeResult CurrentEvaluation
        {
            get;
            private set;
        }


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
            IsToolActive =
                isActive;

            if (!IsToolActive)
            {
                SetVisible(false);
            }
        }


        private void RefreshPreview()
        {
            if (!IsToolActive
                || !targetResolver.HasTarget
                || !mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                SetVisible(false);
                return;
            }

            WallTarget target =
                targetResolver.CurrentTarget;

            bool wallAlreadyExists =
                mapHost.WallConstruction
                    .HasWall(target.Edge);

            if (wallAlreadyExists)
            {
                CurrentEvaluation =
                    WallChangeResult.Rejected(
                        target.Edge,
                        WallChangeFailure.AlreadyExists);

                IsPlacementValid = true;

                ApplyEdgePose(
                    target.Edge);

                spriteRenderer.color =
                    existingColor;

                SetVisible(true);
                return;
            }

            CurrentEvaluation =
                mapHost.WallConstruction
                    .EvaluatePlacement(
                        target.Edge);

            IsPlacementValid =
                CurrentEvaluation.Succeeded;

            ApplyEdgePose(
                target.Edge);

            spriteRenderer.color =
                CurrentEvaluation.Succeeded
                    ? validColor
                    : invalidColor;

            SetVisible(true);
        }


        private void ApplyEdgePose(
            CellEdge edge)
        {
            CellEdgeWorldPose worldPose =
                CellEdgeWorldPose.Calculate(
                    edge,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                worldPose.Rotation);

            ApplySpriteScale(
                worldPose.Length);
        }


        private void ApplySpriteScale(
            float edgeLength)
        {
            Vector3 spriteSize =
                spriteRenderer.sprite.bounds.size;

            float safeSpriteWidth =
                Mathf.Max(
                    spriteSize.x,
                    0.0001f);

            float safeSpriteHeight =
                Mathf.Max(
                    spriteSize.y,
                    0.0001f);

            transform.localScale =
                new Vector3(
                    edgeLength / safeSpriteWidth,
                    previewThickness / safeSpriteHeight,
                    1f);
        }


        private void SetVisible(
            bool isVisible)
        {
            spriteRenderer.enabled =
                isVisible;

            if (!isVisible)
            {
                IsPlacementValid = false;
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallPreviewView has no WallTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallPreviewView has no GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (spriteRenderer == null)
            {
                Debug.LogError(
                    "WallPreviewView has no SpriteRenderer assigned.",
                    this);

                isValid = false;
            }
            else if (spriteRenderer.sprite == null)
            {
                Debug.LogError(
                    "WallPreviewView requires a Sprite on its " +
                    "SpriteRenderer.",
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
            previewThickness =
                Mathf.Max(
                    previewThickness,
                    0.001f);

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }
        }
    }
}
