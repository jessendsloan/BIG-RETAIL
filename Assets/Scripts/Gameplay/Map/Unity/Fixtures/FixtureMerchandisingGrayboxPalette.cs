using BigRetail.Merchandise.Domain;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Temporary, deterministic colors for the merchandising graybox.
    /// Product art can replace this without changing planogram data.
    /// </summary>
    public static class FixtureMerchandisingGrayboxPalette
    {
        public static Color Neutral =>
            new Color(0.72f, 0.77f, 0.80f, 0.90f);

        public static Color Hover =>
            new Color(1f, 0.88f, 0.36f, 1f);

        public static Color Selected =>
            new Color(1f, 0.69f, 0.05f, 1f);

        public static Color Invalid =>
            new Color(1f, 0.12f, 0.10f, 0.98f);

        public static Color ShelfNeutral =>
            new Color(0.72f, 0.77f, 0.80f, 0.12f);

        public static Color ShelfHover =>
            new Color(1f, 0.88f, 0.36f, 0.28f);

        public static Color ShelfSelected =>
            new Color(1f, 0.69f, 0.05f, 0.42f);

        public static Color ShelfInvalid =>
            new Color(1f, 0.12f, 0.10f, 0.58f);


        public static Color ResolveProductColor(ProductId productId)
        {
            switch (productId.Value)
            {
                case "CEREAL":
                    return new Color(0.18f, 0.58f, 0.95f, 0.96f);

                case "SOUP":
                    return new Color(0.96f, 0.46f, 0.18f, 0.96f);

                case "COLA":
                    return new Color(0.64f, 0.28f, 0.88f, 0.96f);

                default:
                {
                    int colorIndex =
                        (productId.GetHashCode() & 0x7fffffff) % 3;

                    return colorIndex switch
                    {
                        0 => new Color(0.20f, 0.70f, 0.48f, 0.96f),
                        1 => new Color(0.92f, 0.72f, 0.18f, 0.96f),
                        _ => new Color(0.30f, 0.64f, 0.88f, 0.96f)
                    };
                }
            }
        }

        public static Color ResolveStockColor(
            ProductId productId,
            float fillRatio)
        {
            Color productColor = ResolveProductColor(productId);
            float clampedFill = Mathf.Clamp01(fillRatio);

            if (clampedFill <= 0f)
            {
                Color empty = Color.Lerp(
                    Neutral,
                    productColor,
                    0.25f);
                empty.a = 0.90f;
                return empty;
            }

            if (clampedFill < 1f)
            {
                Color partial = Color.Lerp(
                    Neutral,
                    productColor,
                    0.65f + clampedFill * 0.25f);
                partial.a = 0.92f + clampedFill * 0.04f;
                return partial;
            }

            productColor.a = 0.98f;
            return productColor;
        }
    }
}
