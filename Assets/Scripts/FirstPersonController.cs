using UnityEngine;

namespace Depthbound
{

    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float acceleration = 28f;
        public float mouseSensitivity = 2.0f;
        public float boostImpulse = 24f; 
        public float boostCooldown = 1.15f;

        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float BoostReady01 => Mathf.Clamp01((Time.time - lastBoostTime) / boostCooldown);
        public bool IsBoostInvulnerable => Time.time - lastBoostTime <= 0.18f;

        private Rigidbody body;
        private Camera viewCamera;
        private Camera weaponCamera;
        private Vector3 cameraBasePosition;
        private float cameraBaseFov;
        private float weaponCameraBaseFov = 68f;
        private float pitch;
        private float smoothMouseX;
        private float smoothMouseY;
        private bool boostRequested;
        private float lastBoostTime = -99f;
        private bool boostAttackConsumed = true;
        private float bobClock;
        private float cameraKick;
        private float rollKick;
        private float fovKick;
        private float stepDistance;
        private float airborneTime;
        private bool wasGrounded = true;
        private bool alternateStep;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            viewCamera = Camera.main != null ? Camera.main : GetComponentInChildren<Camera>();
            if (viewCamera != null)
            {
                cameraBasePosition = viewCamera.transform.localPosition;
                cameraBaseFov = viewCamera.fieldOfView;

                Transform weaponCameraTransform = viewCamera.transform.Find("WeaponCamera");
                if (weaponCameraTransform != null)
                {
                    weaponCamera = weaponCameraTransform.GetComponent<Camera>();
                    if (weaponCamera != null)
                    {
                        weaponCameraBaseFov = weaponCamera.fieldOfView;
                    }
                }
            }
        }

        private void Start()
        {
            QuitUtility.LockCursor();
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) &&
                HUDController.Instance != null)
            {
                HUDController.Instance.TogglePause();
            }

            if (HUDController.Instance != null &&
                HUDController.Instance.IsModalOpen)
            {
                boostRequested = false;
                QuitUtility.UnlockCursor();
                return;
            }

            if (Application.isFocused)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }

                Cursor.visible = false;
            }


            float sensitivity =
                mouseSensitivity * RuntimeSettings.MouseScale;

            float rawMouseX =
                Input.GetAxis("Mouse X") * sensitivity;

            float rawMouseY =
                Input.GetAxis("Mouse Y") * sensitivity;

            float smoothing = 1f - Mathf.Exp(
                -15f * Time.unscaledDeltaTime
            );

            smoothMouseX = Mathf.Lerp(
                smoothMouseX,
                rawMouseX,
                smoothing
            );

            smoothMouseY = Mathf.Lerp(
                smoothMouseY,
                rawMouseY,
                smoothing
            );

            transform.Rotate(
                Vector3.up * smoothMouseX
            );

            pitch = Mathf.Clamp(
                pitch - smoothMouseY,
                -82f,
                82f
            );


            if (Input.GetKeyDown(KeyCode.Space) &&
                Time.time >= lastBoostTime + boostCooldown)
            {
                boostRequested = true;
            }

            UpdateGroundState();
            UpdateCameraFeel();
            UpdateFootsteps();
        }


        private void OnApplicationFocus(bool hasFocus)
        {
            if (HUDController.Instance == null) return;

            if (!hasFocus)
            {
                if (!HUDController.Instance.IsModalOpen)
                {
                    HUDController.Instance.SetPaused(true);
                }

                return;
            }

            if (!HUDController.Instance.IsModalOpen)
            {
                QuitUtility.LockCursor();
            }
        }

        private void FixedUpdate()
        {
            if (HUDController.Instance != null &&
                HUDController.Instance.IsModalOpen)
            {
                return;
            }

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 desiredDirection = (transform.right * input.x + transform.forward * input.y).normalized;
            Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            float targetSpeed = moveSpeed * MoveSpeedMultiplier;
            Vector3 targetVelocity = desiredDirection * targetSpeed;
            Vector3 velocityError = targetVelocity - horizontalVelocity;
            body.AddForce(velocityError * acceleration, ForceMode.Acceleration);

            if (boostRequested)
            {
                boostRequested = false;
                lastBoostTime = Time.time;
                boostAttackConsumed = false;
                Vector3 boostDirection = desiredDirection.sqrMagnitude > 0.01f ? desiredDirection : transform.forward;
                body.AddForce(boostDirection * boostImpulse, ForceMode.Impulse);
                fovKick = Mathf.Max(fovKick, 5.5f);
                cameraKick = Mathf.Max(cameraKick, 0.045f);
                if (ProceduralAudio.Instance != null) ProceduralAudio.Instance.Boost();
            }
        }

        public void AddMovementSpeed(float amount)
        {
            MoveSpeedMultiplier += amount;
        }

        public void AddMobility(float speedAmount, float boostCooldownReduction)
        {
            MoveSpeedMultiplier += speedAmount;
            boostCooldown = Mathf.Max(
                0.68f,
                boostCooldown * Mathf.Clamp01(1f - boostCooldownReduction));
        }

        public void RefundBoostCooldown(
            float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            lastBoostTime -= seconds;
        }

        public bool TryConsumeBoostAttack()
        {
            if (boostAttackConsumed || Time.time - lastBoostTime > 0.55f)
            {
                return false;
            }

            boostAttackConsumed = true;
            return true;
        }

        public void AddCameraImpact(float magnitude, float rollDirection = 1f)
        {
            cameraKick = Mathf.Max(cameraKick, magnitude * 0.035f);
            rollKick += Mathf.Clamp(rollDirection, -1f, 1f) * magnitude * 1.4f;
            fovKick = Mathf.Max(fovKick, magnitude * 0.55f);
        }

        private void UpdateGroundState()
        {
            bool grounded = IsGrounded();

            if (!grounded)
            {
                airborneTime += Time.deltaTime;
            }
            else
            {
                if (!wasGrounded && airborneTime > 0.12f)
                {
                    float landingStrength = Mathf.Clamp01(airborneTime / 0.85f);
                    cameraKick = Mathf.Max(
                        cameraKick,
                        Mathf.Lerp(0.012f, 0.036f, landingStrength));

                    rollKick += Mathf.Sin(Time.time * 13f) * 0.35f * landingStrength;

                    if (ProceduralAudio.Instance != null)
                    {
                        ProceduralAudio.Instance.Land(landingStrength);
                    }
                }

                airborneTime = 0f;
            }

            wasGrounded = grounded;
        }

        private void UpdateFootsteps()
        {
            if (body == null) return;

            Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;

            if (speed < 0.85f || !IsGrounded())
            {
                stepDistance = Mathf.Max(0f, stepDistance - Time.deltaTime * 0.35f);
                return;
            }

            stepDistance += speed * Time.deltaTime;
            float stride = Mathf.Lerp(2.25f, 1.65f, Mathf.Clamp01(speed / 10f));

            if (stepDistance >= stride)
            {
                stepDistance = 0f;
                alternateStep = !alternateStep;

                if (ProceduralAudio.Instance != null)
                {
                    ProceduralAudio.Instance.Footstep(alternateStep);
                }
            }
        }

        private bool IsGrounded()
        {
            int mask = ~(1 << 2);
            return Physics.Raycast(
                transform.position + Vector3.up * 0.30f,
                Vector3.down,
                0.58f,
                mask,
                QueryTriggerInteraction.Ignore);
        }

        private void UpdateCameraFeel()
        {
            if (viewCamera == null || body == null) return;

            Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(0.01f, moveSpeed * MoveSpeedMultiplier));

            if (normalizedSpeed > 0.08f)
            {
                bobClock += Time.deltaTime * Mathf.Lerp(6.5f, 10.5f, normalizedSpeed);
            }

            float bobAmount = 0.028f * normalizedSpeed;
            Vector3 bobOffset = new Vector3(
                Mathf.Cos(bobClock * 0.5f) * bobAmount * 0.65f,
                Mathf.Sin(bobClock) * bobAmount,
                0f);

            float shakeScale = RuntimeSettings.CameraShakeScale;
            Vector3 shakeOffset = Random.insideUnitSphere * cameraKick * shakeScale;
            shakeOffset.z = 0f;
            viewCamera.transform.localPosition = Vector3.Lerp(
                viewCamera.transform.localPosition,
                cameraBasePosition + bobOffset + shakeOffset,
                1f - Mathf.Exp(-18f * Time.deltaTime));

            float movementRoll = -Input.GetAxisRaw("Horizontal") * 1.35f * normalizedSpeed;
            viewCamera.transform.localRotation = Quaternion.Euler(
                pitch + Random.Range(-cameraKick, cameraKick) * 12f * shakeScale,
                Random.Range(-cameraKick, cameraKick) * 8f * shakeScale,
                movementRoll + rollKick * shakeScale);

            float speedFov = Mathf.Clamp(speed - moveSpeed * MoveSpeedMultiplier, 0f, 8f) * 0.45f;
            float targetFov = cameraBaseFov + speedFov + fovKick;
            viewCamera.fieldOfView = Mathf.Lerp(
                viewCamera.fieldOfView,
                targetFov,
                1f - Mathf.Exp(-10f * Time.deltaTime));

            if (weaponCamera != null)
            {
                float worldDelta = viewCamera.fieldOfView - cameraBaseFov;
                float targetWeaponFov =
                    weaponCameraBaseFov +
                    worldDelta * 0.28f;

                weaponCamera.fieldOfView = Mathf.Lerp(
                    weaponCamera.fieldOfView,
                    targetWeaponFov,
                    1f - Mathf.Exp(-10f * Time.deltaTime));
            }

            cameraKick = Mathf.MoveTowards(cameraKick, 0f, Time.deltaTime * 0.30f);
            rollKick = Mathf.Lerp(rollKick, 0f, 1f - Mathf.Exp(-11f * Time.deltaTime));
            fovKick = Mathf.Lerp(fovKick, 0f, 1f - Mathf.Exp(-7f * Time.deltaTime));
        }
    }
}
