using System;
using BigRetail.Construction.Unity.Floors;
using BigRetail.Construction.Unity.Foundations;
using BigRetail.Construction.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Tools
{
    /// <summary>
    /// Ensures that exactly one construction tool owns the shared
    /// construction pointer and input actions at a time.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(300)]
    public sealed class ConstructionToolCoordinator :
        MonoBehaviour
    {
        [Header("Foundation Tools")]

        [SerializeField]
        private FoundationConstructionToolController
            foundationConstructionTool;


        [Header("Wall Tools")]

        [SerializeField]
        private WallConstructionToolController
            wallConstructionTool;

        [SerializeField]
        private WallDemolitionToolController
            wallDemolitionTool;


        [Header("Floor Tools")]

        [SerializeField]
        private FloorConstructionToolController
            floorConstructionTool;

        [SerializeField]
        private FloorDemolitionToolController
            floorDemolitionTool;


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
            if (foundationConstructionTool != null)
            {
                foundationConstructionTool.ToolActiveChanged +=
                    HandleFoundationConstructionActivityChanged;
            }

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

            if (floorConstructionTool != null)
            {
                floorConstructionTool.ToolActiveChanged +=
                    HandleFloorConstructionActivityChanged;
            }

            if (floorDemolitionTool != null)
            {
                floorDemolitionTool.ToolActiveChanged +=
                    HandleFloorDemolitionActivityChanged;
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


        /// <summary>
        /// Cancels any in-progress construction gesture without changing
        /// the selected tool. View rotation calls this before the map
        /// presentation changes.
        /// </summary>
        public void CancelActiveGesture()
        {
            switch (CurrentMode)
            {
                case ConstructionToolMode.BuildFoundations:
                    foundationConstructionTool
                        .CancelCurrentGesture();
                    break;

                case ConstructionToolMode.BuildWalls:
                    wallConstructionTool
                        .CancelCurrentGesture();
                    break;

                case ConstructionToolMode.DemolishWalls:
                    wallDemolitionTool
                        .CancelCurrentGesture();
                    break;

                case ConstructionToolMode.BuildFloors:
                    floorConstructionTool
                        .CancelCurrentGesture();
                    break;

                case ConstructionToolMode.DemolishFloors:
                    floorDemolitionTool
                        .CancelCurrentGesture();
                    break;
            }
        }


        [ContextMenu("Activate Foundation Construction")]
        public void ActivateFoundationConstruction()
        {
            if (RequirePlayMode())
            {
                SetMode(
                    ConstructionToolMode.BuildFoundations);
            }
        }


        [ContextMenu("Activate Wall Construction")]
        public void ActivateWallConstruction()
        {
            if (RequirePlayMode())
            {
                SetMode(
                    ConstructionToolMode.BuildWalls);
            }
        }


        [ContextMenu("Activate Wall Demolition")]
        public void ActivateWallDemolition()
        {
            if (RequirePlayMode())
            {
                SetMode(
                    ConstructionToolMode.DemolishWalls);
            }
        }


        [ContextMenu("Activate Floor Construction")]
        public void ActivateFloorConstruction()
        {
            if (RequirePlayMode())
            {
                SetMode(
                    ConstructionToolMode.BuildFloors);
            }
        }


        [ContextMenu("Activate Floor Demolition")]
        public void ActivateFloorDemolition()
        {
            if (RequirePlayMode())
            {
                SetMode(
                    ConstructionToolMode.DemolishFloors);
            }
        }


        [ContextMenu("Deactivate Construction Tools")]
        public void DeactivateConstructionTools()
        {
            if (RequirePlayMode())
            {
                SetMode(
                    ConstructionToolMode.None);
            }
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
                SetFoundationConstructionActive(
                    mode
                    == ConstructionToolMode.BuildFoundations);

                SetWallConstructionActive(
                    mode
                    == ConstructionToolMode.BuildWalls);

                SetWallDemolitionActive(
                    mode
                    == ConstructionToolMode.DemolishWalls);

                SetFloorConstructionActive(
                    mode
                    == ConstructionToolMode.BuildFloors);

                SetFloorDemolitionActive(
                    mode
                    == ConstructionToolMode.DemolishFloors);

                CurrentMode = mode;
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


        private void SetFoundationConstructionActive(
            bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                foundationConstructionTool.ActivateTool();
            }
            else
            {
                foundationConstructionTool.DeactivateTool();
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


        private void SetFloorConstructionActive(
            bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                floorConstructionTool.ActivateTool();
            }
            else
            {
                floorConstructionTool.DeactivateTool();
            }
        }


        private void SetFloorDemolitionActive(
            bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                floorDemolitionTool.ActivateTool();
            }
            else
            {
                floorDemolitionTool.DeactivateTool();
            }
        }


        private void HandleFoundationConstructionActivityChanged(
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
                    ConstructionToolMode.BuildFoundations,
                    forceRefresh: false);

                return;
            }

            if (CurrentMode
                == ConstructionToolMode.BuildFoundations)
            {
                ApplyMode(
                    ConstructionToolMode.None,
                    forceRefresh: false);
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


        private void HandleFloorConstructionActivityChanged(
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
                    ConstructionToolMode.BuildFloors,
                    forceRefresh: false);

                return;
            }

            if (CurrentMode
                == ConstructionToolMode.BuildFloors)
            {
                ApplyMode(
                    ConstructionToolMode.None,
                    forceRefresh: false);
            }
        }


        private void HandleFloorDemolitionActivityChanged(
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
                    ConstructionToolMode.DemolishFloors,
                    forceRefresh: false);

                return;
            }

            if (CurrentMode
                == ConstructionToolMode.DemolishFloors)
            {
                ApplyMode(
                    ConstructionToolMode.None,
                    forceRefresh: false);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (foundationConstructionTool == null)
            {
                Debug.LogError(
                    "ConstructionToolCoordinator has no " +
                    "FoundationConstructionToolController assigned.",
                    this);

                isValid = false;
            }

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

            if (floorConstructionTool == null)
            {
                Debug.LogError(
                    "ConstructionToolCoordinator has no " +
                    "FloorConstructionToolController assigned.",
                    this);

                isValid = false;
            }

            if (floorDemolitionTool == null)
            {
                Debug.LogError(
                    "ConstructionToolCoordinator has no " +
                    "FloorDemolitionToolController assigned.",
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
            if (foundationConstructionTool != null)
            {
                foundationConstructionTool.ToolActiveChanged -=
                    HandleFoundationConstructionActivityChanged;
            }

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

            if (floorConstructionTool != null)
            {
                floorConstructionTool.ToolActiveChanged -=
                    HandleFloorConstructionActivityChanged;
            }

            if (floorDemolitionTool != null)
            {
                floorDemolitionTool.ToolActiveChanged -=
                    HandleFloorDemolitionActivityChanged;
            }
        }
    }
}
