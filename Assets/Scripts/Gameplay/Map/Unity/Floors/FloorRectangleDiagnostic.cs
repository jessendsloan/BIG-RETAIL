using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Floors
{
    public enum FloorDiagnosticStartupAction
    {
        None,
        BuildRectangle,
        ClearRectangle,
        ToggleRectangle
    }


    /// <summary>
    /// Temporary development tool that constructs or clears one
    /// rectangular floor area beginning at the cell underneath
    /// this GameObject.
    ///
    /// Remove this diagnostic after the real floor tool is working.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class FloorRectangleDiagnostic :
        MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;


        [Header("Coordinate Mapping")]

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private int logicalLevel = 0;


        [Header("Rectangle")]

        [SerializeField, Min(1)]
        private int width = 6;

        [SerializeField, Min(1)]
        private int height = 4;


        [Header("Automatic Test")]

        [SerializeField]
        private FloorDiagnosticStartupAction startupAction =
            FloorDiagnosticStartupAction.None;

        [SerializeField]
        private bool logResults = true;


        private void Start()
        {
            switch (startupAction)
            {
                case FloorDiagnosticStartupAction.None:
                    break;

                case FloorDiagnosticStartupAction.BuildRectangle:
                    BuildRectangle();
                    break;

                case FloorDiagnosticStartupAction.ClearRectangle:
                    ClearRectangle();
                    break;

                case FloorDiagnosticStartupAction.ToggleRectangle:
                    ToggleRectangle();
                    break;

                default:
                    Debug.LogError(
                        $"Unsupported floor diagnostic action: " +
                        $"{startupAction}.",
                        this);
                    break;
            }
        }


        [ContextMenu("Build Floor Rectangle")]
        public void BuildRectangle()
        {
            if (!TryCreatePlan(
                    out Vector3Int startUnityCell,
                    out RectangularCellAreaPlanResult plan))
            {
                return;
            }

            FloorEnsureResult result =
                floorRuntimeHost.FloorConstruction
                    .TryEnsureFloors(
                        plan.Cells);

            if (!logResults)
            {
                return;
            }

            Debug.Log(
                $"Diagnostic floor build processed from Unity cell " +
                $"{startUnityCell}. Requested: " +
                $"{result.RequestedCount}. Created: " +
                $"{result.ChangedCount}. Existing: " +
                $"{result.AlreadyExistingCount}. Skipped: " +
                $"{result.SkippedCount}.",
                this);
        }


        [ContextMenu("Clear Floor Rectangle")]
        public void ClearRectangle()
        {
            if (!TryCreatePlan(
                    out Vector3Int startUnityCell,
                    out RectangularCellAreaPlanResult plan))
            {
                return;
            }

            FloorClearResult result =
                floorRuntimeHost.FloorConstruction
                    .TryClearFloors(
                        plan.Cells);

            if (!logResults)
            {
                return;
            }

            Debug.Log(
                $"Diagnostic floor clearing processed from Unity cell " +
                $"{startUnityCell}. Requested: " +
                $"{result.RequestedCount}. Removed: " +
                $"{result.RemovedCount}. Already empty: " +
                $"{result.AlreadyEmptyCount}.",
                this);
        }


        [ContextMenu("Toggle Floor Rectangle")]
        public void ToggleRectangle()
        {
            if (!TryCreatePlan(
                    out _,
                    out RectangularCellAreaPlanResult plan))
            {
                return;
            }

            bool containsAnyFloor = false;

            for (int index = 0;
                 index < plan.CellCount;
                 index++)
            {
                if (!floorRuntimeHost.FloorConstruction
                    .HasFloor(plan.Cells[index]))
                {
                    continue;
                }

                containsAnyFloor = true;
                break;
            }

            if (containsAnyFloor)
            {
                ClearRectangle();
            }
            else
            {
                BuildRectangle();
            }
        }


        private bool TryCreatePlan(
            out Vector3Int startUnityCell,
            out RectangularCellAreaPlanResult plan)
        {
            startUnityCell = default;
            plan = default;

            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Floor diagnostics require Play Mode.",
                    this);

                return false;
            }

            if (!ValidateReferences())
            {
                return false;
            }

            if (!floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorConstruction == null)
            {
                Debug.LogError(
                    "FloorRectangleDiagnostic could not access an " +
                    "initialized FloorConstructionService.",
                    this);

                return false;
            }

            startUnityCell =
                coordinateTilemap.WorldToCell(
                    transform.position);

            GridPosition startCell =
                new GridPosition(
                    startUnityCell.x,
                    startUnityCell.y,
                    logicalLevel);

            GridPosition endCell =
                startCell.Offset(
                    width - 1,
                    height - 1);

            plan =
                RectangularCellAreaPlanner.Plan(
                    startCell,
                    endCell);

            if (!plan.Succeeded)
            {
                Debug.LogError(
                    $"Floor rectangle planning failed: " +
                    $"{plan.Failure}.",
                    this);

                return false;
            }

            return true;
        }


        [ContextMenu("Snap Marker To Current Cell Center")]
        private void SnapMarkerToCurrentCellCenter()
        {
            if (coordinateTilemap == null)
            {
                Debug.LogWarning(
                    "Assign a Coordinate Tilemap before snapping.",
                    this);

                return;
            }

            Vector3Int unityCell =
                coordinateTilemap.WorldToCell(
                    transform.position);

            transform.position =
                coordinateTilemap.GetCellCenterWorld(
                    unityCell);
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (floorRuntimeHost == null)
            {
                Debug.LogError(
                    "FloorRectangleDiagnostic has no " +
                    "FloorRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "FloorRectangleDiagnostic has no coordinate " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnValidate()
        {
            width =
                Mathf.Max(
                    width,
                    1);

            height =
                Mathf.Max(
                    height,
                    1);
        }
    }
}