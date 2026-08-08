using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Keeps Unity wall views synchronized with model-owned structural and
    /// finish state.
    ///
    /// The runtime model remains the source of truth. This system only creates,
    /// removes, and refreshes presentation objects.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class WallViewSystem : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;


        [Header("Coordinate Mapping")]

        [Tooltip(
            "A Tilemap belonging to the same Grid as the authored map. "
            + "Map Visuals is appropriate.")]
        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private IsometricViewHost viewHost;

        [Tooltip(
            "The logical building level displayed by this view system.")]
        [SerializeField]
        private int logicalLevel = 0;

        [Tooltip(
            "The Unity Tilemap Z layer used to calculate cell centers.")]
        [SerializeField]
        private int unityCellZ = 0;


        [Header("Wall Presentation")]

        [SerializeField]
        private WallSegmentView wallSegmentPrefab;

        [Tooltip(
            "Parent Transform for instantiated wall views. "
            + "When empty, this component's Transform is used.")]
        [SerializeField]
        private Transform wallViewParent;

        [SerializeField]
        private WallDisplayMode startingDisplayMode =
            WallDisplayMode.WallsUp;


        private readonly Dictionary<CellEdge, WallSegmentView>
            wallViews =
                new Dictionary<CellEdge, WallSegmentView>();

        private readonly Dictionary<DoorAssemblyId, DoorAssemblyView>
            doorAssemblyViews =
                new Dictionary<DoorAssemblyId, DoorAssemblyView>();

        private WallState subscribedWallState;
        private WallFinishService subscribedFinishService;
        private DoorAssemblyState subscribedDoorAssemblyState;
        private FoundationState subscribedFoundationState;
        private WallFinishPresentationResolver finishResolver;
        private DoorPresentationResolver doorResolver;
        private FoundationCutawayMap foundationCutawayMap;


        public int WallViewCount =>
            wallViews.Count;

        public int VisibleWallCount =>
            wallViews.Count;

        public WallDisplayMode CurrentDisplayMode
        {
            get;
            private set;
        }


        public event Action<WallDisplayMode, WallDisplayMode>
            DisplayModeChanged;


        private void Awake()
        {
            CurrentDisplayMode =
                startingDisplayMode;

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (wallViewParent == null)
            {
                wallViewParent =
                    transform;
            }
        }


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }

            if (foundationRuntimeHost != null)
            {
                foundationRuntimeHost.Initialized +=
                    HandleFoundationRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanged +=
                    HandleOrientationChanged;
            }

            if (mapHost != null
                && mapHost.IsInitialized)
            {
                AttachToRuntimeModel(
                    mapHost.WallState,
                    mapHost.WallFinishes,
                    mapHost.WallFinishAssets,
                    mapHost.DoorAssemblies,
                    mapHost.DoorDefinitionAssets);
            }

            if (foundationRuntimeHost != null
                && foundationRuntimeHost.IsInitialized)
            {
                AttachToFoundationState(
                    foundationRuntimeHost.FoundationState);
            }
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            if (foundationRuntimeHost != null)
            {
                foundationRuntimeHost.Initialized -=
                    HandleFoundationRuntimeInitialized;
            }

            DetachFromRuntimeModel();
            DetachFromFoundationState();
            ClearAllViews();
        }


        public bool CycleDisplayMode()
        {
            return TrySetDisplayMode(
                WallDisplayModeCycle.Next(
                    CurrentDisplayMode));
        }


        public bool TrySetDisplayMode(
            WallDisplayMode displayMode)
        {
            if (!IsSupportedDisplayMode(displayMode))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayMode),
                    displayMode,
                    "Unknown wall display mode.");
            }

            if (displayMode == CurrentDisplayMode)
            {
                return false;
            }

            WallDisplayMode previousMode =
                CurrentDisplayMode;

            CurrentDisplayMode =
                displayMode;

            RefreshAllWallPresentations();

            DisplayModeChanged?.Invoke(
                previousMode,
                CurrentDisplayMode);

            return true;
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            AttachToRuntimeModel(
                initializedHost.WallState,
                initializedHost.WallFinishes,
                initializedHost.WallFinishAssets,
                initializedHost.DoorAssemblies,
                initializedHost.DoorDefinitionAssets);
        }


        private void HandleFoundationRuntimeInitialized(
            FoundationRuntimeHost initializedHost)
        {
            AttachToFoundationState(
                initializedHost.FoundationState);
        }


        private void AttachToRuntimeModel(
            WallState wallState,
            WallFinishService finishService,
            WallFinishAssetCatalog finishAssets,
            DoorAssemblyState doorAssemblyState,
            DoorDefinitionAssetCatalog doorAssets)
        {
            if (wallState == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallState.",
                    this);
                return;
            }

            if (finishService == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallFinishService.",
                    this);
                return;
            }

            if (finishAssets == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallFinishAssetCatalog.",
                    this);
                return;
            }

            if (doorAssemblyState == null
                || doorAssets == null)
            {
                Debug.LogError(
                    "WallViewSystem received incomplete door presentation "
                    + "services.",
                    this);
                return;
            }

            if (subscribedWallState == wallState
                && subscribedFinishService == finishService
                && subscribedDoorAssemblyState == doorAssemblyState)
            {
                RebuildAllViews();
                return;
            }

            DetachFromRuntimeModel();

            subscribedWallState =
                wallState;

            subscribedFinishService =
                finishService;

            finishResolver =
                new WallFinishPresentationResolver(
                    subscribedFinishService,
                    finishAssets);

            subscribedDoorAssemblyState =
                doorAssemblyState;

            doorResolver =
                new DoorPresentationResolver(
                    subscribedDoorAssemblyState,
                    doorAssets);

            subscribedWallState.WallAdded +=
                HandleWallAdded;

            subscribedWallState.WallRemoved +=
                HandleWallRemoved;

            subscribedFinishService.EffectiveFinishChanged +=
                HandleEffectiveFinishChanged;

            subscribedDoorAssemblyState.AssemblyAdded +=
                HandleDoorAssemblyChanged;

            subscribedDoorAssemblyState.AssemblyRemoved +=
                HandleDoorAssemblyChanged;

            RebuildAllViews();
        }


        private void DetachFromRuntimeModel()
        {
            if (subscribedWallState != null)
            {
                subscribedWallState.WallAdded -=
                    HandleWallAdded;

                subscribedWallState.WallRemoved -=
                    HandleWallRemoved;
            }

            if (subscribedFinishService != null)
            {
                subscribedFinishService.EffectiveFinishChanged -=
                    HandleEffectiveFinishChanged;
            }

            if (subscribedDoorAssemblyState != null)
            {
                subscribedDoorAssemblyState.AssemblyAdded -=
                    HandleDoorAssemblyChanged;

                subscribedDoorAssemblyState.AssemblyRemoved -=
                    HandleDoorAssemblyChanged;
            }

            subscribedWallState =
                null;

            subscribedFinishService =
                null;

            subscribedDoorAssemblyState =
                null;

            finishResolver =
                null;

            doorResolver =
                null;
        }


        private void AttachToFoundationState(
            FoundationState foundationState)
        {
            if (foundationState == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null FoundationState.",
                    this);
                return;
            }

            if (subscribedFoundationState == foundationState)
            {
                RebuildFoundationCutawayMap();
                RefreshAllWallPresentations();
                return;
            }

            DetachFromFoundationState();

            subscribedFoundationState =
                foundationState;

            subscribedFoundationState.FoundationAdded +=
                HandleFoundationChanged;

            subscribedFoundationState.FoundationRemoved +=
                HandleFoundationChanged;

            RebuildFoundationCutawayMap();
            RefreshAllWallPresentations();
        }


        private void DetachFromFoundationState()
        {
            if (subscribedFoundationState == null)
            {
                return;
            }

            subscribedFoundationState.FoundationAdded -=
                HandleFoundationChanged;

            subscribedFoundationState.FoundationRemoved -=
                HandleFoundationChanged;

            subscribedFoundationState =
                null;

            foundationCutawayMap =
                null;
        }


        private void HandleWallAdded(
            CellEdge edge)
        {
            RebuildFoundationCutawayMap();
            CreateWallView(edge);
            RefreshAllWallPresentations();
        }


        private void HandleWallRemoved(
            CellEdge edge)
        {
            RebuildFoundationCutawayMap();
            RemoveWallView(edge);
            RefreshAllWallPresentations();
        }


        private void HandleEffectiveFinishChanged(
            WallFaceKey face,
            WallFinishId finishId)
        {
            if (!wallViews.TryGetValue(
                    face.Edge,
                    out WallSegmentView view)
                || view == null)
            {
                return;
            }

            view.ApplyProjection(
                viewHost.Projection);
        }


        private void HandleDoorAssemblyChanged(
            DoorAssembly assembly)
        {
            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                CellEdge edge =
                    assembly.GetEdge(index);

                if (wallViews.TryGetValue(
                        edge,
                        out WallSegmentView view)
                    && view != null)
                {
                    ApplyWallPresentation(
                        edge,
                        view);
                }
            }

            SynchronizeDoorAssemblyView(
                assembly);
        }


        private void RebuildAllViews()
        {
            ClearAllViews();

            if (subscribedWallState == null
                || finishResolver == null
                || doorResolver == null)
            {
                return;
            }

            RebuildFoundationCutawayMap();

            foreach (
                CellEdge wall
                in subscribedWallState.EnumerateWalls())
            {
                CreateWallView(wall);
            }

            RefreshAllDoorAssemblyPresentations();
        }


        private void CreateWallView(
            CellEdge edge)
        {
            if (edge.FirstCell.Level != logicalLevel
                || finishResolver == null
                || doorResolver == null)
            {
                return;
            }

            if (wallViews.ContainsKey(edge))
            {
                return;
            }

            WallSegmentView view =
                Instantiate(
                    wallSegmentPrefab,
                    wallViewParent);

            try
            {
                view.Initialize(
                    edge,
                    coordinateTilemap,
                    logicalLevel,
                    unityCellZ,
                    viewHost.Projection,
                    finishResolver,
                    doorResolver,
                    ResolveWallHeight(edge));

                wallViews.Add(
                    edge,
                    view);
            }
            catch (Exception exception)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }

                Debug.LogException(
                    exception,
                    this);
            }
        }


        private void RemoveWallView(
            CellEdge edge)
        {
            if (!wallViews.TryGetValue(
                    edge,
                    out WallSegmentView view))
            {
                return;
            }

            wallViews.Remove(edge);

            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            RebuildFoundationCutawayMap();
            RefreshAllWallPresentations();
        }


        private void HandleFoundationChanged(
            GridPosition cell)
        {
            RebuildFoundationCutawayMap();
            RefreshAllWallPresentations();
        }


        private void RefreshAllWallPresentations()
        {
            foreach (
                KeyValuePair<CellEdge, WallSegmentView> entry
                in wallViews)
            {
                if (entry.Value != null
                    && subscribedWallState != null
                    && subscribedWallState.HasWall(
                        entry.Key))
                {
                    ApplyWallPresentation(
                        entry.Key,
                        entry.Value);
                }
            }

            RefreshAllDoorAssemblyPresentations();
        }


        private void RefreshAllDoorAssemblyPresentations()
        {
            if (subscribedDoorAssemblyState == null
                || doorResolver == null)
            {
                return;
            }

            foreach (
                DoorAssembly assembly
                in subscribedDoorAssemblyState.EnumerateAssemblies())
            {
                SynchronizeDoorAssemblyView(
                    assembly);
            }
        }


        private void SynchronizeDoorAssemblyView(
            DoorAssembly changedAssembly)
        {
            if (subscribedDoorAssemblyState == null
                || doorResolver == null
                || !subscribedDoorAssemblyState.TryGetAssembly(
                    changedAssembly.Id,
                    out DoorAssembly assembly)
                || !doorResolver.TryResolveDefinitionAsset(
                    assembly,
                    out DoorDefinitionAsset definitionAsset)
                || !IsPresentationCompatible(
                    assembly,
                    definitionAsset))
            {
                RemoveDoorAssemblyView(
                    changedAssembly.Id);
                return;
            }

            WallDisplaySlope displaySlope =
                default;

            Vector3 worldPosition =
                Vector3.zero;

            Vector3[] panelWorldPositions =
                new Vector3[assembly.SegmentCount];

            int sortingLayerId =
                0;

            int sortingOrder =
                int.MinValue;

            int rendererPriority =
                int.MinValue;

            Material sharedMaterial =
                null;

            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                if (!wallViews.TryGetValue(
                        assembly.GetEdge(index),
                        out WallSegmentView wallView)
                    || wallView == null)
                {
                    RemoveDoorAssemblyView(
                        assembly.Id);
                    return;
                }

                // Wall display height is presentation-only. Doors stay at
                // full authored height while their supporting walls switch
                // between full and low sprites for Up, Cut, and Down modes.

                if (index == 0)
                {
                    displaySlope =
                        wallView.CurrentDisplaySlope;

                    sortingLayerId =
                        wallView.SortingLayerId;

                    sharedMaterial =
                        wallView.SharedMaterial;
                }
                else if (wallView.CurrentDisplaySlope
                         != displaySlope)
                {
                    RemoveDoorAssemblyView(
                        assembly.Id);
                    return;
                }

                worldPosition +=
                    wallView.transform.position;

                panelWorldPositions[index] =
                    wallView.transform.position;

                sortingOrder =
                    Math.Max(
                        sortingOrder,
                        wallView.SortingOrder);

                rendererPriority =
                    Math.Max(
                        rendererPriority,
                        wallView.RendererPriority);
            }

            worldPosition /=
                assembly.SegmentCount;

            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                wallViews[assembly.GetEdge(index)]
                    .AlignDoorAperture(
                        worldPosition);
            }

            if (!doorAssemblyViews.TryGetValue(
                    assembly.Id,
                    out DoorAssemblyView doorView)
                || doorView == null)
            {
                GameObject viewObject =
                    new GameObject();

                viewObject.transform.SetParent(
                    wallViewParent,
                    false);

                doorView =
                    viewObject.AddComponent<DoorAssemblyView>();

                doorView.Initialize(
                    assembly.Id);

                doorAssemblyViews[assembly.Id] =
                    doorView;
            }

            int doorSortingOrder =
                sortingOrder
                + DoorAssemblyView
                    .SortingOrderOffsetFromSupportingWall;

            switch (definitionAsset.PresentationStyle)
            {
                case DoorPresentationStyle.SlidingFourPanel:
                    if (!doorResolver.TryResolveSprites(
                            assembly,
                            displaySlope,
                            out DoorAssemblySprites sprites))
                    {
                        RemoveDoorAssemblyView(
                            assembly.Id);
                        return;
                    }

                    Array.Sort(
                        panelWorldPositions,
                        ComparePanelWorldPositions);

                    DoorViewerSide viewerSide =
                        subscribedFoundationState == null
                            ? DoorViewerSide.Outside
                            : DoorViewerSideResolver.Resolve(
                                assembly.GetEdge(0),
                                viewHost.Projection,
                                subscribedFoundationState);

                    doorView.ApplyPresentation(
                        sprites,
                        displaySlope,
                        viewerSide,
                        panelWorldPositions,
                        worldPosition,
                        sortingLayerId,
                        doorSortingOrder,
                        rendererPriority + 1,
                        sharedMaterial,
                        Color.white);
                    break;

                case DoorPresentationStyle.HingedSinglePanel:
                    if (!doorResolver.TryResolveHingedSprites(
                            assembly,
                            displaySlope,
                            out HingedDoorSprites hingedSprites))
                    {
                        RemoveDoorAssemblyView(
                            assembly.Id);
                        return;
                    }

                    CellEdgeWorldPose closedPose =
                        CellEdgeWorldPose.Calculate(
                            assembly.GetEdge(0),
                            coordinateTilemap,
                            logicalLevel,
                            unityCellZ,
                            viewHost.Projection);

                    CellEdge openLogicalEdge =
                        HingedDoorSwingResolver
                            .ResolveOpenLogicalEdge(
                                assembly.GetEdge(0));

                    CellEdgeWorldPose openPose =
                        CellEdgeWorldPose.Calculate(
                            openLogicalEdge,
                            coordinateTilemap,
                            logicalLevel,
                            unityCellZ,
                            viewHost.Projection);

                    if (!doorResolver.TryResolveHingedSprites(
                            assembly,
                            openPose.DisplaySlope,
                            out HingedDoorSprites openSprites))
                    {
                        RemoveDoorAssemblyView(
                            assembly.Id);
                        return;
                    }

                    Vector3 wallWorldOffset =
                        panelWorldPositions[0]
                        - closedPose.Position;

                    Vector3 openPanelWorldPosition =
                        openPose.Position
                        + wallWorldOffset;

                    int openDoorSortingOrder =
                        WallRenderOrderResolver.ResolveWall(
                            openPose.DisplayEdge)
                        + DoorAssemblyView
                            .SortingOrderOffsetFromSupportingWall;

                    int openDoorRendererPriority =
                        WallRenderOrderResolver.ResolveWallPriority(
                            openPose.DisplayEdge)
                        + 1;

                    doorView.ApplyHingedPresentation(
                        hingedSprites,
                        openSprites.Door,
                        panelWorldPositions[0],
                        openPanelWorldPosition,
                        sortingLayerId,
                        doorSortingOrder,
                        openDoorSortingOrder,
                        rendererPriority + 1,
                        openDoorRendererPriority,
                        sharedMaterial,
                        Color.white);
                    break;

                case DoorPresentationStyle.StaticDoorway:
                    if (!doorResolver.TryResolveDoorwaySprites(
                            assembly,
                            displaySlope,
                            out DoorwaySprites doorwaySprites))
                    {
                        RemoveDoorAssemblyView(
                            assembly.Id);
                        return;
                    }

                    doorView.ApplyDoorwayPresentation(
                        doorwaySprites.Frame,
                        worldPosition,
                        sortingLayerId,
                        doorSortingOrder,
                        rendererPriority + 1,
                        sharedMaterial,
                        Color.white);
                    break;

                default:
                    RemoveDoorAssemblyView(
                        assembly.Id);
                    break;
            }
        }


        private static bool IsPresentationCompatible(
            DoorAssembly assembly,
            DoorDefinitionAsset definitionAsset)
        {
            return definitionAsset.PresentationStyle switch
            {
                DoorPresentationStyle.SlidingFourPanel =>
                    assembly.SegmentCount
                    == DoorAssemblyView.RequiredPanelCount,

                DoorPresentationStyle.HingedSinglePanel =>
                    assembly.SegmentCount
                    == DoorAssemblyView.RequiredHingedPanelCount,

                DoorPresentationStyle.StaticDoorway =>
                    assembly.SegmentCount
                    == definitionAsset.SegmentCount,

                _ => false
            };
        }


        private static int ComparePanelWorldPositions(
            Vector3 left,
            Vector3 right)
        {
            int comparison =
                left.x.CompareTo(
                    right.x);

            return comparison != 0
                ? comparison
                : right.y.CompareTo(
                    left.y);
        }


        private void RemoveDoorAssemblyView(
            DoorAssemblyId assemblyId)
        {
            if (!doorAssemblyViews.TryGetValue(
                    assemblyId,
                    out DoorAssemblyView view))
            {
                return;
            }

            doorAssemblyViews.Remove(
                assemblyId);

            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }


        private void ApplyWallPresentation(
            CellEdge edge,
            WallSegmentView view)
        {
            view.ApplyProjection(
                viewHost.Projection,
                ResolveWallHeight(edge));
        }


        private WallPresentationHeight ResolveWallHeight(
            CellEdge edge)
        {
            bool wallOccludesFoundation =
                foundationCutawayMap != null
                && foundationCutawayMap.ShouldLowerWall(
                    edge);

            return WallPresentationHeightResolver.Resolve(
                    CurrentDisplayMode,
                    wallOccludesFoundation);
        }


        private void RebuildFoundationCutawayMap()
        {
            if (subscribedFoundationState == null
                || viewHost == null
                || viewHost.Projection == null)
            {
                foundationCutawayMap =
                    null;
                return;
            }

            foundationCutawayMap =
                FoundationCutawayMap.Calculate(
                    viewHost.Projection,
                    subscribedFoundationState
                        .EnumerateFoundations(),
                    subscribedWallState != null
                        ? subscribedWallState.EnumerateWalls()
                        : Array.Empty<CellEdge>());
        }


        private void ClearAllViews()
        {
            foreach (
                DoorAssemblyView view
                in doorAssemblyViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            doorAssemblyViews.Clear();

            foreach (
                WallSegmentView view
                in wallViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            wallViews.Clear();
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallViewSystem has no GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (foundationRuntimeHost == null)
            {
                Debug.LogError(
                    "WallViewSystem has no FoundationRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "WallViewSystem has no Coordinate Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "WallViewSystem has no IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            if (wallSegmentPrefab == null)
            {
                Debug.LogError(
                    "WallViewSystem has no WallSegmentView prefab assigned.",
                    this);

                isValid = false;
            }

            if (!IsSupportedDisplayMode(startingDisplayMode))
            {
                Debug.LogError(
                    "WallViewSystem has an unknown starting display mode.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private static bool IsSupportedDisplayMode(
            WallDisplayMode displayMode)
        {
            return displayMode == WallDisplayMode.WallsUp
                || displayMode == WallDisplayMode.Cutaway
                || displayMode == WallDisplayMode.WallsDown;
        }
    }
}
