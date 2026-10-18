using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Depthbound
{
    /// Renders the 3D game through a small point-filtered buffer.
    /// This gives the dungeon the chunky 2D/pixel presentation used by
    /// the current art direction without replacing gameplay physics.
    public class PixelPresentation : MonoBehaviour
    {
        private const int TargetHeight = 180;

        private Camera worldCamera;
        private Camera weaponCamera;
        private Camera displayCamera;
        private RenderTexture pixelTexture;
        private RawImage display;
        private Canvas displayCanvas;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            FindCameras();
            RebuildTarget();
        }

        private void Update()
        {
            if (worldCamera == null ||
                weaponCamera == null)
            {
                FindCameras();
            }

            if (worldCamera != null &&
                pixelTexture == null)
            {
                RebuildTarget();
            }

            if (Screen.width != lastScreenWidth ||
                Screen.height != lastScreenHeight)
            {
                RebuildTarget();
            }
        }

        private void OnDestroy()
        {
            DetachTarget();
            ReleaseTarget();
        }

        private void FindCameras()
        {
            worldCamera = Camera.main;

            if (worldCamera == null)
            {
                return;
            }

            Transform weapon =
                worldCamera.transform.Find("WeaponCamera");

            if (weapon != null)
            {
                weaponCamera =
                    weapon.GetComponent<Camera>();

                if (weaponCamera != null &&
                    pixelTexture != null)
                {
                    weaponCamera.targetTexture =
                        pixelTexture;

                    weaponCamera.allowMSAA = false;
                    weaponCamera.allowHDR = false;
                }
            }

            if (worldCamera != null &&
                pixelTexture != null)
            {
                worldCamera.targetTexture =
                    pixelTexture;
            }
        }

        private void RebuildTarget()
        {
            if (worldCamera == null)
            {
                return;
            }

            lastScreenWidth =
                Mathf.Max(1, Screen.width);

            lastScreenHeight =
                Mathf.Max(1, Screen.height);

            float aspect =
                lastScreenWidth /
                (float)lastScreenHeight;

            int height =
                Mathf.Min(
                    TargetHeight,
                    lastScreenHeight);

            int width =
                Mathf.Max(
                    2,
                    Mathf.RoundToInt(
                        height * aspect));

            width =
                Mathf.Max(
                    2,
                    width / 2 * 2);

            height =
                Mathf.Max(
                    2,
                    height / 2 * 2);

            DetachTarget();
            ReleaseTarget();

            pixelTexture =
                new RenderTexture(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);

            pixelTexture.name =
                "DepthboundPixelBuffer_" +
                width +
                "x" +
                height;

            pixelTexture.filterMode =
                FilterMode.Point;

            pixelTexture.wrapMode =
                TextureWrapMode.Clamp;

            pixelTexture.antiAliasing = 1;
            pixelTexture.useMipMap = false;
            pixelTexture.autoGenerateMips = false;
            pixelTexture.Create();

            worldCamera.targetTexture =
                pixelTexture;

            worldCamera.allowMSAA = false;
            worldCamera.allowHDR = false;

            if (weaponCamera != null)
            {
                weaponCamera.targetTexture =
                    pixelTexture;

                weaponCamera.allowMSAA = false;
                weaponCamera.allowHDR = false;
            }

            EnsureDisplay();
            display.texture = pixelTexture;
        }

        private void EnsureDisplay()
        {
            if (display != null)
            {
                EnsureDisplayCamera();
                return;
            }

            GameObject canvasObject =
                new GameObject(
                    "PixelDisplayCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler));

            canvasObject.transform.SetParent(
                transform,
                false);

            displayCanvas =
                canvasObject.GetComponent<Canvas>();

            displayCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            displayCanvas.pixelPerfect = true;
            displayCanvas.sortingOrder = -100;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject imageObject =
                new GameObject(
                    "PixelDisplay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));

            imageObject.transform.SetParent(
                canvasObject.transform,
                false);

            RectTransform rect =
                imageObject.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            display =
                imageObject.GetComponent<RawImage>();

            display.raycastTarget = false;
            display.color = Color.white;

            EnsureDisplayCamera();
        }

        private void EnsureDisplayCamera()
        {
            if (displayCamera != null)
            {
                return;
            }

            GameObject cameraObject =
                new GameObject(
                    "PixelDisplayCamera",
                    typeof(Camera));

            cameraObject.transform.SetParent(
                transform,
                false);

            displayCamera =
                cameraObject.GetComponent<Camera>();

            // The world and weapon cameras render into the low-resolution
            // texture. Keep one camera targeting Display 1 so the Editor Game
            // view and packaged player have a valid final output surface.
            displayCamera.clearFlags =
                CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = Color.black;
            displayCamera.cullingMask = 0;
            displayCamera.depth = -1000f;
            displayCamera.allowHDR = false;
            displayCamera.allowMSAA = false;
            displayCamera.targetDisplay = 0;
            displayCamera.targetTexture = null;
        }

        private void DetachTarget()
        {
            if (worldCamera != null &&
                worldCamera.targetTexture == pixelTexture)
            {
                worldCamera.targetTexture = null;
            }

            if (weaponCamera != null &&
                weaponCamera.targetTexture == pixelTexture)
            {
                weaponCamera.targetTexture = null;
            }

            if (display != null)
            {
                display.texture = null;
            }
        }

        private void ReleaseTarget()
        {
            if (pixelTexture == null)
            {
                return;
            }

            if (pixelTexture.IsCreated())
            {
                pixelTexture.Release();
            }

            Destroy(pixelTexture);
            pixelTexture = null;
        }
    }
}
