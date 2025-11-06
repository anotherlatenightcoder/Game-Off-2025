using System.Collections;
using UnityEngine;

namespace Route24.GameOff
{
    public class SeatedCameraController : MonoBehaviour
    {
        [Header("Camera Rotation")]
        [SerializeField] private float _mouseSensitivity = 3f;
        [Tooltip("Left/Right Limits")]
        [SerializeField] private float _maxYaw = 45f;
        [Tooltip("Up/Down Limits")]
        [SerializeField] private float _maxPitch = 25f;

        [Header("Focus Settings")]
        [SerializeField] private float _focusDuration = 0.6f;
        [SerializeField] private float _focusDistance = -0.65f;
        [SerializeField] private Vector3 _focusOffset = new Vector3(0f, 0.2f, 0f);

        private float _yaw;
        private float _pitch;
        private Vector3 _initialRotation;

        // Focus state
        private Vector3 _defaultPosition;
        private Quaternion _defaultRotation;
        private bool _isFocusing = false;
        private Coroutine _focusRoutine;

        private void Start()
        {
            _initialRotation = transform.localEulerAngles;
            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;

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
        // Focus System
        // ───────────────────────────────
        public void FocusOn(Transform target)
        {
            if (_focusRoutine != null)
                StopCoroutine(_focusRoutine);

            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;

            _focusRoutine = StartCoroutine(FocusRoutine(target));
        }

        private IEnumerator FocusRoutine(Transform target)
        {
            _isFocusing = true;

            Vector3 targetPos = transform.position + target.forward * _focusDistance + _focusOffset;
            Quaternion targetRot = Quaternion.LookRotation(target.forward, Vector3.up);
            targetRot *= Quaternion.Euler(35f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < _focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _focusDuration);

                transform.position = Vector3.Lerp(_defaultPosition, targetPos, t);
                transform.rotation = Quaternion.Slerp(_defaultRotation, targetRot, t);

                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
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
            float elapsed = 0f;

            while (elapsed < _focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _focusDuration);

                transform.position = Vector3.Lerp(startPos, _defaultPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, _defaultRotation, t);

                yield return null;
            }

            transform.position = _defaultPosition;
            transform.rotation = _defaultRotation;
            _isFocusing = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
