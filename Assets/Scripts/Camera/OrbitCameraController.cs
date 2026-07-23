using UnityEngine;
using UnityEngine.InputSystem;

namespace TestCarsGame.Camera
{
    [DisallowMultipleComponent]
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private OrbitCameraSettings settings;

        private float yaw;
        private float pitch;
        private float distance;
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;
        private float yawVelocity;
        private float pitchVelocity;
        private float distanceVelocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public float Yaw => targetYaw;
        public float Pitch => targetPitch;
        public float Distance => targetDistance;

        private void Awake()
        {
            targetYaw = Settings.initialYaw;
            targetPitch = Settings.initialPitch;
            targetDistance = Settings.initialDistance;
            ClampTargets();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ReadMouse();
            SmoothState();
            ApplyCameraTransform();
        }

        public void Rotate(Vector2 degreesDelta)
        {
            targetYaw += degreesDelta.x;
            targetPitch += degreesDelta.y;
            ClampTargets();
        }

        public void Zoom(float distanceDelta)
        {
            targetDistance += distanceDelta;
            ClampTargets();
        }

        public void RotateManual(Vector2 normalizedInput)
        {
            if (normalizedInput.sqrMagnitude > 1f)
            {
                normalizedInput.Normalize();
            }

            Rotate(normalizedInput * Settings.manualRotationSpeed * Time.deltaTime);
        }

        public void ZoomManual(float normalizedInput)
        {
            Zoom(normalizedInput * Settings.manualZoomSpeed * Time.deltaTime);
        }

        public void SetOrbit(float newYaw, float newPitch, float newDistance, bool snap = false)
        {
            targetYaw = newYaw;
            targetPitch = newPitch;
            targetDistance = newDistance;
            ClampTargets();

            if (snap)
            {
                SnapToTarget();
            }
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            yaw = targetYaw;
            pitch = targetPitch;
            distance = targetDistance;
            yawVelocity = 0f;
            pitchVelocity = 0f;
            distanceVelocity = 0f;
            ApplyCameraTransform();
        }

        private void ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                float horizontal = Settings.invertHorizontal ? -delta.x : delta.x;
                float vertical = Settings.invertVertical ? -delta.y : delta.y;
                Rotate(new Vector2(horizontal, vertical) * Settings.rotationSpeed * Time.deltaTime);
            }

            float scrollSteps = mouse.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(scrollSteps) > Mathf.Epsilon)
            {
                Zoom(-scrollSteps * Settings.zoomSpeed * 0.1f);
            }
        }

        private void SmoothState()
        {
            yaw = SmoothAngle(yaw, targetYaw, ref yawVelocity, Settings.rotationSmoothTime);
            pitch = SmoothAngle(pitch, targetPitch, ref pitchVelocity, Settings.rotationSmoothTime);
            distance = SmoothValue(distance, targetDistance, ref distanceVelocity, Settings.zoomSmoothTime);
        }

        private void ApplyCameraTransform()
        {
            if (target == null)
            {
                return;
            }

            Vector3 orbitCenter = target.position + Settings.targetOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(
                orbitCenter - rotation * Vector3.forward * distance,
                rotation);
        }

        private void ClampTargets()
        {
            targetPitch = Mathf.Clamp(targetPitch, Settings.minPitch, Settings.maxPitch);
            targetDistance = Mathf.Clamp(targetDistance, Settings.minDistance, Settings.maxDistance);
        }

        private static float SmoothAngle(float current, float targetValue, ref float velocity, float smoothTime)
        {
            if (smoothTime <= 0f)
            {
                velocity = 0f;
                return targetValue;
            }

            return Mathf.SmoothDampAngle(current, targetValue, ref velocity, smoothTime);
        }

        private static float SmoothValue(float current, float targetValue, ref float velocity, float smoothTime)
        {
            if (smoothTime <= 0f)
            {
                velocity = 0f;
                return targetValue;
            }

            return Mathf.SmoothDamp(current, targetValue, ref velocity, smoothTime);
        }

        private OrbitCameraSettings Settings => settings != null ? settings : OrbitCameraSettings.Default;
    }
}
