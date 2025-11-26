using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class Switch_StartInspection : MonoBehaviour, ISceneInitializable
    {
        [SerializeField] private IndicatorLight _indicatorLight;
        [SerializeField] private BigButton _bigButton;
        [SerializeField] private string _buttonSoundString = "";
        
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

            if (_bigButton)
            {
                _bigButton.SetOnInteract(OnInteract);
                _bigButton.SetCanInteractAction(CanInteract);
                _bigButton.SetInteractionText(GetInteractionText);
                _bigButton.SetCanShowMassage(CanShowMessage);
            }
            else
                Debug.LogWarning($"[Switch_StartInspection] _big button is null, so switch cannot be interactable");
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
                
                if (_buttonSoundString != "")
                    AudioManager.Instance.PlaySFX(_buttonSoundString);
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