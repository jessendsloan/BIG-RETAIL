using UnityEngine;

namespace BigRetail.Map.Unity
{
    /// <summary>
    /// Gives a location-specific scene anchor a stable identity and logical
    /// map cell. Scenario data refers to the marker ID rather than a scene
    /// object or a transient world position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocationMarkerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string markerId = string.Empty;

        [SerializeField]
        private Vector3Int logicalCell;

        [Tooltip(
            "World-space adjustment applied after the logical cell is "
            + "projected into the active isometric view.")]
        [SerializeField]
        private Vector3 worldOffset;


        public string MarkerId =>
            markerId != null
                ? markerId.Trim()
                : string.Empty;

        public Vector3Int LogicalCell => logicalCell;

        public Vector3 WorldOffset => worldOffset;


        private void OnValidate()
        {
            markerId = MarkerId;
        }
    }
}
