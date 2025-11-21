using System.Collections;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    /// <summary>
    /// Handles player interaction with the Oscilloscope console (wave minigame).
    /// Handles the camera focus, starts the UI, and links the knob input connections to
    /// the waveformRenderer component.
    /// </summary>
    public class OscilloscopeController : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [Header("Camera Settings")]
        [SerializeField] private SeatedCameraController _cameraController;
        [SerializeField] private Transform _cameraFocusPoint;
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField, Range(20f, 60f)] private float _focusFOV = 35f;

        [Header("Oscilloscope UI")]
        [SerializeField] private GameObject _oscilloscopeUI;

        [Header("Waveform Renderer")]
        [SerializeField] private UIWaveformDualRenderer _waveformRenderer;

        [Header("Knob Controls")]
        [SerializeField] private Knob3DController _speedKnob;
        [SerializeField] private Slider3DController _amplitudeKnob;
        [SerializeField] private Knob3DController _frequencyKnob;

        [Header("Settings")]
        [SerializeField, Range(0.1f, 2f)] private float _exitCooldown = 0.5f;

        public bool IsFocused => _inFocus;

        private GameManager _gameManager;
        private bool _inFocus;
        private bool _focusCooldownActive;
        private bool _inputBuffer;

        /// <summary>
        /// Called by the SceneObjectInitializer after core systems are ready.
        /// Initializes references, disables UI by default, and sets up knob linkage.
        /// </summary>
        public void SceneInitialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();

            if (!_waveformRenderer)
            {
                Debug.LogError("[OscilloscopeController] Missing waveform renderer reference.");
                return;
            }

            if (_oscilloscopeUI)
                _oscilloscopeUI.SetActive(false);

            InitializeKnobs();
            SetInitialWaveSettings();
        }
        
        private void InitializeKnobs()
        {
            if (!_speedKnob || !_frequencyKnob)
            {
                Debug.LogWarning($"[OscilloscopeController] Missing knob reference on {name}.");
                return;
            }

            if (!_amplitudeKnob)
            {
                Debug.LogWarning($"[OscilloscopeController] Missing slider reference on {name}.");
                return;
            }

            _speedKnob.InitializeLink(this);
            _frequencyKnob.InitializeLink(this);
            _amplitudeKnob.InitializeLink(this);

            _speedKnob.OnValueChanged += _waveformRenderer.SetPlayerOffset;
            _amplitudeKnob.OnValueChanged += _waveformRenderer.SetPlayerAmplitude;
            _frequencyKnob.OnValueChanged += _waveformRenderer.SetPlayerFrequency;
        }

        private void SetInitialWaveSettings()
        {
            _waveformRenderer.SetPlayerOffset(_speedKnob.GetCurrentValue());
            _waveformRenderer.SetPlayerAmplitude(_amplitudeKnob.GetCurrentValue());
            _waveformRenderer.SetPlayerFrequency(_frequencyKnob.GetCurrentValue());
        }
        
        private void Update()
        {
            if (_inFocus && Input.GetKeyDown(KeyCode.E) && !_inputBuffer)
            {
                _inputBuffer = true;
                SetFocus(false);
                StartCoroutine(ClearInputBuffer());
            }
        }
        
        private void SetFocus(bool state)
        {
            print($"state is {state}");
            if (state == _inFocus) return;

            _inFocus = state;
            GameManager.SetFocusMode(state);

            if (state)
            {
                _cameraController.FocusOn(_cameraFocusPoint, _cameraLookTarget, _focusFOV);
                _waveformRenderer.StartNewSignalGame();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                _waveformRenderer.StopSignals();
                _focusCooldownActive = true;

                _cameraController.ReturnToDefault();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                StartCoroutine(FocusCooldownRoutine());
            }
        }

        /// <summary>
        /// Enforces a short cooldown after exiting focus mode to prevent
        /// immediate re-entry or camera offset. It was due to a
        /// strange bug I was having with the other console.
        /// </summary>
        private IEnumerator FocusCooldownRoutine()
        {
            yield return new WaitForSeconds(_exitCooldown);
            _focusCooldownActive = false;
        }

        private IEnumerator ClearInputBuffer()
        {
            yield return new WaitForEndOfFrame();
            _inputBuffer = false;
        }
        
        public string GetInteractionText() => _inFocus ? string.Empty : "Access Oscilloscope [E]";
        
        public bool CanInteract()
        {
            // UNCOMMENT AFTER TESTING:
            // return !_inFocus && !_focusCooldownActive && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
            return !_inFocus && !_focusCooldownActive && _gameManager;
        }
        
        public bool CanShowMessage()
        {
            // UNCOMMENT AFTER TESTING:
            // return !_inFocus && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
            return !_inFocus && _gameManager;
        }
        
        public void OnInteract()
        {
            if (!_inputBuffer)
            {
                _inputBuffer = true;
                SetFocus(!_inFocus);
                StartCoroutine(ClearInputBuffer());
            }
        }
    }
}
