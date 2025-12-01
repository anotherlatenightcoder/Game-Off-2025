using System;
using System.Collections;
using System.Collections.Generic;
using Route24.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Route24.GameOff
{
    public class ManifestConsoleController : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _highlightBar;
        [SerializeField] private Image _highlightBackground;
        [SerializeField] private Image _scanProgress;
        [SerializeField] private Transform _shipListContainer;
        [SerializeField] private Transform _bannedListContainer;
        [SerializeField] private GameObject _cargoEntryPrefab;
        [SerializeField] private TextMeshProUGUI _shipNameText;
        [SerializeField] private TextMeshProUGUI _shipCodeText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Scan Settings")]
        [SerializeField] private float _scanRevealDelay = 3f;
        [SerializeField] private Color _defaultColor = new(1f, 1f, 1f, 0f);
        [SerializeField] private Color _safeColor = Color.green;
        [SerializeField] private Color _bannedColor = Color.red;
        [SerializeField] private Color _scanColor = Color.cyan;
        
        [Header("Camera Settings")]
        [SerializeField] private SeatedCameraController _cameraController;
        [SerializeField] private Transform _cameraFocusPoint;
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField] private float _focusFOV = 40f;
        
        [Header("Tutorial Settings")]
        [SerializeField] private ShipProfileSO _tutorialShipProfile;

        private List<CargoEntryUI> _shipEntries = new();
        private List<CargoItem> _bannedItems = new();
        private int _currentIndex = 0;
        private bool _inFocus = false;
        private bool _recentlyExited = false;
        private bool _isScanning = false;
        private float _exitCooldown = 0.5f;
        private GameManager _gameManager;
        private EventHub _eventHub;
        private CargoManifestManager _manifestManager;
        private UpgradeShopController _upgradeShopController;
        private ShipProfile _currentShip;
        private Coroutine _autoDecodeRoutine;
        private Coroutine _autoScanRoutine;
        
        private static readonly float[] MaskPercentages =
        {
            0.2f,  // Day 1, 20% chance
            0.35f, // Day 2, 35% chance
            0.5f,  // Day 3, 50% chance
            0.75f, // Day 4, 75% chance
            1f     // Day 5+, ALWAYS!
        };
        
        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _manifestManager = ServiceLocator.Get<CargoManifestManager>();
            _gameManager = ServiceLocator.Get<GameManager>();
            _upgradeShopController = ServiceLocator.Get<UpgradeShopController>();
            
            _eventHub.Subscribe<Tutorial_OnInspectionStartedEvent>(OnTutorialStartInspection);
            _eventHub.Subscribe<TutorialCompletedEvent>(OnStartup);
            _eventHub.Subscribe<InspectionStartedEvent>(OnInspectionStarted);
            _eventHub.Subscribe<InspectionCompletedEvent>(OnInspectionEnded);
            _eventHub.Subscribe<BannedCargoGeneratedEvent>(OnBannedListGenerated);
            _eventHub.Subscribe<BannedCargoClearedEvent>(OnBannedListCleared);
            
            if (_highlightBar)
                _highlightBar.gameObject.SetActive(false);

            if (_scanProgress)
            {
                _scanProgress.fillAmount = 0f;
                _scanProgress.gameObject.SetActive(false);
            }

            if (_highlightBackground)
                _highlightBackground.color = _defaultColor;
            
            _canvasGroup.alpha = 0f;
        }

        private void OnTutorialStartInspection(Tutorial_OnInspectionStartedEvent obj)
        {
            ClearShipList();
            ServiceLocator.Get<CargoManifestManager>().GenerateDailyBannedList(0);
            _currentShip = _tutorialShipProfile.Profile;
            PopulateShipList(_currentShip.CargoList);
            PopulateShipDetails(false);
            _canvasGroup.alpha = 1f;
        }

        private void OnStartup(TutorialCompletedEvent obj)
        {
            ClearShipList();
            _canvasGroup.alpha = 1f;
        }

        // ─────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────

        private void OnInspectionStarted(InspectionStartedEvent evt)
        {
            _currentShip = evt.Ship;
            PopulateShipList(_currentShip.CargoList);
            PopulateShipDetails();
            
            if (HasCargoAutoScanTier2())
            {
                // Run all scans at the same time
                if (_autoScanRoutine != null)
                    StopCoroutine(_autoScanRoutine);

                _autoScanRoutine = StartCoroutine(AutoScanAllEntries_Instant());
            }
            else if (HasCargoAutoScan())
            {
                // Sequential scans
                if (_autoScanRoutine != null)
                    StopCoroutine(_autoScanRoutine);

                _autoScanRoutine = StartCoroutine(AutoScanAllEntries());
            }
        }
        
        private void OnInspectionEnded(InspectionCompletedEvent evt)
        {
            _currentShip = null;
            
            if (_autoDecodeRoutine != null)
            {
                StopCoroutine(_autoDecodeRoutine);
                _autoDecodeRoutine = null;
            }
            
            if (_autoScanRoutine != null)
            {
                StopCoroutine(_autoScanRoutine);
                _autoScanRoutine = null;
            }
            
            ClearShipList();
        }

        private void OnBannedListGenerated(BannedCargoGeneratedEvent evt)
        {
            Debug.Log($"[ManifestConsole] Banned list ready for Day {evt.Day}, populating display...");
            PopulateBannedList(evt.BannedItems);
        }

        private void OnBannedListCleared(BannedCargoClearedEvent evt)
        {
            Debug.Log("[ManifestConsole] Banned list cleared — cleaning display.");
            ClearBannedList();
        }

        private void ClearBannedList()
        {
            foreach (Transform child in _bannedListContainer)
                Destroy(child.gameObject);
            
            _bannedItems.Clear();
        }

        private void Update()
        {
            if (!_inFocus) return;
            
            HandleInput();
            
            if (Input.GetKeyDown(KeyCode.E))
                ExitFocus();
        }

        private void PopulateBannedList(IReadOnlyList<CargoItem> bannedItems)
        {
            foreach (Transform child in _bannedListContainer)
                Destroy(child.gameObject);

            foreach (CargoItem item in bannedItems)
            {
                var obj = Instantiate(_cargoEntryPrefab, _bannedListContainer);
                obj.GetComponentInChildren<TextMeshProUGUI>().text = item.DisplayName;
            }

            _bannedItems = new List<CargoItem>(bannedItems);
        }

        private void PopulateShipList(List<CargoItem> cargoList)
        {
            foreach (Transform child in _shipListContainer)
                Destroy(child.gameObject);
            _shipEntries.Clear();

            foreach (CargoItem cargo in cargoList)
            {
                var obj = Instantiate(_cargoEntryPrefab, _shipListContainer);
                var entry = obj.GetComponent<CargoEntryUI>();
                entry.Initialize(cargo);
                _shipEntries.Add(entry);
            }

            _currentIndex = 0;
            UpdateHighlight();
        }

        private float GetMaskPercentageByDay()
        {
            int day = _gameManager.currentDay;
            
            if (day <= MaskPercentages.Length)
                return MaskPercentages[day - 1];

            return MaskPercentages[MaskPercentages.Length - 1]; // 1f
        }
        
        private void PopulateShipDetails(bool mask = true)
        {
            string shipCode = _currentShip.EntryCode;

            if (!HasKeypadInstantDecode())
            {
                if (mask && Random.value < GetMaskPercentageByDay())
                {
                    if (!string.IsNullOrEmpty(shipCode))
                    {
                        int index = Random.Range(0, shipCode.Length);

                        char[] chars = shipCode.ToCharArray();
                        chars[index] = '*';

                        shipCode = new string(chars);

                        if (HasKeypadDelayedDecode())
                        {
                            if (_autoDecodeRoutine != null)
                                StopCoroutine(_autoDecodeRoutine);

                            _autoDecodeRoutine = StartCoroutine(AutoDecodeRoutine(_currentShip.EntryCode));
                        }
                    }
                }
                else
                {
                    if (_autoDecodeRoutine != null)
                        StopCoroutine(_autoDecodeRoutine);
                }
            }
            
            _shipNameText.text = "SHIP: " + _currentShip.ShipName;
            _shipCodeText.text = "CODE: " + shipCode;
        }
        
        private IEnumerator AutoDecodeRoutine(string fullCode)
        {
            float delay = 10f;
            float elapsed = 0f;

            while (elapsed < delay)
            {
                // If the ship clears, we should quit
                if (_currentShip == null)
                    yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _shipCodeText.text = "CODE: " + fullCode;
            _autoDecodeRoutine = null;
        }
        
        private IEnumerator AutoScanAllEntries()
        {
            if (_shipEntries == null || _shipEntries.Count == 0)
                yield break;
            
            foreach (var entry in _shipEntries)
            {
                if (entry == null || entry.IsScanned)
                    continue;
                
                yield return StartCoroutine(ScanRoutine(entry));

                // A tiny buffer otherwise we get some weird UI glitches
                yield return new WaitForSeconds(0.1f);

                // If inspection ended early, stop (ie sunk or dayover)
                if (_currentShip == null)
                    yield break;
            }

            _autoScanRoutine = null;
        }
        
        private IEnumerator AutoScanAllEntries_Instant()
        {
            if (_shipEntries == null || _shipEntries.Count == 0)
                yield break;

            List<Coroutine> running = new List<Coroutine>();
            
            foreach (var entry in _shipEntries)
            {
                if (entry == null || entry.IsScanned)
                    continue;
                
                running.Add(StartCoroutine(ScanRoutine(entry)));
            }
            
            foreach (var c in running)
                yield return c;

            _autoScanRoutine = null;
        }

        private void ClearShipList()
        {
            foreach (Transform child in _shipListContainer)
                Destroy(child.gameObject);
            _shipEntries.Clear();
            
            _shipCodeText.text = "";
            _shipNameText.text = "";
            
            UpdateHighlight();
        }

        private void HandleInput()
        {
            if (_isScanning) return;
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            bool upPressed = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            bool downPressed = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
                
            if (scroll > 0f || upPressed)
                ChangeSelection(-1);
            else if (scroll < 0f || downPressed)
                ChangeSelection(1);
            
            if (Input.GetKeyDown(KeyCode.Space))
                ScanCurrent();
            
            if (Input.GetKeyDown(KeyCode.E))
                ExitFocus();
        }

        private void ChangeSelection(int dir)
        {
            if (_shipEntries == null || _shipEntries.Count == 0)
                return;
            
            _currentIndex = Mathf.Clamp(_currentIndex + dir, 0, _shipEntries.Count - 1);
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            if (!_highlightBar || _shipEntries == null || _shipEntries.Count == 0)
            {
                if (_highlightBar)
                    _highlightBar.gameObject.SetActive(false);
                return;
            }
            
            if (_highlightBar && _currentIndex < _shipEntries.Count)
                _highlightBar.position = _shipEntries[_currentIndex].transform.position;
            
            AudioManager.Instance.PlaySFX("CARGO_SELECTION");
        }

        private void ScanCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _shipEntries.Count) return;
            var entry = _shipEntries[_currentIndex];
            if (entry.IsScanned) return;

            StartCoroutine(ScanRoutine(entry));
        }
        
        private IEnumerator ScanRoutine(CargoEntryUI entry)
        {
            _isScanning = true;

            WaveGameStats.Instance.TrackCargoScanned();
            
            AudioManager.Instance.PlaySFX("CARGO_CONFIRM");

            // Set up visuals
            if (_scanProgress)
            {
                _scanProgress.fillAmount = 0f;
                _scanProgress.color = _scanColor;
                _scanProgress.gameObject.SetActive(true);
            }

            if (_highlightBackground)
                _highlightBackground.color = _scanColor;
            
            // Adjust the time it takes to scan an entry based on whether we've purchased upgrades
            float scanRevealTime = _scanRevealDelay - CargoReduceScanTime();

            // Fill animation
            float elapsed = 0f;
            while (elapsed < scanRevealTime)
            {
                elapsed += Time.deltaTime;
                if (_scanProgress)
                    _scanProgress.fillAmount = Mathf.Clamp01(elapsed / scanRevealTime);
                
                yield return null;
            }

            // Perform the reveal
            bool isBanned = _bannedItems.Exists(x => x.Id == entry.Cargo.Id);
            yield return entry.ScanReveal(0f, _bannedItems);

            // Flash background color
            Color flashColor = isBanned ? _bannedColor : _safeColor;
            yield return FlashBackground(flashColor);

            if (_scanProgress)
                _scanProgress.gameObject.SetActive(false);

            if (_highlightBackground)
                _highlightBackground.color = _defaultColor;

            _isScanning = false;

            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
            {
                _eventHub?.Publish(new Tutorial_OnCargoScannedEvent());
            }
        }

        private IEnumerator FlashBackground(Color flashColor)
        {
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 6f, 1f);
                _highlightBackground.color = Color.Lerp(_defaultColor, flashColor, t);
                yield return null;
            }

            _highlightBackground.color = _defaultColor;
        }

        // ─────────────────────────────────────────────
        // Camera Focus Logic
        // ─────────────────────────────────────────────
        public void EnterFocus()
        {
            _inFocus = true;
            GameManager.SetFocusMode(true);
            
            _cameraController.FocusOn(_cameraFocusPoint, _cameraLookTarget, _focusFOV);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_highlightBar)
                _highlightBar.gameObject.SetActive(true);

            UpdateHighlight();
            
            // Tutorial related
            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
            {
                _eventHub?.Publish(new Tutorial_OnCargoFocusedEvent());
            }
        }

        public void ExitFocus()
        {
            _inFocus = false;
            _recentlyExited = true;
            GameManager.SetFocusMode(false);
            _cameraController.ReturnToDefault();

            // Restore normal look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (_highlightBar)
                _highlightBar.gameObject.SetActive(false);
            
            StartCoroutine(ExitCooldownRoutine());
        }
        
        private IEnumerator ExitCooldownRoutine()
        {
            yield return new WaitForSeconds(_exitCooldown);
            _recentlyExited = false;
        }

        // ─────────────────────────────────────────────
        // IInteractable Implementation
        // ─────────────────────────────────────────────
        public string GetInteractionText()
        {
            if (_inFocus) return string.Empty;
            return "Open Manifest [E]";
        }

        public bool CanInteract()
        {
            if (_recentlyExited) 
                return false;
            
            if (_inFocus || !_gameManager)
                return false;

            if (_gameManager.CurrentGameplayState == GameplayState.Inspecting)
                return true;

            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
            {
                if (TutorialController.Instance.IsStepCompleted(TutorialStep.InspectionStarted) &&
                    !TutorialController.Instance.IsStepCompleted(TutorialStep.CargoEntryScanned))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanShowMessage()
        {
            if (_inFocus || !_gameManager)
                return false;

            if (_gameManager.CurrentGameplayState == GameplayState.Inspecting)
                return true;

            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
            {
                if (TutorialController.Instance.IsStepCompleted(TutorialStep.InspectionStarted) &&
                    !TutorialController.Instance.IsStepCompleted(TutorialStep.CargoEntryScanned))
                {
                    return true;
                }
            }

            return false;
        }

        public void OnInteract()
        {
            if (_recentlyExited) 
                return;
            
            if (_inFocus)
                ExitFocus();
            else
                EnterFocus();
        }
        
        // ─────────────────────────────────────────────
        // Stuff to help upgrades, idk, im tired
        // ─────────────────────────────────────────────
        private bool HasKeypadDelayedDecode()
        {
            return _upgradeShopController.IsPurchased("Keypad_T1");
        }
        
        private bool HasKeypadInstantDecode()
        {
            return _upgradeShopController.IsPurchased("Keypad_T2");
        }

        private bool HasCargoAutoScan()
        {
            return _upgradeShopController.IsPurchased("Cargo_AutoScan_T1");
        }
        
        private bool HasCargoAutoScanTier2()
        {
            return _upgradeShopController.IsPurchased("Cargo_AutoScan_T2");
        }

        private float CargoReduceScanTime()
        {
            if (_upgradeShopController.IsPurchased("Cargo_T2"))
                return 2f;
            
            if (_upgradeShopController.IsPurchased("Cargo_T1"))
                return 1f;
            
            return 0f;
        }
    }
}
