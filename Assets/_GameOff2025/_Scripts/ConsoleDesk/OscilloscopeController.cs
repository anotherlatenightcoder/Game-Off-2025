using System.Collections;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class OscilloscopeController : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [Header("Camera Settings")]
        [SerializeField] private SeatedCameraController _cameraController;

        [SerializeField] private Transform _cameraFocusPoint;
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField] private float _focusFOV = 35f;
        
        [Header("Oscilloscope UI")]
        [SerializeField] private GameObject _oscilloscopeUI;
        
        [Header("Waveform")]
        [SerializeField] private UIWaveformDualRenderer _waveformRenderer;

        [Header("Controls")]
        [SerializeField] private Knob3DController _speedKnob;
        [SerializeField] private Knob3DController _amplitudeKnob;
        [SerializeField] private Knob3DController _frequencyKnob;
        
        public bool IsFocused => _inFocus;
        
        private GameManager _gameManager;
        private EventHub _eventHub;
        private bool _inFocus = false;
        private bool _recentlyExited = false;
        private float _exitCooldown = 0.5f;
        
        // ─────────────────────────────────────────────
        // Initialization
        // ─────────────────────────────────────────────
        public void SceneInitialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            if (_oscilloscopeUI)
                _oscilloscopeUI.SetActive(false);
            
            _speedKnob.InitializeLink(this);
            _amplitudeKnob.InitializeLink(this);
            _frequencyKnob.InitializeLink(this);
            
            _speedKnob.OnValueChanged += _waveformRenderer.SetPlayerSpeed;
            _amplitudeKnob.OnValueChanged += _waveformRenderer.SetPlayerAmplitude;
            _frequencyKnob.OnValueChanged += _waveformRenderer.SetPlayerFrequency;
        }

        private void Update()
        {
            if (!_inFocus) return;

            if (Input.GetKeyDown(KeyCode.E))
                ExitFocus();
        }
        
        // ─────────────────────────────────────────────
        // Focus Handling
        // ─────────────────────────────────────────────
        private void EnterFocus()
        {
            if (_inFocus) return;
            
            _inFocus = true;
            GameManager.SetFocusMode(true);
            
            _cameraController.FocusOn(_cameraFocusPoint, _cameraLookTarget, _focusFOV);
            
            if (_oscilloscopeUI)
                _oscilloscopeUI.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ExitFocus()
        {
            if (!_inFocus) return;
            
            _inFocus = false;
            _recentlyExited = true;
            GameManager.SetFocusMode(false);
            
            _cameraController.ReturnToDefault();
            
            if (_oscilloscopeUI)
                _oscilloscopeUI.SetActive(false);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(ExitCooldownRoutine());
        }
        
        private IEnumerator ExitCooldownRoutine()
        {
            yield return new WaitForSeconds(_exitCooldown);
            _recentlyExited = false;
        }
        
        // ─────────────────────────────────────────────
        // IInteractable Implementation
        // ─────────────────────────────────────────────
        public string GetInteractionText()
        {
            if (_inFocus) return string.Empty;
            return "Access Oscilloscope [E]";
        }

        public bool CanInteract()
        {
            return !_inFocus && !_recentlyExited && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public bool CanShowMessage()
        {
            return !_inFocus && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public void OnInteract()
        {
            if (_inFocus)
                ExitFocus();
            else
                EnterFocus();
        }
    }
}
