using System;
using Unity.Mathematics;
using UnityEngine;

namespace Route24.GameOff
{
    public class Knob3DController : MonoBehaviour
    {
        [SerializeField] private Transform _knobMesh;
        [SerializeField] private float _minValue = 0.05f;
        [SerializeField] private float _maxValue = 1f;
        [SerializeField] private string _label = "Knob";

        // set below 3 values according to fbx model
        private float _minAngle = -80f;
        private float _maxAngle = 80f;
        private float _sensitivity = 0.3f;

        public event Action<float> OnValueChanged;

        private bool _isDragging;
        private float _currentAngle;
        private OscilloscopeController _parent;
        private Vector2 _lastMousePos;
        private const float _minRotationInput = 20;

        private void Start()
        {
            UpdateRotationView();
        }

        public void InitializeLink(OscilloscopeController parent)
        {
            _parent = parent;
        }

        private void OnMouseDown()
        {
            if (!_parent || !_parent.IsFocused) return;

            _isDragging = true;
            _lastMousePos = Input.mousePosition;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined; // this should not be here
        }

        private void OnMouseDrag()
        {
            if (!_isDragging) return;

            CalculateAngle();
            UpdateRotationView();
            FireValueChangedEvent();
        }

        private void OnMouseUp()
        {
            _isDragging = false;
            Cursor.visible = true; 
        }

        private void CalculateAngle() // if angle = 0: %100 horizontal input, if angle min or max: %100 vertical input. 
        {
            Vector2 mousePos = (Vector2)Input.mousePosition;
            Vector2 delta = (mousePos - _lastMousePos);
            _lastMousePos = mousePos;

            float normalized = Mathf.InverseLerp(_minAngle, _maxAngle, _currentAngle);
            float verticalWeight = Mathf.Abs(Mathf.Lerp(-1f, 1f, normalized));
            float horizontalWeight = 1f - verticalWeight;

            float verticalInput = delta.y * verticalWeight;
            if (_currentAngle < 0f) verticalInput *= -1f;

            float horizontalInput = delta.x * horizontalWeight;

            float blendedDelta = (horizontalInput - verticalInput) * _sensitivity;

            if (math.abs(blendedDelta) < _minRotationInput * Time.deltaTime)
                return;

            _currentAngle = Mathf.Clamp(_currentAngle + blendedDelta, _minAngle, _maxAngle);
        }

        private void UpdateRotationView()
        {
            if (_knobMesh != null)
                _knobMesh.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
        }

        private void FireValueChangedEvent()
        {
            float t = Mathf.InverseLerp(_minAngle, _maxAngle, _currentAngle);
            float value = Mathf.Lerp(_minValue, _maxValue, t);
            OnValueChanged?.Invoke(value);
        }
    }
}
