using System.Collections;
using UnityEngine;
using Route24.Core;

namespace Route24.GameOff
{
    public class LeverSwitchController : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [Header("Lever Settings")]
        [SerializeField] private Transform _leverHandle;
        [SerializeField] private float _minAngle = -30f;
        [SerializeField] private float _maxAngle = 30f;
        [SerializeField] private float _toggleDuration = 0.5f;

        private bool _isOn = false;
        private float _currentAngle = 0f;
        private EventHub _eventHub;
        private Coroutine _toggleRoutine;
        
        private void Awake()
        {
            _currentAngle = _minAngle;
            if (_leverHandle)
                _leverHandle.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
        }

        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            // SetLeverState(false, instant: true);
        }

        // ─────────────────────────────────────────────
        // Core Logic
        // ─────────────────────────────────────────────
        private void SetLeverState(bool on, bool instant = false)
        {
            _isOn = on;
            float targetAngle = on ? _maxAngle : _minAngle;

            if (instant)
            {
                _currentAngle = targetAngle;
                _leverHandle.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
            }
            else
            {
                if (_toggleRoutine != null)
                    StopCoroutine(_toggleRoutine);
                _toggleRoutine = StartCoroutine(SmoothToggle(targetAngle));
            }

            if (on)
                _eventHub.Publish(new LeverActivatedEvent());
            else
                _eventHub.Publish(new LeverDeactivatedEvent());
        }

        private IEnumerator SmoothToggle(float targetAngle)
        {
            float startAngle = _currentAngle;
            float elapsed = 0f;

            while (elapsed < _toggleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _toggleDuration);
                _currentAngle = Mathf.Lerp(startAngle, targetAngle, t);
                _leverHandle.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
                yield return null;
            }

            _currentAngle = targetAngle;
            _leverHandle.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
        }

        // ─────────────────────────────────────────────
        // IInteractable Implementation
        // ─────────────────────────────────────────────
        public string GetInteractionText() => _isOn ? "Power Off [E]" : "Power On [E]";
        public bool CanInteract() => true;
        public bool CanShowMessage() => true;

        public void OnInteract()
        {
            SetLeverState(!_isOn);
        }

        public void ForceOff() => SetLeverState(false);
    }

    public struct LeverActivatedEvent { }
    public struct LeverDeactivatedEvent { }
}
