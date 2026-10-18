using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Depthbound
{
    public static class PixelSpriteFactory
    {
        private static readonly Dictionary<int, Sprite> cache =
            new Dictionary<int, Sprite>();

        private static readonly Color32 Clear =
            new Color32(0, 0, 0, 0);

        private static readonly Color32 Outline =
            new Color32(25, 27, 25, 255);

        private static readonly Color32 Eye =
            new Color32(215, 45, 32, 255);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cache.Clear();
        }

        public static Sprite GetEnemy(
            EnemyArchetype archetype,
            bool boss)
        {
            int key =
                boss
                    ? 100 + (int)archetype
                    : (int)archetype;

            if (cache.TryGetValue(
                key,
                out Sprite cached))
            {
                return cached;
            }

            Sprite created =
                boss
                    ? BuildBoss(archetype)
                    : BuildEnemy(archetype);

            cache[key] = created;
            return created;
        }

        private static Sprite BuildEnemy(
            EnemyArchetype archetype)
        {
            const int size = 32;

            Texture2D texture =
                MakeTexture(
                    "PixelEnemy_" + archetype,
                    size,
                    size);

            switch (archetype)
            {
                case EnemyArchetype.Mole:
                    DrawMole(texture);
                    break;

                case EnemyArchetype.Armored:
                    DrawArmored(texture);
                    break;

                case EnemyArchetype.Mech:
                    DrawMech(texture);
                    break;

                case EnemyArchetype.Skeleton:
                    DrawSkeleton(texture);
                    break;

                case EnemyArchetype.Bat:
                    DrawBat(texture);
                    break;

                default:
                    DrawSlime(texture);
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
                "RuntimePixel_" + archetype;

            return sprite;
        }

        private static Sprite BuildBoss(
            EnemyArchetype archetype)
        {
            const int size = 48;

            int act =
                Mathf.Clamp(
                    (int)archetype,
                    0,
                    2);

            Texture2D texture =
                MakeTexture(
                    "PixelBoss_Warden_" + act,
                    size,
                    size);

            Color32 armor =
                act == 0
                    ? new Color32(
                        91,
                        72,
                        53,
                        255)
                    : act == 1
                        ? new Color32(
                            84,
                            86,
                            82,
                            255)
                        : new Color32(
                            65,
                            58,
                            56,
                            255);

            Color32 lightArmor =
                act == 0
                    ? new Color32(
                        137,
                        108,
                        76,
                        255)
                    : act == 1
                        ? new Color32(
                            142,
                            139,
                            124,
                            255)
                        : new Color32(
                            112,
                            97,
                            89,
                            255);

            Color32 cloth =
                act == 0
                    ? new Color32(
                        78,
                        65,
                        44,
                        255)
                    : act == 1
                        ? new Color32(
                            116,
                            74,
                            39,
                            255)
                        : new Color32(
                            111,
                            42,
                            35,
                            255);

            Color32 crest =
                act == 0
                    ? new Color32(
                        161,
                        126,
                        77,
                        255)
                    : act == 1
                        ? new Color32(
                            184,
                            145,
                            74,
                            255)
                        : new Color32(
                            176,
                            77,
                            50,
                            255);

            FillRect(
                texture,
                13,
                5,
                34,
                28,
                Outline);

            FillRect(
                texture,
                15,
                7,
                32,
                27,
                armor);

            FillRect(
                texture,
                8,
                18,
                15,
                31,
                Outline);

            FillRect(
                texture,
                33,
                18,
                40,
                31,
                Outline);

            FillRect(
                texture,
                9,
                19,
                15,
                29,
                lightArmor);

            FillRect(
                texture,
                33,
                19,
                39,
                29,
                lightArmor);

            FillRect(
                texture,
                17,
                28,
                30,
                40,
                Outline);

            FillRect(
                texture,
                19,
                30,
                28,
                38,
                armor);

            FillRect(
                texture,
                17,
                11,
                30,
                17,
                cloth);

            FillRect(
                texture,
                11,
                4,
                18,
                12,
                Outline);

            FillRect(
                texture,
                30,
                4,
                37,
                12,
                Outline);

            FillRect(
                texture,
                12,
                5,
                17,
                11,
                armor);

            FillRect(
                texture,
                31,
                5,
                36,
                11,
                armor);

            FillRect(
                texture,
                20,
                31,
                22,
                32,
                Eye);

            FillRect(
                texture,
                25,
                31,
                27,
                32,
                Eye);

            if (act == 0)
            {
                // Burrow Warden: broad digging claws and low crown.
                FillRect(
                    texture,
                    4,
                    17,
                    9,
                    20,
                    Outline);

                FillRect(
                    texture,
                    39,
                    17,
                    44,
                    20,
                    Outline);

                FillRect(
                    texture,
                    5,
                    18,
                    9,
                    19,
                    crest);

                FillRect(
                    texture,
                    39,
                    18,
                    43,
                    19,
                    crest);

                FillRect(
                    texture,
                    20,
                    40,
                    27,
                    43,
                    Outline);

                FillRect(
                    texture,
                    21,
                    40,
                    26,
                    42,
                    crest);
            }
            else if (act == 1)
            {
                // Iron Warden: gear-like crest and heavier plate edges.
                FillRect(
                    texture,
                    18,
                    40,
                    29,
                    44,
                    Outline);

                FillRect(
                    texture,
                    21,
                    39,
                    26,
                    45,
                    Outline);

                FillRect(
                    texture,
                    20,
                    41,
                    27,
                    43,
                    crest);

                FillRect(
                    texture,
                    22,
                    40,
                    25,
                    44,
                    crest);

                FillRect(
                    texture,
                    8,
                    24,
                    11,
                    29,
                    crest);

                FillRect(
                    texture,
                    36,
                    24,
                    39,
                    29,
                    crest);
            }
            else
            {
                // Ashen Warden: split horn crown and glowing ash chest.
                FillRect(
                    texture,
                    15,
                    39,
                    20,
                    46,
                    Outline);

                FillRect(
                    texture,
                    28,
                    39,
                    33,
                    46,
                    Outline);

                FillRect(
                    texture,
                    16,
                    41,
                    19,
                    45,
                    crest);

                FillRect(
                    texture,
                    29,
                    41,
                    32,
                    45,
                    crest);

                FillRect(
                    texture,
                    21,
                    13,
                    26,
                    17,
                    new Color32(
                        151,
                        54,
                        38,
                        255));

                FillRect(
                    texture,
                    22,
                    14,
                    25,
                    16,
                    new Color32(
                        207,
                        87,
                        53,
                        255));
            }

            FillRect(
                texture,
                21,
                8,
                26,
                12,
                crest);

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
                act == 0
                    ? "RuntimePixel_BurrowWarden"
                    : act == 1
                        ? "RuntimePixel_IronWarden"
                        : "RuntimePixel_AshenWarden";

            return sprite;
        }

        private static void DrawMole(
            Texture2D texture)
        {
            Color32 fur =
                new Color32(
                    103,
                    72,
                    49,
                    255);

            Color32 light =
                new Color32(
                    150,
                    111,
                    75,
                    255);

            Color32 claw =
                new Color32(
                    190,
                    175,
                    137,
                    255);

            FillEllipse(
                texture,
                16,
                11,
                9,
                8,
                Outline);

            FillEllipse(
                texture,
                16,
                12,
                7,
                6,
                fur);

            FillEllipse(
                texture,
                16,
                20,
                7,
                6,
                Outline);

            FillEllipse(
                texture,
                16,
                20,
                5,
                4,
                fur);

            FillRect(
                texture,
                13,
                16,
                18,
                19,
                light);

            FillRect(
                texture,
                10,
                20,
                11,
                21,
                Eye);

            FillRect(
                texture,
                20,
                20,
                21,
                21,
                Eye);

            FillRect(
                texture,
                4,
                7,
                10,
                9,
                Outline);

            FillRect(
                texture,
                22,
                7,
                27,
                9,
                Outline);

            FillRect(
                texture,
                5,
                8,
                10,
                8,
                claw);

            FillRect(
                texture,
                22,
                8,
                26,
                8,
                claw);
        }

        private static void DrawArmored(
            Texture2D texture)
        {
            Color32 steel =
                new Color32(
                    105,
                    108,
                    105,
                    255);

            Color32 steelLight =
                new Color32(
                    151,
                    151,
                    142,
                    255);

            Color32 cloth =
                new Color32(
                    112,
                    50,
                    43,
                    255);

            FillRect(
                texture,
                10,
                5,
                21,
                17,
                Outline);

            FillRect(
                texture,
                11,
                6,
                20,
                16,
                steel);

            FillRect(
                texture,
                9,
                17,
                22,
                25,
                Outline);

            FillRect(
                texture,
                11,
                18,
                20,
                24,
                steelLight);

            FillRect(
                texture,
                11,
                15,
                20,
                18,
                cloth);

            FillRect(
                texture,
                12,
                21,
                14,
                22,
                Eye);

            FillRect(
                texture,
                17,
                21,
                19,
                22,
                Eye);

            FillRect(
                texture,
                4,
                8,
                9,
                18,
                Outline);

            FillRect(
                texture,
                5,
                9,
                8,
                17,
                new Color32(
                    91,
                    65,
                    45,
                    255));

            FillRect(
                texture,
                22,
                7,
                24,
                20,
                Outline);

            FillRect(
                texture,
                23,
                9,
                23,
                22,
                steelLight);
        }

        private static void DrawMech(
            Texture2D texture)
        {
            Color32 rust =
                new Color32(
                    111,
                    79,
                    45,
                    255);

            Color32 metal =
                new Color32(
                    91,
                    88,
                    77,
                    255);

            Color32 brass =
                new Color32(
                    153,
                    113,
                    56,
                    255);

            FillRect(
                texture,
                8,
                5,
                23,
                19,
                Outline);

            FillRect(
                texture,
                10,
                7,
                21,
                18,
                rust);

            FillRect(
                texture,
                10,
                19,
                21,
                26,
                Outline);

            FillRect(
                texture,
                12,
                20,
                19,
                24,
                metal);

            FillRect(
                texture,
                12,
                21,
                14,
                22,
                Eye);

            FillRect(
                texture,
                17,
                21,
                19,
                22,
                Eye);

            FillEllipse(
                texture,
                16,
                12,
                4,
                4,
                Outline);

            FillEllipse(
                texture,
                16,
                12,
                2,
                2,
                brass);

            FillRect(
                texture,
                4,
                8,
                8,
                17,
                Outline);

            FillRect(
                texture,
                23,
                8,
                27,
                17,
                Outline);
        }

        private static void DrawSkeleton(
            Texture2D texture)
        {
            Color32 bone =
                new Color32(
                    194,
                    186,
                    163,
                    255);

            Color32 shadow =
                new Color32(
                    125,
                    119,
                    104,
                    255);

            FillRect(
                texture,
                12,
                7,
                19,
                17,
                Outline);

            FillRect(
                texture,
                14,
                8,
                17,
                17,
                bone);

            for (int y = 10; y <= 16; y += 3)
            {
                FillRect(
                    texture,
                    10,
                    y,
                    21,
                    y + 1,
                    bone);
            }

            FillEllipse(
                texture,
                16,
                22,
                7,
                6,
                Outline);

            FillEllipse(
                texture,
                16,
                22,
                5,
                4,
                bone);

            FillRect(
                texture,
                12,
                22,
                13,
                23,
                Eye);

            FillRect(
                texture,
                18,
                22,
                19,
                23,
                Eye);

            FillRect(
                texture,
                15,
                18,
                16,
                20,
                shadow);

            FillRect(
                texture,
                23,
                8,
                24,
                23,
                Outline);

            FillRect(
                texture,
                24,
                12,
                24,
                25,
                bone);
        }

        private static void DrawBat(
            Texture2D texture)
        {
            Color32 wing =
                new Color32(
                    88,
                    50,
                    76,
                    255);

            Color32 body =
                new Color32(
                    66,
                    51,
                    61,
                    255);

            FillRect(
                texture,
                2,
                11,
                11,
                20,
                Outline);

            FillRect(
                texture,
                20,
                11,
                29,
                20,
                Outline);

            FillRect(
                texture,
                4,
                13,
                11,
                19,
                wing);

            FillRect(
                texture,
                20,
                13,
                27,
                19,
                wing);

            FillEllipse(
                texture,
                16,
                15,
                6,
                7,
                Outline);

            FillEllipse(
                texture,
                16,
                15,
                4,
                5,
                body);

            FillRect(
                texture,
                13,
                17,
                14,
                18,
                Eye);

            FillRect(
                texture,
                17,
                17,
                18,
                18,
                Eye);

            FillRect(
                texture,
                11,
                22,
                13,
                25,
                Outline);

            FillRect(
                texture,
                18,
                22,
                20,
                25,
                Outline);
        }

        private static void DrawSlime(
            Texture2D texture)
        {
            Color32 slime =
                new Color32(
                    88,
                    133,
                    72,
                    255);

            Color32 light =
                new Color32(
                    132,
                    166,
                    103,
                    255);

            FillEllipse(
                texture,
                16,
                10,
                10,
                8,
                Outline);

            FillEllipse(
                texture,
                16,
                11,
                8,
                6,
                slime);

            FillRect(
                texture,
                10,
                5,
                21,
                10,
                slime);

            FillRect(
                texture,
                12,
                13,
                14,
                15,
                light);

            FillRect(
                texture,
                11,
                10,
                12,
                11,
                Outline);

            FillRect(
                texture,
                19,
                10,
                20,
                11,
                Outline);
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
            int safeRx = Mathf.Max(1, rx);
            int safeRy = Mathf.Max(1, ry);

            for (int y = -safeRy; y <= safeRy; y++)
            {
                for (int x = -safeRx; x <= safeRx; x++)
                {
                    float nx =
                        x / (float)safeRx;

                    float ny =
                        y / (float)safeRy;

                    if (nx * nx + ny * ny <= 1f)
                    {
                        int px = cx + x;
                        int py = cy + y;

                        if (px >= 0 &&
                            px < texture.width &&
                            py >= 0 &&
                            py < texture.height)
                        {
                            texture.SetPixel(
                                px,
                                py,
                                color);
                        }
                    }
                }
            }
        }
    }

    public class PixelBillboard : MonoBehaviour
    {
        private Camera viewCamera;
        private Transform anchorParent;
        private Vector3 anchorLocalPosition;
        private bool anchorInitialized;

        private void Start()
        {
            CaptureAnchor();
        }

        private void LateUpdate()
        {
            if (!anchorInitialized ||
                anchorParent != transform.parent)
            {
                CaptureAnchor();
            }

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera == null)
            {
                return;
            }

            Vector3 anchorPosition =
                anchorParent != null
                    ? anchorParent.TransformPoint(
                        anchorLocalPosition)
                    : transform.position;

            if (viewCamera.targetTexture != null)
            {
                Vector3 screen =
                    viewCamera.WorldToScreenPoint(
                        anchorPosition);

                if (screen.z > 0f)
                {
                    screen.x = Mathf.Round(screen.x);
                    screen.y = Mathf.Round(screen.y);

                    transform.position =
                        viewCamera.ScreenToWorldPoint(
                            screen);
                }
                else
                {
                    transform.position =
                        anchorPosition;
                }
            }
            else
            {
                transform.position =
                    anchorPosition;
            }

            Vector3 direction =
                transform.position -
                viewCamera.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
        }

        private void CaptureAnchor()
        {
            anchorParent = transform.parent;
            anchorLocalPosition = transform.localPosition;
            anchorInitialized = true;
        }
    }

}
