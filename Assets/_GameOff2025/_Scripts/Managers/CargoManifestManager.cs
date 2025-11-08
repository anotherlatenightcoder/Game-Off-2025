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
    }
}
