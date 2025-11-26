using System.Collections;
using UnityEngine;
using Route24.Core;

namespace Route24.GameOff
{
    public class Switch : BaseSensor, IInteractable
    {
        [Header("Start Settings")]
        [SerializeField] private bool _startActive;

        [Header("Lever Settings")]
        [SerializeField] private Transform _model;
        [SerializeField] private float _minAngle = -30f;
        [SerializeField] private float _maxAngle = 30f;
        [SerializeField] private float _toggleDuration = 0.5f;
        [SerializeField] private string _toggleOnText = "Switch On [E]";
        [SerializeField] private string _toggleOffText = "Switch Off [E]";

        private Coroutine _toggleRoutine;
        [SerializeField] private float _currentAngle;

        private void Awake()
        {
            SetState(_startActive, instant: true);
        }

        // Public API
        public void Toggle() => SetState(!IsActive);
        public void ForceOn(bool instant = false) => SetState(true, instant);
        public void ForceOff(bool instant = false) => SetState(false, instant);

        // Core Logic
        private void SetState(bool active, bool instant = false)
        {
            if (IsActive == active && !instant)
                return;

            IsActive = active;
            float targetAngle = active ? _maxAngle : _minAngle;

            if (instant)
            {
                _currentAngle = targetAngle;

                if (_model != null)
                    _model.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);

                return;
            }
            else
                if (_toggleRoutine != null)
                StopCoroutine(_toggleRoutine);

            _toggleRoutine = StartCoroutine(SmoothToggle(targetAngle));

            if (active)
                FireActivated();
            else
                FireDeactivated();
        }

        private IEnumerator SmoothToggle(float targetAngle)
        {
            float startAngle = _currentAngle;
            float elapsed = 0f;

            while (elapsed < _toggleDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.SmoothStep(0f, 1f, elapsed / _toggleDuration);
                _currentAngle = Mathf.Lerp(startAngle, targetAngle, t);

                if (_model != null)
                    _model.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);

                yield return null;
            }

            _currentAngle = targetAngle;

            if (_model != null)
                _model.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
        }

        // IInteractable Implementation
        public string GetInteractionText() => IsActive ? _toggleOffText : _toggleOnText;
        public bool CanInteract() => true;
        public bool CanShowMessage() => true;

        public void OnInteract()
        {
            Toggle();
        }
    }
}
