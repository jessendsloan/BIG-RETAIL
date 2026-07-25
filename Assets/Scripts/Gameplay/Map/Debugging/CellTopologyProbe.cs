#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Debugging
{
    /// <summary>
    /// Visualizes the four neighboring Unity Tilemap cells around
    /// one selected cell.
    ///
    /// This is a temporary diagnostic tool. It does not modify
    /// Tilemap contents or create gameplay data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CellTopologyProbe : MonoBehaviour
    {
        [Header("Reference")]

        [Tooltip(
            "The Tilemap whose cell-coordinate orientation " +
            "is being inspected.")]
        [SerializeField]
        private Tilemap referenceTilemap;


        [Header("Probe Cell")]

        [Tooltip(
            "When enabled, the probe uses the Tilemap cell underneath " +
            "this GameObject's Transform.")]
        [SerializeField]
        private bool deriveCellFromTransform = true;

        [Tooltip(
            "Used when Derive Cell From Transform is disabled. " +
            "These are Unity Tilemap cell coordinates.")]
        [SerializeField]
        private Vector3Int probeCell = Vector3Int.zero;


        [Header("Markers")]

        [SerializeField, Min(0.01f)]
        private float centerMarkerRadius = 0.08f;

        [SerializeField, Min(0.01f)]
        private float neighborMarkerRadius = 0.06f;

        [SerializeField, Min(0.01f)]
        private float edgeMarkerSize = 0.06f;


        [Header("Labels")]

        [Tooltip(
            "How far each label is pushed outward from its marker.")]
        [SerializeField, Min(0f)]
        private float labelDistance = 0.28f;

        [Tooltip(
            "Additional world-space height applied to every label.")]
        [SerializeField, Min(0f)]
        private float labelHeightOffset = 0.08f;


#if UNITY_EDITOR
        private static GUIStyle labelStyle;

        private static GUIStyle LabelStyle
        {
            get
            {
                if (labelStyle == null)
                {
                    labelStyle =
                        new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment =
                                TextAnchor.MiddleCenter,

                            fontSize = 11
                        };

                    labelStyle.normal.textColor =
                        Color.white;
                }

                return labelStyle;
            }
        }
#endif


        private void OnDrawGizmosSelected()
        {
            if (referenceTilemap == null)
            {
                return;
            }

            Vector3Int selectedCell =
                ResolveProbeCell();

            Vector3Int positiveXCell =
                selectedCell + Vector3Int.right;

            Vector3Int negativeXCell =
                selectedCell + Vector3Int.left;

            Vector3Int positiveYCell =
                selectedCell + Vector3Int.up;

            Vector3Int negativeYCell =
                selectedCell + Vector3Int.down;


            Vector3 center =
                referenceTilemap.GetCellCenterWorld(
                    selectedCell);

            Vector3 positiveX =
                referenceTilemap.GetCellCenterWorld(
                    positiveXCell);

            Vector3 negativeX =
                referenceTilemap.GetCellCenterWorld(
                    negativeXCell);

            Vector3 positiveY =
                referenceTilemap.GetCellCenterWorld(
                    positiveYCell);

            Vector3 negativeY =
                referenceTilemap.GetCellCenterWorld(
                    negativeYCell);


            DrawConnection(
                center,
                positiveX,
                Color.red);

            DrawConnection(
                center,
                negativeX,
                Color.cyan);

            DrawConnection(
                center,
                positiveY,
                Color.green);

            DrawConnection(
                center,
                negativeY,
                Color.magenta);


            Gizmos.color = Color.white;
            Gizmos.DrawSphere(
                center,
                centerMarkerRadius);


            DrawNeighborMarker(
                positiveX,
                Color.red);

            DrawNeighborMarker(
                negativeX,
                Color.cyan);

            DrawNeighborMarker(
                positiveY,
                Color.green);

            DrawNeighborMarker(
                negativeY,
                Color.magenta);


#if UNITY_EDITOR
            DrawLabel(
                center,
                center,
                $"CENTER\nUnity Cell {selectedCell}");

            DrawLabel(
                center,
                positiveX,
                $"X + 1\n{positiveXCell}");

            DrawLabel(
                center,
                negativeX,
                $"X - 1\n{negativeXCell}");

            DrawLabel(
                center,
                positiveY,
                $"Y + 1\n{positiveYCell}");

            DrawLabel(
                center,
                negativeY,
                $"Y - 1\n{negativeYCell}");
#endif
        }


        private Vector3Int ResolveProbeCell()
        {
            if (deriveCellFromTransform)
            {
                return referenceTilemap.WorldToCell(
                    transform.position);
            }

            return probeCell;
        }


        private void DrawConnection(
            Vector3 start,
            Vector3 end,
            Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(start, end);

            Vector3 midpoint =
                Vector3.Lerp(start, end, 0.5f);

            Gizmos.DrawCube(
                midpoint,
                Vector3.one * edgeMarkerSize);
        }


        private void DrawNeighborMarker(
            Vector3 position,
            Color color)
        {
            Gizmos.color = color;

            Gizmos.DrawSphere(
                position,
                neighborMarkerRadius);
        }


#if UNITY_EDITOR
        private void DrawLabel(
            Vector3 center,
            Vector3 markerPosition,
            string text)
        {
            Vector3 outwardDirection;

            if (markerPosition == center)
            {
                outwardDirection = Vector3.up;
            }
            else
            {
                outwardDirection =
                    (markerPosition - center).normalized;
            }

            Vector3 labelPosition =
                markerPosition
                + outwardDirection * labelDistance
                + Vector3.up * labelHeightOffset;

            Camera sceneCamera =
                SceneView.currentDrawingSceneView != null
                    ? SceneView.currentDrawingSceneView.camera
                    : null;

            if (sceneCamera != null)
            {
                Vector3 towardCamera =
                    (sceneCamera.transform.position
                    - labelPosition).normalized;

                labelPosition +=
                    towardCamera * 0.02f;
            }

            Handles.Label(
                labelPosition,
                text,
                LabelStyle);
        }
#endif


        [ContextMenu("Snap Marker To Current Cell Center")]
        private void SnapMarkerToCurrentCellCenter()
        {
            if (referenceTilemap == null)
            {
                Debug.LogWarning(
                    "Assign a Reference Tilemap before " +
                    "snapping the probe.",
                    this);

                return;
            }

            Vector3Int selectedCell =
                ResolveProbeCell();

            transform.position =
                referenceTilemap.GetCellCenterWorld(
                    selectedCell);
        }


        private void OnValidate()
        {
            centerMarkerRadius =
                Mathf.Max(centerMarkerRadius, 0.01f);

            neighborMarkerRadius =
                Mathf.Max(neighborMarkerRadius, 0.01f);

            edgeMarkerSize =
                Mathf.Max(edgeMarkerSize, 0.01f);

            labelDistance =
                Mathf.Max(labelDistance, 0f);

            labelHeightOffset =
                Mathf.Max(labelHeightOffset, 0f);
        }
    }
}