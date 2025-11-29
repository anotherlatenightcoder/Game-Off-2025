using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class UpgradeShopController : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 20;

        private Dictionary<string, UpgradeData> _upgrades = new();

        public void Initialize()
        {
            LoadAllUpgrades();
        }

        private void LoadAllUpgrades()
        {
            // Example setup — replace with your real data
            AddUpgrade("Keypad_Level1", "Keypad", 1, cost: 15, description: "Automatically decodes a scrambled code after 10 seconds.");
            AddUpgrade("Keypad_Level2", "Keypad", 2, cost: 30, description: "All codes are automatically decoded by default.");

            AddUpgrade("Osc_MatchTime_Level1", "Oscillator", 1, cost: 10, description: "Reduces signal match time by 1.5 seconds.");
            AddUpgrade("Osc_MatchTime_Level2", "Oscillator", 2, cost: 15, description: "Reduces signal match time by 3 seconds.");
            AddUpgrade("Osc_MatchPerc_Level1", "Oscillator", 1, cost: 10, description: "Reduce match percentage needed by 10%.");
            AddUpgrade("Osc_MatchPerc_Level2", "Oscillator", 2, cost: 15, description: "Reduce match percentage needed by 20%.");
            AddUpgrade("Osc_Random_Level1", "Oscillator", 1, cost: 30, description: "Removes any random interference signals.");

            AddUpgrade("Cargo_Scan1", "Cargo", 1, cost: 10, description: "Reduce scan time by 1 second.");
            AddUpgrade("Cargo_Scan2", "Cargo", 2, cost: 15, description: "Reduce scan time by 2 seconds.");
            AddUpgrade("Cargo_AutoScan", "Cargo", 1, cost: 30, description: "Automatically scans all items one by one on inspection start.");
        }

        private void AddUpgrade(string id, string category, int tier, int cost, string description)
        {
            _upgrades[id] = new UpgradeData(id, category, tier, cost, description);
        }

        public IReadOnlyDictionary<string, UpgradeData> Upgrades => _upgrades;

        public bool IsPurchased(string id)
        {
            return _upgrades[id].Purchased;
        }

        public bool TryPurchase(string id)
        {
            if (!_upgrades.ContainsKey(id)) return false;

            UpgradeData upgrade = _upgrades[id];

            // Must not already be purchased
            if (upgrade.Purchased) return false;

            // Must own previous tier
            if (upgrade.Tier > 1)
            {
                string required = $"{upgrade.Category}_Level{upgrade.Tier - 1}";
                if (!_upgrades.ContainsKey(required) || !_upgrades[required].Purchased)
                    return false;
            }

            // Must have enough money
            var currency = ServiceLocator.Get<CurrencyManager>();
            if (!currency.HasEnoughCurrency(upgrade.Cost))
                return false;

            currency.RemoveCurrency(upgrade.Cost, $"Purchased {upgrade.Id}");
            upgrade.Purchased = true;
            _upgrades[upgrade.Id] = upgrade;

            return true;
        }
    }

    public struct UpgradeData
    {
        public string Id;
        public string Category;
        public int Tier;
        public int Cost;
        public bool Purchased;
        public string Description;

        public UpgradeData(string id, string category, int tier, int cost, string description)
        {
            Id = id;
            Category = category;
            Tier = tier;
            Cost = cost;
            Purchased = false;
            Description = description;
        }
    }
}
