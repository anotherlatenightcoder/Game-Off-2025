using Route24.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
        private PauseManager _pauseManager;
        private List<UpgradeUIElement> _elements = new();

        private int _currentIndex = -1;
        private float _spaceHoldTimer = 0f;
        private const float SpaceHoldDuration = 2f;

        public void SceneInitialize()
        {
            _controller = ServiceLocator.Get<UpgradeShopController>();
            _pauseManager = ServiceLocator.Get<PauseManager>();
            Hide();
        }

        public void Show()
        {
            _panelCanvas.alpha = 1f;
            _panelCanvas.interactable = true;
            _panelCanvas.blocksRaycasts = true;

            RefreshUI();
            HighlightFirstValidItem();
        }

        public void Hide()
        {
            _panelCanvas.alpha = 0f;
            _panelCanvas.interactable = false;
            _panelCanvas.blocksRaycasts = false;
        }

        private void Update()
        {
            if (!_panelCanvas.interactable) return;
            if (_pauseManager.IsPaused) return;

            HandleNavigationInput();
            HandlePurchaseInput();
            HandleSpaceToStartNextDay();
            HandleMouseScroll();
        }

        private void HandleNavigationInput()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                MoveSelection(+1);

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                MoveSelection(-1);
        }

        private void HandleMouseScroll()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.1f)
            {
                MoveSelection(scroll > 0 ? -1 : 1);
            }
        }

        private void HandlePurchaseInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) && _currentIndex >= 0)
            {
                var el = _elements[_currentIndex];
                if (el.CanPurchase)
                {
                    OnBuyPressed(el.Data);
                }
            }
        }

        private void HandleSpaceToStartNextDay()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                _spaceHoldTimer += Time.deltaTime;
                if (_spaceHoldTimer >= SpaceHoldDuration)
                {
                    ServiceLocator.Get<GameManager>().StartNextDay();
                    _spaceHoldTimer = 0f;
                    Hide();
                }
            }
            else
            {
                _spaceHoldTimer = 0f;
            }
        }

        private void MoveSelection(int direction)
        {
            if (_elements.Count == 0) return;

            int tries = _elements.Count;

            do
            {
                _currentIndex += direction;

                if (_currentIndex < 0) _currentIndex = _elements.Count - 1;
                if (_currentIndex >= _elements.Count) _currentIndex = 0;

                tries--;

            } while (tries > 0 && !_elements[_currentIndex].CanHighlight);

            HighlightCurrentItem();
        }

        private void HighlightFirstValidItem()
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i].CanHighlight)
                {
                    _currentIndex = i;
                    HighlightCurrentItem();
                    return;
                }
            }
        }

        private void HighlightCurrentItem()
        {
            for (int i = 0; i < _elements.Count; i++)
                _elements[i].SetHighlighted(i == _currentIndex);
        }

        private void RefreshUI()
        {
            foreach (Transform t in upgradeListHolder)
                Destroy(t.gameObject);
            _elements.Clear();

            var currency = ServiceLocator.Get<CurrencyManager>();
            balanceText.text = $"Balance: ${currency.CurrencyAmount}";

            string lastCategory = null;
            bool firstCategory = true;

            foreach (var upgrade in _controller.AllUpgradesOrdered)
            {
                // Category change
                if (upgrade.Category != lastCategory)
                {
                    if (!firstCategory)
                        Instantiate(categorySpacerPrefab, upgradeListHolder);

                    var header = Instantiate(categoryHeaderPrefab, upgradeListHolder);
                    header.SetCategoryName(upgrade.Category);

                    lastCategory = upgrade.Category;
                    firstCategory = false;
                }

                var element = Instantiate(upgradePrefab, upgradeListHolder);
                element.Setup(upgrade, OnBuyPressed);
                _elements.Add(element);
            }
        }

        private void OnBuyPressed(UpgradeData data)
        {
            if (_controller.TryPurchase(data.Id))
            {
                RefreshUI();
                HighlightFirstValidItem();
                AudioManager.Instance.PlaySFX("CARGO_CONFIRM");

                WaveGameStats.Instance.TrackUpgrade(data, ServiceLocator.Get<GameManager>().currentDay);
            }
        }
    }
}
