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

        private List<CargoEntryUI> _shipEntries = new();
        private List<string> _bannedItems = new();
        private int _currentIndex = 0;
        private bool _inFocus = false;
        private bool _recentlyExited = false;
        private bool _isScanning = false;
        private float _exitCooldown = 0.5f;
        private GameManager _gameManager;
        private EventHub _eventHub;
        private CargoManifestManager _manifestManager;
        [SerializeField] private SeatedCameraController _cameraController;
        

        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _manifestManager = ServiceLocator.Get<CargoManifestManager>();
            _gameManager = ServiceLocator.Get<GameManager>();

            _eventHub.Subscribe<ShipArrivedForInspectionEvent>(OnShipArrived);
            
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

        private void Update()
        {
            if (!_inFocus) return;
            
            HandleInput();
            
            // While in focus, pressing E exits the console
            if (Input.GetKeyDown(KeyCode.E))
                ExitFocus();
        }

        private void OnShipArrived(ShipArrivedForInspectionEvent evt)
        {
            // Create new manifest each ship
            PopulateBannedList();
            PopulateShipList(evt.Ship.CargoList);
        }

        private void PopulateBannedList()
        {
            _bannedItems = new List<string>(_manifestManager.BannedCargo);
            foreach (Transform child in _bannedListContainer)
                Destroy(child.gameObject);

            foreach (string item in _bannedItems)
            {
                var obj = Instantiate(_cargoEntryPrefab, _bannedListContainer);
                obj.GetComponentInChildren<TextMeshProUGUI>().text = item;
            }
        }

        private void PopulateShipList(List<string> cargoList)
        {
            foreach (Transform child in _shipListContainer)
                Destroy(child.gameObject);
            _shipEntries.Clear();

            foreach (string item in cargoList)
            {
                // distort the text before scanning
                string scrambled = DistortText(item);
                var obj = Instantiate(_cargoEntryPrefab, _shipListContainer);
                var entry = obj.GetComponent<CargoEntryUI>();
                entry.Initialize(scrambled, item);
                _shipEntries.Add(entry);
            }

            _currentIndex = 0;
            UpdateHighlight();
        }

        private string DistortText(string original)
        {
            var chars = original.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Random.value < 0.3f) chars[i] = '?';
                else if (Random.value < 0.1f) chars[i] = (char)Random.Range(65, 90);
            }
            return new string(chars);
        }

        private void HandleInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) ChangeSelection(-1);
            else if (scroll < 0f) ChangeSelection(1);
            
            if (Input.GetKeyDown(KeyCode.Space))
                ScanCurrent();

            if (Input.GetKeyDown(KeyCode.E)) ExitFocus();
        }

        private void ChangeSelection(int dir)
        {
            _currentIndex = Mathf.Clamp(_currentIndex + dir, 0, _shipEntries.Count - 1);
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
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
            bool isBanned = _bannedItems.Contains(entry.ActualName);
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
            _cameraController.FocusOn(transform);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_highlightBar)
            {
                Debug.Log("[ManifestConsole] Enabling highlight bar");
                _highlightBar.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[ManifestConsole] Highlight bar is null!");
            }

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
