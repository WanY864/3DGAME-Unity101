using UnityEngine;

namespace Depthbound
{
    public class PixelFlameFlicker : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private Color baseColor = Color.white;
        private float timer;
        private int step;

        public void Configure(
            bool bossFlame)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            baseScale =
                transform.localScale;

            baseColor =
                bossFlame
                    ? new Color(
                        0.88f,
                        0.56f,
                        0.38f,
                        1f)
                    : new Color(
                        0.96f,
                        0.78f,
                        0.48f,
                        1f);

            ApplyStep();
        }

        private void Awake()
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            baseScale =
                transform.localScale;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer < 0.09f)
            {
                return;
            }

            timer = 0f;
            step = (step + 1) % 4;
            ApplyStep();
        }

        private void ApplyStep()
        {
            float x =
                step % 2 == 0
                    ? 1f
                    : 0.92f;

            float y =
                step == 1 ||
                step == 2
                    ? 1.07f
                    : 0.96f;

            transform.localScale =
                new Vector3(
                    baseScale.x * x,
                    baseScale.y * y,
                    baseScale.z);

            if (spriteRenderer != null)
            {
                float brightness =
                    step == 2
                        ? 1f
                        : 0.86f;

                spriteRenderer.color =
                    new Color(
                        baseColor.r * brightness,
                        baseColor.g * brightness,
                        baseColor.b * brightness,
                        baseColor.a);
            }
        }
    }
}
