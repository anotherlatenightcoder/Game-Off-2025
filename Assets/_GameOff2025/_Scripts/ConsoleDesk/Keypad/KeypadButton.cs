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
        
        [Header("Color Flash Settings")]
        [SerializeField] private Renderer _flashRenderer;
        [SerializeField] private Color _flashColor = new Color(0.172549f, 0.2431373f, 0.3843138f);
        [SerializeField] private float _flashDuration = 0.2f;

        private Vector3 _defaultLocalPos;
        private bool _isPressAnimating;
        private Material _buttonMaterial; 
        private Color _defaultColor;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            _defaultLocalPos = transform.localPosition;
            
            if (_flashRenderer)
            {
                // Clone material so each button has its own instance
                _buttonMaterial = new Material(_flashRenderer.material);
                _flashRenderer.material = _buttonMaterial;
                _defaultColor = _buttonMaterial.color;
            }
            else
            {
                Debug.LogWarning($"[KeypadButton] No renderer found on {_pressTransform?.name}");
            }
        }

        private void OnMouseDown()
        {
            if (_controller == null) return;

            _controller.PressDigit(_digit);
            
            AudioManager.Instance.PlaySFX("GENERIC_BUTTON");
            
            AnimatePress();
            FlashColor();
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
        
        private void FlashColor()
        {
            if (_buttonMaterial == null) return;
            
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(FlashColorRoutine());
        }

        private IEnumerator FlashColorRoutine()
        {
            _buttonMaterial.color = _flashColor;

            yield return new WaitForSeconds(_flashDuration);
            
            _buttonMaterial.color = _defaultColor;
            _flashRoutine = null;
        }
    }   
}
