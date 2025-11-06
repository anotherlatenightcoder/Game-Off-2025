using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class Button_Approve : MonoBehaviour, IInteractable
    {
        [SerializeField] private IndicatorLight _indicatorLight;
        [SerializeField] private string interactionText = "Approve [E]";

        private GameManager _gameManager;
        private EventHub _eventHub;
        private bool _isActive = false;

        private void Start()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();

            // Subscribe to events to know when button should be active
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
            // We can still show the text if the inspection has started,
            // even if not yet ready (for example, after adding minigames later)
            return _gameManager && _gameManager.CurrentGameplayState == GameplayState.Inspecting;
        }

        public void OnInteract()
        {
            if (!CanInteract())
                return;

            Debug.Log("[Button_Approve] Ship approved!");
            _indicatorLight?.SetLight(false);
            _gameManager?.CompleteInspection(true);
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
