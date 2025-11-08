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

        private float _yaw;
        private float _pitch;
        private Vector3 _initialRotation;

        private Vector3 _defaultPosition;
        private Quaternion _defaultRotation;
        private float _defaultFOV;

        private Camera _cam;
        private bool _isFocusing = false;
        private Coroutine _focusRoutine;

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

        // ───────────────────────────────
        // Focus System (now with external anchor support)
        // ───────────────────────────────
        public void FocusOn(Transform focusPoint, Transform lookTarget, float newFOV = 40f)
        {
            if (_focusRoutine != null)
                StopCoroutine(_focusRoutine);

            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;
            _defaultFOV = _cam.fieldOfView;

            _focusRoutine = StartCoroutine(FocusRoutine(focusPoint, lookTarget, newFOV));
        }

        private IEnumerator FocusRoutine(Transform focusPoint, Transform lookTarget, float newFOV)
        {
            _isFocusing = true;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startFOV = _cam.fieldOfView;

            Vector3 endPos = focusPoint.position;
            Quaternion endRot = Quaternion.LookRotation(lookTarget.position - focusPoint.position, Vector3.up);

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
            _isFocusing = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
