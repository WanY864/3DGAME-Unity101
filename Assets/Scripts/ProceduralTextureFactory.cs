using System.Collections.Generic;
using UnityEngine;

namespace Depthbound
{
    public static class ProceduralTextureFactory
    {
        private static readonly Dictionary<string, Texture2D> Cache =
            new Dictionary<string, Texture2D>();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCache()
        {
            Cache.Clear();
        }

        public static Texture2D Stone(
            string key,
            int size,
            int seed,
            Color dark,
            Color light,
            float grainScale = 7f,
            float crackStrength = 0.18f)
        {
            if (Cache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "Generated_" + key;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            Color[] pixels = new Color[size * size];
            float seedX = seed * 13.731f;
            float seedY = seed * 7.913f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    float broad = Mathf.PerlinNoise(
                        seedX + u * grainScale,
                        seedY + v * grainScale);

                    float fine = Mathf.PerlinNoise(
                        seedX * 0.43f + u * grainScale * 4.2f,
                        seedY * 0.37f + v * grainScale * 4.2f);

                    float veins = Mathf.Abs(
                        Mathf.PerlinNoise(
                            seedX * 0.21f + u * grainScale * 1.7f,
                            seedY * 0.19f + v * grainScale * 1.7f) - 0.5f) * 2f;

                    float crack = Mathf.SmoothStep(0.84f, 0.98f, veins) * crackStrength;
                    float tone = Mathf.Clamp01(broad * 0.72f + fine * 0.28f - crack);

                    Color color = Color.Lerp(dark, light, tone);
                    color *= Mathf.Lerp(0.88f, 1.06f, fine);
                    color.a = 1f;

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            Cache[key] = texture;
            return texture;
        }

        public static Texture2D Metal(
            string key,
            int size,
            int seed,
            Color dark,
            Color light)
        {
            if (Cache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "Generated_" + key;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            Color[] pixels = new Color[size * size];
            float seedX = seed * 5.17f;
            float seedY = seed * 9.31f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    float noise = Mathf.PerlinNoise(
                        seedX + u * 18f,
                        seedY + v * 2.2f);

                    float scratch = Mathf.PerlinNoise(
                        seedX * 0.4f + u * 3f,
                        seedY * 0.6f + v * 42f);

                    float value = Mathf.Clamp01(noise * 0.62f + scratch * 0.38f);
                    Color color = Color.Lerp(dark, light, value);
                    color.a = 1f;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            Cache[key] = texture;
            return texture;
        }

        public static void Apply(Material material, Texture texture, Vector2 tiling)
        {
            if (material == null || texture == null) return;

            material.mainTexture = texture;
            material.mainTextureScale = tiling;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }
        }
    }
}
