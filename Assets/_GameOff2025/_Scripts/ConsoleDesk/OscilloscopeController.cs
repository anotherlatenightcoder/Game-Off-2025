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
        [SerializeField] private Knob3DController _amplitudeKnob;
        [SerializeField] private Knob3DController _frequencyKnob;

        [Header("Settings")]
        [SerializeField, Range(0.1f, 2f)] private float _exitCooldown = 0.5f;

        public bool IsFocused => _inFocus;

        private GameManager _gameManager;
        private bool _inFocus;
        private bool _focusCooldownActive;

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
            Knob3DController[] knobs = { _speedKnob, _amplitudeKnob, _frequencyKnob };

            foreach (var knob in knobs)
            {
                if (!knob)
                {
                    Debug.LogWarning($"[OscilloscopeController] Missing knob reference on {name}.");
                    continue;
                }

                knob.InitializeLink(this);
            }

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
            if (_inFocus && Input.GetKeyDown(KeyCode.E))
                SetFocus(false);
        }
        
        private void SetFocus(bool state)
        {
            if (state == _inFocus) return;

            _inFocus = state;
            GameManager.SetFocusMode(state);

            if (state)
            {
                _cameraController.FocusOn(_cameraFocusPoint, _cameraLookTarget, _focusFOV);
                
                if (IsTutorialMode())
                    SetupTutorialWave();
                else
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
        
        private bool IsTutorialMode()
        {
            return _gameManager.CurrentGameplayState == GameplayState.Tutorial &&
                   TutorialController.Instance.TutorialStep1Completed &&
                   !TutorialController.Instance.TutorialStep2Completed;
        }
        
        // This is to load up some dummy data for the tutorial to test
        private void SetupTutorialWave()
        {
            // Force oscilloscope UI visible
            if (_oscilloscopeUI)
                _oscilloscopeUI.SetActive(true);

            // Disable all random ship variation
            _waveformRenderer.StopSignals();

            // Turn on manual mode (no randomness, no matching surprises)
            _waveformRenderer.gameObject.SetActive(true);

            // Tutorial: Set a fixed “target” wave
            // All axes match except amplitude
            _waveformRenderer.SetTutorialModeWave(
                shipAmplitude: 0.6f,     // target
                shipFrequency: 3f,
                shipOffset: 0f,
                playerAmplitude: 0.3f,   // wrong, needs adjusting
                playerFrequency: 3f,
                playerOffset: 0f
            );
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
        
        public string GetInteractionText() => _inFocus ? string.Empty : "Access Oscilloscope [E]";
        
        public bool CanInteract()
        {
            if (_inFocus || _focusCooldownActive || _gameManager == null)
                return false;
            
            // Allow access during normal gameplay inspection
            if (_gameManager.CurrentGameplayState == GameplayState.Inspecting)
                return true;
            
            // Case for the tutorial, but only after step 1 has been completed
            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial &&
                TutorialController.Instance.TutorialStep1Completed)
                return true;

            return false;
        }
        
        public bool CanShowMessage()
        {
            if (_inFocus) return false;

            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
                return TutorialController.Instance.TutorialStep1Completed;
            
            return _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }
        
        public void OnInteract() => SetFocus(!_inFocus);
    }
}
