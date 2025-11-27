using System.Collections;
using Route24.Core;
using TMPro;
using UnityEngine;

namespace Route24.GameOff
{
    public class KeypadUI : MonoBehaviour, ISceneInitializable
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _displayText;

        [Header("Display Settings")]
        [SerializeField] private int _codeLength = 4;

        private KeypadController _controller;
        private EventHub _eventHub;
        private string _currentDisplay = "";
        private string _enteredCode = "";

        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            
            Hide();
            
            // Yes I know, these should go to their own functions but its the usual last 
            // minute jam rush so I'm just inlining the logic :shrug:
            _eventHub.Subscribe<Tutorial_KeypadPoweredOnEvent>(evt =>
            {
                ResetDisplay();
                Show();
            });
            
            _eventHub.Subscribe<TutorialCompletedEvent>(evt =>
            {
                ClearDisplay();
                Hide();
            });
            
            _eventHub.Subscribe<InspectionStartedEvent>(evt =>
            {
                ResetDisplay();
                Show();
            });
            
            _eventHub.Subscribe<InspectionCompletedEvent>(evt =>
            {
                ClearDisplay();
                Hide();
            });
        }

        private void OnDestroy()
        {
            if (_eventHub == null) return;

            _eventHub.Unsubscribe<InspectionStartedEvent>(null);
            _eventHub.Unsubscribe<InspectionCompletedEvent>(null);
        }

        public void DigitPressed(string digit)
        {
            _enteredCode = digit;
            
            Debug.Log("[KeypadUI] Received pressed: " + _enteredCode);
            
            UpdateDisplay();
        }

        public void ResetDisplay()
        {
            _currentDisplay = new string('*', _codeLength);
            RefreshText();
        }

        private void ClearDisplay()
        {
            _currentDisplay = "";
            RefreshText();
        }

        private void UpdateDisplay()
        {
            int enteredCount = _enteredCode.Length;
            
            string masked = "";
            for (int i = 0; i < _codeLength; i++)
            {
                if (i < enteredCount)
                    masked += _enteredCode[i];
                else
                    masked += "*";
            }

            _currentDisplay = masked;
            RefreshText();
        }

        private void RefreshText()
        {
            if (_displayText)
                _displayText.text = _currentDisplay;
        }
        

        public void Show()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }
}
