using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays the single wall edge currently targeted by
    /// the demolition tool.
    ///
    /// Orange means a wall will be removed.
    /// Gray means the edge is already empty.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(200)]
    public sealed class WallDemolitionPreviewView :
        MonoBehaviour
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
        private float previewThickness = 0.12f;

        [SerializeField]
        private Color removableColor =
            new Color(
                1f,
                0.5f,
                0.08f,
                0.95f);

        [SerializeField]
        private Color alreadyEmptyColor =
            new Color(
                0.55f,
                0.55f,
                0.55f,
                0.65f);

        [SerializeField]
        private Vector3 worldPositionOffset =
            Vector3.zero;


        public bool IsToolActive { get; private set; }

        public bool HasRemovableWall { get; private set; }

        public bool IsVisible =>
            spriteRenderer != null
            && spriteRenderer.enabled;


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

            CellEdge edge =
                targetResolver.CurrentTarget.Edge;

            HasRemovableWall =
                mapHost.WallConstruction
                    .HasWall(edge);

            CellEdgeWorldPose worldPose =
                CellEdgeWorldPose.Calculate(
                    edge,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ);

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                worldPose.Rotation);

            ApplySpriteScale(
                worldPose.Length);

            spriteRenderer.color =
                HasRemovableWall
                    ? removableColor
                    : alreadyEmptyColor;

            SetVisible(true);
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
                HasRemovableWall = false;
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView has no " +
                    "WallTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView has no " +
                    "GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (spriteRenderer == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView has no " +
                    "SpriteRenderer assigned.",
                    this);

                isValid = false;
            }
            else if (spriteRenderer.sprite == null)
            {
                Debug.LogError(
                    "WallDemolitionPreviewView requires a Sprite.",
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