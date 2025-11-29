using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class UpgradeShopUI : MonoBehaviour, ISceneInitializable
    {
        [SerializeField] private CanvasGroup _panelCanvas;
        [SerializeField] private Transform upgradeListHolder;
        [SerializeField] private UpgradeUIElement upgradePrefab;

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

            foreach (var kvp in _controller.Upgrades)
            {
                var data = kvp.Value;

                var element = Instantiate(upgradePrefab, upgradeListHolder);
                element.Setup(data, OnBuyPressed);
            }
        }

        private void OnBuyPressed(UpgradeData data)
        {
            if (_controller.TryPurchase(data.Id))
                RefreshUI();   
        }
    }
}