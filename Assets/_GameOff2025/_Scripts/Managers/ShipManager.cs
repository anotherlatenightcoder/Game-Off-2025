using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class ShipManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 5;
        
        private GameManager _gameManager;
        private EventHub _eventHub;
        
        [Header("SO References")]
        [SerializeField] private List<ShipsConfig> _shipsConfig;

        #region  Spawn Transforms
        private Transform _shipSpawn;
        private Transform _shipDock;
        private Transform _shipExit;
        private Transform _shipSink;
        #endregion
        
        private ShipController _currentShipController;
        private ShipProfile _currentShipProfile;
        private int currentShipIndex = -1;
        private int _day;
        private int _dayIndex => _day - 1;
        
        public void Initialize()
        {
            print("[ShipManager] Initialized.");
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
        }
        
        public void RegisterSceneWaypoints(Transform spawn, Transform dock, Transform exit, Transform sink)
        {
            _shipSpawn = spawn;
            _shipDock = dock;
            _shipExit = exit;
            _shipSink = sink;

            Debug.Log("[GameManager] Waypoints registered from scene.");
        }

        public void StartNewDay(int day)
        {
            _day = day;
            if(_day <= 0 || _day >= _shipsConfig.Count)
            {
                Debug.LogError("[ShipManager] Invalid day index for ship config. auto set to 1");
                _day = 1;
            }
            
            currentShipIndex = -1;
        }
        
        public void OnShipReadyForInspection(ShipController ship)
        {
            Debug.Log($"[GameManager] {_currentShipProfile.ShipName} ready for inspection.");
            _gameManager.OnShipReadyForInspection();
            _eventHub?.Publish(new ShipArrivedForInspectionEvent(currentShipIndex, _currentShipProfile));
            
            AudioTestManager.Instance.PlaySFX("SHIP_ARRIVED");
        }
        
        public bool TrySpawnNextShip()
        {
            currentShipIndex++;

            if (currentShipIndex >= _shipsConfig[_dayIndex].ShipProfiles.Length)
            {
                Debug.Log("[ShipManager] No more ships for today.");
                return false;
            }

            _currentShipProfile = _shipsConfig[_dayIndex].ShipProfiles[currentShipIndex].Profile;
            
            if (_currentShipProfile.ShipPrefab != null && _shipSpawn != null)
            {
                GameObject shipInstance = Instantiate(_currentShipProfile.ShipPrefab, _shipSpawn.position, Quaternion.identity);
                _currentShipController = shipInstance.GetComponent<ShipController>();
                _currentShipController.Initialize(_shipSpawn, _shipDock, _shipExit, _shipSink);
                _currentShipController.MoveToDock();
                Debug.Log($"Ship incoming: {_currentShipProfile.ShipName}");
            }
            else 
                Debug.Log("[ShipManager] failed to spawn ship.");
            
            
            return true;
        }
        
        public void HandleInspectionComplete(bool timedOut, bool approved)
        {
            if (timedOut)
            {
                Debug.Log($"[ShipManager] Timed out on {_currentShipProfile.ShipName} — sinking ship.");
                _currentShipController?.Sink();
            }
            else if (approved)
            {
                Debug.Log($"[ShipManager] Ship approved, exiting normally.");
                _currentShipController?.MoveToExit();
            }
            else
            {
                Debug.Log($"[ShipManager] Ship declined, returning to spawn.");
                _currentShipController?.Decline();
            }
        }
        
        public float GetInspectionTimeForCurrentShip() => _currentShipProfile.ShipInspectionTime;
        public ShipProfile GetCurrentShipProfile() => _currentShipProfile;
    }
}