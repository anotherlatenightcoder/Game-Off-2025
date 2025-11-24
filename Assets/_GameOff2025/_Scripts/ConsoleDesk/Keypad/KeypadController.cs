using System.Collections;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class KeypadController : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [Header("Lights")]
        [SerializeField] private KeypadLights _lights;

        [Header("Settings")]
        [SerializeField] private int _codeLength = 4;

        private string _requiredCode = "";
        private string _enteredCode = "";
        private bool _powered = false;
        
        private GameManager _gameManager;
        private EventHub _eventHub;

        private bool _tutorialMode => 
            _gameManager.CurrentGameplayState == GameplayState.Tutorial &&
            TutorialController.Instance.TutorialStep3Completed &&
            !TutorialController.Instance.TutorialStep4Completed;

        public void SceneInitialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();

            // Listen for incoming ship-code assignment
            _eventHub.Subscribe<InspectionStartedEvent>(evt =>
            {
                _requiredCode = evt.Ship.EntryCode;
                ResetCode();
            });
        }
        
        public void SetPowered(bool powered)
        {
            _powered = powered;

            if (_powered)
                _lights.SetReady(false);
            else
                _lights.SetOff();
        }

        public void PressDigit(int digit)
        {
            if (!_powered) return;

            // This is for when we're in the tutorial
            if (_tutorialMode)
            {
                HandleTutorialDigit(digit);
                return;
            }
            
            _enteredCode += digit.ToString();
            _lights.FlashGreen();

            if (_enteredCode.Length >= _codeLength)
                ValidateCode();
        }

        private void ValidateCode()
        {
            if (_enteredCode == _requiredCode)
            {
                Debug.Log("[KEYPAD] Correct code entered!");

                _eventHub.Publish(new KeypadTestEnteredEvent());
                _lights.FlashGreenStrong();
            }
            else
            {
                Debug.Log("[KEYPAD] WRONG CODE");
                _lights.FlashRed();
            }

            ResetCode();
        }

        private void HandleTutorialDigit(int digit)
        {
            _enteredCode += digit.ToString();
            _lights.FlashGreen();

            if (_enteredCode.Length < _codeLength)
                return;

            if (_enteredCode == "0000" || _enteredCode == "1234")
            {
                Debug.Log("[KEYPAD] Tutorial code accepted");
                _eventHub.Publish(new KeypadTestEnteredEvent());
                _lights.FlashGreenStrong();
            }
            else
            {
                Debug.Log("[KEYPAD] Tutorial wrong code");
                _lights.FlashRed();
            }

            ResetCode();
        }

        private void ResetCode()
        {
            _enteredCode = "";
        }
        
        public bool CanInteract() => true;
        public bool CanShowMessage() => true;
        public string GetInteractionText() => _powered ? "" : "Keypad (off)";
        public void OnInteract() { }
    }
}
