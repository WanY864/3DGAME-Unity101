using UnityEngine;

namespace Depthbound
{
    public static class ProceduralWeaponMesh
    {
        public static Transform CreateBlade(
            Transform parent,
            string name,
            Vector3 localPosition,
            float width,
            float bodyLength,
            float tipLength,
            float thickness,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = 2;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            float halfWidth = width * 0.5f;
            float halfThickness = thickness * 0.5f;
            float end = bodyLength;
            float tip = bodyLength + tipLength;

            Vector3[] vertices =
            {
                new Vector3(-halfWidth,  halfThickness, 0f),
                new Vector3( halfWidth,  halfThickness, 0f),
                new Vector3( halfWidth,  halfThickness, end),
                new Vector3(0f,         halfThickness, tip),
                new Vector3(-halfWidth,  halfThickness, end),

                new Vector3(-halfWidth, -halfThickness, 0f),
                new Vector3( halfWidth, -halfThickness, 0f),
                new Vector3( halfWidth, -halfThickness, end),
                new Vector3(0f,        -halfThickness, tip),
                new Vector3(-halfWidth, -halfThickness, end)
            };

            int[] triangles =
            {
                0, 1, 2,
                0, 2, 4,
                4, 2, 3,

                7, 6, 5,
                9, 7, 5,
                8, 7, 9,

                0, 5, 6,
                0, 6, 1,

                1, 6, 7,
                1, 7, 2,

                2, 7, 8,
                2, 8, 3,

                3, 8, 9,
                3, 9, 4,

                4, 9, 5,
                4, 5, 0
            };

            Vector2[] uv =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0.82f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, 0.82f),

                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0.82f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, 0.82f)
            };

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer bladeRenderer = go.GetComponent<MeshRenderer>();
            bladeRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            bladeRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", 0.38f);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.42f);
                }

                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", 0.42f);
                }

                bladeRenderer.sharedMaterial = material;
            }

            return go.transform;
        }

        public static Transform CreatePommel(
            Transform parent,
            string name,
            Vector3 localPosition,
            float size,
            Color color)
        {
            GameObject pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = name;
            pommel.layer = 2;
            pommel.transform.SetParent(parent, false);
            pommel.transform.localPosition = localPosition;
            pommel.transform.localScale = new Vector3(size, size * 0.72f, size);

            Collider collider = pommel.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            Renderer pommelRenderer = pommel.GetComponent<Renderer>();
            pommelRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            pommelRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", 0.32f);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.38f);
                }

                pommelRenderer.sharedMaterial = material;
            }

            return pommel.transform;
        }
    }
}
