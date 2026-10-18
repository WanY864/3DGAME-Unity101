using UnityEngine;

namespace Depthbound
{
    public static class PixelDungeonTextureFactory
    {
        private static Texture2D floor;
        private static Texture2D wall;
        private static Texture2D wood;
        private static Texture2D rust;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            floor = null;
            wall = null;
            wood = null;
            rust = null;
        }

        public static void ApplyDungeonTextures(
            Material floorMaterial,
            Material wallMaterial,
            Material trimMaterial,
            Material woodMaterial,
            Material rustMaterial)
        {
            Apply(
                floorMaterial,
                GetFloor(),
                new Vector2(4f, 4f));

            Apply(
                wallMaterial,
                GetWall(),
                new Vector2(4f, 2f));

            Apply(
                trimMaterial,
                GetWall(),
                new Vector2(3f, 2f));

            Apply(
                woodMaterial,
                GetWood(),
                new Vector2(2f, 3f));

            Apply(
                rustMaterial,
                GetRust(),
                new Vector2(2f, 3f));
        }

        private static Texture2D GetFloor()
        {
            if (floor != null)
            {
                return floor;
            }

            const int size = 16;

            Color32 baseColor =
                new Color32(
                    210,
                    214,
                    210,
                    255);

            Color32 light =
                new Color32(
                    232,
                    234,
                    228,
                    255);

            Color32 dark =
                new Color32(
                    142,
                    148,
                    145,
                    255);

            floor =
                NewTexture(
                    "PixelDungeonFloor",
                    size,
                    size,
                    baseColor);

            // Flagstone seams.
            FillRect(
                floor,
                0,
                0,
                15,
                1,
                dark);

            FillRect(
                floor,
                0,
                8,
                15,
                9,
                dark);

            FillRect(
                floor,
                0,
                0,
                1,
                7,
                dark);

            FillRect(
                floor,
                8,
                8,
                9,
                15,
                dark);

            // Small hand-placed highlights / chips.
            Set(
                floor,
                3,
                4,
                light);

            Set(
                floor,
                12,
                6,
                light);

            Set(
                floor,
                5,
                12,
                light);

            Set(
                floor,
                13,
                13,
                dark);

            floor.Apply(false, true);
            return floor;
        }

        private static Texture2D GetWall()
        {
            if (wall != null)
            {
                return wall;
            }

            const int size = 16;

            Color32 baseColor =
                new Color32(
                    205,
                    210,
                    204,
                    255);

            Color32 light =
                new Color32(
                    230,
                    233,
                    224,
                    255);

            Color32 dark =
                new Color32(
                    138,
                    144,
                    140,
                    255);

            wall =
                NewTexture(
                    "PixelDungeonWall",
                    size,
                    size,
                    baseColor);

            FillRect(
                wall,
                0,
                0,
                15,
                1,
                dark);

            FillRect(
                wall,
                0,
                8,
                15,
                9,
                dark);

            FillRect(
                wall,
                7,
                0,
                8,
                7,
                dark);

            FillRect(
                wall,
                3,
                8,
                4,
                15,
                dark);

            FillRect(
                wall,
                1,
                2,
                5,
                2,
                light);

            FillRect(
                wall,
                10,
                11,
                14,
                11,
                light);

            Set(
                wall,
                12,
                4,
                dark);

            Set(
                wall,
                6,
                13,
                light);

            wall.Apply(false, true);
            return wall;
        }

        private static Texture2D GetWood()
        {
            if (wood != null)
            {
                return wood;
            }

            const int size = 16;

            Color32 baseColor =
                new Color32(
                    220,
                    211,
                    198,
                    255);

            Color32 light =
                new Color32(
                    244,
                    230,
                    207,
                    255);

            Color32 dark =
                new Color32(
                    145,
                    132,
                    120,
                    255);

            wood =
                NewTexture(
                    "PixelDungeonWood",
                    size,
                    size,
                    baseColor);

            for (int x = 0; x < size; x += 5)
            {
                FillRect(
                    wood,
                    x,
                    0,
                    x,
                    15,
                    dark);
            }

            FillRect(
                wood,
                2,
                4,
                4,
                4,
                light);

            FillRect(
                wood,
                8,
                11,
                12,
                11,
                light);

            Set(
                wood,
                13,
                6,
                dark);

            wood.Apply(false, true);
            return wood;
        }

        private static Texture2D GetRust()
        {
            if (rust != null)
            {
                return rust;
            }

            const int size = 16;

            Color32 baseColor =
                new Color32(
                    214,
                    205,
                    188,
                    255);

            Color32 orange =
                new Color32(
                    236,
                    194,
                    155,
                    255);

            Color32 dark =
                new Color32(
                    139,
                    137,
                    128,
                    255);

            rust =
                NewTexture(
                    "PixelDungeonRust",
                    size,
                    size,
                    baseColor);

            FillRect(
                rust,
                0,
                0,
                15,
                1,
                dark);

            FillRect(
                rust,
                7,
                2,
                8,
                15,
                dark);

            FillRect(
                rust,
                2,
                5,
                4,
                7,
                orange);

            FillRect(
                rust,
                11,
                10,
                14,
                12,
                orange);

            Set(
                rust,
                5,
                13,
                dark);

            Set(
                rust,
                13,
                4,
                orange);

            rust.Apply(false, true);
            return rust;
        }

        private static Texture2D NewTexture(
            string name,
            int width,
            int height,
            Color32 fill)
        {
            Texture2D texture =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);

            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.anisoLevel = 0;

            Color32[] pixels =
                new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fill;
            }

            texture.SetPixels32(pixels);
            return texture;
        }

        private static void Apply(
            Material material,
            Texture2D texture,
            Vector2 scale)
        {
            if (material == null ||
                texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture(
                    "_BaseMap",
                    texture);

                material.SetTextureScale(
                    "_BaseMap",
                    scale);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture(
                    "_MainTex",
                    texture);

                material.SetTextureScale(
                    "_MainTex",
                    scale);
            }

            material.mainTexture = texture;
            material.mainTextureScale = scale;
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
                    texture.SetPixel(
                        x,
                        y,
                        color);
                }
            }
        }

        private static void Set(
            Texture2D texture,
            int x,
            int y,
            Color32 color)
        {
            if (x < 0 ||
                x >= texture.width ||
                y < 0 ||
                y >= texture.height)
            {
                return;
            }

            texture.SetPixel(
                x,
                y,
                color);
        }
    }
}
