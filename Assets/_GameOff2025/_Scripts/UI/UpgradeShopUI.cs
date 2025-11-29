using Route24.Core;
using TMPro;
using UnityEngine;

namespace Route24.GameOff
{
    public class UpgradeShopUI : MonoBehaviour, ISceneInitializable
    {
        [SerializeField] private CanvasGroup _panelCanvas;
        [SerializeField] private Transform upgradeListHolder;
        [SerializeField] private UpgradeUIElement upgradePrefab;
        [SerializeField] private CategoryHeaderUI categoryHeaderPrefab;
        [SerializeField] private GameObject categorySpacerPrefab;
        [SerializeField] private TextMeshProUGUI balanceText;

        private UpgradeShopController _controller;

        public void SceneInitialize()
        {
            _controller = ServiceLocator.Get<UpgradeShopController>();
            Hide();
        }

        public void Show()
        {
            _panelCanvas.alpha = 1f;
            _panelCanvas.interactable = true;
            _panelCanvas.blocksRaycasts = true;

            RefreshUI();
        }

        public void Hide()
        {
            _panelCanvas.alpha = 0f;
            _panelCanvas.interactable = false;
            _panelCanvas.blocksRaycasts = false;
        }

        private void RefreshUI()
        {
            foreach (Transform t in upgradeListHolder)
                Destroy(t.gameObject);

            string lastCategory = null;
            bool firstCategory = true;

            foreach (var upgrade in _controller.AllUpgradesOrdered)
            {
                // When a new category is encountered, insert a header (+ optional spacer)
                if (upgrade.Category != lastCategory)
                {
                    // Add a spacer ONLY for categories after the first one
                    if (!firstCategory)
                    {
                        Instantiate(categorySpacerPrefab, upgradeListHolder);
                    }

                    // Add the category header
                    var header = Instantiate(categoryHeaderPrefab, upgradeListHolder);
                    header.SetCategoryName(upgrade.Category);

                    // Track where we are
                    lastCategory = upgrade.Category;
                    firstCategory = false;
                }

                // Add the upgrade item
                var element = Instantiate(upgradePrefab, upgradeListHolder);
                element.Setup(upgrade, OnBuyPressed);
            }
        }

        private void OnBuyPressed(UpgradeData data)
        {
            if (_controller.TryPurchase(data.Id))
                RefreshUI();
        }
    }
}
