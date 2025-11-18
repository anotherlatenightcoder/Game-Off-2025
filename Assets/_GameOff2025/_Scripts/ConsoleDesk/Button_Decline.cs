using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class Button_Decline : MonoBehaviour, IInteractable
    {
        [SerializeField] private IndicatorLight _indicatorLight;
        [SerializeField] private string interactionText = "Decline [E]";
        [SerializeField] private string _buttonSoundString = "";

        private GameManager _gameManager;
        private EventHub _eventHub;
        private bool _isActive = false;

        private void Start()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();

            // Subscribe to relevant events
            _eventHub.Subscribe<InspectionStartedEvent>(e => Activate());
            _eventHub.Subscribe<InspectionCompletedEvent>(e => Deactivate());
            _eventHub.Subscribe<ShipArrivedForInspectionEvent>(e => Deactivate());
            _eventHub.Subscribe<DayEndedEvent>(e => Deactivate());

            Deactivate();
        }

        public string GetInteractionText() => interactionText;

        public bool CanInteract()
        {
            return _isActive && _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public bool CanShowMessage()
        {
            return _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public void OnInteract()
        {
            if (!CanInteract())
                return;

            Debug.Log("[Button_Decline] Ship declined!");
            _indicatorLight?.SetLight(false);
            _gameManager?.CompleteInspection(false);
            
            if (_buttonSoundString != "")
                AudioTestManager.Instance.PlaySFX(_buttonSoundString);
        }

        private void Activate()
        {
            _isActive = true;
            _indicatorLight?.SetLight(true);
        }

        private void Deactivate()
        {
            _isActive = false;
            _indicatorLight?.SetLight(false);
        }
    }
}