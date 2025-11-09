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
        private float timer;
        
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
            StartCoroutine(DelayedStartOfDay());
        }
        
        private IEnumerator DelayedStartOfDay()
        {
            Debug.Log("[GameManager] Preparing environment...");
            yield return new WaitForSeconds(5f);

            Debug.Log("[GameManager] Starting first day...");
            StartNewDay();
        }

        private void Update()
        {
            HandleDebugInput();
            HandleTimer();
        }

        private void HandleDebugInput()
        {
            if (inspectionActive)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    CompleteInspection(true);

                if (Input.GetKeyDown(KeyCode.Alpha2))
                    CompleteInspection(false);
            }
        }
        
        private void HandleTimer()
        {
            if (!inspectionActive) return;

            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Debug.Log($"Time ran out for ship {_shipManager.GetCurrentShipProfile().ShipName}");
                CompleteInspection(false, true);
            }
        }
        
        private void StartNewDay()
        {
            Debug.Log($"=== Starting Day {currentDay} ===");
            _shipManager.StartNewDay(currentDay);
            
            _eventHub?.Publish(new DayStartedEvent(currentDay));
            
            if (_shipManager.TrySpawnNextShip())
                state = GameplayState.WaitingForShip;
            else 
                EndOfDay();
                
        }

        public void OnShipReadyForInspection()
        {
            state = GameplayState.ReadyForInspection;
        }

        public void OnShipExitComplete()
        {
            StartCoroutine(WaitThenNextShip());
        }

        public void StartInspection()
        {
            inspectionActive = true;
            timer = _shipManager.GetInspectionTimeForCurrentShip();
            state = GameplayState.Inspecting;
            Debug.Log("Inspection started. (Press 1 to Approve, 2 to Decline)");
            
            _eventHub?.Publish(new InspectionStartedEvent(_shipManager.GetCurrentShipProfile()));
        }

        public void CompleteInspection(bool approved, bool timedOut = false)
        {
            inspectionActive = false;
            state = GameplayState.WaitingForShip;
            
            var ship = _shipManager.GetCurrentShipProfile();
            bool correct = approved == ship.IsValid && !timedOut;
            
            _shipManager.HandleInspectionComplete(timedOut, approved); 
            
            _eventHub?.Publish(new InspectionCompletedEvent(approved, timedOut, ship));
        }

        private IEnumerator WaitThenNextShip()
        {
            yield return new WaitForSeconds(transitionDelay);

            if (_shipManager.TrySpawnNextShip())
                state = GameplayState.WaitingForShip;
            else 
                EndOfDay();
        }

        private void EndOfDay()
        {
            state = GameplayState.DayComplete; 
            Debug.Log($"=== End of Day {currentDay} ===");
            
            _eventHub?.Publish(new DayEndedEvent(currentDay));
            
            currentDay++;
            StartCoroutine(WaitThenNextDay());
        }
        
        private IEnumerator WaitThenNextDay()
        {
            yield return new WaitForSeconds(3f);
            StartNewDay();
        }
    }
}