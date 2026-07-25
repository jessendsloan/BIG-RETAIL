using System;
using BigRetail.Map.Construction;
using UnityEngine;

namespace BigRetail.Construction.Unity.History
{
    /// <summary>
    /// Owns the neutral construction history for the active gameplay
    /// session.
    ///
    /// The selected mode is fixed when this host initializes. Tool
    /// switching does not affect history; replacing the gameplay
    /// session naturally creates a new host and a fresh history.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class ConstructionHistoryHost : MonoBehaviour
    {
        [SerializeField]
        private ConstructionHistoryMode historyMode =
            ConstructionHistoryMode.Standard;


        public bool IsInitialized =>
            History != null;

        public ConstructionHistoryMode Mode =>
            IsInitialized
                ? History.Mode
                : historyMode;

        public ConstructionHistory History
        {
            get;
            private set;
        }


        public event Action Initialized;


        private void Awake()
        {
            TryInitialize();
        }


        /// <summary>
        /// Selects the mode before the host initializes.
        ///
        /// This supports future game-mode bootstrapping while keeping
        /// history policy immutable during a running session.
        /// </summary>
        public bool TryConfigureMode(
            ConstructionHistoryMode mode)
        {
            if (IsInitialized)
            {
                return false;
            }

            historyMode = mode;

            return true;
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            try
            {
                History =
                    new ConstructionHistory(
                        historyMode);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Debug.LogException(
                    exception,
                    this);

                return false;
            }

            Initialized?.Invoke();

            Debug.Log(
                $"Construction history initialized in " +
                $"{History.Mode} mode.",
                this);

            return true;
        }


        [ContextMenu("Clear Construction History")]
        public void ClearHistory()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Construction history can only be cleared during " +
                    "Play Mode.",
                    this);

                return;
            }

            if (!TryInitialize())
            {
                Debug.LogError(
                    "Construction history could not be initialized.",
                    this);

                return;
            }

            History.Clear();

            Debug.Log(
                "Construction history cleared.",
                this);
        }
    }
}
