using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class UpgradeShopController : MonoBehaviour, IService, IInitializable
    {
        public static UpgradeShopController Instance { get; private set; }

        public int InitializationPriority => 20;

        /// <summary>
        /// Dictionary for quick lookup by ID.
        /// </summary>
        private Dictionary<string, UpgradeData> _upgradeLookup = new();

        /// <summary>
        /// Preserves the exact order upgrades are added.
        /// </summary>
        private List<UpgradeData> _upgradeList = new();

        public IReadOnlyList<UpgradeData> AllUpgradesOrdered => _upgradeList;

        public void Initialize()
        {
            Instance = this;
            LoadAllUpgrades();
        }

        // --------------------------------------------------------------------------------------
        // UPGRADE CREATION LOGIC
        // --------------------------------------------------------------------------------------

        private void LoadAllUpgrades()
        {
            // Keypad
            AddUpgrade("Keypad", "Keypad", 1, 15, "Automatically decodes a scrambled code after 10 seconds.");
            AddUpgrade("Keypad", "Keypad", 2, 30, "All codes are automatically decoded by default.");

            // Oscillator – multiple upgrade lines inside same category
            AddUpgrade("Osc_MatchTime", "Oscillator", 1, 10, "Reduces signal match time by 1.5 seconds.");
            AddUpgrade("Osc_MatchTime", "Oscillator", 2, 15, "Reduces signal match time by 3 seconds.");

            AddUpgrade("Osc_MatchPerc", "Oscillator", 1, 10, "Reduce match percentage needed by 10%.");
            AddUpgrade("Osc_MatchPerc", "Oscillator", 2, 15, "Reduce match percentage needed by 20%.");

            AddUpgrade("Osc_Random", "Oscillator", 1, 30, "Removes any random interference signals.");

            // Cargo
            AddUpgrade("Cargo", "Cargo", 1, 10, "Reduce scan time by 1 second.");
            AddUpgrade("Cargo", "Cargo", 2, 15, "Reduce scan time by 2 seconds.");

            AddUpgrade("Cargo_AutoScan", "Cargo", 1, 30, "Automatically scans all items on inspection start.");
        }

        /// <summary>
        /// Adds a new upgrade with auto-generated tiered ID.
        /// baseId: Osc_MatchTime
        /// tier: 2
        /// final ID: Osc_MatchTime_T2
        /// </summary>
        private void AddUpgrade(string baseId, string category, int tier, int cost, string description)
        {
            string finalId = $"{baseId}_T{tier}";

            var upgrade = new UpgradeData(
                id: finalId,
                baseId: baseId,
                category: category,
                tier: tier,
                cost: cost,
                description: description
            );

            _upgradeLookup[finalId] = upgrade;
            _upgradeList.Add(upgrade);
        }

        // --------------------------------------------------------------------------------------
        // UPGRADE PURCHASE LOGIC
        // --------------------------------------------------------------------------------------

        public bool TryPurchase(string upgradeId)
        {
            if (!_upgradeLookup.TryGetValue(upgradeId, out var upgrade))
                return false;

            if (upgrade.Purchased)
                return false;

            // Must own previous tier if tier > 1
            if (upgrade.Tier > 1)
            {
                string requiredId = $"{upgrade.BaseId}_T{upgrade.Tier - 1}";

                if (!_upgradeLookup.TryGetValue(requiredId, out var prevTier) ||
                    !prevTier.Purchased)
                {
                    return false;
                }
            }

            // Check currency
            var currency = ServiceLocator.Get<CurrencyManager>();

            if (!currency.HasEnoughCurrency(upgrade.Cost))
                return false;

            currency.RemoveCurrency(upgrade.Cost, $"Purchased {upgrade.Id}");

            // Apply purchase
            upgrade.Purchased = true;
            _upgradeLookup[upgradeId] = upgrade;
            SyncOrderedList(upgrade);

            return true;
        }

        /// <summary>
        /// Replaces the modified upgrade inside the ordered list.
        /// </summary>
        private void SyncOrderedList(UpgradeData updated)
        {
            for (int i = 0; i < _upgradeList.Count; i++)
            {
                if (_upgradeList[i].Id == updated.Id)
                {
                    _upgradeList[i] = updated;
                    return;
                }
            }
        }

        // --------------------------------------------------------------------------------------
        // LOCKED STATE CHECK
        // --------------------------------------------------------------------------------------

        public bool IsLocked(UpgradeData upgrade)
        {
            if (upgrade.Tier == 1) return false;

            string requiredId = $"{upgrade.BaseId}_T{upgrade.Tier - 1}";

            if (!_upgradeLookup.TryGetValue(requiredId, out var prevTier))
                return true;

            return !prevTier.Purchased;
        }

        public bool IsPurchased(string id)
        {
            return _upgradeLookup[id].Purchased;
        }
    }

    // ==========================================================================================
    // UPGRADE DATA STRUCT
    // ==========================================================================================

    [System.Serializable]
    public struct UpgradeData
    {
        public string Id;          // Osc_MatchTime_T2
        public string BaseId;      // Osc_MatchTime
        public string Category;    // Oscillator
        public int Tier;           // 2
        public int Cost;
        public bool Purchased;
        public string Description;

        public UpgradeData(string id, string baseId, string category, int tier, int cost, string description)
        {
            Id = id;
            BaseId = baseId;
            Category = category;
            Tier = tier;
            Cost = cost;
            Purchased = false;
            Description = description;
        }
    }
}
