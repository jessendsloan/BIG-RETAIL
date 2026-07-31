using BigRetail.Construction.Unity.Input;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Cells
{
    /// <summary>
    /// Resolves the shared construction pointer to one logical grid cell.
    ///
    /// This component performs coordinate conversion only.
    /// It does not evaluate construction rules or modify map state.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class GridCellTargetResolver :
        MonoBehaviour
    {
        [Header("Pointer")]

        [SerializeField]
        private ConstructionPointerController
            pointerController;

        [SerializeField]
        private ConstructionUiInputGate uiInputGate;

        [SerializeField]
        private Camera targetCamera;


        [Header("Grid Mapping")]

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private int logicalLevel = 0;

        [SerializeField]
        private int unityCellZ = 0;


        public bool HasTarget { get; private set; }

        public GridPosition CurrentCell { get; private set; }

        public Vector3Int CurrentUnityCell
        {
            get;
            private set;
        }

        public Vector3 PointerWorldPosition
        {
            get;
            private set;
        }


        public Tilemap CoordinateTilemap =>
            coordinateTilemap;

        public int LogicalLevel =>
            logicalLevel;

        public int UnityCellZ =>
            unityCellZ;


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
            }
        }


        private void LateUpdate()
        {
            ResolveCurrentCell();
        }


        private void ResolveCurrentCell()
        {
            if (uiInputGate != null
                && uiInputGate.IsPointerOverConstructionUi)
            {
                HasTarget = false;
                return;
            }

            Ray pointerRay =
                targetCamera.ScreenPointToRay(
                    pointerController.ScreenPosition);

            Plane tilemapPlane =
                new Plane(
                    coordinateTilemap.transform.forward,
                    coordinateTilemap.transform.position);

            if (!tilemapPlane.Raycast(
                    pointerRay,
                    out float distance))
            {
                HasTarget = false;
                return;
            }

            PointerWorldPosition =
                pointerRay.GetPoint(distance);

            Vector3Int resolvedUnityCell =
                coordinateTilemap.WorldToCell(
                    PointerWorldPosition);

            CurrentUnityCell =
                new Vector3Int(
                    resolvedUnityCell.x,
                    resolvedUnityCell.y,
                    unityCellZ);

            CurrentCell =
                viewHost.Projection.ToLogicalCell(
                    new GridPosition(
                        CurrentUnityCell.x,
                        CurrentUnityCell.y,
                        logicalLevel));

            HasTarget = true;
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (pointerController == null)
            {
                Debug.LogError(
                    "GridCellTargetResolver has no " +
                    "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (targetCamera == null)
            {
                Debug.LogError(
                    "GridCellTargetResolver has no Camera assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "GridCellTargetResolver has no coordinate " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "GridCellTargetResolver has no " +
                    "IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
