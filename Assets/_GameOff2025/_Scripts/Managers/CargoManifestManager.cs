using System.Collections.Generic;
using System.Linq;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class CargoManifestManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 50;

        [Header("Cargo Database")]
        [SerializeField] private List<CargoItem> _allCargoItems = new();

        private List<CargoItem> _bannedCargo = new();
        private List<CargoItem> _validCargoItems;
        private EventHub _eventHub;

        public IReadOnlyList<CargoItem> BannedCargo => _bannedCargo;
        public IReadOnlyList<CargoItem> AllCargo => _allCargoItems;

        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub.Subscribe<DayStartedEvent>(OnDayStarted);
            _eventHub.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            GenerateDailyBannedList(evt.Day);
            SetValidCargoItems();
        }

        private void OnDayEnded(DayEndedEvent evt)
        {
            ClearBannedCargo();
        }

        private void GenerateDailyBannedList(int day)
        {
            _bannedCargo.Clear();

            int bannedCount = Random.Range(2, 4);
            var pool = new List<CargoItem>(_allCargoItems);

            for (int i = 0; i < bannedCount && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                _bannedCargo.Add(pool[index]);
                pool.RemoveAt(index);
            }

            Debug.Log($"[CargoManifest] Banned cargo for Day {day}: {string.Join(", ", _bannedCargo.Select(c => c.DisplayName))}");
            _eventHub?.Publish(new BannedCargoGeneratedEvent(day, _bannedCargo));
        }

        public void ClearBannedCargo()
        {
            _bannedCargo.Clear();
            _eventHub?.Publish(new BannedCargoClearedEvent());
        }

        public List<CargoItem> GenerateRandomCargoList()
        {
            var result = new List<CargoItem>();
            var pool = new List<CargoItem>(_allCargoItems);

            int itemCount = Random.Range(2, 7);
            for (int i = 0; i < itemCount && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }

        public bool IsCargoBanned(CargoItem item)
        {
            return _bannedCargo.Exists(x => x.Id == item.Id);
        }
        
        public List<CargoItem> GenerateRandomCargoList(bool isValidCargo , int minCargoCount, int maxCargoCount)
        {
            if(isValidCargo)
                return GenerateRandomValidCargoList(minCargoCount, maxCargoCount);
            return GenerateRandomCargoListWithBannedItems(minCargoCount, maxCargoCount);
        }
        
        private List<CargoItem> GenerateRandomValidCargoList(int minCargoCount, int maxCargoCount)
        {
            var result = new List<CargoItem>();

            if (_validCargoItems.Count == 0)
            {
                Debug.LogWarning("[CargoManifest] No valid cargo available!");
                return result;
            }

            int itemCount = Random.Range(minCargoCount, maxCargoCount + 1);
            itemCount = Mathf.Min(itemCount, _validCargoItems.Count); // avoid overflow

            // Pick random items without removing from the original list
            for (int i = 0; i < itemCount; i++)
            {
                int index = Random.Range(0, _validCargoItems.Count);
                result.Add(_validCargoItems[index]);
            }

            return result;
        }

        private List<CargoItem> GenerateRandomCargoListWithBannedItems(int minCargoCount, int maxCargoCount)
        {
            var result = new List<CargoItem>();

            if (_allCargoItems.Count == 0)
            {
                Debug.LogWarning("[CargoManifest] No cargo available!");
                return result;
            }

            int itemCount = Random.Range(minCargoCount, maxCargoCount + 1);
            itemCount = Mathf.Min(itemCount, _allCargoItems.Count);

            // Pick 1 or 2 banned items randomly
            int bannedToInclude = Mathf.Min(Random.Range(1, 3), _bannedCargo.Count);
            for (int i = 0; i < bannedToInclude; i++)
            {
                int index = Random.Range(0, _bannedCargo.Count);
                result.Add(_bannedCargo[index]);
                itemCount--; // remaining items will be valid cargo
            }

            // Fill the rest with valid cargo
            for (int i = 0; i < itemCount; i++)
            {
                int index = Random.Range(0, _validCargoItems.Count);
                result.Add(_validCargoItems[index]);
            }

            // Shuffle so banned items aren't always first
            return result.OrderBy(_ => Random.value).ToList();
        }

        private void SetValidCargoItems()
        {
            _validCargoItems = _allCargoItems
                .Where(item => !_bannedCargo.Exists(b => b.Id == item.Id))
                .ToList();

        }
    }
}
