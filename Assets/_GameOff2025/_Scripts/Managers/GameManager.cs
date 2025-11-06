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

        [Header("Day Settings")]
        public int currentDay = 1;
        public float transitionDelay = 5f;
        public int shipsPerDay = 3;
        
        private Transform _shipSpawn;
        private Transform _shipDock;
        private Transform _shipExit;
        private Transform _shipSink;
        private ShipController _currentShipController;
        
        private List<ShipProfile> currentDayShips = new();
        private int currentShipIndex = -1;
        private float timer;
        private bool inspectionActive = false;

        private GameplayState state = GameplayState.WaitingForShip;
        private EventHub _eventHub;
        
        public static bool IsInFocusMode { get; private set; }

        public static void SetFocusMode(bool value)
        {
            IsInFocusMode = value;
        }
        
        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub?.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
        }

        private void OnSceneLoaded(SceneLoadedEvent scene)
        {
            // TESTING LOGIC
            if (scene.SceneName == "EnedProto")
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
                Debug.Log($"Time ran out for ship {currentDayShips[currentShipIndex].ShipName}");
                CompleteInspection(false, true);
            }
        }
        
        private void StartNewDay()
        {
            Debug.Log($"=== Starting Day {currentDay} ===");
            GenerateShipsForDay();
            currentShipIndex = -1;
            
            _eventHub?.Publish(new DayStartedEvent(currentDay));
            SpawnNextShip();
        }

        private void GenerateShipsForDay()
        {
            currentDayShips.Clear();
            
            // TEST
            var defaultShipPrefab = Resources.Load<GameObject>("Prefabs/Ship/ShipModel");
            
            var manifestManager = ServiceLocator.Get<CargoManifestManager>();
            if (manifestManager == null)
            {
                Debug.LogError("[GameManager] CargoManifestManager not found in ServiceLocator.");
                return;
            }

            for (int i = 0; i < shipsPerDay; i++)
            {
                bool isValid = Random.value > 0.5f;
                
                var ship = new ShipProfile
                {
                    ShipName = $"Submarine-{currentDay}-{i + 1}",
                    CargoList = manifestManager.GenerateRandomCargoList(),
                    IsValid = isValid,
                    ShipPrefab = defaultShipPrefab
                };
                
                currentDayShips.Add(ship);
            }
        }

        private void SpawnNextShip()
        {
            currentShipIndex++;

            if (currentShipIndex >= currentDayShips.Count)
            {
                EndOfDay();
                return;
            }

            var ship = currentDayShips[currentShipIndex];
            
            if (ship.ShipPrefab != null && _shipSpawn != null)
            {
                GameObject shipInstance = Instantiate(ship.ShipPrefab, _shipSpawn.position, Quaternion.identity);
                _currentShipController = shipInstance.GetComponent<ShipController>();
                _currentShipController.Initialize(_shipSpawn, _shipDock, _shipExit, _shipSink);
                _currentShipController.MoveToDock();
            }
            
            Debug.Log($"Ship incoming: {ship.ShipName}");

            state = GameplayState.WaitingForShip;
        }
        
        public void OnShipReadyForInspection(ShipController ship)
        {
            Debug.Log($"[GameManager] {currentDayShips[currentShipIndex].ShipName} ready for inspection.");
            state = GameplayState.ReadyForInspection;
            _eventHub?.Publish(new ShipArrivedForInspectionEvent(currentShipIndex, currentDayShips[currentShipIndex]));
        }

        public void OnShipExitComplete()
        {
            StartCoroutine(WaitThenNextShip());
        }

        public void StartInspection()
        {
            inspectionActive = true;
            timer = currentDayShips[currentShipIndex].ShipInspectionTime;
            state = GameplayState.Inspecting;
            Debug.Log("Inspection started. (Press 1 to Approve, 2 to Decline)");
            
            var ship = currentDayShips[currentShipIndex];
            _eventHub?.Publish(new InspectionStartedEvent(ship));
        }

        public void CompleteInspection(bool approved, bool timedOut = false)
        {
            inspectionActive = false;
            state = GameplayState.WaitingForShip;
            
            var ship = currentDayShips[currentShipIndex];
            bool correct = approved == ship.IsValid && !timedOut;
            
            if (timedOut)
            {
                Debug.Log($"[GameManager] Timed out on {ship.ShipName} — sinking ship.");
                _currentShipController?.Sink();
            }
            else if (approved)
            {
                Debug.Log($"[GameManager] Ship approved, exiting normally.");
                _currentShipController?.MoveToExit();
            }
            else
            {
                Debug.Log($"[GameManager] Ship declined, returning to spawn.");
                _currentShipController?.Decline();
            }

            _eventHub?.Publish(new InspectionCompletedEvent(approved, timedOut, ship));
        }

        private IEnumerator WaitThenNextShip()
        {
            yield return new WaitForSeconds(transitionDelay);

            SpawnNextShip();
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
        
        public void RegisterSceneWaypoints(Transform spawn, Transform dock, Transform exit, Transform sink)
        {
            _shipSpawn = spawn;
            _shipDock = dock;
            _shipExit = exit;
            _shipSink = sink;

            Debug.Log("[GameManager] Waypoints registered from scene.");
        }
    }
}