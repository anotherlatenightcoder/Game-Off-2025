using System.Collections;
using UnityEngine;

namespace Route24.GameOff
{
    public class SeatedCameraController : MonoBehaviour
    {
        [Header("Camera Rotation")]
        [SerializeField] private float _mouseSensitivity = 3f;
        [SerializeField] private float _maxYaw = 45f;
        [SerializeField] private float _maxPitch = 25f;

        [Header("Focus Settings")]
        [SerializeField] private float _focusDuration = 0.6f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float _zoomFOV = 40f;
        [SerializeField] private float _zoomDuration = 0.35f;

        private float _yaw;
        private float _pitch;
        private Vector3 _initialRotation;

        private Vector3 _defaultPosition;
        private Quaternion _defaultRotation;
        private float _defaultFOV;

        private Camera _cam;
        private bool _isFocusing = false;
        private bool _isZoomed = false;
        private Coroutine _focusRoutine;
        private Coroutine _zoomRoutine;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _initialRotation = transform.localEulerAngles;

            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;
            _defaultFOV = _cam.fieldOfView;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_isFocusing) return;

            HandleMouseLook();
            HandleZoomToggle();
        }
        
        private void HandleZoomToggle()
        {
            if (Input.GetMouseButtonDown(1))
            {
                ToggleZoom();
            }
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

            _yaw += mouseX;
            _pitch -= mouseY;

            _yaw = Mathf.Clamp(_yaw, -_maxYaw, _maxYaw);
            _pitch = Mathf.Clamp(_pitch, -_maxPitch, _maxPitch);

            transform.localEulerAngles = new Vector3(
                _initialRotation.x + _pitch,
                _initialRotation.y + _yaw,
                _initialRotation.z
            );
        }
        
        private void ToggleZoom()
        {
            if (_zoomRoutine != null)
                StopCoroutine(_zoomRoutine);

            _zoomRoutine = StartCoroutine(_isZoomed ? ZoomOutRoutine() : ZoomInRoutine());
            _isZoomed = !_isZoomed;
        }
        
        private IEnumerator ZoomInRoutine()
        {
            float startFOV = _cam.fieldOfView;
            float endFOV = _zoomFOV;

            float elapsed = 0f;
            while (elapsed < _zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _zoomDuration);

                _cam.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
                yield return null;
            }

            _cam.fieldOfView = endFOV;
        }

        private IEnumerator ZoomOutRoutine()
        {
            float startFOV = _cam.fieldOfView;
            float endFOV = _defaultFOV;

            float elapsed = 0f;
            while (elapsed < _zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _zoomDuration);

                _cam.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
                yield return null;
            }

            _cam.fieldOfView = endFOV;
        }

        // ───────────────────────────────
        // Focus System
        // ───────────────────────────────
        public void FocusOn(Transform focusPoint, Transform lookTarget, float newFOV = 40f)
        {
            if (_focusRoutine != null)
                StopCoroutine(_focusRoutine);

            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;

            // When entering focus:
            // - DO NOT reset zoom!
            // - DO NOT reset to default FOV!
            // Instead we lerp from current FOV → newFOV naturally.

            // Disable zoom input but DO NOT change FOV
            if (_zoomRoutine != null)
                StopCoroutine(_zoomRoutine);

            _isZoomed = false;

            _focusRoutine = StartCoroutine(FocusRoutine(focusPoint, lookTarget, newFOV));
        }

        private IEnumerator FocusRoutine(Transform focusPoint, Transform lookTarget, float newFOV)
        {
            _isFocusing = true;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startFOV = _cam.fieldOfView; // ← smooth transition from current zoom state

            Vector3 endPos = focusPoint.position;
            Quaternion endRot = Quaternion.LookRotation(
                lookTarget.position - focusPoint.position,
                Vector3.up
            );

            float elapsed = 0f;
            while (elapsed < _focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _focusDuration);

                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                _cam.fieldOfView = Mathf.Lerp(startFOV, newFOV, t);

                yield return null;
            }

            transform.position = endPos;
            transform.rotation = endRot;
            _cam.fieldOfView = newFOV;
        }

        public void ReturnToDefault()
        {
            if (_focusRoutine != null)
                StopCoroutine(_focusRoutine);

            _focusRoutine = StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startFOV = _cam.fieldOfView;

            float elapsed = 0f;
            while (elapsed < _focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _focusDuration);

                transform.position = Vector3.Lerp(startPos, _defaultPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, _defaultRotation, t);
                _cam.fieldOfView = Mathf.Lerp(startFOV, _defaultFOV, t);

                yield return null;
            }

            transform.position = _defaultPosition;
            transform.rotation = _defaultRotation;
            _cam.fieldOfView = _defaultFOV;

            // Fully out of focus: allow zoom normally
            _isZoomed = false;
            _isFocusing = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
