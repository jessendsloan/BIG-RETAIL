using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Small runtime art fallback that keeps the delivery slice playable
    /// when an authored supplier-load tier has not been assigned.
    /// </summary>
    internal sealed class InboundDeliveryPlaceholderSprites : IDisposable
    {
        private const int TextureSize = 64;

        private readonly Texture2D palletTexture;
        private readonly Texture2D boxTexture;


        public Sprite Pallet { get; }

        public Sprite Box { get; }


        public InboundDeliveryPlaceholderSprites()
        {
            palletTexture = CreatePalletTexture();
            boxTexture = CreateBoxTexture();
            Pallet = CreateSprite(
                palletTexture,
                "Graybox Inbound Pallet",
                new Vector2(0.5f, 0.08f));
            Box = CreateSprite(
                boxTexture,
                "Graybox Supplier Carton",
                new Vector2(0.5f, 0.12f));
        }


        public void Dispose()
        {
            DestroyRuntimeObject(Pallet);
            DestroyRuntimeObject(Box);
            DestroyRuntimeObject(palletTexture);
            DestroyRuntimeObject(boxTexture);
        }


        private static Texture2D CreatePalletTexture()
        {
            Texture2D texture = CreateTexture("Graybox Inbound Pallet Texture");
            Vector2Int[] top =
            {
                new Vector2Int(5, 25),
                new Vector2Int(32, 39),
                new Vector2Int(59, 25),
                new Vector2Int(32, 11)
            };
            Vector2Int[] front =
            {
                new Vector2Int(5, 25),
                new Vector2Int(32, 11),
                new Vector2Int(32, 6),
                new Vector2Int(5, 20)
            };
            Vector2Int[] side =
            {
                new Vector2Int(32, 11),
                new Vector2Int(59, 25),
                new Vector2Int(59, 20),
                new Vector2Int(32, 6)
            };

            FillPolygon(texture, front, new Color32(142, 93, 47, 255));
            FillPolygon(texture, side, new Color32(116, 72, 36, 255));
            FillPolygon(texture, top, new Color32(202, 143, 75, 255));
            OutlinePolygon(texture, front);
            OutlinePolygon(texture, side);
            OutlinePolygon(texture, top);

            DrawLine(
                texture,
                new Vector2Int(18, 18),
                new Vector2Int(45, 31),
                new Color32(72, 45, 25, 255));
            DrawLine(
                texture,
                new Vector2Int(31, 12),
                new Vector2Int(31, 37),
                new Color32(72, 45, 25, 255));
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }

        private static Texture2D CreateBoxTexture()
        {
            Texture2D texture = CreateTexture("Graybox Supplier Carton Texture");
            Vector2Int[] top =
            {
                new Vector2Int(7, 42),
                new Vector2Int(32, 56),
                new Vector2Int(57, 42),
                new Vector2Int(32, 28)
            };
            Vector2Int[] left =
            {
                new Vector2Int(7, 42),
                new Vector2Int(32, 28),
                new Vector2Int(32, 9),
                new Vector2Int(7, 23)
            };
            Vector2Int[] right =
            {
                new Vector2Int(32, 28),
                new Vector2Int(57, 42),
                new Vector2Int(57, 23),
                new Vector2Int(32, 9)
            };

            FillPolygon(texture, left, new Color32(218, 218, 218, 255));
            FillPolygon(texture, right, new Color32(174, 174, 174, 255));
            FillPolygon(texture, top, new Color32(246, 246, 246, 255));
            OutlinePolygon(texture, left);
            OutlinePolygon(texture, right);
            OutlinePolygon(texture, top);

            Color32 tape = new Color32(55, 55, 55, 255);
            DrawLine(
                texture,
                new Vector2Int(27, 53),
                new Vector2Int(27, 31),
                tape);
            DrawLine(
                texture,
                new Vector2Int(28, 53),
                new Vector2Int(28, 31),
                tape);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }

        private static Texture2D CreateTexture(string name)
        {
            Texture2D texture = new Texture2D(
                TextureSize,
                TextureSize,
                TextureFormat.RGBA32,
                mipChain: false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[TextureSize * TextureSize];
            texture.SetPixels32(pixels);
            return texture;
        }

        private static Sprite CreateSprite(
            Texture2D texture,
            string name,
            Vector2 pivot)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                pivot,
                TextureSize,
                extrude: 0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static void FillPolygon(
            Texture2D texture,
            IReadOnlyList<Vector2Int> points,
            Color32 color)
        {
            int minimumY = int.MaxValue;
            int maximumY = int.MinValue;

            for (int index = 0; index < points.Count; index++)
            {
                minimumY = Mathf.Min(minimumY, points[index].y);
                maximumY = Mathf.Max(maximumY, points[index].y);
            }

            List<float> intersections = new List<float>(points.Count);

            for (int y = minimumY; y <= maximumY; y++)
            {
                intersections.Clear();

                for (int index = 0; index < points.Count; index++)
                {
                    Vector2Int first = points[index];
                    Vector2Int second = points[(index + 1) % points.Count];

                    if ((first.y <= y && second.y > y)
                        || (second.y <= y && first.y > y))
                    {
                        float x = first.x
                            + (y - first.y)
                            * (second.x - first.x)
                            / (float)(second.y - first.y);
                        intersections.Add(x);
                    }
                }

                intersections.Sort();

                for (int index = 0;
                     index + 1 < intersections.Count;
                     index += 2)
                {
                    int minimumX = Mathf.CeilToInt(intersections[index]);
                    int maximumX = Mathf.FloorToInt(intersections[index + 1]);

                    for (int x = minimumX; x <= maximumX; x++)
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void OutlinePolygon(
            Texture2D texture,
            IReadOnlyList<Vector2Int> points)
        {
            Color32 outline = new Color32(28, 28, 28, 255);

            for (int index = 0; index < points.Count; index++)
            {
                DrawLine(
                    texture,
                    points[index],
                    points[(index + 1) % points.Count],
                    outline);
            }
        }

        private static void DrawLine(
            Texture2D texture,
            Vector2Int start,
            Vector2Int end,
            Color32 color)
        {
            int x = start.x;
            int y = start.y;
            int deltaX = Mathf.Abs(end.x - start.x);
            int deltaY = Mathf.Abs(end.y - start.y);
            int stepX = start.x < end.x ? 1 : -1;
            int stepY = start.y < end.y ? 1 : -1;
            int error = deltaX - deltaY;

            while (true)
            {
                SetPixel(texture, x, y, color);

                if (x == end.x && y == end.y)
                {
                    break;
                }

                int doubledError = error * 2;

                if (doubledError > -deltaY)
                {
                    error -= deltaY;
                    x += stepX;
                }

                if (doubledError < deltaX)
                {
                    error += deltaX;
                    y += stepY;
                }
            }
        }

        private static void SetPixel(
            Texture2D texture,
            int x,
            int y,
            Color32 color)
        {
            if (x >= 0
                && x < texture.width
                && y >= 0
                && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
