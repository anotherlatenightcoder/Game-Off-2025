using Route24.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Route24.GameOff
{
    public class TutorialController : MonoBehaviour, ISceneInitializable
    {
        [SerializeField] private TutorialUIController _tutorialUI;
        [SerializeField] private float _skipTimerLength = 2f;
        [SerializeField] private KeypadController _keypad;
        
        public bool TutorialStep1Completed => _step1_PowerOn;
        public bool TutorialStep2Completed => _step2_ScopeCalibrated;
        public bool TutorialStep3Completed => _step3_KeypadPowerOn;
        public bool TutorialStep4Completed => _step4_CodeEntered;
        public bool TutorialStep5Completed => _step4_CodeEntered;
        public bool TutorialInProgress => _gameManager.CurrentGameplayState == GameplayState.Tutorial;
        public static TutorialController Instance;
        
        private GameManager _gameManager;
        private EventHub _eventHub;

        private bool _step1_PowerOn;
        private bool _step2_ScopeCalibrated;
        private bool _step3_KeypadPowerOn;
        private bool _step4_CodeEntered;
        private bool _step5_LigthsPowerOn;
        private bool _step6_GatesOpened;

        private bool _hasInteractedYet = false;
        private float _skipTimer = 0f;

        public void SceneInitialize()
        {
            Instance = this;
            
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            _eventHub.Subscribe<ConsolePoweredOnEvent>(OnPowerOn);
            _eventHub.Subscribe<ScopeCalibrationCompleteEvent>(OnScopeComplete);
            _eventHub.Subscribe<KeypadPoweredOnEvent>(OnKeypadPowerOn);
            _eventHub.Subscribe<KeypadTestEnteredEvent>(OnCodeEntered);
            _eventHub.Subscribe<LightsPoweredOnEvent>(OnLightsPowerOnEntered);
            _eventHub.Subscribe<DockGatesOpenedEvent>(OnGatesOpened);
            
            _eventHub.Subscribe<TutorialStartedEvent>(evt => ShowSkipUI());
        }

        private void Update()
        {
            if (_gameManager.CurrentGameplayState != GameplayState.Tutorial) 
                return;
            
            // Allow skipping the tutorial (i've only allowed this during the start, before
            // the player has interacted with anything. Once you start the tutorial there is
            // no skipping? or we should keep it all the way through but then need to clear all the consoles etc)
            if (!_hasInteractedYet)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    _skipTimer += Time.deltaTime;
                    _tutorialUI.UpdateSkipFill(_skipTimer / _skipTimerLength);
                    if (_skipTimer >= _skipTimerLength)
                    {
                        HideSkipUI();
                        CompleteTutorialNow();
                    }
                }
                else
                {
                    _skipTimer = 0f;
                    _tutorialUI.UpdateSkipFill(0f);
                }
            }
        }

        private void OnPowerOn(ConsolePoweredOnEvent evt)
        {
            _step1_PowerOn = true;
            _hasInteractedYet = true;
            _tutorialUI.SetStepCompleted(1);
            HideSkipUI();
            
            var scope = ServiceLocator.Get<OscilloscopeController>();
            if (scope != null)
                scope.StartBootSequence();
        }

        private void OnScopeComplete(ScopeCalibrationCompleteEvent evt)
        {
            if (!_step1_PowerOn) return;
            _step2_ScopeCalibrated = true;
            _tutorialUI.SetStepCompleted(2);
        }
        
        private void OnKeypadPowerOn(KeypadPoweredOnEvent evt)
        {
            if (!_step2_ScopeCalibrated) return;
            _step3_KeypadPowerOn = true;
            _keypad.SetPowered(true);
            _tutorialUI.SetStepCompleted(3);
        }
        
        private void OnCodeEntered(KeypadTestEnteredEvent evt)
        {
            if (!_step3_KeypadPowerOn) return;
            _step4_CodeEntered = true;
            _tutorialUI.SetStepCompleted(4);
        }
        
        private void OnLightsPowerOnEntered(LightsPoweredOnEvent evt)
        {
            if (!_step4_CodeEntered) return;
            _step5_LigthsPowerOn = true;
            _tutorialUI.SetStepCompleted(5);
        }

        private void OnGatesOpened(DockGatesOpenedEvent evt)
        {
            if (!_step5_LigthsPowerOn) return;
            _step6_GatesOpened = true;
            CompleteTutorialNow();
        }

        private void CompleteTutorialNow()
        {
            _gameManager.CompleteTutorial();
        }

        private void ShowSkipUI()
        {
            _tutorialUI.ShowSkipPrompt();
            _tutorialUI.ShowChecklist("Power on the console");
        }

        private void HideSkipUI()
        {
            _tutorialUI.HideSkipPrompt();
        }

        private void ShowNextStep()
        {
            _tutorialUI.SetStepCompleted(1);
        }
    }   
}
