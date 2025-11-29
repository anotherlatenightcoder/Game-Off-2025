using Route24.Core;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class UpgradeUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private GameObject lockedBadge;

        private UpgradeData _data;
        private System.Action<UpgradeData> _buyAction;

        public void Setup(UpgradeData data, System.Action<UpgradeData> buyAction)
        {
            _data = data;
            _buyAction = buyAction;

            nameText.text = $"{data.Category} Lv {data.Tier}";
            costText.text = $"${data.Cost}";

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _buyAction(data));

            UpdateState();
        }

        private void UpdateState()
        {
            var shop = ServiceLocator.Get<UpgradeShopController>();

            if (shop.IsPurchased(_data.Id))
            {
                buyButton.gameObject.SetActive(false);
                ownedBadge.SetActive(true);
                lockedBadge.SetActive(false);
                return;
            }

            // Check tier lock
            if (_data.Tier > 1)
            {
                string prereq = $"{_data.Category}_Level{_data.Tier - 1}";
                if (!shop.IsPurchased(prereq))
                {
                    buyButton.gameObject.SetActive(false);
                    ownedBadge.SetActive(false);
                    lockedBadge.SetActive(true);
                    return;
                }
            }

            // Available for purchase
            buyButton.gameObject.SetActive(true);
            ownedBadge.SetActive(false);
            lockedBadge.SetActive(false);
        }
    }
}