using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class Switch_StartInspection : MonoBehaviour, IInteractable, ISceneInitializable
    {
        [SerializeField] private IndicatorLight _indicatorLight;
        
        public string interactionText = "Start Inspection [E]";
        
        private GameManager _gameManager;
        private EventHub _eventHub;

        public void SceneInitialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            _eventHub.Subscribe<ShipArrivedForInspectionEvent>(e => SetLightOn());
            _eventHub.Subscribe<InspectionStartedEvent>(e => SetLightOff());

            SetLightOff();
        }

        public string GetInteractionText() => interactionText;

        public bool CanInteract()
        {
            return _gameManager && _gameManager.CurrentGameplayState == GameplayState.ReadyForInspection;
        }

        public bool CanShowMessage()
        {
            return _gameManager && _gameManager.CurrentGameplayState == GameplayState.ReadyForInspection;
        }

        public void OnInteract()
        {
            if (_gameManager)
            {
                Debug.Log("Switch flipped — starting inspection!");
                _indicatorLight?.SetLight(false);
                _gameManager.StartInspection();
            }
        }
        
        private void SetLightOn()
        {
            _indicatorLight?.SetLight(true);
        }

        private void SetLightOff()
        {
            _indicatorLight?.SetLight(false);
        }
    }
}