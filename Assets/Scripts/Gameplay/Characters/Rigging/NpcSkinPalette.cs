using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("sourceCameraLeftShade")]
        [FormerlySerializedAs("nearShade")]
        private float farSideShade = 0.82f;


        public string DisplayName => displayName;

        public Color SkinColor => skinColor;


        public void Configure(
            string newDisplayName,
            Color newSkinColor,
            float newFarSideShade = 0.82f)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            skinColor = newSkinColor;
            farSideShade = Mathf.Clamp(
                newFarSideShade,
                0.5f,
                1f);
        }


        public Color GetColor(
            bool shadeForDepth)
        {
            return shadeForDepth
                ? NpcAppearanceUtility.Shade(
                    skinColor,
                    farSideShade)
                : skinColor;
        }
    }
}
