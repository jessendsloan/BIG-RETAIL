using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Projects ready BIG Wholesale fixture-equipment orders as physical
    /// graybox pallets.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-39)]
    public sealed class FixtureEquipmentDeliveryViewSystem : MonoBehaviour
    {
        private const float PalletWidth = 0.9f;
        private const float BoxWidth = 0.7f;
        private const float AuthoredLoadWidth = 0.95f;

        [SerializeField]
        private FixtureEquipmentRuntimeHost equipmentRuntimeHost;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private Transform viewParent;

        [Tooltip(
            "BIG Wholesale owns the Equipment Catalog. Its authored load "
            + "sprites are reused for fixture-equipment arrivals.")]
        [SerializeField]
        private SupplierDefinitionAsset equipmentSupplierAsset;

        [SerializeField]
        private Color equipmentColor =
            new Color(0.82f, 0.2f, 0.15f, 1f);

        [SerializeField]
        private string sortingLayerName = "Default";

        [SerializeField]
        private Vector3 worldPositionOffset =
            new Vector3(0f, -0.12f, 0f);

        private readonly Dictionary<long, GameObject> views =
            new Dictionary<long, GameObject>();
        private InboundDeliveryPlaceholderSprites placeholderSprites;
        private ReceivingAreaState subscribedReceivingState;


        public int VisibleLoadCount => views.Count;


        private void Awake()
        {
            if (viewParent == null)
            {
                viewParent = transform;
            }
        }

        private void OnEnable()
        {
            equipmentRuntimeHost.Initialized += HandleRuntimeInitialized;
            equipmentRuntimeHost.StateChanged += HandleStateChanged;
            receivingAreaRuntimeHost.Initialized +=
                HandleReceivingInitialized;
            viewHost.OrientationChanging += HandleOrientationChanging;
            viewHost.OrientationChanged += HandleOrientationChanged;
        }

        private void Start()
        {
            placeholderSprites = new InboundDeliveryPlaceholderSprites();
            AttachReceivingState();
            RebuildViews();
        }

        private void OnDisable()
        {
            if (equipmentRuntimeHost != null)
            {
                equipmentRuntimeHost.Initialized -=
                    HandleRuntimeInitialized;
                equipmentRuntimeHost.StateChanged -= HandleStateChanged;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized -=
                    HandleReceivingInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -= HandleOrientationChanging;
                viewHost.OrientationChanged -= HandleOrientationChanged;
            }

            DetachReceivingState();
            ClearViews();
        }

        private void OnDestroy()
        {
            placeholderSprites?.Dispose();
            placeholderSprites = null;
        }

        public void RebuildViews()
        {
            ClearViews();

            if (placeholderSprites == null
                || equipmentRuntimeHost == null
                || !equipmentRuntimeHost.IsInitialized
                || receivingAreaRuntimeHost?.State == null
                || viewHost == null
                || !viewHost.IsInitialized)
            {
                return;
            }

            equipmentRuntimeHost.RefreshReceivingReservations();

            foreach (FixtureEquipmentOrder order
                     in equipmentRuntimeHost.Orders.EnumerateReadyOrders())
            {
                ReceivingLoadId loadId =
                    ReceivingLoadId.EquipmentOrder(order.OrderNumber);

                if (!receivingAreaRuntimeHost.State.TryGetReservation(
                        loadId,
                        out GridPosition slot))
                {
                    continue;
                }

                views.Add(order.OrderNumber, CreateLoadView(order, slot));
            }
        }

        private GameObject CreateLoadView(
            FixtureEquipmentOrder order,
            GridPosition slot)
        {
            int visibleBoxCount = Mathf.Clamp(order.Quantity, 1, 4);
            Sprite authoredLoad =
                equipmentSupplierAsset != null
                && string.Equals(
                    equipmentSupplierAsset.SupplierIdValue,
                    order.SupplierId,
                    StringComparison.Ordinal)
                    ? equipmentSupplierAsset.GetDeliveryLoadSprite(
                        visibleBoxCount)
                    : null;
            GameObject root = new GameObject(
                $"{order.SupplierDisplayName} Equipment Order "
                + order.OrderNumber);
            root.transform.SetParent(viewParent, true);
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
                IsometricRenderOrderResolver.ResolveCell(displayCell);

            if (authoredLoad != null)
            {
                CreateRenderer(
                    root.transform,
                    $"{order.SupplierDisplayName} Equipment Load "
                    + visibleBoxCount,
                    authoredLoad,
                    Color.white,
                    AuthoredLoadWidth,
                    Vector3.zero,
                    0);
            }
            else
            {
                CreateRenderer(
                    root.transform,
                    $"{order.SupplierDisplayName} Equipment Pallet",
                    placeholderSprites.Pallet,
                    Color.white,
                    PalletWidth,
                    Vector3.zero,
                    0);

                for (int index = 0; index < visibleBoxCount; index++)
                {
                    float x = index % 2 == 0 ? -0.12f : 0.12f;
                    float y = 0.1f + (index / 2) * 0.3f;
                    CreateRenderer(
                        root.transform,
                        $"{order.SupplierDisplayName} Equipment Carton "
                        + (index + 1),
                        placeholderSprites.Box,
                        equipmentColor,
                        BoxWidth,
                        new Vector3(x, y, 0f),
                        index + 1);
                }
            }

            FixtureEquipmentDeliveryLoadView handle =
                root.AddComponent<FixtureEquipmentDeliveryLoadView>();
            handle.Initialize(
                order.OrderNumber,
                order.SupplierId,
                order.SupplierDisplayName,
                order.FixtureDefinitionId.Value,
                order.Quantity,
                slot);
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
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            if (sprite != null && sprite.bounds.size.x > Mathf.Epsilon)
            {
                float scale = targetWidth / sprite.bounds.size.x;
                child.transform.localScale = new Vector3(scale, scale, 1f);
            }

            return renderer;
        }

        private void HandleRuntimeInitialized(
            FixtureEquipmentRuntimeHost initializedHost)
        {
            RebuildViews();
        }

        private void HandleStateChanged()
        {
            RebuildViews();
        }

        private void HandleReceivingInitialized(
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
            ReceivingAreaState next =
                receivingAreaRuntimeHost != null
                && receivingAreaRuntimeHost.IsInitialized
                    ? receivingAreaRuntimeHost.State
                    : null;

            if (subscribedReceivingState == next)
            {
                return;
            }

            DetachReceivingState();
            subscribedReceivingState = next;

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
    }


    public sealed class FixtureEquipmentDeliveryLoadView : MonoBehaviour
    {
        public long OrderNumber { get; private set; }
        public string SupplierId { get; private set; }
        public string SupplierDisplayName { get; private set; }
        public string FixtureDefinitionId { get; private set; }
        public int Quantity { get; private set; }
        public GridPosition StagingCell { get; private set; }

        internal void Initialize(
            long orderNumber,
            string supplierId,
            string supplierDisplayName,
            string fixtureDefinitionId,
            int quantity,
            GridPosition stagingCell)
        {
            OrderNumber = orderNumber;
            SupplierId = supplierId;
            SupplierDisplayName = supplierDisplayName;
            FixtureDefinitionId = fixtureDefinitionId;
            Quantity = quantity;
            StagingCell = stagingCell;
        }
    }
}
