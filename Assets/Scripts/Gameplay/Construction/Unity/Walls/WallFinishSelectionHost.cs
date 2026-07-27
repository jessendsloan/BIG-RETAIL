using System;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Owns the wall finish currently selected by the player-facing wall tool.
    ///
    /// This is tool state rather than map state. Future UI buttons can call
    /// SelectFinish without changing the runtime wall or finish services.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(125)]
    public sealed class WallFinishSelectionHost : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;


        [Header("Starting Selection")]

        [Tooltip(
            "Finish selected when Gameplay begins. When empty, the catalog "
            + "default is selected.")]
        [SerializeField]
        private WallFinishAsset startingFinish;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logSelectionChanges =
            true;


        public bool IsInitialized { get; private set; }

        public WallFinishId SelectedFinishId { get; private set; }

        public WallFinishAsset SelectedFinishAsset { get; private set; }


        public event Action<WallFinishId> SelectedFinishChanged;


        private void Awake()
        {
            if (mapHost == null)
            {
                Debug.LogError(
                    "WallFinishSelectionHost has no GridMapHost assigned.",
                    this);

                enabled = false;
            }
        }


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }
        }


        private void Start()
        {
            if (mapHost != null
                && mapHost.IsInitialized)
            {
                InitializeSelection();
            }
        }


        public bool SelectFinish(
            WallFinishAsset finishAsset)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning(
                    "The wall finish selection is not initialized.",
                    this);
                return false;
            }

            if (finishAsset == null)
            {
                Debug.LogWarning(
                    "A null wall finish cannot be selected.",
                    this);
                return false;
            }

            WallFinishId finishId;

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
            WallFinishId finishId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning(
                    "The wall finish selection is not initialized.",
                    this);
                return false;
            }

            if (!mapHost.WallFinishCatalog.Contains(
                    finishId)
                || !mapHost.WallFinishAssets.TryGetAsset(
                    finishId,
                    out WallFinishAsset finishAsset))
            {
                Debug.LogWarning(
                    $"Wall finish '{finishId}' is not registered in the active catalog.",
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
                    $"Wall tool selected finish '{SelectedFinishId}'.",
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
                mapHost.WallFinishCatalog.DefaultFinishId);
        }


        [ContextMenu("Select Authored Starting Finish")]
        public void SelectStartingFinish()
        {
            if (!IsInitialized)
            {
                return;
            }

            WallFinishAsset finishAsset =
                startingFinish != null
                    ? startingFinish
                    : mapHost.WallFinishAssets.DefaultFinish;

            SelectFinish(
                finishAsset);
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            InitializeSelection();
        }


        private void InitializeSelection()
        {
            if (IsInitialized
                || mapHost == null
                || !mapHost.IsInitialized
                || mapHost.WallFinishCatalog == null
                || mapHost.WallFinishAssets == null)
            {
                return;
            }

            IsInitialized = true;

            WallFinishAsset initialFinish =
                startingFinish != null
                    ? startingFinish
                    : mapHost.WallFinishAssets.DefaultFinish;

            if (!SelectFinish(
                    initialFinish))
            {
                IsInitialized = false;
                enabled = false;

                Debug.LogError(
                    "WallFinishSelectionHost could not select its starting finish.",
                    this);
            }
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }
        }
    }
}
