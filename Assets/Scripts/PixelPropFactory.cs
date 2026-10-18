using System.Collections.Generic;
using UnityEngine;

namespace Depthbound
{
    public enum PixelPropKind
    {
        WeaponRack,
        CrateStack,
        BarrelStack,
        BrokenCrate,
        Gear,
        PipeBank,
        MachineChest,
        Sarcophagus,
        UrnCluster,
        BonePile,
        ThroneRelic,
        TorchFlame
    }

    public static class PixelPropFactory
    {
        private static readonly Dictionary<int, Sprite> cache =
            new Dictionary<int, Sprite>();

        private static readonly Color32 Clear =
            new Color32(0, 0, 0, 0);

        private static readonly Color32 Outline =
            new Color32(27, 27, 23, 255);

        private static readonly Color32 WoodDark =
            new Color32(74, 47, 29, 255);

        private static readonly Color32 Wood =
            new Color32(112, 72, 42, 255);

        private static readonly Color32 WoodLight =
            new Color32(146, 100, 57, 255);

        private static readonly Color32 MetalDark =
            new Color32(69, 68, 59, 255);

        private static readonly Color32 Metal =
            new Color32(112, 108, 91, 255);

        private static readonly Color32 Rust =
            new Color32(128, 78, 39, 255);

        private static readonly Color32 RustLight =
            new Color32(164, 105, 51, 255);

        private static readonly Color32 BoneDark =
            new Color32(119, 111, 91, 255);

        private static readonly Color32 Bone =
            new Color32(180, 171, 143, 255);

        private static readonly Color32 BoneLight =
            new Color32(211, 199, 164, 255);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache.Clear();
        }

        public static Sprite Get(
            PixelPropKind kind)
        {
            int key = (int)kind;

            if (cache.TryGetValue(
                key,
                out Sprite cached))
            {
                return cached;
            }

            Sprite created =
                Build(kind);

            cache[key] = created;
            return created;
        }

        private static Sprite Build(
            PixelPropKind kind)
        {
            const int size = 32;

            Texture2D texture =
                MakeTexture(
                    "PixelProp_" + kind,
                    size,
                    size);

            switch (kind)
            {
                case PixelPropKind.WeaponRack:
                    DrawWeaponRack(texture);
                    break;

                case PixelPropKind.CrateStack:
                    DrawCrateStack(texture, false);
                    break;

                case PixelPropKind.BarrelStack:
                    DrawBarrelStack(texture);
                    break;

                case PixelPropKind.BrokenCrate:
                    DrawCrateStack(texture, true);
                    break;

                case PixelPropKind.Gear:
                    DrawGear(texture);
                    break;

                case PixelPropKind.PipeBank:
                    DrawPipeBank(texture);
                    break;

                case PixelPropKind.MachineChest:
                    DrawMachineChest(texture);
                    break;

                case PixelPropKind.Sarcophagus:
                    DrawSarcophagus(texture);
                    break;

                case PixelPropKind.UrnCluster:
                    DrawUrnCluster(texture);
                    break;

                case PixelPropKind.BonePile:
                    DrawBonePile(texture);
                    break;

                case PixelPropKind.ThroneRelic:
                    DrawThroneRelic(texture);
                    break;

                default:
                    DrawTorchFlame(texture);
                    break;
            }

            texture.Apply(false, true);

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        size,
                        size),
                    new Vector2(
                        0.5f,
                        0f),
                    16f);

            sprite.name =
                "RuntimePixelProp_" + kind;

            return sprite;
        }

        private static void DrawWeaponRack(
            Texture2D texture)
        {
            FillRect(texture, 4, 3, 27, 6, Outline);
            FillRect(texture, 5, 4, 26, 5, Wood);

            FillRect(texture, 6, 6, 9, 27, Outline);
            FillRect(texture, 7, 7, 8, 26, WoodDark);

            FillRect(texture, 22, 6, 25, 27, Outline);
            FillRect(texture, 23, 7, 24, 26, WoodDark);

            FillRect(texture, 7, 20, 24, 23, Outline);
            FillRect(texture, 8, 21, 23, 22, WoodLight);

            for (int x = 11; x <= 20; x += 4)
            {
                FillRect(texture, x, 8, x + 1, 26, MetalDark);
                FillRect(texture, x, 9, x, 25, Metal);
                FillRect(texture, x - 1, 25, x + 2, 27, BoneLight);
            }
        }

        private static void DrawCrateStack(
            Texture2D texture,
            bool broken)
        {
            DrawCrate(
                texture,
                4,
                3,
                16,
                14);

            DrawCrate(
                texture,
                broken ? 14 : 15,
                broken ? 4 : 8,
                28,
                broken ? 13 : 19);

            if (!broken)
            {
                DrawCrate(
                    texture,
                    7,
                    14,
                    18,
                    23);
            }
            else
            {
                FillRect(texture, 16, 14, 19, 20, Outline);
                FillRect(texture, 17, 15, 18, 19, WoodLight);
                FillRect(texture, 21, 13, 26, 15, Outline);
                FillRect(texture, 22, 14, 25, 14, Wood);
            }
        }

        private static void DrawCrate(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1)
        {
            FillRect(texture, x0, y0, x1, y1, Outline);
            FillRect(texture, x0 + 1, y0 + 1, x1 - 1, y1 - 1, Wood);
            FillRect(texture, x0 + 2, y0 + 2, x1 - 2, y0 + 3, WoodLight);
            FillRect(texture, x0 + 2, y1 - 3, x1 - 2, y1 - 2, WoodDark);

            for (int i = 1; i < 3; i++)
            {
                int x =
                    Mathf.RoundToInt(
                        Mathf.Lerp(
                            x0 + 2,
                            x1 - 2,
                            i / 3f));

                FillRect(texture, x, y0 + 1, x + 1, y1 - 1, WoodDark);
            }
        }

        private static void DrawBarrelStack(
            Texture2D texture)
        {
            DrawBarrel(texture, 3, 2, 13, 19);
            DrawBarrel(texture, 11, 3, 21, 20);
            DrawBarrel(texture, 19, 2, 29, 19);
            DrawBarrel(texture, 8, 15, 18, 29);
        }

        private static void DrawBarrel(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1)
        {
            FillRect(texture, x0 + 2, y0, x1 - 2, y1, Outline);
            FillRect(texture, x0 + 1, y0 + 3, x1 - 1, y1 - 3, Outline);
            FillRect(texture, x0 + 2, y0 + 2, x1 - 2, y1 - 2, Wood);
            FillRect(texture, x0 + 3, y0 + 3, x1 - 3, y1 - 3, WoodLight);
            FillRect(texture, x0 + 1, y0 + 5, x1 - 1, y0 + 6, MetalDark);
            FillRect(texture, x0 + 1, y1 - 6, x1 - 1, y1 - 5, MetalDark);
        }

        private static void DrawGear(
            Texture2D texture)
        {
            FillEllipse(texture, 16, 16, 10, 10, Outline);
            FillRect(texture, 14, 3, 18, 29, Outline);
            FillRect(texture, 3, 14, 29, 18, Outline);
            FillRect(texture, 7, 7, 25, 25, Rust);
            FillEllipse(texture, 16, 16, 6, 6, MetalDark);
            FillEllipse(texture, 16, 16, 3, 3, Clear);

            FillRect(texture, 15, 5, 17, 8, RustLight);
            FillRect(texture, 15, 24, 17, 27, RustLight);
            FillRect(texture, 5, 15, 8, 17, RustLight);
            FillRect(texture, 24, 15, 27, 17, RustLight);
        }

        private static void DrawPipeBank(
            Texture2D texture)
        {
            for (int i = 0; i < 4; i++)
            {
                int x = 5 + i * 6;
                FillRect(texture, x, 3, x + 3, 28, Outline);
                FillRect(texture, x + 1, 4, x + 2, 27, i % 2 == 0 ? Rust : Metal);
            }

            FillRect(texture, 4, 9, 27, 12, Outline);
            FillRect(texture, 5, 10, 26, 11, RustLight);
            FillRect(texture, 4, 21, 27, 24, Outline);
            FillRect(texture, 5, 22, 26, 23, Metal);
        }

        private static void DrawMachineChest(
            Texture2D texture)
        {
            FillRect(texture, 4, 4, 27, 18, Outline);
            FillRect(texture, 5, 5, 26, 17, MetalDark);
            FillRect(texture, 6, 7, 25, 15, Rust);
            FillRect(texture, 5, 18, 26, 23, Outline);
            FillRect(texture, 6, 19, 25, 22, Metal);
            FillRect(texture, 13, 11, 18, 16, Outline);
            FillRect(texture, 14, 12, 17, 15, RustLight);

            FillRect(texture, 6, 3, 9, 5, Outline);
            FillRect(texture, 22, 3, 25, 5, Outline);
        }

        private static void DrawSarcophagus(
            Texture2D texture)
        {
            FillRect(texture, 7, 3, 24, 23, Outline);
            FillRect(texture, 9, 5, 22, 21, BoneDark);
            FillRect(texture, 11, 8, 20, 19, Bone);

            FillRect(texture, 12, 21, 19, 27, Outline);
            FillRect(texture, 13, 22, 18, 26, Bone);

            FillRect(texture, 14, 9, 17, 17, BoneLight);
            FillRect(texture, 11, 13, 20, 15, BoneLight);
        }

        private static void DrawUrnCluster(
            Texture2D texture)
        {
            DrawUrn(texture, 3, 3, 12, 18, Wood);
            DrawUrn(texture, 10, 4, 20, 24, Rust);
            DrawUrn(texture, 19, 3, 28, 20, BoneDark);
        }

        private static void DrawUrn(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1,
            Color32 color)
        {
            int cx = (x0 + x1) / 2;
            FillRect(texture, cx - 2, y1 - 1, cx + 2, y1 + 1, Outline);
            FillRect(texture, cx - 1, y1, cx + 1, y1 + 1, color);
            FillEllipse(
                texture,
                cx,
                (y0 + y1) / 2,
                Mathf.Max(2, (x1 - x0) / 2),
                Mathf.Max(3, (y1 - y0) / 2),
                Outline);
            FillEllipse(
                texture,
                cx,
                (y0 + y1) / 2,
                Mathf.Max(1, (x1 - x0) / 2 - 1),
                Mathf.Max(2, (y1 - y0) / 2 - 1),
                color);
        }

        private static void DrawBonePile(
            Texture2D texture)
        {
            FillEllipse(texture, 16, 5, 12, 4, Outline);
            FillEllipse(texture, 16, 6, 11, 3, BoneDark);

            FillRect(texture, 5, 8, 13, 10, Outline);
            FillRect(texture, 6, 9, 12, 9, Bone);

            FillRect(texture, 17, 10, 27, 12, Outline);
            FillRect(texture, 18, 11, 26, 11, BoneLight);

            FillEllipse(texture, 13, 13, 4, 4, Outline);
            FillEllipse(texture, 13, 13, 3, 3, Bone);
            FillRect(texture, 11, 10, 15, 13, Bone);
            FillRect(texture, 11, 13, 12, 14, Outline);
            FillRect(texture, 14, 13, 15, 14, Outline);
        }

        private static void DrawThroneRelic(
            Texture2D texture)
        {
            FillRect(texture, 8, 3, 23, 9, Outline);
            FillRect(texture, 9, 4, 22, 8, BoneDark);

            FillRect(texture, 10, 8, 21, 26, Outline);
            FillRect(texture, 12, 10, 19, 24, Rust);

            FillRect(texture, 8, 17, 11, 22, Outline);
            FillRect(texture, 20, 17, 23, 22, Outline);

            FillRect(texture, 14, 13, 17, 18, BoneLight);
            FillRect(texture, 13, 15, 18, 16, BoneLight);
        }

        private static void DrawTorchFlame(
            Texture2D texture)
        {
            Color32 ember =
                new Color32(
                    146,
                    56,
                    25,
                    255);

            Color32 flame =
                new Color32(
                    203,
                    104,
                    39,
                    255);

            Color32 core =
                new Color32(
                    232,
                    158,
                    66,
                    255);

            FillRect(
                texture,
                13,
                4,
                19,
                16,
                Outline);

            FillRect(
                texture,
                11,
                7,
                21,
                13,
                Outline);

            FillRect(
                texture,
                14,
                5,
                18,
                18,
                ember);

            FillRect(
                texture,
                12,
                8,
                20,
                13,
                flame);

            FillRect(
                texture,
                14,
                8,
                18,
                14,
                core);

            FillRect(
                texture,
                15,
                19,
                17,
                23,
                Outline);

            FillRect(
                texture,
                16,
                19,
                16,
                22,
                flame);
        }

        private static Texture2D MakeTexture(
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
            texture.filterMode =
                FilterMode.Point;
            texture.wrapMode =
                TextureWrapMode.Clamp;

            Color32[] pixels =
                new Color32[
                    width * height];

            for (int i = 0;
                 i < pixels.Length;
                 i++)
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

            for (int y = minY;
                 y <= maxY;
                 y++)
            {
                for (int x = minX;
                     x <= maxX;
                     x++)
                {
                    texture.SetPixel(
                        x,
                        y,
                        color);
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
            int safeRx =
                Mathf.Max(1, rx);

            int safeRy =
                Mathf.Max(1, ry);

            for (int y = -safeRy;
                 y <= safeRy;
                 y++)
            {
                for (int x = -safeRx;
                     x <= safeRx;
                     x++)
                {
                    float nx =
                        x / (float)safeRx;

                    float ny =
                        y / (float)safeRy;

                    if (nx * nx +
                        ny * ny >
                        1f)
                    {
                        continue;
                    }

                    int px = cx + x;
                    int py = cy + y;

                    if (px < 0 ||
                        px >= texture.width ||
                        py < 0 ||
                        py >= texture.height)
                    {
                        continue;
                    }

                    texture.SetPixel(
                        px,
                        py,
                        color);
                }
            }
        }
    }
}
