using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace TestCarsGame.Camera
{
    [DisallowMultipleComponent]
    public sealed class OrbitCameraController : MonoBehaviour
    {
        private enum GestureState
        {
            None,
            OneFingerOrbit,
            TwoFingerPinch
        }

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

        private readonly HashSet<int> ignoredTouchIds = new HashSet<int>();
        private GestureState gestureState = GestureState.None;
        private int orbitTouchId = -1;
        private Vector2 orbitTouchLastPosition;
        private bool orbitTouchInitialized;
        private bool skipSingleTouchFrame;
        private int pinchTouchIdA = -1;
        private int pinchTouchIdB = -1;
        private float pinchDistance;
        private bool pinchDistanceInitialized;

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
            ReadTouch();
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

        private void ReadTouch()
        {
            if (!Settings.enableTouchInput)
            {
                ClearTouchState();
                return;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                ClearTouchState();
                return;
            }

            int activeTouchCount = 0;
            TouchControl firstTouch = null;
            TouchControl secondTouch = null;
            bool hasIgnoredTouch = false;

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                TouchControl touch = touchscreen.touches[i];
                if (touch == null || !touch.press.isPressed)
                {
                    continue;
                }

                if (ShouldIgnoreTouch(touch))
                {
                    hasIgnoredTouch = true;
                    continue;
                }

                activeTouchCount++;
                if (firstTouch == null)
                {
                    firstTouch = touch;
                }
                else if (secondTouch == null)
                {
                    secondTouch = touch;
                }
            }

            if (activeTouchCount == 0)
            {
                if (hasIgnoredTouch)
                {
                    return;
                }

                ClearTouchState();
                return;
            }

            if (activeTouchCount == 1)
            {
                UpdateOneFingerOrbit(firstTouch);
                return;
            }

            UpdateTwoFingerPinch(firstTouch, secondTouch);
        }

        private bool ShouldIgnoreTouch(TouchControl touch)
        {
            if (!Settings.ignoreTouchesOverUI)
            {
                return false;
            }

            int touchId = GetTouchId(touch);
            if (touchId < 0)
            {
                return false;
            }

            if (ignoredTouchIds.Contains(touchId))
            {
                return true;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
            if (touch.press.wasPressedThisFrame && overUi)
            {
                ignoredTouchIds.Add(touchId);
                return true;
            }

            return false;
        }

        private void UpdateOneFingerOrbit(TouchControl touch)
        {
            if (touch == null)
            {
                return;
            }

            if (gestureState == GestureState.TwoFingerPinch)
            {
                gestureState = GestureState.None;
                skipSingleTouchFrame = true;
                orbitTouchId = -1;
                orbitTouchInitialized = false;
            }

            int touchId = GetTouchId(touch);
            if (skipSingleTouchFrame)
            {
                skipSingleTouchFrame = false;
                gestureState = GestureState.OneFingerOrbit;
                orbitTouchId = touchId;
                orbitTouchLastPosition = touch.position.ReadValue();
                orbitTouchInitialized = true;
                return;
            }

            if (gestureState != GestureState.OneFingerOrbit || !orbitTouchInitialized || orbitTouchId != touchId)
            {
                gestureState = GestureState.OneFingerOrbit;
                orbitTouchId = touchId;
                orbitTouchLastPosition = touch.position.ReadValue();
                orbitTouchInitialized = true;
                return;
            }

            Vector2 currentPosition = touch.position.ReadValue();
            Vector2 delta = currentPosition - orbitTouchLastPosition;
            orbitTouchLastPosition = currentPosition;

            if (delta.sqrMagnitude > 0.0001f)
            {
                float horizontal = Settings.invertHorizontal ? -delta.x : delta.x;
                float vertical = Settings.invertVertical ? -delta.y : delta.y;
                Rotate(new Vector2(horizontal, vertical) * Settings.touchRotationSpeed);
            }
        }

        private void UpdateTwoFingerPinch(TouchControl firstTouch, TouchControl secondTouch)
        {
            if (firstTouch == null || secondTouch == null)
            {
                return;
            }

            int firstTouchId = GetTouchId(firstTouch);
            int secondTouchId = GetTouchId(secondTouch);

            if (gestureState != GestureState.TwoFingerPinch || firstTouchId != pinchTouchIdA || secondTouchId != pinchTouchIdB)
            {
                gestureState = GestureState.TwoFingerPinch;
                pinchTouchIdA = firstTouchId;
                pinchTouchIdB = secondTouchId;
                pinchDistance = GetTouchDistance(firstTouch, secondTouch);
                pinchDistanceInitialized = true;
                return;
            }

            if (!pinchDistanceInitialized)
            {
                pinchDistance = GetTouchDistance(firstTouch, secondTouch);
                pinchDistanceInitialized = true;
                return;
            }

            float currentDistance = GetTouchDistance(firstTouch, secondTouch);
            float distanceDelta = currentDistance - pinchDistance;
            pinchDistance = currentDistance;

            if (Mathf.Abs(distanceDelta) > Mathf.Epsilon)
            {
                Zoom(distanceDelta * Settings.pinchZoomSpeed);
            }
        }

        private static float GetTouchDistance(TouchControl firstTouch, TouchControl secondTouch)
        {
            if (firstTouch == null || secondTouch == null)
            {
                return 0f;
            }

            return Vector2.Distance(firstTouch.position.ReadValue(), secondTouch.position.ReadValue());
        }

        private static int GetTouchId(TouchControl touch)
        {
            if (touch == null)
            {
                return -1;
            }

            return touch.touchId.ReadValue();
        }

        private void ClearTouchState()
        {
            gestureState = GestureState.None;
            orbitTouchId = -1;
            orbitTouchLastPosition = Vector2.zero;
            orbitTouchInitialized = false;
            skipSingleTouchFrame = false;
            pinchTouchIdA = -1;
            pinchTouchIdB = -1;
            pinchDistance = 0f;
            pinchDistanceInitialized = false;
            ignoredTouchIds.Clear();
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
