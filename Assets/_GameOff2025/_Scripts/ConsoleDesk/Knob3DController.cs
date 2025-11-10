using System;
using UnityEngine;

namespace Route24.GameOff
{
    public class Knob3DController : MonoBehaviour
    {
        [SerializeField] private Transform _knobMesh;
        [SerializeField] private float _minAngle = -60f;
        [SerializeField] private float _maxAngle = 60f;
        [SerializeField] private float _minValue = 0.05f;
        [SerializeField] private float _maxValue = 1f;
        [SerializeField] private float _sensitivity = 0.5f;
        [SerializeField] private string _label = "Knob";

        public event Action<float> OnValueChanged;

        private bool _isDragging;
        private float _normalizedValue = 0.5f;
        private float _currentAngle;
        private OscilloscopeController _parent;

        private void Start()
        {
            UpdateRotation();
        }
        
        public void InitializeLink(OscilloscopeController parent)
        {
            _parent = parent;
        }

        private void OnMouseDown()
        {
            Debug.Log("OnMouseDown");
            
            if (!_parent || !_parent.IsFocused)
            {
                Debug.Log("no parent, or not focused");
                return;
            }

            _isDragging = true;
        }

        private void OnMouseDrag()
        {
            if (!_isDragging) return;

            float delta = -Input.GetAxis("Mouse Y") * _sensitivity;
            
            Debug.Log("Drag: " + delta);
            
            _normalizedValue = Mathf.Clamp01(_normalizedValue + delta * 0.02f);
            UpdateRotation();

            OnValueChanged?.Invoke(Mathf.Lerp(_minValue, _maxValue, _normalizedValue));
        }

        private void OnMouseUp()
        {
            _isDragging = false;
        }

        private void UpdateRotation()
        {
            _currentAngle = Mathf.Lerp(_minAngle, _maxAngle, _normalizedValue);
            if (_knobMesh)
                _knobMesh.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
        }
    }
}