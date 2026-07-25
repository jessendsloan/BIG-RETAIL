using System;
using BigRetail.Construction.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Tools
{
    /// <summary>
    /// Ensures that only one construction tool owns the shared
    /// construction input at a time.
    ///
    /// Individual tools still own their interaction behavior.
    /// This coordinator owns tool selection only.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(300)]
    public sealed class ConstructionToolCoordinator :
        MonoBehaviour
    {
        [Header("Tools")]

        [SerializeField]
        private WallConstructionToolController
            wallConstructionTool;

        [SerializeField]
        private WallDemolitionToolController
            wallDemolitionTool;


        [Header("Starting State")]

        [SerializeField]
        private ConstructionToolMode startingMode =
            ConstructionToolMode.BuildWalls;


        public ConstructionToolMode CurrentMode
        {
            get;
            private set;
        } = ConstructionToolMode.None;


        public event Action<ConstructionToolMode> ModeChanged;


        private bool isInitialized;

        private bool isApplyingMode;


        private void Awake()
        {
            isInitialized =
                ValidateReferences();
        }


        private void OnEnable()
        {
            if (wallConstructionTool != null)
            {
                wallConstructionTool.ToolActiveChanged +=
                    HandleWallConstructionActivityChanged;
            }

            if (wallDemolitionTool != null)
            {
                wallDemolitionTool.ToolActiveChanged +=
                    HandleWallDemolitionActivityChanged;
            }
        }


        private void Start()
        {
            if (!isInitialized)
            {
                enabled = false;
                return;
            }

            ApplyMode(
                startingMode,
                forceRefresh: true);
        }


        public void SetMode(
            ConstructionToolMode mode)
        {
            ApplyMode(
                mode,
                forceRefresh: false);
        }


        [ContextMenu("Activate Wall Construction")]
        public void ActivateWallConstruction()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            SetMode(
                ConstructionToolMode.BuildWalls);
        }


        [ContextMenu("Activate Wall Demolition")]
        public void ActivateWallDemolition()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            SetMode(
                ConstructionToolMode.DemolishWalls);
        }


        [ContextMenu("Deactivate Construction Tools")]
        public void DeactivateConstructionTools()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            SetMode(
                ConstructionToolMode.None);
        }


        private void ApplyMode(
            ConstructionToolMode mode,
            bool forceRefresh)
        {
            if (!isInitialized)
            {
                return;
            }

            if (!forceRefresh
                && CurrentMode == mode)
            {
                return;
            }

            ConstructionToolMode previousMode =
                CurrentMode;

            isApplyingMode = true;

            try
            {
                SetWallConstructionActive(
                    mode
                    == ConstructionToolMode.BuildWalls);

                SetWallDemolitionActive(
                    mode
                    == ConstructionToolMode.DemolishWalls);

                CurrentMode =
                    mode;
            }
            finally
            {
                isApplyingMode = false;
            }

            if (forceRefresh
                || previousMode != CurrentMode)
            {
                ModeChanged?.Invoke(
                    CurrentMode);

                Debug.Log(
                    $"Construction tool mode changed to " +
                    $"{CurrentMode}.",
                    this);
            }
        }


        private void SetWallConstructionActive(
            bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                wallConstructionTool.ActivateTool();
            }
            else
            {
                wallConstructionTool.DeactivateTool();
            }
        }


        private void SetWallDemolitionActive(
            bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                wallDemolitionTool.ActivateTool();
            }
            else
            {
                wallDemolitionTool.DeactivateTool();
            }
        }


        private void HandleWallConstructionActivityChanged(
            bool isActive)
        {
            if (isApplyingMode
                || !isInitialized)
            {
                return;
            }

            if (isActive)
            {
                ApplyMode(
                    ConstructionToolMode.BuildWalls,
                    forceRefresh: false);

                return;
            }

            if (CurrentMode
                == ConstructionToolMode.BuildWalls)
            {
                ApplyMode(
                    ConstructionToolMode.None,
                    forceRefresh: false);
            }
        }


        private void HandleWallDemolitionActivityChanged(
            bool isActive)
        {
            if (isApplyingMode
                || !isInitialized)
            {
                return;
            }

            if (isActive)
            {
                ApplyMode(
                    ConstructionToolMode.DemolishWalls,
                    forceRefresh: false);

                return;
            }

            if (CurrentMode
                == ConstructionToolMode.DemolishWalls)
            {
                ApplyMode(
                    ConstructionToolMode.None,
                    forceRefresh: false);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (wallConstructionTool == null)
            {
                Debug.LogError(
                    "ConstructionToolCoordinator has no " +
                    "WallConstructionToolController assigned.",
                    this);

                isValid = false;
            }

            if (wallDemolitionTool == null)
            {
                Debug.LogError(
                    "ConstructionToolCoordinator has no " +
                    "WallDemolitionToolController assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private bool RequirePlayMode()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning(
                "Construction tools can only be changed " +
                "during Play Mode.",
                this);

            return false;
        }


        private void OnDisable()
        {
            if (wallConstructionTool != null)
            {
                wallConstructionTool.ToolActiveChanged -=
                    HandleWallConstructionActivityChanged;
            }

            if (wallDemolitionTool != null)
            {
                wallDemolitionTool.ToolActiveChanged -=
                    HandleWallDemolitionActivityChanged;
            }
        }
    }
}