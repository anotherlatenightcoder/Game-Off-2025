using System;
using System.Collections;
using UnityEngine;

namespace Route24.GameOff
{
    public class KeypadButton : MonoBehaviour
    {
        [SerializeField] private int _digit = 0;
        [SerializeField] private KeypadController _controller;
        [SerializeField] private Transform _pressTransform;
        [SerializeField] private float _pressDepth = 0.01f;
        [SerializeField] private float _pressSpeed = 8f;

        private Vector3 _defaultLocalPos;
        private bool _isPressAnimating;

        private void Awake()
        {
            _defaultLocalPos = transform.localPosition;
        }

        private void OnMouseDown()
        {
            if (_controller == null) return;

            _controller.PressDigit(_digit);
            AnimatePress();
        }

        private void AnimatePress()
        {
            if (_isPressAnimating) return;

            StartCoroutine(AnimatePressRoutine());
        }

        private IEnumerator AnimatePressRoutine()
        {
            _isPressAnimating = true;
            
            Vector3 target = _defaultLocalPos + new Vector3(0, -_pressDepth, 0);
            Vector3 start = _defaultLocalPos;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * _pressSpeed;
                _pressTransform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }
            
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * _pressSpeed;
                _pressTransform.localPosition = Vector3.Lerp(target, start, t);
                yield return null;
            }

            _isPressAnimating = false;
        }
    }   
}
