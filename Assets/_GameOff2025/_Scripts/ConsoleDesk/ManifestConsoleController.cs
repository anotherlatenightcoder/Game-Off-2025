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

        [Header("Scan Settings")]
        [SerializeField] private float _scanRevealDelay = 1f;
        [SerializeField] private Color _defaultColor = new(1f, 1f, 1f, 0f);
        [SerializeField] private Color _safeColor = Color.green;
        [SerializeField] private Color _bannedColor = Color.red;
        [SerializeField] private Color _scanColor = Color.cyan;
        
        [Header("Switches")]
        [SerializeField] private LeverSwitchController _consoleLeverSwitch;
        
        [Header("Camera Settings")]
        [SerializeField] private SeatedCameraController _cameraController;
        [SerializeField] private Transform _cameraFocusPoint;
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField] private float _focusFOV = 40f;

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
        private ShipProfile _currentShip;
        
        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _manifestManager = ServiceLocator.Get<CargoManifestManager>();
            _gameManager = ServiceLocator.Get<GameManager>();

            _eventHub.Subscribe<LeverActivatedEvent>(OnLeverOn);
            _eventHub.Subscribe<LeverDeactivatedEvent>(OnLeverOff);
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
        }
        
        // ─────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────

        private void OnInspectionStarted(InspectionStartedEvent evt)
        {
            _currentShip = evt.Ship;
        }
        
        private void OnInspectionEnded(InspectionCompletedEvent evt)
        {
            _currentShip = null;
            
            ClearShipList();
            
            _consoleLeverSwitch?.ForceOff();
        }
        
        private void OnLeverOn(LeverActivatedEvent evt)
        {
            Debug.Log("[ManifestConsole] Lever turned ON — loading ship cargo.");
            if (_currentShip != null)
                PopulateShipList(_currentShip.CargoList);
        }

        private void OnLeverOff(LeverDeactivatedEvent evt)
        {
            Debug.Log("[ManifestConsole] Lever turned OFF — clearing manifest.");
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

        private void ClearShipList()
        {
            foreach (Transform child in _shipListContainer)
                Destroy(child.gameObject);
            _shipEntries.Clear();
            
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

            // Set up visuals
            if (_scanProgress)
            {
                _scanProgress.fillAmount = 0f;
                _scanProgress.color = _scanColor;
                _scanProgress.gameObject.SetActive(true);
            }

            if (_highlightBackground)
                _highlightBackground.color = _scanColor;

            // Fill animation
            float elapsed = 0f;
            while (elapsed < _scanRevealDelay)
            {
                elapsed += Time.deltaTime;
                if (_scanProgress)
                    _scanProgress.fillAmount = Mathf.Clamp01(elapsed / _scanRevealDelay);
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
            return !_inFocus && !_recentlyExited && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public bool CanShowMessage()
        {
            return !_inFocus && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public void OnInteract()
        {
            if (_inFocus)
                ExitFocus();
            else
                EnterFocus();
        }
    }
}
