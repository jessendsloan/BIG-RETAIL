using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Purchasing.Domain;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Projects ready supplier purchase orders into designated Receiving cells.
    /// Each purchase order selects one of four authored supplier-load sprites;
    /// receiving the order removes its load through the fulfillment event.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class InboundDeliveryViewSystem : MonoBehaviour
    {
        private const float AuthoredLoadTargetWidth = 0.95f;
        private const float PalletTargetWidth = 0.9f;
        private const float BoxTargetWidth = 0.7f;
        private const float BoxVerticalStep = 0.29f;

        [Header("Runtime")]

        [SerializeField]
        private PurchasingRuntimeHost purchasingRuntimeHost;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;


        [Header("Presentation")]

        [Tooltip(
            "Optional shared 1x1 isometric pallet sprite. A generated pallet "
            + "is used until final art is assigned.")]
        [SerializeField]
        private Sprite palletSprite;

        [SerializeField]
        private Transform viewParent;

        [SerializeField]
        private string sortingLayerName = "Default";

        [SerializeField]
        private Vector3 worldPositionOffset =
            new Vector3(0f, -0.12f, 0f);

        private readonly Dictionary<long, GameObject> views =
            new Dictionary<long, GameObject>();

        private InboundDeliveryPlaceholderSprites placeholderSprites;
        private ReceivingAreaState subscribedReceivingState;
        private bool hasStarted;


        public int VisibleLoadCount =>
            views.Count;

        public int StagingSlotCount =>
            receivingAreaRuntimeHost != null
                ? receivingAreaRuntimeHost.OperationalCellCount
                : 0;


        private void Awake()
        {
            if (viewParent == null)
            {
                viewParent = transform;
            }
        }

        private void OnEnable()
        {
            if (purchasingRuntimeHost != null)
            {
                purchasingRuntimeHost.Initialized +=
                    HandlePurchasingInitialized;
                purchasingRuntimeHost.DeliveriesChanged +=
                    HandleDeliveriesChanged;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized +=
                    HandleReceivingAreaInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging +=
                    HandleOrientationChanging;
                viewHost.OrientationChanged +=
                    HandleOrientationChanged;
            }

            if (hasStarted)
            {
                AttachReceivingState();
                RebuildViews();
            }
        }

        private void Start()
        {
            hasStarted = true;

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            placeholderSprites =
                new InboundDeliveryPlaceholderSprites();
            AttachReceivingState();
            RebuildViews();
        }

        private void OnDisable()
        {
            if (purchasingRuntimeHost != null)
            {
                purchasingRuntimeHost.Initialized -=
                    HandlePurchasingInitialized;
                purchasingRuntimeHost.DeliveriesChanged -=
                    HandleDeliveriesChanged;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized -=
                    HandleReceivingAreaInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;
                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            ClearViews();
            DetachReceivingState();
        }

        private void OnDestroy()
        {
            placeholderSprites?.Dispose();
            placeholderSprites = null;
        }


        public bool TryGetAssignedSlot(
            long orderNumber,
            out GridPosition slot)
        {
            if (receivingAreaRuntimeHost != null
                && receivingAreaRuntimeHost.State != null)
            {
                return receivingAreaRuntimeHost.State.TryGetReservation(
                    orderNumber,
                    out slot);
            }

            slot = default;
            return false;
        }

        public void RebuildViews()
        {
            ClearViews();

            if (placeholderSprites == null
                || purchasingRuntimeHost == null
                || !purchasingRuntimeHost.IsInitialized
                || purchasingRuntimeHost.Fulfillment == null
                || receivingAreaRuntimeHost == null
                || !receivingAreaRuntimeHost.IsInitialized
                || receivingAreaRuntimeHost.State == null
                || receivingAreaRuntimeHost.Reservations == null
                || viewHost == null
                || !viewHost.IsInitialized)
            {
                return;
            }

            List<InboundDeliveryLoad> loads =
                new List<InboundDeliveryLoad>();

            foreach (
                InboundDeliveryLoad load
                in purchasingRuntimeHost.Fulfillment
                    .EnumerateReadyDeliveries())
            {
                loads.Add(load);
            }

            List<long> readyOrderNumbers = new List<long>(loads.Count);

            for (int index = 0; index < loads.Count; index++)
            {
                readyOrderNumbers.Add(loads[index].OrderNumber);
            }

            receivingAreaRuntimeHost.Reservations.Synchronize(
                readyOrderNumbers);

            for (int index = 0; index < loads.Count; index++)
            {
                InboundDeliveryLoad load = loads[index];

                if (!receivingAreaRuntimeHost.State.TryGetReservation(
                        load.OrderNumber,
                        out GridPosition slot))
                {
                    continue;
                }

                views.Add(
                    load.OrderNumber,
                    CreateLoadView(load, slot));
            }
        }


        private void HandlePurchasingInitialized(
            PurchasingRuntimeHost initializedHost)
        {
            RebuildViews();
        }

        private void HandleDeliveriesChanged()
        {
            RebuildViews();
        }

        private void HandleReceivingAreaInitialized(
            ReceivingAreaRuntimeHost initializedHost)
        {
            AttachReceivingState();
            RebuildViews();
        }

        private void HandleReceivingAreaChanged()
        {
            RebuildViews();
        }

        private void HandleOrientationChanging(
            IsometricViewOrientation previous,
            IsometricViewOrientation next)
        {
            ClearViews();
        }

        private void HandleOrientationChanged(
            IsometricViewOrientation previous,
            IsometricViewOrientation current)
        {
            RebuildViews();
        }

        private void AttachReceivingState()
        {
            ReceivingAreaState nextState =
                receivingAreaRuntimeHost != null
                && receivingAreaRuntimeHost.IsInitialized
                    ? receivingAreaRuntimeHost.State
                    : null;

            if (subscribedReceivingState == nextState)
            {
                return;
            }

            DetachReceivingState();
            subscribedReceivingState = nextState;

            if (subscribedReceivingState != null)
            {
                subscribedReceivingState.AreaChanged +=
                    HandleReceivingAreaChanged;
            }
        }

        private void DetachReceivingState()
        {
            if (subscribedReceivingState == null)
            {
                return;
            }

            subscribedReceivingState.AreaChanged -=
                HandleReceivingAreaChanged;
            subscribedReceivingState = null;
        }

        private GameObject CreateLoadView(
            InboundDeliveryLoad load,
            GridPosition slot)
        {
            SupplierDefinition supplier =
                purchasingRuntimeHost.Catalog.Suppliers
                    .GetRequired(load.SupplierId);
            SupplierDefinitionAsset supplierAsset =
                FindSupplierAsset(load.SupplierId);
            Color supplierColor =
                supplierAsset != null
                    ? supplierAsset.AccentColor
                    : Color.white;
            Sprite authoredLoad =
                supplierAsset != null
                    ? supplierAsset.GetDeliveryLoadSprite(
                        load.VisibleBoxCount)
                    : null;

            GameObject root = new GameObject(
                $"Inbound {supplier.DisplayName} PO {load.OrderNumber}");
            root.transform.SetParent(viewParent, worldPositionStays: true);
            root.transform.position =
                viewHost.GetLogicalCellCenterWorld(
                    slot,
                    coordinateTilemap)
                + worldPositionOffset;

            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(slot);
            SortingGroup sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = sortingLayerName;
            sortingGroup.sortingOrder =
                WallRenderOrderResolver.ResolveCell(displayCell);

            SpriteRenderer loadRenderer;

            if (authoredLoad != null)
            {
                loadRenderer = CreateRenderer(
                    root.transform,
                    $"Supplier Load Tier {load.VisibleBoxCount}",
                    authoredLoad,
                    Color.white,
                    AuthoredLoadTargetWidth,
                    Vector3.zero,
                    sortingOrder: 0);
            }
            else
            {
                loadRenderer = CreateRenderer(
                    root.transform,
                    "Pallet",
                    palletSprite != null
                        ? palletSprite
                        : placeholderSprites.Pallet,
                    Color.white,
                    PalletTargetWidth,
                    Vector3.zero,
                    sortingOrder: 0);

                for (int index = 0;
                     index < load.VisibleBoxCount;
                     index++)
                {
                    float horizontalOffset = index == 1
                        ? 0.035f
                        : index == 2
                            ? -0.025f
                            : index == 3
                                ? 0.025f
                                : 0f;
                    Vector3 localPosition =
                        new Vector3(
                            horizontalOffset,
                            0.09f + index * BoxVerticalStep,
                            0f);

                    CreateRenderer(
                        root.transform,
                        $"Supplier Carton {index + 1}",
                        placeholderSprites.Box,
                        supplierColor,
                        BoxTargetWidth,
                        localPosition,
                        sortingOrder: index + 1);
                }
            }

            InboundDeliveryLoadView loadView =
                root.AddComponent<InboundDeliveryLoadView>();
            loadView.Initialize(
                load.OrderNumber,
                load.SupplierId,
                load.PurchasePackCount,
                load.RemainingUnitCount,
                load.VisibleBoxCount,
                slot,
                loadRenderer);
            return root;
        }

        private SpriteRenderer CreateRenderer(
            Transform parent,
            string objectName,
            Sprite sprite,
            Color color,
            float targetWidth,
            Vector3 localPosition,
            int sortingOrder)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, worldPositionStays: false);
            child.transform.localPosition = localPosition;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            float spriteWidth = sprite != null
                ? sprite.bounds.size.x
                : 0f;

            if (spriteWidth > Mathf.Epsilon)
            {
                float uniformScale = targetWidth / spriteWidth;
                child.transform.localScale =
                    new Vector3(
                        uniformScale,
                        uniformScale,
                        1f);
            }

            return renderer;
        }

        private SupplierDefinitionAsset FindSupplierAsset(
            SupplierId supplierId)
        {
            SupplierCatalogAsset supplierCatalog =
                purchasingRuntimeHost.CatalogAsset?.SupplierCatalog;

            if (supplierCatalog == null)
            {
                return null;
            }

            IReadOnlyList<SupplierDefinitionAsset> assets =
                supplierCatalog.Suppliers;

            for (int index = 0; index < assets.Count; index++)
            {
                SupplierDefinitionAsset asset = assets[index];

                if (asset != null
                    && string.Equals(
                        asset.SupplierIdValue,
                        supplierId.Value,
                        StringComparison.Ordinal))
                {
                    return asset;
                }
            }

            return null;
        }

        private void ClearViews()
        {
            foreach (GameObject view in views.Values)
            {
                if (view != null)
                {
                    Destroy(view);
                }
            }

            views.Clear();
        }

        private bool ValidateReferences()
        {
            bool valid = purchasingRuntimeHost != null
                && receivingAreaRuntimeHost != null
                && viewHost != null
                && coordinateTilemap != null;

            if (!valid)
            {
                Debug.LogError(
                    "InboundDeliveryViewSystem requires Purchasing, the "
                    + "Receiving Area runtime, isometric view, and coordinate "
                    + "Tilemap references.",
                    this);
            }

            return valid;
        }
    }


    /// <summary>
    /// Runtime inspection handle for one visible inbound supplier load.
    /// </summary>
    public sealed class InboundDeliveryLoadView : MonoBehaviour
    {
        public long OrderNumber { get; private set; }

        public SupplierId SupplierId { get; private set; }

        public int PurchasePackCount { get; private set; }

        public int RemainingUnitCount { get; private set; }

        public int VisibleBoxCount { get; private set; }

        public GridPosition StagingCell { get; private set; }

        public SpriteRenderer LoadRenderer { get; private set; }

        public SpriteRenderer PalletRenderer =>
            LoadRenderer;


        internal void Initialize(
            long orderNumber,
            SupplierId supplierId,
            int purchasePackCount,
            int remainingUnitCount,
            int visibleBoxCount,
            GridPosition stagingCell,
            SpriteRenderer loadRenderer)
        {
            OrderNumber = orderNumber;
            SupplierId = supplierId;
            PurchasePackCount = purchasePackCount;
            RemainingUnitCount = remainingUnitCount;
            VisibleBoxCount = visibleBoxCount;
            StagingCell = stagingCell;
            LoadRenderer = loadRenderer;
        }
    }
}
