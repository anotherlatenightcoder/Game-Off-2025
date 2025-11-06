using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class CargoManifestManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 50;

        private List<string> _bannedCargo = new();
        private EventHub _eventHub;
        
        public IReadOnlyList<string> BannedCargo => _bannedCargo;
        
        private static readonly string[] POSSIBLE_ITEMS = new[]
        {
            "Fuel Cells", "Oxygen Tanks", "Food Supplies", "Sealed Crate",
            "Medical Supplies", "Bio Samples", "Machine Parts", "Weapons", "Explosives", "Waste Barrels"
        };
        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub.Subscribe<DayStartedEvent>(OnDayStarted);
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            GenerateDailyBannedList(evt.Day);
        }

        private void GenerateDailyBannedList(int day)
        {
            _bannedCargo.Clear();
            
            // Randomly ban like 2 or 3 cargo types
            int bannedCount = Random.Range(2, 4);
            var itemPool = new List<string>(POSSIBLE_ITEMS);

            for (int i = 0; i < bannedCount; i++)
            {
                int index = Random.Range(0, itemPool.Count);
                _bannedCargo.Add(itemPool[index]);
                itemPool.RemoveAt(index);
            }
            
            Debug.Log($"[CargoManifest] Banned cargo for Day {day}: {string.Join(", ", _bannedCargo)}");
        }
        
        public List<string> GenerateRandomCargoList()
        {
            var result = new List<string>();
            var itemPool = new List<string>(POSSIBLE_ITEMS);

            int itemCount = Random.Range(2, 7);

            for (int i = 0; i < itemCount && itemPool.Count > 0; i++)
            {
                int index = Random.Range(0, itemPool.Count);
                result.Add(itemPool[index]);
                itemPool.RemoveAt(index);
            }

            return result;
        }
        
        public bool IsCargoBanned(string item) => _bannedCargo.Contains(item);
    }   
}
