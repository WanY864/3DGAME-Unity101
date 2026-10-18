using UnityEngine;
using UnityEngine.Rendering;

namespace Depthbound
{
    public class DungeonRelicPickup : MonoBehaviour
    {
        private int actIndex;
        private bool collected;
        private SpriteRenderer spriteRenderer;
        private float phase;

        public void Configure(int act)
        {
            actIndex = Mathf.Clamp(act, 0, 2);
            phase = actIndex * 1.7f;

            SphereCollider trigger =
                GetComponent<SphereCollider>();

            if (trigger == null)
            {
                trigger =
                    gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = 0.72f;


            BuildVisual();
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            phase += Time.deltaTime;

            transform.localPosition =
                new Vector3(
                    transform.localPosition.x,
                    0.88f +
                    Mathf.Sin(phase * 2.1f) * 0.10f,
                    transform.localPosition.z);

            transform.Rotate(
                0f,
                Time.deltaTime * 22f,
                0f,
                Space.Self);
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (collected ||
                !other.CompareTag("Player"))
            {
                return;
            }

            PlayerHealth health =
                other.GetComponent<PlayerHealth>();

            PlayerCombat combat =
                other.GetComponent<PlayerCombat>();

            FirstPersonController controller =
                other.GetComponent<FirstPersonController>();

            switch (actIndex)
            {
                case 0:
                    if (health != null)
                    {
                        health.IncreaseMaxHealth(5f);
                        health.Heal(20f);
                    }
                    break;

                case 1:
                    if (combat != null)
                    {
                        combat.AddDamage(0.10f);
                    }
                    break;

                default:
                    if (controller != null)
                    {
                        controller.AddMobility(
                            0.06f,
                            0.08f);
                    }

                    if (health != null)
                    {
                        health.Heal(12f);
                    }
                    break;
            }

            collected = true;

            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.RecordRelic();
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowRelicAcquired(
                    actIndex);
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.Boon();
            }

            CombatEffects.SpawnRuneBurst(
                transform.position,
                1.1f);

            Destroy(gameObject);
        }

        private void BuildVisual()
        {
            GameObject spriteObject =
                new GameObject(
                    "RelicSprite",
                    typeof(SpriteRenderer),
                    typeof(PixelBillboard));

            spriteObject.transform.SetParent(
                transform,
                false);

            spriteRenderer =
                spriteObject.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite =
                BuildSprite(actIndex);

            spriteRenderer.sortingOrder = 6;
            spriteRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            spriteRenderer.receiveShadows = false;

            GameObject glowObject =
                new GameObject(
                    "RelicGlow",
                    typeof(Light));

            glowObject.transform.SetParent(
                transform,
                false);

            Light glow =
                glowObject.GetComponent<Light>();

            glow.type = LightType.Point;
            glow.range = 2.2f;
            glow.intensity = 0.45f;
            glow.shadows = LightShadows.None;

            glow.color =
                actIndex == 0
                    ? new Color(
                        0.72f,
                        0.56f,
                        0.28f)
                    : actIndex == 1
                        ? new Color(
                            0.72f,
                            0.42f,
                            0.18f)
                        : new Color(
                            0.58f,
                            0.68f,
                            0.58f);
        }

        private static Sprite BuildSprite(
            int act)
        {
            const int size = 16;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32 clear =
                new Color32(0, 0, 0, 0);

            Color32 outline =
                new Color32(
                    30,
                    30,
                    28,
                    255);

            Color32[] pixels =
                new Color32[size * size];

            for (int i = 0;
                 i < pixels.Length;
                 i++)
            {
                pixels[i] = clear;
            }

            texture.SetPixels32(pixels);

            if (act == 0)
            {
                DrawRect(
                    texture,
                    3,
                    4,
                    12,
                    11,
                    outline);

                DrawRect(
                    texture,
                    4,
                    5,
                    11,
                    10,
                    new Color32(
                        132,
                        91,
                        48,
                        255));

                DrawRect(
                    texture,
                    6,
                    8,
                    9,
                    9,
                    new Color32(
                        211,
                        179,
                        104,
                        255));
            }
            else if (act == 1)
            {
                DrawRect(
                    texture,
                    3,
                    3,
                    12,
                    12,
                    outline);

                DrawRect(
                    texture,
                    5,
                    5,
                    10,
                    10,
                    new Color32(
                        127,
                        92,
                        55,
                        255));

                DrawRect(
                    texture,
                    7,
                    2,
                    8,
                    13,
                    new Color32(
                        181,
                        134,
                        72,
                        255));

                DrawRect(
                    texture,
                    2,
                    7,
                    13,
                    8,
                    new Color32(
                        181,
                        134,
                        72,
                        255));
            }
            else
            {
                DrawRect(
                    texture,
                    5,
                    2,
                    10,
                    13,
                    outline);

                DrawRect(
                    texture,
                    6,
                    3,
                    9,
                    12,
                    new Color32(
                        189,
                        183,
                        160,
                        255));

                DrawRect(
                    texture,
                    3,
                    7,
                    12,
                    8,
                    new Color32(
                        128,
                        123,
                        109,
                        255));
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
                        0.5f),
                    16f);

            sprite.name =
                "RuntimeRelic_" + act;

            return sprite;
        }

        private static void DrawRect(
            Texture2D texture,
            int x0,
            int y0,
            int x1,
            int y1,
            Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (x >= 0 &&
                        x < texture.width &&
                        y >= 0 &&
                        y < texture.height)
                    {
                        texture.SetPixel(
                            x,
                            y,
                            color);
                    }
                }
            }
        }
    }
}
