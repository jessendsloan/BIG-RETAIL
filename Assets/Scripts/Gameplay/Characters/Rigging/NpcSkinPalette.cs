using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    [CreateAssetMenu(
        fileName = "SkinPalette",
        menuName = "Big Retail/Characters/Skin Palette")]
    public sealed class NpcSkinPalette : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Skin Palette";

        [SerializeField]
        private Color skinColor = new Color(0.80f, 0.58f, 0.42f, 1f);

        [Range(0.5f, 1f)]
        [SerializeField]
        private float sourceCameraLeftShade = 0.82f;


        public string DisplayName => displayName;

        public Color SkinColor => skinColor;


        public void Configure(
            string newDisplayName,
            Color newSkinColor,
            float newSourceCameraLeftShade = 0.82f)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            skinColor = newSkinColor;
            sourceCameraLeftShade = Mathf.Clamp(
                newSourceCameraLeftShade,
                0.5f,
                1f);
        }


        public Color GetColor(
            bool shadeForDepth)
        {
            return shadeForDepth
                ? NpcAppearanceUtility.Shade(
                    skinColor,
                    sourceCameraLeftShade)
                : skinColor;
        }
    }
}
