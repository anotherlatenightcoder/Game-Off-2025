using System;
using System.Collections;
using System.Collections.Generic;
using Route24.Core;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Route24.GameOff
{
    public class ManifestConsoleController : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _highlightBar;
        [SerializeField] private Transform _shipListContainer;
        [SerializeField] private Transform _bannedListContainer;
        [SerializeField] private GameObject _cargoEntryPrefab; // Simple TMPro entry

        [Header("Scan Settings")]
        [SerializeField] private float _scanRevealDelay = 1f;

        private List<CargoEntryUI> _shipEntries = new();
        private List<string> _bannedItems = new();
        private int _currentIndex = 0;
        private bool _inFocus = false;
        private bool _recentlyExited = false;
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
            {
                _highlightBar.position = _shipEntries[_currentIndex].transform.position;
            }
        }

        private void ScanCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _shipEntries.Count) return;
            var entry = _shipEntries[_currentIndex];
            if (entry.IsScanned) return;

            StartCoroutine(entry.ScanReveal(_scanRevealDelay, _bannedItems));
        }

        // ─────────────────────────────────────────────
        // Camera Focus Logic
        // ─────────────────────────────────────────────
        public void EnterFocus()
        {
            _inFocus = true;
            GameManager.SetFocusMode(true);
            _cameraController.FocusOn(transform);

            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
