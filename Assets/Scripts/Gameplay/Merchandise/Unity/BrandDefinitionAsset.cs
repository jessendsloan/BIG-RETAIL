using System;
using BigRetail.Merchandise.Domain;
using UnityEngine;

namespace BigRetail.Merchandise.Unity
{
    /// <summary>
    /// Authoring and optional presentation data for one consumer brand.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BrandDefinition",
        menuName = "Big Retail/Merchandise/Brand Definition")]
    public sealed class BrandDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string brandId;

        [SerializeField]
        private string displayName;

        [TextArea(1, 3)]
        [SerializeField]
        private string identity;

        [Tooltip("Optional brand mark. A text stub is shown when absent.")]
        [SerializeField]
        private Sprite logo;

        [SerializeField]
        private Color accentColor = new Color(0.85f, 0.36f, 0.18f, 1f);


        public string DisplayName =>
            displayName;

        public string Identity =>
            identity;

        public Sprite Logo =>
            logo;

        public Color AccentColor =>
            accentColor;


        public bool TryCreateDefinition(
            out BrandDefinition definition,
            out string error)
        {
            try
            {
                definition =
                    new BrandDefinition(
                        new BrandId(brandId),
                        displayName);
                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                definition = null;
                error = $"{name}: {exception.Message}";
                return false;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            brandId = NormalizeIdentifier(brandId);
            displayName = NormalizeText(displayName);
            identity = NormalizeText(identity);
            accentColor.a = 1f;
        }

        private static string NormalizeIdentifier(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeText(string value)
        {
            return value == null
                ? string.Empty
                : value.Trim();
        }
#endif
    }
}
