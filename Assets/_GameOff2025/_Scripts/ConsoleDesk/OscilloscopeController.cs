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
        [SerializeField] private AudioSource _audioSource;
        
        private float baseVolume;
        public bool IsFocused => _inFocus;
        public bool IsTutorialBooting => _isBooting;
        public bool IsWaveMatched => _isWaveMatched;
        
        private GameManager _gameManager;
        private EventHub _eventHub;
        private bool _inFocus;
        private bool _focusCooldownActive;
        private bool _isBooting;
        private bool _isWaveMatched = false;

        /// <summary>
        /// Called by the SceneObjectInitializer after core systems are ready.
        /// Initializes references, disables UI by default, and sets up knob linkage.
        /// </summary>
        public void SceneInitialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();

            if (!_waveformRenderer)
            {
                Debug.LogError("[OscilloscopeController] Missing waveform renderer reference.");
                return;
            }
            
            _waveformRenderer.Setup(this);

            InitializeKnobs();
            SetInitialWaveSettings();
            
            baseVolume = _audioSource.volume;

            if (AudioManager.Instance != null)
                _audioSource.volume = baseVolume * AudioManager.Instance.sfxVolume; 
            
            _eventHub?.Subscribe<InspectionStartedEvent>(OnInspectionStarted);
            _eventHub?.Subscribe<SfxVolumeChangedEvent>(OnSfxVolumeChanged);
            
            ServiceLocator.Register(typeof(OscilloscopeController), this);
        }

        private void OnSfxVolumeChanged(SfxVolumeChangedEvent obj)
        {
            _audioSource.volume = baseVolume * obj.Volume;
        }

        private void OnInspectionStarted(InspectionStartedEvent obj)
        {
            _isWaveMatched = false;
            _waveformRenderer.ResetMatchStateForNewSession();
            _waveformRenderer.StartNewSignalGame();
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

            _speedKnob.OnValueChanged += _waveformRenderer.SetPlayerOffset;
            _amplitudeKnob.OnValueChanged += _waveformRenderer.SetPlayerAmplitude;
            _frequencyKnob.OnValueChanged += _waveformRenderer.SetPlayerFrequency;

        }

        private void SetInitialWaveSettings()
        {
            _waveformRenderer.SetPlayerOffset(_speedKnob.GetCurrentValue());
            _waveformRenderer.SetPlayerAmplitude(_amplitudeKnob.GetCurrentValue());
            _waveformRenderer.SetPlayerFrequency(_frequencyKnob.GetCurrentValue());
            
            // We should also set the knob rotation based on its starting value,
            // otherwise when you click to drag it ends up jumping the wave
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
            if (_focusCooldownActive)
                return;
            
            if (state == _inFocus) return;

            _inFocus = state;
            GameManager.SetFocusMode(state);

            if (state)
            {
                _cameraController.FocusOn(_cameraFocusPoint, _cameraLookTarget, _focusFOV);
                _waveformRenderer.SetMatchUIVisible(true);

                if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
                {
                    _eventHub?.Publish(new Tutorial_ScopeFocusEvent());
                }
                
                _audioSource.Play();
                
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                _focusCooldownActive = true;
                _cameraController.ReturnToDefault();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                
                _audioSource.Stop();

                StartCoroutine(FocusCooldownRoutine());
            }
        }

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
                SetupTutorialWave();
            }

            _waveformRenderer.SetMatchUIVisible(true);
            _waveformRenderer.ResetMatchStateForNewSession();
        }
        
        // This is to load up some dummy data for the tutorial to test
        private void SetupTutorialWave()
        {
            // Disable all random ship variation
            _waveformRenderer.StopSignals();

            // Tutorial: Set a fixed “target” wave
            // All axes match except amplitude
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
            if (_focusCooldownActive)
                return false;
            
            if (_inFocus || _gameManager == null)
                return false;
            
            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial &&
                TutorialController.Instance.IsStepCompleted(TutorialStep.ConsolePoweredOn) &&
                !TutorialController.Instance.IsStepCompleted(TutorialStep.CalibrationComplete))
                return true;

            return _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }
        
        public bool CanShowMessage()
        {
            if (_inFocus) return false;

            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial &&
                TutorialController.Instance.IsStepCompleted(TutorialStep.ConsolePoweredOn) &&
                !TutorialController.Instance.IsStepCompleted(TutorialStep.CalibrationComplete))
                return true;
            
            return _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }
        
        public void OnInteract() => SetFocus(!_inFocus);

        public void WaveMatched()
        {
            _audioSource.Stop();
            _isWaveMatched = true;
            AudioManager.Instance.PlaySFX("SCANNER_MATCH");
            _eventHub?.Publish(new WaveMatchedEvent());
        }
    }
}
