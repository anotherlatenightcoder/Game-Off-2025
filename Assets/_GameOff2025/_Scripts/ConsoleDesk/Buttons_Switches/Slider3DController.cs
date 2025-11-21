using System;
using UnityEngine;

namespace Route24.GameOff
{
    public class Slider3DController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform _sliderMesh;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = 1f;
        [SerializeField] private string _label = "Slider";

        [Header("View")]
        [SerializeField] private float _minPos = -0.5f;
        [SerializeField] private float _maxPos = 0.5f;
        [SerializeField] private float _sensitivity = 0.01f; 

        public event Action<float> OnValueChanged;

        private bool _isDragging;
        private float _currentPosition;
        private float _currentRatio;
        private OscilloscopeController _parent;
        private Vector2 _lastMousePos;
        
        private void Start()
        {
            if (_sliderMesh != null)
            {
                _currentPosition = Mathf.Clamp(_sliderMesh.localPosition.y, _minPos, _maxPos);
                _currentRatio = Mathf.InverseLerp(_minPos, _maxPos, _currentPosition);
            }
            UpdatePositionView();
        }

        public void InitializeLink(OscilloscopeController parent)
        {
            _parent = parent;
        }

        public float GetCurrentValue()
        {
            return Mathf.Lerp(_minValue, _maxValue, _currentRatio);
        }

        private void OnMouseDown()
        {
            if (!_parent || !_parent.IsFocused) return;

            _isDragging = true;
            _lastMousePos = Input.mousePosition;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnMouseDrag()
        {
            if (!_isDragging) return;

            CalculatePosition();
            UpdatePositionView();
            FireValueChangedEvent();
        }

        private void OnMouseUp()
        {
            _isDragging = false;
            Cursor.visible = true;
        }

        private void CalculatePosition()
        {
            Vector2 mousePos = (Vector2)Input.mousePosition;
            Vector2 delta = (mousePos - _lastMousePos);
            _lastMousePos = mousePos;

            float verticalInput = delta.y * _sensitivity;
            _currentRatio = Mathf.Clamp01(_currentRatio + verticalInput);
        }

        private void UpdatePositionView()
        {
            if (_sliderMesh != null)
            {
                Vector3 pos = _sliderMesh.localPosition;
                pos.y = Mathf.Lerp(_minPos, _maxPos, _currentRatio);
                _sliderMesh.localPosition = pos;
            }
        }

        private void FireValueChangedEvent()
        {
            OnValueChanged?.Invoke(GetCurrentValue());
        }
    }
}
