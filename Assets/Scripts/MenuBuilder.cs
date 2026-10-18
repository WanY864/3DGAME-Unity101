using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Depthbound
{
    public static class MenuBuilder
    {
        private static Material menuTorchMetalMaterial;
        private static Material menuTorchFlameMaterial;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeMaterials()
        {
            menuTorchMetalMaterial = null;
            menuTorchFlameMaterial = null;
        }

        public static void Build()
        {
            if (GameObject.Find("MenuRuntime") != null) return;

            Time.timeScale = 1f;
            QuitUtility.UnlockCursor();

            RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.012f, 0.017f, 0.026f);
            RenderSettings.fogDensity = 0.023f;

            GameObject runtime = new GameObject("MenuRuntime");
            ProceduralAudio audio = runtime.AddComponent<ProceduralAudio>();
            audio.SetMenuMix();
            runtime.AddComponent<SceneFadeController>();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));

                eventSystem.transform.SetParent(runtime.transform);
            }

            Camera camera = CreateCamera(runtime.transform);
            CreateBackdrop(runtime.transform, camera);
            CreateMenuUI(runtime.transform);
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject go = new GameObject(
                "MenuCamera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(MenuCameraDrift));

            go.tag = "MainCamera";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, 2.75f, -8.8f);
            go.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);

            Camera cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.010f, 0.014f, 0.022f);
            cam.fieldOfView = 57f;
            cam.farClipPlane = 60f;
            cam.allowHDR = true;
            cam.allowMSAA = true;

            return cam;
        }

        private static void CreateBackdrop(Transform parent, Camera camera)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material stone = MakeMaterial(shader, new Color(0.075f, 0.084f, 0.102f), 0.08f, 0.26f);
            Material trim = MakeMaterial(shader, new Color(0.18f, 0.20f, 0.23f), 0.30f, 0.42f);
            Material gate = MakeMaterial(shader, new Color(0.032f, 0.038f, 0.048f), 0.62f, 0.52f);
            Material rune = MakeMaterial(shader, new Color(0.08f, 0.46f, 0.56f), 0.08f, 0.44f);

            ProceduralTextureFactory.Apply(
                stone,
                ProceduralTextureFactory.Stone(
                    "MenuStone",
                    128,
                    101,
                    new Color(0.58f, 0.60f, 0.64f),
                    new Color(1.04f, 1.06f, 1.10f),
                    6.5f,
                    0.20f),
                new Vector2(3.5f, 3.5f));

            ProceduralTextureFactory.Apply(
                trim,
                ProceduralTextureFactory.Stone(
                    "MenuTrim",
                    96,
                    109,
                    new Color(0.65f, 0.66f, 0.69f),
                    new Color(1.04f, 1.05f, 1.08f),
                    8.5f,
                    0.10f),
                new Vector2(2.5f, 2.5f));

            ProceduralTextureFactory.Apply(
                gate,
                ProceduralTextureFactory.Metal(
                    "MenuGate",
                    96,
                    131,
                    new Color(0.50f, 0.52f, 0.55f),
                    new Color(1.05f, 1.06f, 1.08f)),
                new Vector2(2.0f, 2.0f));

            if (rune.HasProperty("_EmissionColor"))
            {
                rune.EnableKeyword("_EMISSION");
                rune.SetColor("_EmissionColor", new Color(0.02f, 0.55f, 0.74f));
            }

            GameObject keyLight = new GameObject("ColdKeyLight", typeof(Light));
            keyLight.transform.SetParent(parent);
            keyLight.transform.rotation = Quaternion.Euler(48f, -24f, 0f);

            Light directional = keyLight.GetComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0.30f;
            directional.color = new Color(0.48f, 0.58f, 0.80f);
            directional.shadows = LightShadows.Soft;
            directional.shadowStrength = 0.38f;

            CreatePrimitive(parent, PrimitiveType.Cube, "Floor",
                new Vector3(0f, -0.35f, 5.0f),
                new Vector3(18f, 0.7f, 24f),
                stone);

            CreatePrimitive(parent, PrimitiveType.Cube, "LeftWall",
                new Vector3(-5.4f, 2.65f, 5.0f),
                new Vector3(1.2f, 6f, 17f),
                stone);

            CreatePrimitive(parent, PrimitiveType.Cube, "RightWall",
                new Vector3(5.4f, 2.65f, 5.0f),
                new Vector3(1.2f, 6f, 17f),
                stone);

            CreatePrimitive(parent, PrimitiveType.Cube, "Ceiling",
                new Vector3(0f, 5.55f, 5.0f),
                new Vector3(12f, 0.7f, 17f),
                stone);

            GameObject menuGate = CreatePrimitive(
                parent,
                PrimitiveType.Cube,
                "Gate",
                new Vector3(0f, 2.25f, 6.2f),
                new Vector3(5.4f, 4.5f, 0.55f),
                gate);

            menuGate.AddComponent<MenuGateSequence>();

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 3.55f;

                CreatePrimitive(parent, PrimitiveType.Cube, "GateColumn",
                    new Vector3(x, 2.0f, 5.72f),
                    new Vector3(0.78f, 4.25f, 0.78f),
                    trim);

                CreatePrimitive(parent, PrimitiveType.Cube, "GateColumnBase",
                    new Vector3(x, 0.28f, 5.72f),
                    new Vector3(1.18f, 0.42f, 1.18f),
                    trim);

                CreatePrimitive(parent, PrimitiveType.Cube, "GateColumnCap",
                    new Vector3(x, 4.05f, 5.72f),
                    new Vector3(1.12f, 0.34f, 1.12f),
                    trim);
            }

            CreatePrimitive(parent, PrimitiveType.Cube, "GateLintel",
                new Vector3(0f, 4.34f, 5.72f),
                new Vector3(7.9f, 0.55f, 0.84f),
                trim);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    CreatePrimitive(parent, PrimitiveType.Cube, "SideButtress",
                        new Vector3(side * 4.72f, 1.65f, i * 4.0f - 0.5f),
                        new Vector3(0.42f, 3.45f, 0.55f),
                        trim);
                }
            }

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "RunSigil";
            core.transform.SetParent(parent);
            core.transform.position = new Vector3(0f, 0.025f, 2.2f);
            core.transform.localScale = new Vector3(2.1f, 0.025f, 2.1f);

            Object.Destroy(core.GetComponent<Collider>());
            core.GetComponent<Renderer>().sharedMaterial = rune;
            core.AddComponent<RunePulse>();

            GameObject runeLightObject = new GameObject("RunSigilLight", typeof(Light));
            runeLightObject.transform.SetParent(core.transform, false);
            runeLightObject.transform.localPosition = new Vector3(0f, 22f, 0f);

            Light runeLight = runeLightObject.GetComponent<Light>();
            runeLight.type = LightType.Point;
            runeLight.range = 6.5f;
            runeLight.intensity = 1.1f;
            runeLight.color = new Color(0.10f, 0.58f, 0.72f);

            CreateMenuTorch(parent, new Vector3(-4.7f, 2.5f, 0.2f), new Color(1f, 0.42f, 0.10f));
            CreateMenuTorch(parent, new Vector3(4.7f, 2.5f, 0.2f), new Color(1f, 0.42f, 0.10f));

            GameObject emberField = new GameObject("EmberField", typeof(ParticleSystem));
            emberField.transform.SetParent(parent);
            emberField.transform.position = new Vector3(0f, 1.5f, 2.8f);

            ParticleSystem particles = emberField.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
            main.startColor = new Color(0.30f, 0.52f, 0.68f, 0.60f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 8f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 3.5f, 10f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);

            CombatEffects.ApplyParticleMaterial(
                particles,
                new Color(0.30f, 0.52f, 0.68f, 0.60f));
        }

        private static void CreateMenuTorch(Transform parent, Vector3 position, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (menuTorchMetalMaterial == null)
            {
                menuTorchMetalMaterial = MakeMaterial(
                    shader,
                    new Color(0.20f, 0.21f, 0.22f),
                    0.65f,
                    0.55f);

                menuTorchMetalMaterial.name =
                    "RuntimeMenuTorchMetal";
            }

            if (menuTorchFlameMaterial == null)
            {
                menuTorchFlameMaterial = MakeMaterial(
                    shader,
                    color,
                    0f,
                    0.20f);

                menuTorchFlameMaterial.name =
                    "RuntimeMenuTorchFlame";

                if (menuTorchFlameMaterial.HasProperty("_EmissionColor"))
                {
                    menuTorchFlameMaterial.EnableKeyword("_EMISSION");
                    menuTorchFlameMaterial.SetColor(
                        "_EmissionColor",
                        color * 3f);
                }
            }

            Material metal = menuTorchMetalMaterial;
            Material flameMaterial = menuTorchFlameMaterial;

            Transform root = new GameObject("MenuTorch").transform;
            root.SetParent(parent);
            root.position = position;

            CreatePrimitive(root, PrimitiveType.Cube, "Bracket",
                new Vector3(0f, -0.12f, 0f),
                new Vector3(0.20f, 0.55f, 0.20f),
                metal);

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            flame.transform.SetParent(root, false);
            flame.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            flame.transform.localScale = new Vector3(0.22f, 0.36f, 0.22f);
            Object.Destroy(flame.GetComponent<Collider>());
            Renderer flameRenderer = flame.GetComponent<Renderer>();
            flameRenderer.sharedMaterial = flameMaterial;
            flameRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            flameRenderer.receiveShadows = false;

            GameObject lightObject = new GameObject("Light", typeof(Light), typeof(TorchFlicker));
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.40f, 0f);

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = 2.6f;
            light.color = color;
            light.shadows =
                position.x < 0f
                    ? LightShadows.Soft
                    : LightShadows.None;
            light.renderMode =
                position.x < 0f
                    ? LightRenderMode.ForcePixel
                    : LightRenderMode.ForceVertex;
            light.shadowStrength = 0.58f;
        }

        private static void CreateMenuUI(Transform parent)
        {
            Canvas canvas = UIFactory.CreateCanvas("MainMenuCanvas");
            canvas.transform.SetParent(parent);

            Image shade = UIFactory.CreatePanel(
                canvas.transform,
                "Shade",
                new Color(0f, 0f, 0f, 0.28f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            shade.raycastTarget = false;

            Image vignette = UIFactory.CreatePanel(
                canvas.transform,
                "MenuVignette",
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            vignette.sprite = UIFactory.VignetteSprite;
            vignette.raycastTarget = false;

            Image titlePlate = UIFactory.CreatePanel(
                canvas.transform,
                "TitlePlate",
                new Color(0.015f, 0.020f, 0.030f, 0.70f),
                new Vector2(0.19f, 0.665f),
                new Vector2(0.81f, 0.905f),
                Vector2.zero,
                Vector2.zero);
            titlePlate.raycastTarget = false;

            Text title = UIFactory.CreateText(
                canvas.transform,
                "Title",
                "D E P T H B O U N D",
                66,
                TextAnchor.MiddleCenter,
                new Color(0.92f, 0.95f, 0.98f),
                new Vector2(0.16f, 0.73f),
                new Vector2(0.84f, 0.88f),
                Vector2.zero,
                Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            title.raycastTarget = false;

            Text subtitle = UIFactory.CreateText(
                canvas.transform,
                "Subtitle",
                "A FIRST-PERSON DUNGEON RUN",
                20,
                TextAnchor.MiddleCenter,
                new Color(0.42f, 0.76f, 0.86f),
                new Vector2(0.22f, 0.665f),
                new Vector2(0.78f, 0.73f),
                Vector2.zero,
                Vector2.zero);
            subtitle.raycastTarget = false;

            UIFactory.CreateButton(
                canvas.transform,
                "StartButton",
                "DESCEND",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f),
                new Vector2(330f, 68f),
                () => MenuGateSequence.BeginDescend(),
                new Color(0.065f, 0.27f, 0.34f, 0.97f));

            UIFactory.CreateButton(
                canvas.transform,
                "QuitButton",
                "QUIT",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -96f),
                new Vector2(330f, 56f),
                QuitUtility.Quit,
                new Color(0.16f, 0.055f, 0.065f, 0.94f));

            Text loopText = UIFactory.CreateText(
                canvas.transform,
                "Loop",
                "CLEAR THE CHAMBER   |   CHOOSE A BOON   |   GO DEEPER",
                18,
                TextAnchor.MiddleCenter,
                new Color(0.68f, 0.72f, 0.77f),
                new Vector2(0.18f, 0.24f),
                new Vector2(0.82f, 0.31f),
                Vector2.zero,
                Vector2.zero);
            loopText.raycastTarget = false;

            Text controls = UIFactory.CreateText(
                canvas.transform,
                "Controls",
                "WASD  MOVE      MOUSE  LOOK      LMB  ATTACK      RMB  BLOCK\nQ  SWITCH WEAPON      SPACE  BOOST      ESC  PAUSE",
                17,
                TextAnchor.MiddleCenter,
                new Color(0.54f, 0.59f, 0.65f),
                new Vector2(0.12f, 0.07f),
                new Vector2(0.88f, 0.18f),
                Vector2.zero,
                Vector2.zero);
            controls.raycastTarget = false;

            Text version = UIFactory.CreateText(
                canvas.transform,
                "Version",
                "PROTOTYPE v1.0.0",
                14,
                TextAnchor.LowerLeft,
                new Color(0.36f, 0.40f, 0.46f),
                new Vector2(0.02f, 0.012f),
                new Vector2(0.18f, 0.05f),
                Vector2.zero,
                Vector2.zero);
            version.raycastTarget = false;

            Image settingsPlate = UIFactory.CreatePanel(
                canvas.transform,
                "SettingsPlate",
                new Color(0.012f, 0.020f, 0.030f, 0.80f),
                new Vector2(0.79f, 0.385f),
                new Vector2(0.985f, 0.645f),
                Vector2.zero,
                Vector2.zero);
            settingsPlate.raycastTarget = false;

            Text settingsLabel = UIFactory.CreateText(
                canvas.transform,
                "SettingsLabel",
                "SETTINGS",
                13,
                TextAnchor.MiddleRight,
                new Color(0.36f, 0.48f, 0.56f),
                new Vector2(0.81f, 0.612f),
                new Vector2(0.97f, 0.638f),
                Vector2.zero,
                Vector2.zero);
            settingsLabel.raycastTarget = false;

            Text shake = UIFactory.CreateText(
                canvas.transform,
                "MenuShake",
                "",
                14,
                TextAnchor.MiddleRight,
                new Color(0.50f, 0.58f, 0.64f),
                new Vector2(0.81f, 0.565f),
                new Vector2(0.925f, 0.602f),
                Vector2.zero,
                Vector2.zero);
            shake.raycastTarget = false;

            Text displayMode = UIFactory.CreateText(
                canvas.transform,
                "MenuDisplay",
                "",
                14,
                TextAnchor.MiddleRight,
                new Color(0.50f, 0.58f, 0.64f),
                new Vector2(0.80f, 0.505f),
                new Vector2(0.925f, 0.542f),
                Vector2.zero,
                Vector2.zero);
            displayMode.raycastTarget = false;

            Text sensitivity = UIFactory.CreateText(
                canvas.transform,
                "MenuSensitivity",
                "",
                14,
                TextAnchor.MiddleRight,
                new Color(0.50f, 0.58f, 0.64f),
                new Vector2(0.81f, 0.445f),
                new Vector2(0.925f, 0.482f),
                Vector2.zero,
                Vector2.zero);
            sensitivity.raycastTarget = false;

            Text volume = UIFactory.CreateText(
                canvas.transform,
                "MenuVolume",
                "",
                14,
                TextAnchor.MiddleRight,
                new Color(0.50f, 0.58f, 0.64f),
                new Vector2(0.81f, 0.405f),
                new Vector2(0.925f, 0.442f),
                Vector2.zero,
                Vector2.zero);
            volume.raycastTarget = false;

            System.Action refreshSettings = () =>
            {
                shake.text =
                    "SHAKE  " +
                    Mathf.RoundToInt(RuntimeSettings.CameraShakeScale * 100f) +
                    "%";

                displayMode.text =
                    RuntimeSettings.Fullscreen
                        ? "DISPLAY  FULLSCREEN"
                        : "DISPLAY  WINDOWED";

                sensitivity.text =
                    "SENS  " +
                    RuntimeSettings.MouseScale.ToString("0.0") +
                    "x";

                volume.text =
                    "VOL  " +
                    Mathf.RoundToInt(RuntimeSettings.MasterVolume * 100f) +
                    "%";
            };

            UIFactory.CreateButton(
                canvas.transform,
                "MenuShakeMinus",
                "-",
                new Vector2(0.945f, 0.583f),
                Vector2.zero,
                new Vector2(38f, 30f),
                () =>
                {
                    RuntimeSettings.AdjustCameraShake(-0.25f);
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            UIFactory.CreateButton(
                canvas.transform,
                "MenuShakePlus",
                "+",
                new Vector2(0.972f, 0.583f),
                Vector2.zero,
                new Vector2(38f, 30f),
                () =>
                {
                    RuntimeSettings.AdjustCameraShake(0.25f);
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            Button fullButton = UIFactory.CreateButton(
                canvas.transform,
                "MenuDisplayToggle",
                "FULL",
                new Vector2(0.955f, 0.523f),
                Vector2.zero,
                new Vector2(82f, 30f),
                () =>
                {
                    RuntimeSettings.ToggleFullscreen();
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            // 主菜单 FULL 按钮使用小字，避免挤出边框。
            fullButton.GetComponentInChildren<Text>().fontSize = 14;

            UIFactory.CreateButton(
                canvas.transform,
                "MenuSensMinus",
                "-",
                new Vector2(0.945f, 0.463f),
                Vector2.zero,
                new Vector2(38f, 30f),
                () =>
                {
                    RuntimeSettings.AdjustMouseScale(-0.10f);
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            UIFactory.CreateButton(
                canvas.transform,
                "MenuSensPlus",
                "+",
                new Vector2(0.972f, 0.463f),
                Vector2.zero,
                new Vector2(38f, 30f),
                () =>
                {
                    RuntimeSettings.AdjustMouseScale(0.10f);
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            UIFactory.CreateButton(
                canvas.transform,
                "MenuVolMinus",
                "-",
                new Vector2(0.945f, 0.423f),
                Vector2.zero,
                new Vector2(38f, 30f),
                () =>
                {
                    RuntimeSettings.AdjustMasterVolume(-0.10f);
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            UIFactory.CreateButton(
                canvas.transform,
                "MenuVolPlus",
                "+",
                new Vector2(0.972f, 0.423f),
                Vector2.zero,
                new Vector2(38f, 30f),
                () =>
                {
                    RuntimeSettings.AdjustMasterVolume(0.10f);
                    refreshSettings();
                },
                new Color(0.055f, 0.10f, 0.13f, 0.94f));

            refreshSettings();
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(
            Shader shader,
            Color color,
            float metallic,
            float smoothness)
        {
            Material material = new Material(shader);
            material.color = color;
            material.enableInstancing = true;

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }
    }

    public class MenuGateSequence : MonoBehaviour
    {
        public static MenuGateSequence Instance { get; private set; }

        private Vector3 closedPosition;
        private bool started;

        private void Awake()
        {
            Instance = this;
            closedPosition = transform.localPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void BeginDescend()
        {
            if (Instance != null)
            {
                Instance.Begin();
            }
            else
            {
                SceneFadeController.Load("Game");
            }
        }

        private void Begin()
        {
            if (started) return;
            started = true;
            StartCoroutine(SequenceRoutine());
        }

        private System.Collections.IEnumerator SequenceRoutine()
        {
            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.GateOpen(transform.position);
            }

            Vector3 start = transform.localPosition;
            Vector3 end = closedPosition + Vector3.up * 4.8f;
            const float liftDuration = 0.52f;
            bool loadStarted = false;

            for (float elapsed = 0f; elapsed < liftDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / liftDuration);
                float eased = t * t * (3f - 2f * t);
                transform.localPosition = Vector3.Lerp(start, end, eased);

                if (!loadStarted && t >= 0.28f)
                {
                    loadStarted = true;
                    SceneFadeController.Load("Game");
                }

                yield return null;
            }

            transform.localPosition = end;

            if (!loadStarted)
            {
                SceneFadeController.Load("Game");
            }
        }
    }

    public class MenuCameraDrift : MonoBehaviour
    {
        private Vector3 basePosition;
        private Quaternion baseRotation;

        private void Start()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        private void Update()
        {
            float t = Time.unscaledTime;

            transform.position = basePosition + new Vector3(
                Mathf.Sin(t * 0.18f) * 0.12f,
                Mathf.Sin(t * 0.23f) * 0.055f,
                Mathf.Cos(t * 0.15f) * 0.08f);

            transform.rotation = baseRotation * Quaternion.Euler(
                Mathf.Sin(t * 0.16f) * 0.35f,
                Mathf.Sin(t * 0.13f) * 0.65f,
                0f);
        }
    }
}
