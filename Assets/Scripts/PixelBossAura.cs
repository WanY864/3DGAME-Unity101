using UnityEngine;
using UnityEngine.Rendering;

namespace Depthbound
{
    public class PixelBossAura : MonoBehaviour
    {
        private const int BlockCount = 6;

        private readonly Transform[] blocks =
            new Transform[BlockCount];

        private Material material;
        private float stepTimer;
        private int step;

        public void Configure(Color color)
        {
            material = BuildMaterial(color);

            for (int i = 0;
                 i < BlockCount;
                 i++)
            {
                GameObject block =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                block.name =
                    "AuraPixel_" + i;

                block.transform.SetParent(
                    transform,
                    false);

                block.transform.localScale =
                    Vector3.one * 0.12f;

                Collider collider =
                    block.GetComponent<Collider>();

                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer =
                    block.GetComponent<Renderer>();

                renderer.sharedMaterial = material;
                renderer.shadowCastingMode =
                    ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                blocks[i] = block.transform;
            }

            ApplyStep();
        }

        private void Update()
        {
            stepTimer += Time.deltaTime;

            if (stepTimer < 0.11f)
            {
                return;
            }

            stepTimer = 0f;
            step = (step + 1) % 8;
            ApplyStep();
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void ApplyStep()
        {
            for (int i = 0;
                 i < blocks.Length;
                 i++)
            {
                Transform block = blocks[i];

                if (block == null)
                {
                    continue;
                }

                int phase =
                    (step + i * 2) % 8;

                float side =
                    i % 2 == 0
                        ? -1f
                        : 1f;

                float layer =
                    i / 2;

                float x =
                    side *
                    (0.72f +
                     layer * 0.16f);

                float y =
                    0.45f +
                    ((phase + i) % 5) *
                    0.36f;

                float z =
                    ((phase % 3) - 1) *
                    0.08f;

                block.localPosition =
                    new Vector3(
                        x,
                        y,
                        z);

                float size =
                    phase % 2 == 0
                        ? 0.12f
                        : 0.09f;

                block.localScale =
                    Vector3.one * size;
            }
        }

        private static Material BuildMaterial(
            Color color)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material created =
                new Material(shader);

            created.name =
                "RuntimePixelBossAura";

            created.color = color;

            if (created.HasProperty(
                "_BaseColor"))
            {
                created.SetColor(
                    "_BaseColor",
                    color);
            }

            if (created.HasProperty(
                "_EmissionColor"))
            {
                created.EnableKeyword(
                    "_EMISSION");

                created.SetColor(
                    "_EmissionColor",
                    color * 1.15f);
            }

            return created;
        }
    }
}
