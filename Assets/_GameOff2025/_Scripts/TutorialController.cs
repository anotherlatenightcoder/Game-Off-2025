using System.Collections;
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
        
        private bool[] _completedSteps = new bool[12]; // I'm cheating by not using 0, shhh
        
        public static TutorialController Instance;
        
        private GameManager _gameManager;
        private EventHub _eventHub;
        private bool _hasInteractedYet = false;
        private float _skipTimer = 0f;

        public void SceneInitialize()
        {
            Instance = this;
            
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            // New events to listen to
            _eventHub.Subscribe<Tutorial_OnInspectionStartedEvent>(OnInspectionStarted);
            _eventHub.Subscribe<Tutorial_OnCargoFocusedEvent>(OnCargoFocused);
            _eventHub.Subscribe<Tutorial_OnCargoScannedEvent>(OnCargoScanned);
            _eventHub.Subscribe<Tutorial_ConsolePoweredOnEvent>(OnPowerOn);
            _eventHub.Subscribe<Tutorial_ScopeFocusEvent>(OnScopeFocus);
            _eventHub.Subscribe<Tutorial_ScopeCalibrationCompleteEvent>(OnScopeComplete);
            _eventHub.Subscribe<Tutorial_KeypadPoweredOnEvent>(OnKeypadPowerOn);
            _eventHub.Subscribe<Tutorial_KeypadTestEnteredEvent>(OnCodeEntered);
            _eventHub.Subscribe<Tutorial_ShipApproveDeclineEvent>(OnShipApproveDecline);
            _eventHub.Subscribe<LightsPoweredOnEvent>(OnLightsPowerOnEntered);
            _eventHub.Subscribe<Tutorial_DockGatesOpenedEvent>(OnGatesOpened);
            
            _eventHub.Subscribe<TutorialStartedEvent>(evt => StartTutorial());
        }

        private void OnShipApproveDecline(Tutorial_ShipApproveDeclineEvent obj)
        {
            CompleteStep(TutorialStep.ShipApproveDecline);
        }

        private void OnCargoScanned(Tutorial_OnCargoScannedEvent obj)
        {
            CompleteStep(TutorialStep.CargoEntryScanned);
        }

        private void OnCargoFocused(Tutorial_OnCargoFocusedEvent obj)
        {
            CompleteStep(TutorialStep.CargoTerminalFocus);
        }

        public bool IsStepCompleted(TutorialStep step)
        {
            return _completedSteps[(int)step];
        }
        
        private void CompleteStep(TutorialStep step)
        {
            _completedSteps[(int)step] = true;
            _tutorialUI.ShowStep((int)step);
            _tutorialUI.StrikeStep((int)step-1);

            // if (step == TutorialStep.DockGatesOpened)
            // {
            //     _tutorialUI.ClearAll();
            // }
        }

        private void OnInspectionStarted(Tutorial_OnInspectionStartedEvent obj)
        {
            CompleteStep(TutorialStep.InspectionStarted);
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
                        StartCoroutine(CompleteTutorialNow(true));
                    }
                }
                else
                {
                    _skipTimer = 0f;
                    _tutorialUI.UpdateSkipFill(0f);
                }
            }
        }

        private void OnPowerOn(Tutorial_ConsolePoweredOnEvent evt)
        {
            CompleteStep(TutorialStep.ConsolePoweredOn);
            
            var scope = ServiceLocator.Get<OscilloscopeController>();
            if (scope != null)
                scope.StartBootSequence();
        }
        
        private void OnScopeFocus(Tutorial_ScopeFocusEvent evt)
        {
            CompleteStep(TutorialStep.OscillatorFocus);
        }
        
        private void OnScopeComplete(Tutorial_ScopeCalibrationCompleteEvent evt)
        {
            CompleteStep(TutorialStep.CalibrationComplete);
        }
        
        private void OnKeypadPowerOn(Tutorial_KeypadPoweredOnEvent evt)
        {
            CompleteStep(TutorialStep.KeypadPowerOn);
        }
        
        private void OnCodeEntered(Tutorial_KeypadTestEnteredEvent evt)
        {
            CompleteStep(TutorialStep.CodeEntered);
        }
        
        private void OnLightsPowerOnEntered(LightsPoweredOnEvent evt)
        {
            CompleteStep(TutorialStep.LightsPoweredOn);
        }
        
        private void OnGatesOpened(Tutorial_DockGatesOpenedEvent evt)
        {
            CompleteStep(TutorialStep.DockGatesOpened);

            StartCoroutine(CompleteTutorialNow(false));
        }

        private IEnumerator CompleteTutorialNow(bool instant)
        {
            if (instant)
            {
                WaveGameStats.Instance.MarkTutorialSkipped();
            }
            
            yield return new WaitForSeconds(1f);
            // I've added the instant flag because when we skip the tutorial,
            // we want to instantly turn everything on. Currently there is a bug if
            // do the last task in the tutorial, during the switch flip animation it 
            // suddenly snaps because of this...
            _tutorialUI.ClearAll();
            _gameManager.CompleteTutorial();
        }

        private void HideSkipUI()
        {
            _tutorialUI.HideSkipPrompt();
        }

        private void StartTutorial()
        {
            _tutorialUI.ClearAll();
            _tutorialUI.ShowStep(0);
            _tutorialUI.ShowSkip();
        }
    }   
}
