using UnityEngine;

namespace TestCarsGame.Camera
{
    [CreateAssetMenu(
        fileName = "OrbitCameraSettings",
        menuName = "Test Cars Game/Camera/Orbit Camera Settings")]
    public sealed class OrbitCameraSettings : ScriptableObject
    {
        [Header("Initial State")]
        public float initialYaw;
        public float initialPitch = 25f;
        [Min(0.01f)] public float initialDistance = 7f;

        [Header("Orbit")]
        [Min(0.01f)] public float rotationSpeed = 180f;
        [Min(0.01f)] public float manualRotationSpeed = 90f;
        public bool invertHorizontal;
        public bool invertVertical = true;
        public float minPitch = -15f;
        public float maxPitch = 65f;

        [Header("Zoom")]
        [Min(0.01f)] public float zoomSpeed = 8f;
        [Min(0.01f)] public float manualZoomSpeed = 5f;
        [Min(0.01f)] public float minDistance = 3f;
        [Min(0.01f)] public float maxDistance = 12f;

        [Header("Touch Input")]
        [Tooltip("Enable one-finger orbit and two-finger pinch gestures for touch devices.")]
        public bool enableTouchInput = true;
        [Tooltip("Sensitivity for one-finger touch orbiting. Higher values make the camera rotate more for the same drag distance.")]
        [Min(0.001f)] public float touchRotationSpeed = 0.35f;
        [Tooltip("Sensitivity for two-finger pinch zooming. Higher values make the zoom change more for the same pinch distance change.")]
        [Min(0.001f)] public float pinchZoomSpeed = 0.01f;
        [Tooltip("When enabled, touches that begin over Unity UI are ignored for the duration of that touch interaction.")]
        public bool ignoreTouchesOverUI = true;

        [Header("Smoothing")]
        [Min(0f)] public float rotationSmoothTime = 0.08f;
        [Min(0f)] public float zoomSmoothTime = 0.08f;

        [Header("Target")]
        public Vector3 targetOffset = Vector3.up;

        private static OrbitCameraSettings defaultSettings;

        public static OrbitCameraSettings Default
        {
            get
            {
                if (defaultSettings == null)
                {
                    defaultSettings = CreateInstance<OrbitCameraSettings>();
                }

                return defaultSettings;
            }
        }

        private void OnValidate()
        {
            if (maxPitch < minPitch)
            {
                maxPitch = minPitch;
            }

            if (maxDistance < minDistance)
            {
                maxDistance = minDistance;
            }
        }
    }
}
