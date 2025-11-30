using System;
using System.Collections.Generic;
using System.Linq;
using Route24.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Route24.GameOff
{
    public class ShipManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 5;

        private const int _arrayLength = 10; // max length for random generation ship arrays
        
        private GameManager _gameManager;
        private CargoManifestManager _cargoManifestManager;
        private EventHub _eventHub;
        
        [Header("SO References")]
        [SerializeField] private List<DayConfig> _dayConfigs;
        
        private List<ShipProfile> _shipProfiles = new List<ShipProfile>(10);

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
            _gameManager = ServiceLocator.Get<GameManager>();
            _cargoManifestManager = ServiceLocator.Get<CargoManifestManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            _eventHub.Subscribe<DayEndedEvent>(OnDayEnded);
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
            if(_day <= 0 || _day >= _dayConfigs.Count)
            {
                Debug.LogError("[ShipManager] Invalid day index for ship config. auto set to 1");
                _day = 1;
            }

            currentShipIndex = -1;
            
            GenerateRandomShipProfiles(_dayConfigs[_day]);
        }
        
        public void OnShipReadyForInspection(ShipController ship)
        {
            _currentShipProfile.EntryCode = Random.Range(0, 10000).ToString("D4");
            
            Debug.Log($"[GameManager] {_currentShipProfile.ShipName} ready for inspection.");
            _gameManager.OnShipReadyForInspection();
            _eventHub?.Publish(new ShipArrivedForInspectionEvent(currentShipIndex, _currentShipProfile));
        }
        
        public void SpawnNewShip()
        {
            if (_gameManager.CurrentGameplayState != GameplayState.WaitingForShip)
                return;
            
            currentShipIndex++;

            if (currentShipIndex >= _shipProfiles.Count)
            {
                currentShipIndex = 0;
                GenerateRandomShipProfiles(_dayConfigs[_day]);
            }

            _currentShipProfile = _shipProfiles[currentShipIndex];
            
            if (_currentShipProfile.ShipPrefab != null && _shipSpawn != null)
            {
                GameObject shipInstance = Instantiate(_currentShipProfile.ShipPrefab, _shipSpawn.position, Quaternion.identity);
                _currentShipController = shipInstance.GetComponent<ShipController>();
                _currentShipController.Initialize(_shipSpawn, _shipDock, _shipExit, _shipSink);
                _currentShipController.MoveToDock();
                
                AudioManager.Instance.PlaySFX("SHIP_SPAWN");
                
                Debug.Log($"Ship incoming: {_currentShipProfile.ShipName}");

                var shipCargo = _currentShipProfile.CargoList;
                var bannedCargo = _cargoManifestManager.BannedCargo;
                
                _shipProfiles[currentShipIndex].IsValid = !shipCargo.Any(item => bannedCargo.Contains(item));
            }
            else 
                Debug.Log("[ShipManager] failed to spawn ship.");
            
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
                _currentShipController = null;
            }
            else
            {
                Debug.Log($"[ShipManager] Ship declined, returning to spawn.");
                _currentShipController?.Decline();
            }
        }
        
        public ShipProfile GetCurrentShipProfile() => _currentShipProfile;

        private void SetShipProfilesFromDayConfig(DayConfig dayConfig)
        {
            _shipProfiles.Clear();                
            
            foreach (var shipEntry in dayConfig.ShipProfiles)
                _shipProfiles.Add(shipEntry.Profile);
        }
        
        private void GenerateRandomShipProfiles(DayConfig dayConfig)
        {
            _shipProfiles.Clear();

            Span<bool> validIndices = stackalloc bool[_arrayLength];
            Span<bool> timedIndices = stackalloc bool[_arrayLength];

            validIndices.SetRandomTrueValues(dayConfig.ValidPercentage);
            timedIndices.SetRandomTrueValues(dayConfig.TimedShipRatio);


            for (int i = 0; i < _arrayLength; i++) 
                _shipProfiles.Add(new ShipProfile()
                {
                    ShipName = ShipNames.GetRandomShipName(),
                    IsValid = validIndices[i],
                    ShipInspectionTime = GetShipTime(timedIndices[i]),
                    CargoList = _cargoManifestManager.GenerateRandomCargoList(validIndices[i], dayConfig.MinCargoCount, dayConfig.MaxCargoCount),
                    ShipPrefab = GetRandomShipPrefab()
                });
        }

        private GameObject GetRandomShipPrefab()
        {
            float roll = Random.value;

            if (roll < 0.3f)
                return Resources.Load<GameObject>("Prefabs/Ship/ShipModel2");

            return Resources.Load<GameObject>("Prefabs/Ship/ShipModel");
        }

        private int GetShipTime(bool isTimed)
        {
            if (!isTimed)
                return -1;

            return Random.Range(25, 35);
        }

        private void OnDayEnded(DayEndedEvent obj)
        {
            if(_currentShipController)
                _currentShipController.Decline();
        }
    }
}