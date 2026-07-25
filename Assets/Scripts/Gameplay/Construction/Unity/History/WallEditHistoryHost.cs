using System;
using BigRetail.Map.Unity;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.History
{
    /// <summary>
    /// Owns the wall-edit history for the currently active map.
    ///
    /// The history survives construction-tool changes but naturally
    /// resets when this gameplay scene and its map are replaced.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class WallEditHistoryHost : MonoBehaviour
    {
        [SerializeField]
        private GridMapHost mapHost;


        public bool IsInitialized =>
            History != null;

        public WallEditHistory History
        {
            get;
            private set;
        }


        public event Action Initialized;


        private void Awake()
        {
            TryInitialize();
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "WallEditHistoryHost could not initialize because " +
                    "GridMapHost has not produced a " +
                    "WallConstructionService.",
                    this);
            }
        }


        /// <summary>
        /// Creates the history once the authoritative wall service
        /// becomes available.
        /// </summary>
        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                return false;
            }

            History =
                new WallEditHistory(
                    mapHost.WallConstruction);

            Initialized?.Invoke();

            Debug.Log(
                "Wall edit history initialized.",
                this);

            return true;
        }


        [ContextMenu("Clear Wall History")]
        public void ClearHistory()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Wall history can only be cleared during Play Mode.",
                    this);

                return;
            }

            if (!TryInitialize())
            {
                Debug.LogError(
                    "Wall history could not be initialized.",
                    this);

                return;
            }

            History.Clear();

            Debug.Log(
                "Wall edit history cleared.",
                this);
        }


        private void OnValidate()
        {
            if (mapHost == null)
            {
                Debug.LogWarning(
                    "WallEditHistoryHost requires a GridMapHost.",
                    this);
            }
        }
    }
}