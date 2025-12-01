using System.Collections;
using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class GameManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 98;
        public GameplayState CurrentGameplayState => state;
        public static bool IsInFocusMode { get; private set; }

        [Header("Day Settings")]
        public int currentDay = 1;
        public float transitionDelay = 5f;
        
        private GameplayState state = GameplayState.WaitingForShip;
        private ShipManager _shipManager;
        private EventHub _eventHub;
        private bool inspectionActive = false;
        private Coroutine _dayTimerCoroutine;
        private bool _tutorialCompleted = false;
        
        public static void SetFocusMode(bool value)
        {
            IsInFocusMode = value;
        }
        
        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub?.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            _shipManager = ServiceLocator.Get<ShipManager>();
        }

        private void OnSceneLoaded(SceneLoadedEvent scene)
        {
            StartCoroutine(StartTutorialRoutine());
        }

        public void StartNextDay()
        {
            StartCoroutine(DelayedStartOfDay());
        }
        
        private IEnumerator DelayedStartOfDay()
        {
            yield return new WaitForSeconds(ConstGameStats.DelayBeforeDayStart);
            
            state = GameplayState.WaitingForShip;
            
            StartNewDay();
        }
        
        public void StartNewDay()
        {
            if (CurrentGameplayState == GameplayState.Tutorial)
                return;
            
            Debug.Log($"=== Starting Day {currentDay} ===");
            
            WaveGameStats.Instance.MarkDayStart(currentDay);
            
            AudioManager.Instance.PlaySFX("DAY_START");
            state = GameplayState.WaitingForShip;
            
            _eventHub?.Publish(new DayStartedEvent(currentDay));
            _shipManager.StartNewDay(currentDay);
            _dayTimerCoroutine = StartCoroutine(DayTimeCoroutine());
            _shipManager.SpawnNewShip();
        }

        public void OnShipReadyForInspection()
        {
            state = GameplayState.ReadyForInspection;
        }

        public void OnShipExitComplete()
        {
            if(state == GameplayState.WaitingForShip)
                StartCoroutine(WaitThenNextShip());
        }

        public void StartInspection()
        {
            inspectionActive = true;
            state = GameplayState.Inspecting;
            _eventHub?.Publish(new InspectionStartedEvent(_shipManager.GetCurrentShipProfile()));
        }

        public void CompleteInspection(bool approved, bool timedOut = false)
        {
            inspectionActive = false;
            
            if(state != GameplayState.DayComplete)
                state = GameplayState.WaitingForShip;
            
            var ship = _shipManager.GetCurrentShipProfile();
            bool correct = approved == ship.IsValid && !timedOut;
            
            _shipManager.HandleInspectionComplete(timedOut, approved); 
            
            _eventHub?.Publish(new InspectionCompletedEvent(approved, correct, timedOut, ship));
            
            WaveGameStats.Instance.TrackShipOutcome(approved, correct);
        }

        private IEnumerator WaitThenNextShip()
        {
            yield return null;
            
            if (state != GameplayState.WaitingForShip)
                yield break;
            
            yield return new WaitForSeconds(transitionDelay);
            
            if (state != GameplayState.WaitingForShip)
                yield break;
            
            _shipManager.SpawnNewShip();
            state = GameplayState.WaitingForShip;
        }

        private void EndOfDay()
        {
            state = GameplayState.DayComplete; 
            StopCoroutine(WaitThenNextShip());
            StopCoroutine(DayTimeCoroutine());
            
            WaveGameStats.Instance.MarkDayEnd(currentDay);
            
            Debug.Log($"=== End of Day {currentDay} ===");
            
            _eventHub?.Publish(new DayEndedEvent(currentDay));
            
            currentDay++;
        }
        
        private IEnumerator DayTimeCoroutine()
        {
            // Little hack here to add a few seconds to the daytime routine since the clock doesn't quite match up
            yield return new WaitForSeconds(ConstGameStats.DayTime+2);
            EndOfDay();
        }
        
        private IEnumerator StartTutorialRoutine()
        {
            state = GameplayState.Tutorial;
            
            yield return new WaitForSeconds(1.0f);
            
            _eventHub?.Publish(new TutorialStartedEvent());
            
            while (!_tutorialCompleted)
                yield return null;
        }

        public void CompleteTutorial()
        {
            if (_tutorialCompleted)
                return;
            
            _tutorialCompleted = true;
            state = GameplayState.WaitingForShip;
            
            _eventHub?.Publish(new LightsPoweredOnEvent(true));
            _eventHub?.Publish(new TutorialCompletedEvent());
            
            StartCoroutine(DelayedStartOfDay());
        }
    }
}