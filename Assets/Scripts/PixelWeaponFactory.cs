using System.Collections.Generic;
using UnityEngine;

namespace Depthbound
{
    public static class PixelWeaponFactory
    {
        private static readonly Dictionary<string, Sprite> cache =
            new Dictionary<string, Sprite>();

        private static readonly Color32 Clear =
            new Color32(0, 0, 0, 0);

        private static readonly Color32 Outline =
            new Color32(22, 23, 22, 255);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache.Clear();
        }

        public static Sprite Dagger()
        {
            if (cache.TryGetValue("single_sword", out Sprite cached))
            {
                return cached;
            }

            const int width = 72;
            const int height = 72;

            Texture2D texture =
                CreateTexture(
                    "PixelSingleSword",
                    width,
                    height);

            Color32 steel =
                new Color32(205, 205, 190, 255);

            Color32 highlight =
                new Color32(232, 225, 202, 255);

            Color32 grip =
                new Color32(92, 58, 38, 255);

            Color32 brass =
                new Color32(148, 99, 45, 255);

            // Longer one-handed blade, still kept as a broad pixel silhouette.
            DrawThickLine(
                texture,
                27,
                18,
                60,
                65,
                8,
                Outline);

            DrawThickLine(
                texture,
                28,
                20,
                58,
                63,
                5,
                steel);

            DrawThickLine(
                texture,
                31,
                22,
                57,
                61,
                1,
                highlight);

            // Guard.
            DrawThickLine(
                texture,
                18,
                20,
                34,
                14,
                5,
                Outline);

            DrawThickLine(
                texture,
                20,
                20,
                33,
                16,
                2,
                brass);

            // Grip and pommel.
            DrawThickLine(
                texture,
                14,
                8,
                27,
                19,
                8,
                Outline);

            DrawThickLine(
                texture,
                15,
                9,
                26,
                18,
                5,
                grip);

            FillEllipse(
                texture,
                12,
                7,
                5,
                5,
                Outline);

            FillEllipse(
                texture,
                12,
                7,
                3,
                3,
                brass);

            texture.Apply(false, true);

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.25f),
                    64f);

            sprite.name = "RuntimePixelSingleSword";
            cache["single_sword"] = sprite;
            return sprite;
        }

        public static Sprite SwordShield()
        {
            if (cache.TryGetValue("swordshield", out Sprite cached))
            {
                return cached;
            }

            const int width = 96;
            const int height = 72;

            Texture2D texture =
                CreateTexture(
                    "PixelSwordShield",
                    width,
                    height);

            Color32 wood =
                new Color32(96, 57, 31, 255);

            Color32 woodLight =
                new Color32(128, 77, 37, 255);

            Color32 rim =
                new Color32(125, 122, 108, 255);

            Color32 rimLight =
                new Color32(176, 167, 144, 255);

            Color32 steel =
                new Color32(205, 205, 190, 255);

            Color32 steelLight =
                new Color32(236, 228, 203, 255);

            Color32 grip =
                new Color32(75, 48, 35, 255);

            // Round shield on left.
            FillEllipse(
                texture,
                29,
                27,
                23,
                23,
                Outline);

            FillEllipse(
                texture,
                29,
                27,
                20,
                20,
                rim);

            FillEllipse(
                texture,
                29,
                27,
                17,
                17,
                wood);

            FillRect(
                texture,
                14,
                25,
                44,
                28,
                woodLight);

            FillRect(
                texture,
                27,
                12,
                30,
                42,
                woodLight);

            FillEllipse(
                texture,
                29,
                27,
                7,
                7,
                Outline);

            FillEllipse(
                texture,
                29,
                27,
                4,
                4,
                rimLight);

            // Sword on right, leaning toward upper-right.
            DrawThickLine(
                texture,
                61,
                12,
                82,
                62,
                8,
                Outline);

            DrawThickLine(
                texture,
                62,
                14,
                80,
                59,
                5,
                steel);

            DrawThickLine(
                texture,
                66,
                17,
                79,
                56,
                1,
                steelLight);

            DrawThickLine(
                texture,
                53,
                19,
                69,
                13,
                6,
                Outline);

            DrawThickLine(
                texture,
                55,
                19,
                68,
                15,
                3,
                rim);

            DrawThickLine(
                texture,
                49,
                5,
                62,
                15,
                9,
                Outline);

            DrawThickLine(
                texture,
                51,
                6,
                61,
                14,
                5,
                grip);

            texture.Apply(false, true);

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.25f),
                    64f);

            sprite.name = "RuntimePixelSwordShield";
            cache["swordshield"] = sprite;
            return sprite;
        }

        private static Texture2D CreateTexture(
            string name,
            int width,
            int height)
        {
            Texture2D texture =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);

            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels =
                new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Clear;
            }

            texture.SetPixels32(pixels);
            return texture;
        }

        private static void FillRect(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1,
            Color32 color)
        {
            int minX =
                Mathf.Clamp(
                    Mathf.Min(x0, x1),
                    0,
                    texture.width - 1);

            int maxX =
                Mathf.Clamp(
                    Mathf.Max(x0, x1),
                    0,
                    texture.width - 1);

            int minY =
                Mathf.Clamp(
                    Mathf.Min(y0, y1),
                    0,
                    texture.height - 1);

            int maxY =
                Mathf.Clamp(
                    Mathf.Max(y0, y1),
                    0,
                    texture.height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void FillEllipse(
            Texture2D texture,
            int cx,
            int cy,
            int rx,
            int ry,
            Color32 color)
        {
            int safeRx = Mathf.Max(1, rx);
            int safeRy = Mathf.Max(1, ry);

            for (int y = -safeRy; y <= safeRy; y++)
            {
                for (int x = -safeRx; x <= safeRx; x++)
                {
                    float nx = x / (float)safeRx;
                    float ny = y / (float)safeRy;

                    if (nx * nx + ny * ny <= 1f)
                    {
                        int px = cx + x;
                        int py = cy + y;

                        if (px >= 0 &&
                            px < texture.width &&
                            py >= 0 &&
                            py < texture.height)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private static void DrawThickLine(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1,
            int thickness,
            Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            int radius = Mathf.Max(0, thickness / 2);

            int x = x0;
            int y = y0;

            while (true)
            {
                FillRect(
                    texture,
                    x - radius,
                    y - radius,
                    x + radius,
                    y + radius,
                    color);

                if (x == x1 && y == y1)
                {
                    break;
                }

                int e2 = error * 2;

                if (e2 >= dy)
                {
                    error += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y += sy;
                }
            }
        }
    }
}
