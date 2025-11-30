using System.Collections.Generic;
using Route24.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class PauseMenuUI : MonoBehaviour, ISceneInitializable
    {
        [Header("Root UI")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _pausedText;

        [Header("Status Block")]
        [SerializeField] private TextMeshProUGUI _dayText;
        [SerializeField] private TextMeshProUGUI _timeRemainingText;
        [SerializeField] private TextMeshProUGUI _balanceText;

        [Header("Sliders")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _sensitivitySlider;

        [Header("Upgrades")]
        [SerializeField] private Transform _upgradesContainer;
        [SerializeField] private UpgradeUIElement _upgradeEntryPrefab;

        private EventHub _eventHub;
        private GameManager _gameManager;
        private TubeClockController _tubeClockController;
        private CurrencyManager _currencyManager;
        private UpgradeShopController _upgradeShopController;

        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _gameManager = ServiceLocator.Get<GameManager>();
            _currencyManager = ServiceLocator.Get<CurrencyManager>();
            _tubeClockController = ServiceLocator.Get<TubeClockController>();
            _upgradeShopController = ServiceLocator.Get<UpgradeShopController>();

            _eventHub.Subscribe<GamePausedEvent>(OnPauseChanged);
            
            _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            _musicSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);

            Hide();
        }

        private void OnPauseChanged(GamePausedEvent e)
        {
            if (e.IsPaused)
                Show();
            else
                Hide();
        }

        private void Show()
        {
            UpdateStatusBlock();
            UpdatePurchasedUpgrades();
            
            if (AudioManager.Instance != null)
                _musicSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        private void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void UpdateStatusBlock()
        {
            _dayText.text = $"Day: {_gameManager.currentDay}";
            _timeRemainingText.text = $"Time Left: {_tubeClockController.GetRemainingTimeFormatted()}";
            _balanceText.text = $"Balance: ${_currencyManager.CurrencyAmount:C0}";
        }

        private void UpdatePurchasedUpgrades()
        {
            foreach (Transform child in _upgradesContainer)
                Destroy(child.gameObject);

            var purchased = new Dictionary<string, List<UpgradeData>>();
            
            foreach (var upgrade in _upgradeShopController.AllUpgradesOrdered)
            {
                if (!upgrade.Purchased) 
                    continue;

                if (!purchased.ContainsKey(upgrade.Category))
                    purchased[upgrade.Category] = new List<UpgradeData>();

                purchased[upgrade.Category].Add(upgrade);
            }
            
            if (purchased.Count == 0)
            {
                Debug.Log("No upgrades purchased yet.");
            }
            else
            {
                foreach (var category in purchased.Keys)
                {
                    // We could ouput headings of the category of upgrade purchased
                    // but as an overview this is effort

                    foreach (var upgrade in purchased[category])
                    {
                        var element = Instantiate(_upgradeEntryPrefab, _upgradesContainer);
                        element.Setup(upgrade);
                    }
                }
            }
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
        }
    }
}
