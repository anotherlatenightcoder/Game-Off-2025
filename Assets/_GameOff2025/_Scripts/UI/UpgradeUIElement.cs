using Route24.Core;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class UpgradeUIElement : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image entryBG;
        [SerializeField] private Color normalColor;
        [SerializeField] private Color highlightedColor;
        [SerializeField] private Color disabledColor;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Badges")]
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private GameObject lockedBadge;

        private UpgradeData _data;
        private System.Action<UpgradeData> _buyAction;

        private bool _locked;
        private bool _purchased;
        private bool _affordable;

        public bool CanHighlight => _affordable && !_purchased && !_locked;
        public bool CanPurchase => CanHighlight;
        public UpgradeData Data => _data;

        public void Setup(UpgradeData data)
        {
            _data = data;
            _buyAction = null;

            nameText.text = data.Description;
            tierText.text = $"Tier {data.Tier}";
            costText.text = $"${data.Cost}";

            UpdateState();

            entryBG.color = normalColor;
        }
        public void Setup(UpgradeData data, System.Action<UpgradeData> buyAction)
        {
            _data = data;
            _buyAction = buyAction;

            nameText.text = data.Description;
            tierText.text = $"Tier {data.Tier}";
            costText.text = $"${data.Cost}";

            UpdateState();

            entryBG.color = normalColor;
        }

        private void UpdateState()
        {
            var shop = ServiceLocator.Get<UpgradeShopController>();
            var currency = ServiceLocator.Get<CurrencyManager>();

            _purchased = shop.IsPurchased(_data.Id);

            if (_purchased)
            {
                ownedBadge.SetActive(true);
                lockedBadge.SetActive(false);
                _locked = false;
                _affordable = false;
                entryBG.color = disabledColor;
                return;
            }

            // Tier lock
            if (_data.Tier > 1)
            {
                string requiredId = $"{_data.BaseId}_T{_data.Tier - 1}";
                _locked = !shop.IsPurchased(requiredId);
            }
            else
            {
                _locked = false;
            }

            lockedBadge.SetActive(_locked);
            ownedBadge.SetActive(false);

            if (_locked)
            {
                entryBG.color = disabledColor;
                _affordable = false;
                return;
            }

            _affordable = currency.HasEnoughCurrency(_data.Cost);

            entryBG.color = _affordable ? normalColor : disabledColor;
        }

        public void SetHighlighted(bool active)
        {
            if (!CanHighlight)
            {
                entryBG.color = disabledColor;
                return;
            }

            entryBG.color = active ? highlightedColor : normalColor;
            AudioManager.Instance.PlaySFX("CARGO_SELECTION");
        }
    }
}
