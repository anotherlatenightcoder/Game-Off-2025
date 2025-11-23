using System.Collections;
using UnityEngine;
using Route24.Core;

namespace Route24.GameOff
{
    public class GenericSwitch : BaseSensor, IInteractable, ISceneInitializable
    {
        [Header("Start Settings")]
        [SerializeField] protected bool _startActive;

        [Header("Lever Settings")]
        [SerializeField] protected Transform _model;
        [SerializeField] protected float _minAngle = -30f;
        [SerializeField] protected float _maxAngle = 30f;
        [SerializeField] protected float _toggleDuration = 0.5f;

        [Header("Interaction Text")]
        [Tooltip("Text when toggling from OFF > ON")]
        [SerializeField] protected string _toggleOnText = "Switch On [E]";
        [Tooltip("Text when toggling from ON > OFF")]
        [SerializeField] protected string _toggleOffText = "Switch Off [E]";

        protected Coroutine _toggleRoutine;
        protected float _currentAngle;

        protected EventHub _eventHub;

        public virtual void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            SetState(_startActive, instant: true);
        }
        
        public void Toggle() => SetState(!IsActive);
        public void ForceOn(bool instant = false) => SetState(true, instant);
        public void ForceOff(bool instant = false) => SetState(false, instant);
        
        protected virtual void SetState(bool active, bool instant = false)
        {
            if (IsActive == active && !instant)
                return;

            IsActive = active;
            float targetAngle = active ? _maxAngle : _minAngle;

            if (instant)
            {
                _currentAngle = targetAngle;
                ApplyRotation();
                return;
            }

            if (_toggleRoutine != null)
                StopCoroutine(_toggleRoutine);

            _toggleRoutine = StartCoroutine(SmoothToggle(targetAngle));

            if (active)
                OnToggledOn();
            else
                OnToggledOff();
        }

        protected IEnumerator SmoothToggle(float targetAngle)
        {
            float startAngle = _currentAngle;
            float elapsed = 0f;

            while (elapsed < _toggleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _toggleDuration);
                _currentAngle = Mathf.Lerp(startAngle, targetAngle, t);
                ApplyRotation();
                yield return null;
            }

            _currentAngle = targetAngle;
            ApplyRotation();
        }

        protected void ApplyRotation()
        {
            if (_model != null)
                _model.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
        }
        
        protected virtual void OnToggledOn()
        {
            // _eventHub?.Publish(new LeverActivatedEvent());
        }

        protected virtual void OnToggledOff()
        {
            // _eventHub?.Publish(new LeverDeactivatedEvent());
        }
        
        public virtual string GetInteractionText() =>
            IsActive ? _toggleOffText : _toggleOnText;

        public virtual bool CanInteract() => true;

        public virtual bool CanShowMessage() => true;

        public virtual void OnInteract() => Toggle();
    }
}
