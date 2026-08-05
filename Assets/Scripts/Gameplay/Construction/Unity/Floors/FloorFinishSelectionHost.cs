using System;
using System.Collections.Generic;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.Floors;
using UnityEngine;

namespace BigRetail.Construction.Unity.Floors
{
    /// <summary>
    /// Owns the Floor finish currently selected by the player-facing Floor
    /// construction tool.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(125)]
    public sealed class FloorFinishSelectionHost :
        MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;


        [Header("Starting Selection")]

        [Tooltip(
            "Finish selected when Gameplay begins. When empty, the catalog "
            + "default is selected.")]
        [SerializeField]
        private FloorFinishAsset startingFinish;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logSelectionChanges =
            true;


        public bool IsInitialized { get; private set; }

        public FloorFinishId SelectedFinishId { get; private set; }

        public FloorFinishAsset SelectedFinishAsset { get; private set; }


        public event Action<FloorFinishId> SelectedFinishChanged;


        private void Awake()
        {
            if (floorRuntimeHost == null)
            {
                Debug.LogError(
                    "FloorFinishSelectionHost has no FloorRuntimeHost assigned.",
                    this);

                enabled = false;
            }
        }


        private void OnEnable()
        {
            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized +=
                    HandleFloorRuntimeInitialized;
            }
        }


        private void Start()
        {
            if (floorRuntimeHost != null
                && floorRuntimeHost.IsInitialized)
            {
                InitializeSelection();
            }
        }


        public IEnumerable<FloorFinishAsset> EnumerateAvailableFinishes()
        {
            if (!IsInitialized
                || floorRuntimeHost == null
                || floorRuntimeHost.FloorFinishAssets == null)
            {
                return Array.Empty<FloorFinishAsset>();
            }

            return floorRuntimeHost
                .FloorFinishAssets
                .EnumerateAssets();
        }


        public bool SelectFinish(
            FloorFinishAsset finishAsset)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning(
                    "The Floor finish selection is not initialized.",
                    this);

                return false;
            }

            if (finishAsset == null)
            {
                Debug.LogWarning(
                    "A null Floor finish cannot be selected.",
                    this);

                return false;
            }

            FloorFinishId finishId;

            try
            {
                finishId =
                    finishAsset.Id;
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    finishAsset);

                return false;
            }

            return SelectFinish(
                finishId);
        }


        public bool SelectFinish(
            FloorFinishId finishId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning(
                    "The Floor finish selection is not initialized.",
                    this);

                return false;
            }

            if (!floorRuntimeHost.FloorFinishCatalog.Contains(
                    finishId)
                || !floorRuntimeHost.FloorFinishAssets.TryGetAsset(
                    finishId,
                    out FloorFinishAsset finishAsset))
            {
                Debug.LogWarning(
                    $"Floor finish '{finishId}' is not registered in the "
                    + "active catalog.",
                    this);

                return false;
            }

            if (SelectedFinishId == finishId)
            {
                return true;
            }

            SelectedFinishId =
                finishId;

            SelectedFinishAsset =
                finishAsset;

            SelectedFinishChanged?.Invoke(
                SelectedFinishId);

            if (logSelectionChanges)
            {
                Debug.Log(
                    $"Floor tool selected finish '{SelectedFinishId}'.",
                    this);
            }

            return true;
        }


        [ContextMenu("Select Catalog Default Finish")]
        public void SelectDefaultFinish()
        {
            if (!IsInitialized)
            {
                return;
            }

            SelectFinish(
                floorRuntimeHost
                    .FloorFinishCatalog
                    .DefaultFinishId);
        }


        [ContextMenu("Select Authored Starting Finish")]
        public void SelectStartingFinish()
        {
            if (!IsInitialized)
            {
                return;
            }

            FloorFinishAsset finishAsset =
                startingFinish != null
                    ? startingFinish
                    : floorRuntimeHost
                        .FloorFinishAssets
                        .DefaultFinish;

            SelectFinish(
                finishAsset);
        }


        private void HandleFloorRuntimeInitialized(
            FloorRuntimeHost initializedHost)
        {
            InitializeSelection();
        }


        private void InitializeSelection()
        {
            if (IsInitialized
                || floorRuntimeHost == null
                || !floorRuntimeHost.IsInitialized
                || floorRuntimeHost.FloorFinishCatalog == null
                || floorRuntimeHost.FloorFinishAssets == null)
            {
                return;
            }

            IsInitialized = true;

            FloorFinishAsset initialFinish =
                startingFinish != null
                    ? startingFinish
                    : floorRuntimeHost
                        .FloorFinishAssets
                        .DefaultFinish;

            if (!SelectFinish(
                    initialFinish))
            {
                IsInitialized = false;
                enabled = false;

                Debug.LogError(
                    "FloorFinishSelectionHost could not select its "
                    + "starting finish.",
                    this);
            }
        }


        private void OnDisable()
        {
            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized -=
                    HandleFloorRuntimeInitialized;
            }
        }
    }
}
