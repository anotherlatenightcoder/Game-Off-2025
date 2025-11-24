using System.Collections;
using Route24.Core;
using UnityEngine;
using UnityEngine.UI;

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
        
        [Header("Waveform Renderer")]
        [SerializeField] private UIWaveformDualRenderer _waveformRenderer;

        [Header("Knob Controls")]
        [SerializeField] private Knob3DController _speedKnob;
        [SerializeField] private Knob3DController _amplitudeKnob;
        [SerializeField] private Knob3DController _frequencyKnob;

        [Header("Settings")]
        [SerializeField, Range(0.1f, 2f)] private float _exitCooldown = 0.5f;

        public bool IsFocused => _inFocus;
        public bool IsTutorialBooting => _isBooting;
        
        private GameManager _gameManager;
        private bool _inFocus;
        private bool _focusCooldownActive;
        private bool _isBooting;

        // NEW — Ensures tutorial init happens ONCE
        private bool _tutorialWaveInitialized = false;

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
            
            _waveformRenderer.ParentController = this;

            InitializeKnobs();
            SetInitialWaveSettings();
            
            ServiceLocator.Register(typeof(OscilloscopeController), this);
        }
        
        public void ExitFromTutorialSuccess()
        {
            StartCoroutine(DelayedExitRoutine());
        }
        
        private IEnumerator DelayedExitRoutine()
        {
            yield return new WaitForSeconds(2f);
            
            if (_inFocus)
                SetFocus(false);
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

            // Hook once — never hook again
            _speedKnob.OnValueChanged += _waveformRenderer.SetPlayerOffset;
            _amplitudeKnob.OnValueChanged += _waveformRenderer.SetPlayerAmplitude;
            _frequencyKnob.OnValueChanged += _waveformRenderer.SetPlayerFrequency;
        }

        private void SetInitialWaveSettings()
        {
            _waveformRenderer.SetPlayerOffset(_speedKnob.GetCurrentValue());
            _waveformRenderer.SetPlayerAmplitude(_amplitudeKnob.GetCurrentValue());
            _waveformRenderer.SetPlayerFrequency(_frequencyKnob.GetCurrentValue());
            
            _speedKnob.SetKnobRotationFromValue(_speedKnob.GetCurrentValue());
            _amplitudeKnob.SetKnobRotationFromValue(_amplitudeKnob.GetCurrentValue());
            _frequencyKnob.SetKnobRotationFromValue(_frequencyKnob.GetCurrentValue());
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
                _waveformRenderer.SetMatchUIVisible(true);

                // NORMAL GAMEPLAY — reset each time
                if (!IsTutorialMode())
                {
                    _waveformRenderer.StartNewSignalGame();
                    _waveformRenderer.ResetMatchStateForNewSession();
                }
                else
                {
                    // TUTORIAL — ONLY initialize wave ONCE
                    if (!_tutorialWaveInitialized)
                    {
                        SetupTutorialWave();
                        _tutorialWaveInitialized = true;
                    }

                    // DO NOT reset tutorial waves on re-entry.
                    _waveformRenderer.SetTutorialMatchUIState();
                }
                
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                _waveformRenderer.StopSignals();
                _focusCooldownActive = true;
                
                _waveformRenderer.SetMatchUIVisible(false);

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

        // █████████████████████████████████████████████████████████
        // BOOT SEQUENCE + TUTORIAL WAVE INITIALIZATION
        // █████████████████████████████████████████████████████████

        public void StartBootSequence()
        {
            _isBooting = true;
            _waveformRenderer.StartBootSequence();
        }

        public void EndBootSequence()
        {
            if (_isBooting)
            {
                _isBooting = false;

                // Run tutorial setup once
                if (!_tutorialWaveInitialized)
                {
                    SetupTutorialWave();
                    _tutorialWaveInitialized = true;
                }
            }

            _waveformRenderer.SetMatchUIVisible(true);

            // DO NOT reset here during tutorial
            if (!IsTutorialMode())
                _waveformRenderer.ResetMatchStateForNewSession();
        }
        
        private void SetupTutorialWave()
        {
            _waveformRenderer.StopSignals();

            _waveformRenderer.SetTutorialModeWave(
                shipAmplitude: 0.6f,
                shipFrequency: 3f,
                shipOffset: 0f,
                playerAmplitude: 0f,
                playerFrequency: 3f,
                playerOffset: 0f,
                holdtimer: 5f
            );
        }


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
            
            // During tutorial, allow only until step 2 ends
            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial &&
                !TutorialController.Instance.TutorialStep2Completed)
                return true;

            return false;
        }
        
        public bool CanShowMessage()
        {
            if (_inFocus) return false;

            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
            {
                if (TutorialController.Instance.TutorialStep1Completed &&
                    !TutorialController.Instance.TutorialStep2Completed)
                    return true;
            }
            
            return _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }
        
        public void OnInteract() => SetFocus(!_inFocus);
    }
}
